namespace Appointo.Tools;

public sealed record ToolExecutionResult(bool Success, string Message, object? Payload = null);
public sealed record CheckAvailabilityToolRequest(DateOnly Date, TimeOnly StartTime, int DurationMinutes);
public sealed record CreateAppointmentToolRequest(string CustomerName, string PhoneNumber, string ServiceType, DateOnly Date, TimeOnly StartTime, string? Notes);
public sealed record CancelAppointmentToolRequest(Guid AppointmentId);
public sealed record RescheduleAppointmentToolRequest(Guid AppointmentId, DateOnly NewDate, TimeOnly NewStartTime);
public sealed record FindCustomerAppointmentsToolRequest(string CustomerName, string? PhoneNumber);
public sealed record FindNextAvailableSlotToolRequest(DateOnly From, string ServiceType, string PreferredPartOfDay);
public sealed record AppointmentToolSchema(string Name, string Description, IReadOnlyList<string> RequiredFields);
