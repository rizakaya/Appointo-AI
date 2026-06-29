namespace Appointo.Agent;

public sealed class ConversationState
{
    public Guid ConversationId { get; } = Guid.NewGuid();
    public AppointmentIntent Intent { get; set; } = AppointmentIntent.Unknown;
    public Dictionary<string, string> CollectedFields { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<string> MissingFields { get; } = [];
    public string? LastQuestion { get; set; }
}
