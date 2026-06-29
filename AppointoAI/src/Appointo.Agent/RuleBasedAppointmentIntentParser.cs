namespace Appointo.Agent;

public sealed class RuleBasedAppointmentIntentParser : IAppointmentIntentParser
{
    private readonly StructuredAppointmentParser _parser;

    public RuleBasedAppointmentIntentParser(StructuredAppointmentParser? parser = null)
    {
        _parser = parser ?? new StructuredAppointmentParser();
    }

    public Task<AppointmentIntentResult> ParseAsync(string message, DateOnly today, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_parser.Parse(message, today));
    }
}
