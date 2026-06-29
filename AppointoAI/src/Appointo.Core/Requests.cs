namespace Appointo.Core;

public sealed record CreateAppointmentRequest(string CustomerName, string PhoneNumber, string ServiceType, DateOnly Date, TimeOnly StartTime, string? Notes);
public sealed record RescheduleAppointmentRequest(Guid AppointmentId, DateOnly NewDate, TimeOnly NewStartTime);
public sealed record AvailableSlotDto(DateOnly Date, TimeOnly StartTime, TimeOnly EndTime);
