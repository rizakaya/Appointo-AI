namespace Appointo.Tools;

public sealed record ToolExecutionLogEntry(
    DateTime TimestampUtc,
    string ToolName,
    UserRole Role,
    bool Success,
    string Stage,
    string Message);

public interface IToolExecutionLogger
{
    void Log(ToolExecutionLogEntry entry);

    IReadOnlyList<ToolExecutionLogEntry> GetEntries();
}

public sealed class InMemoryToolExecutionLogger : IToolExecutionLogger
{
    private readonly List<ToolExecutionLogEntry> _entries = [];

    public void Log(ToolExecutionLogEntry entry)
    {
        _entries.Add(entry);
    }

    public IReadOnlyList<ToolExecutionLogEntry> GetEntries() => _entries.AsReadOnly();
}
