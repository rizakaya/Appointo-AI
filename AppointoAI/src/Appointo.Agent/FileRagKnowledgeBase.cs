namespace Appointo.Agent;

public sealed class FileRagKnowledgeBase : IRagKnowledgeBase
{
    private readonly IReadOnlyList<KnowledgeBaseArticle> _articles;

    public FileRagKnowledgeBase(string baseDirectory)
    {
        var docsDirectory = ResolveDocsDirectory(baseDirectory);
        _articles =
        [
            LoadArticle(docsDirectory, "services.md", "Hizmet Bilgileri"),
            LoadArticle(docsDirectory, "working-hours.md", "Calisma Saatleri"),
            LoadArticle(docsDirectory, "cancellation-policy.md", "Iptal Politikasi")
        ];
    }

    public Task<string> AnswerAsync(string question, CancellationToken cancellationToken = default)
    {
        var normalizedQuestion = Normalize(question);
        var article = SelectArticle(normalizedQuestion);
        return Task.FromResult(FormatAnswer(article));
    }

    private KnowledgeBaseArticle SelectArticle(string normalizedQuestion)
    {
        if (ContainsAny(normalizedQuestion, "iptal", "vazgec", "cancel", "politika"))
        {
            return _articles.First(article => article.Path.EndsWith("cancellation-policy.md", StringComparison.OrdinalIgnoreCase));
        }

        if (ContainsAny(normalizedQuestion, "calisma", "saat", "ogle", "ne zaman acik", "kacta aciliyor", "kacta kapaniyor"))
        {
            return _articles.First(article => article.Path.EndsWith("working-hours.md", StringComparison.OrdinalIgnoreCase));
        }

        return _articles.First(article => article.Path.EndsWith("services.md", StringComparison.OrdinalIgnoreCase));
    }

    private static string FormatAnswer(KnowledgeBaseArticle article)
    {
        var lines = article.Content
            .Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries)
            .Where(line => !line.StartsWith("#", StringComparison.Ordinal))
            .Select(line => line.Trim())
            .Where(line => line.Length > 0)
            .ToList();

        return $"{article.Title}:{Environment.NewLine}{string.Join(Environment.NewLine, lines)}";
    }

    private static KnowledgeBaseArticle LoadArticle(string docsDirectory, string fileName, string title)
    {
        var path = Path.Combine(docsDirectory, fileName);
        var content = File.ReadAllText(path);
        return new KnowledgeBaseArticle(title, path, content);
    }

    private static string ResolveDocsDirectory(string baseDirectory)
    {
        var current = new DirectoryInfo(baseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "docs");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException($"docs klasoru bulunamadi. Baslangic dizini: {baseDirectory}");
    }

    private static bool ContainsAny(string value, params string[] needles) => needles.Any(value.Contains);

    private static string Normalize(string value)
    {
        return value.Trim().ToLowerInvariant()
            .Replace('ı', 'i')
            .Replace('ğ', 'g')
            .Replace('ü', 'u')
            .Replace('ş', 's')
            .Replace('ö', 'o')
            .Replace('ç', 'c');
    }
}
