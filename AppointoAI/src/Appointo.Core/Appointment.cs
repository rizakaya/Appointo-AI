namespace Appointo.Core;

public sealed class Appointment
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string CustomerName { get; init; }
    public required string PhoneNumber { get; init; }
    public required string ServiceType { get; init; }
    public DateOnly Date { get; init; }
    public TimeOnly StartTime { get; init; }
    public TimeOnly EndTime { get; init; }
    public AppointmentStatus Status { get; private set; } = AppointmentStatus.Scheduled;
    public string? Notes { get; init; }

    public void Cancel() => Status = AppointmentStatus.Cancelled;
}
