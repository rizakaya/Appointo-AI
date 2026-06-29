namespace Appointo.Agent;

public sealed record OllamaOptions(string BaseUrl, string Model)
{
    public static OllamaOptions Default { get; } = new("http://localhost:11434", "qwen3.6:latest");
}
