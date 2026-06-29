namespace Appointo.Agent;

public interface IChatCompletionClient
{
    Task<string> ChatAsync(string systemPrompt, string userMessage, CancellationToken cancellationToken = default);
}
