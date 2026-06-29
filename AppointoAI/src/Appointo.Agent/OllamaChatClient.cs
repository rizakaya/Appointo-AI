using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Appointo.Agent;

public sealed class OllamaChatClient
{
    private readonly HttpClient _httpClient;
    private readonly OllamaOptions _options;

    public OllamaChatClient(HttpClient httpClient, OllamaOptions? options = null)
    {
        _httpClient = httpClient;
        _options = options ?? OllamaOptions.Default;
        _httpClient.BaseAddress ??= new Uri(_options.BaseUrl);
    }

    public async Task<string> ChatAsync(string systemPrompt, string userMessage, CancellationToken cancellationToken = default)
    {
        var request = new OllamaChatRequest(
            _options.Model,
            false,
            [new OllamaMessage("system", systemPrompt), new OllamaMessage("user", userMessage)]);

        var response = await _httpClient.PostAsJsonAsync("/api/chat", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(cancellationToken);
        return result?.Message?.Content ?? string.Empty;
    }

    private sealed record OllamaChatRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("stream")] bool Stream,
        [property: JsonPropertyName("messages")] IReadOnlyList<OllamaMessage> Messages);

    private sealed record OllamaMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);

    private sealed record OllamaChatResponse([property: JsonPropertyName("message")] OllamaMessage? Message);
}
