namespace Appointo.Agent;

public sealed record AppointmentIntentResult(
    AppointmentIntent Intent,
    string? CustomerName,
    string? PhoneNumber,
    string? ServiceType,
    DateOnly? Date,
    TimeOnly? Time,
    string? TimePreference,
    string? Notes,
    IReadOnlyList<string> MissingFields);
