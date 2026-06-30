namespace Appointo.Agent;

public interface IRagKnowledgeBase
{
    Task<string> AnswerAsync(string question, CancellationToken cancellationToken = default);
}
