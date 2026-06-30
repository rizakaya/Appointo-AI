using Appointo.Core;

namespace Appointo.Tools;

public sealed class ToolGateway
{
    private readonly AppointmentService _appointmentService;
    private readonly PermissionMatrix _permissionMatrix;
    private readonly IToolExecutionLogger _logger;

    public ToolGateway(AppointmentService appointmentService, PermissionMatrix? permissionMatrix = null, IToolExecutionLogger? logger = null)
    {
        _appointmentService = appointmentService;
        _permissionMatrix = permissionMatrix ?? new PermissionMatrix();
        _logger = logger ?? new InMemoryToolExecutionLogger();
    }

    public async Task<ToolExecutionResult> ExecuteAsync(string toolName, object request, UserContext user, CancellationToken cancellationToken = default)
    {
        Log(toolName, user.Role, success: true, stage: "Attempt", message: "Tool cagrisi alindi.");

        if (!_permissionMatrix.CanExecute(toolName, user))
        {
            var denied = new ToolExecutionResult(false, "Bu tool'u calistirmak icin yetkiniz yok.");
            Log(toolName, user.Role, denied.Success, "Denied", denied.Message);
            return denied;
        }

        var result = toolName switch
        {
            AppointmentToolNames.CheckAvailability => await CheckAvailabilityAsync((CheckAvailabilityToolRequest)request, cancellationToken),
            AppointmentToolNames.CreateAppointment => await CreateAppointmentAsync((CreateAppointmentToolRequest)request, cancellationToken),
            AppointmentToolNames.CancelAppointment => await CancelAppointmentAsync((CancelAppointmentToolRequest)request, cancellationToken),
            AppointmentToolNames.FindCustomerAppointments => await FindCustomerAppointmentsAsync((FindCustomerAppointmentsToolRequest)request, cancellationToken),
            AppointmentToolNames.FindNextAvailableSlot => await FindNextAvailableSlotAsync((FindNextAvailableSlotToolRequest)request, cancellationToken),
            _ => new ToolExecutionResult(false, "Bilinmeyen tool.")
        };

        Log(toolName, user.Role, result.Success, "Completed", result.Message);
        return result;
    }

    public IReadOnlyList<AppointmentToolSchema> GetSchemas()
    {
        return
        [
            new(AppointmentToolNames.CheckAvailability, "Secilen tarih ve saatin uygunlugunu kontrol eder.", ["date", "startTime", "durationMinutes"]),
            new(AppointmentToolNames.CreateAppointment, "Yeni randevu olusturur.", ["customerName", "phoneNumber", "serviceType", "date", "startTime"]),
            new(AppointmentToolNames.CancelAppointment, "Randevuyu iptal eder.", ["appointmentId"]),
            new(AppointmentToolNames.RescheduleAppointment, "Randevuyu yeniden planlar.", ["appointmentId", "newDate", "newStartTime"]),
            new(AppointmentToolNames.FindCustomerAppointments, "Musterinin randevularini bulur.", ["customerName"]),
            new(AppointmentToolNames.FindNextAvailableSlot, "En yakin musait slotlari bulur.", ["from", "serviceType"])
        ];
    }

    public IReadOnlyList<ToolExecutionLogEntry> GetLogs() => _logger.GetEntries();

    private async Task<ToolExecutionResult> CheckAvailabilityAsync(CheckAvailabilityToolRequest request, CancellationToken cancellationToken)
    {
        var result = await _appointmentService.CheckAvailabilityAsync(request.Date, request.StartTime, request.DurationMinutes, cancellationToken);
        return new ToolExecutionResult(result.Success, result.Message, result.Value);
    }

    private async Task<ToolExecutionResult> CreateAppointmentAsync(CreateAppointmentToolRequest request, CancellationToken cancellationToken)
    {
        var result = await _appointmentService.CreateAsync(new CreateAppointmentRequest(request.CustomerName, request.PhoneNumber, request.ServiceType, request.Date, request.StartTime, request.Notes), cancellationToken);
        return new ToolExecutionResult(result.Success, result.Message, result.Value);
    }

    private async Task<ToolExecutionResult> CancelAppointmentAsync(CancelAppointmentToolRequest request, CancellationToken cancellationToken)
    {
        var result = await _appointmentService.CancelAsync(request.AppointmentId, cancellationToken);
        return new ToolExecutionResult(result.Success, result.Message, result.Value);
    }

    private async Task<ToolExecutionResult> FindCustomerAppointmentsAsync(FindCustomerAppointmentsToolRequest request, CancellationToken cancellationToken)
    {
        var appointments = await _appointmentService.FindCustomerAppointmentsAsync(request.CustomerName, request.PhoneNumber, cancellationToken);
        return new ToolExecutionResult(true, $"{appointments.Count} randevu bulundu.", appointments);
    }

    private async Task<ToolExecutionResult> FindNextAvailableSlotAsync(FindNextAvailableSlotToolRequest request, CancellationToken cancellationToken)
    {
        var result = await _appointmentService.FindNextAvailableSlotsAsync(request.From, request.ServiceType, request.PreferredPartOfDay, cancellationToken: cancellationToken);
        if (!result.Success || result.Value is null || result.Value.Count == 0)
        {
            return new ToolExecutionResult(false, "Uygun slot bulunamadi.", result.Value);
        }

        var text = string.Join(", ", result.Value.Select(x => $"{x.Date:dd.MM.yyyy} {x.StartTime:HH:mm}"));
        return new ToolExecutionResult(true, $"Uygun slotlar: {text}", result.Value);
    }

    private void Log(string toolName, UserRole role, bool success, string stage, string message)
    {
        _logger.Log(new ToolExecutionLogEntry(DateTime.UtcNow, toolName, role, success, stage, message));
    }
}
