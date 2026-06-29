namespace Appointo.Agent;

public interface IAppointmentIntentParser
{
    Task<AppointmentIntentResult> ParseAsync(string message, DateOnly today, CancellationToken cancellationToken = default);
}
