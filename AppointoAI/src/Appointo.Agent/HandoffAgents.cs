namespace Appointo.Agent;

public sealed record HandoffDecision(string TargetAgent, string Reason)
{
    public static HandoffDecision Appointment(string reason) => new("AppointmentAgent", reason);
    public static HandoffDecision Customer(string reason) => new("CustomerAgent", reason);
    public static HandoffDecision Availability(string reason) => new("AvailabilityAgent", reason);
    public static HandoffDecision Support(string reason) => new("SupportAgent", reason);
}
