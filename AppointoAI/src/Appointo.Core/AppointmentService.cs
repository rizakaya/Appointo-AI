namespace Appointo.Core;

public sealed class AppointmentService
{
    private readonly IAppointmentRepository _repository;
    private readonly ServiceCatalog _serviceCatalog;
    private readonly BusinessHours _businessHours;
    private readonly Func<DateTime> _now;

    public AppointmentService(IAppointmentRepository repository, ServiceCatalog? serviceCatalog = null, BusinessHours? businessHours = null, Func<DateTime>? now = null)
    {
        _repository = repository;
        _serviceCatalog = serviceCatalog ?? new ServiceCatalog();
        _businessHours = businessHours ?? BusinessHours.Default;
        _now = now ?? (() => DateTime.Now);
    }

    public async Task<OperationResult<Appointment>> CreateAsync(CreateAppointmentRequest request, CancellationToken cancellationToken = default)
    {
        var validation = ValidateRequest(request);
        if (!validation.Success)
        {
            return OperationResult<Appointment>.Fail(validation.Message);
        }

        var duration = _serviceCatalog.GetDurationMinutes(request.ServiceType);
        var endTime = request.StartTime.AddMinutes(duration);
        var availability = await CheckAvailabilityAsync(request.Date, request.StartTime, duration, cancellationToken);
        if (!availability.Success)
        {
            return OperationResult<Appointment>.Fail(availability.Message);
        }

        var existing = await _repository.ListAsync(cancellationToken);
        var duplicate = existing.Any(x =>
            x.Status == AppointmentStatus.Scheduled &&
            x.Date == request.Date &&
            string.Equals(x.CustomerName, request.CustomerName, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(x.ServiceType, request.ServiceType, StringComparison.OrdinalIgnoreCase));

        if (duplicate)
        {
            return OperationResult<Appointment>.Fail("Ayni kisiye ayni gun ayni hizmet icin ikinci aktif randevu olusturulamaz.");
        }

        var appointment = new Appointment
        {
            CustomerName = request.CustomerName.Trim(),
            PhoneNumber = request.PhoneNumber.Trim(),
            ServiceType = request.ServiceType.Trim(),
            Date = request.Date,
            StartTime = request.StartTime,
            EndTime = endTime,
            Notes = request.Notes
        };

        await _repository.AddAsync(appointment, cancellationToken);
        return OperationResult<Appointment>.Ok(appointment, "Randevu olusturuldu.");
    }

    public async Task<OperationResult<bool>> CheckAvailabilityAsync(DateOnly date, TimeOnly startTime, int durationMinutes, CancellationToken cancellationToken = default)
    {
        var endTime = startTime.AddMinutes(durationMinutes);
        if (!_businessHours.IsInsideWorkingHours(startTime, endTime))
        {
            return OperationResult<bool>.Fail("Secilen saat calisma saatleri disinda veya ogle molasina denk geliyor.");
        }

        if (date.ToDateTime(startTime) <= _now())
        {
            return OperationResult<bool>.Fail("Gecmis tarihe randevu olusturulamaz.");
        }

        var appointments = await _repository.ListAsync(cancellationToken);
        var overlaps = appointments.Any(x =>
            x.Status == AppointmentStatus.Scheduled &&
            x.Date == date &&
            startTime < x.EndTime &&
            endTime > x.StartTime);

        return overlaps ? OperationResult<bool>.Fail("Secilen saat dolu gorunuyor.") : OperationResult<bool>.Ok(true, "Secilen saat uygun.");
    }

    public async Task<OperationResult<IReadOnlyList<AvailableSlotDto>>> FindNextAvailableSlotsAsync(DateOnly from, string serviceType, string preferredPartOfDay = "any", int count = 3, CancellationToken cancellationToken = default)
    {
        var duration = _serviceCatalog.GetDurationMinutes(serviceType);
        var slots = new List<AvailableSlotDto>();

        for (var dayOffset = 0; dayOffset < 14 && slots.Count < count; dayOffset++)
        {
            var date = from.AddDays(dayOffset);
            foreach (var start in CandidateStarts(preferredPartOfDay))
            {
                var availability = await CheckAvailabilityAsync(date, start, duration, cancellationToken);
                if (availability.Success)
                {
                    slots.Add(new AvailableSlotDto(date, start, start.AddMinutes(duration)));
                    if (slots.Count == count)
                    {
                        break;
                    }
                }
            }
        }

        return OperationResult<IReadOnlyList<AvailableSlotDto>>.Ok(slots, "Musait slotlar listelendi.");
    }

    public async Task<OperationResult<bool>> CancelAsync(Guid appointmentId, CancellationToken cancellationToken = default)
    {
        var appointment = await _repository.GetByIdAsync(appointmentId, cancellationToken);
        if (appointment is null)
        {
            return OperationResult<bool>.Fail("Randevu bulunamadi.");
        }

        if (appointment.Date.ToDateTime(appointment.StartTime).AddHours(-2) < _now())
        {
            return OperationResult<bool>.Fail("Randevu saatine 2 saatten az kaldigi icin iptal edilemez.");
        }

        appointment.Cancel();
        return OperationResult<bool>.Ok(true, "Randevu iptal edildi.");
    }

    public async Task<IReadOnlyList<Appointment>> FindCustomerAppointmentsAsync(string customerName, string? phoneNumber = null, CancellationToken cancellationToken = default)
    {
        var appointments = await _repository.ListAsync(cancellationToken);
        return appointments
            .Where(x => string.Equals(x.CustomerName, customerName, StringComparison.OrdinalIgnoreCase))
            .Where(x => phoneNumber is null || x.PhoneNumber == phoneNumber)
            .OrderBy(x => x.Date)
            .ThenBy(x => x.StartTime)
            .ToList();
    }

    public async Task<IReadOnlyList<Appointment>> ListAsync(DateOnly from, DateOnly to, bool includeCancelled = false, CancellationToken cancellationToken = default)
    {
        var appointments = await _repository.ListAsync(cancellationToken);
        return appointments
            .Where(x => x.Date >= from && x.Date <= to)
            .Where(x => includeCancelled || x.Status != AppointmentStatus.Cancelled)
            .OrderBy(x => x.Date)
            .ThenBy(x => x.StartTime)
            .ToList();
    }

    private OperationResult<bool> ValidateRequest(CreateAppointmentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerName)) return OperationResult<bool>.Fail("Musteri adi zorunludur.");
        if (string.IsNullOrWhiteSpace(request.PhoneNumber)) return OperationResult<bool>.Fail("Telefon numarasi zorunludur.");
        if (string.IsNullOrWhiteSpace(request.ServiceType)) return OperationResult<bool>.Fail("Hizmet tipi zorunludur.");
        return OperationResult<bool>.Ok(true, "Gecerli istek.");
    }

    private static IEnumerable<TimeOnly> CandidateStarts(string preferredPartOfDay)
    {
        var starts = new[]
        {
            new TimeOnly(9, 0), new TimeOnly(9, 30), new TimeOnly(10, 0), new TimeOnly(10, 30), new TimeOnly(11, 0),
            new TimeOnly(13, 0), new TimeOnly(13, 30), new TimeOnly(14, 0), new TimeOnly(14, 30), new TimeOnly(15, 0),
            new TimeOnly(15, 30), new TimeOnly(16, 0), new TimeOnly(16, 30)
        };

        return preferredPartOfDay.ToLowerInvariant() switch
        {
            "morning" => starts.Where(x => x.Hour < 12),
            "afternoon" => starts.Where(x => x.Hour >= 13),
            _ => starts
        };
    }
}
