using Gccs.Application.Ai;
using Gccs.Domain.Tenancy;

namespace Gccs.Infrastructure.Ai;

public sealed class InMemoryAiRetrievalSourceRepository : IAiRetrievalSourceRepository
{
    private readonly List<AiRetrievalSourceDto> _sources = [];

    public TenantDataPosture DataPosture { get; set; } = TenantDataPosture.NoCui;

    public Task<AiRetrievalSourceBatch> SearchSourcesAsync(
        AiRetrievalSourceQuery query,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new AiRetrievalSourceBatch(
            DataPosture,
            _sources
                .Where(source => MatchesQuestion(source, query.Question))
                .Take(query.MaximumCandidates)
                .ToArray(),
            "deterministic-token-minimum-match-v1"));

    public void Seed(params AiRetrievalSourceDto[] sources)
    {
        foreach (var source in sources)
        {
            if (_sources.Any(existing => string.Equals(existing.Id, source.Id, StringComparison.Ordinal)))
                throw new InvalidOperationException($"Retrieval source '{source.Id}' is already seeded.");
            if (_sources.Count >= AiRetrievalAssistantService.MaximumCandidateCount)
                throw new InvalidOperationException("The bounded in-memory retrieval source limit was exceeded.");
            _sources.Add(source);
        }
    }

    private static bool MatchesQuestion(AiRetrievalSourceDto source, string question)
    {
        var terms = Tokenize(question);
        if (terms.Count == 0)
            return false;

        var searchable = Tokenize(string.Join(" ", source.Keywords.Append(source.Title).Append(source.Summary)));
        return terms.Count(searchable.Contains) >= Math.Min(2, terms.Count);
    }

    private static HashSet<string> Tokenize(string value) => value
        .Split([' ', '/', ':', '-', '_', '.', ',', '(', ')', '?', '!'], StringSplitOptions.RemoveEmptyEntries)
        .Select(token => NormalizeToken(token))
        .Where(token => token.Length >= 3 && !StopTerms.Contains(token))
        .ToHashSet(StringComparer.Ordinal);

    private static readonly HashSet<string> StopTerms = new(
        ["about", "and", "answer", "approved", "doe", "does", "explain", "for", "from", "how", "into", "the", "this", "what", "with"],
        StringComparer.Ordinal);

    private static string NormalizeToken(string value)
    {
        var token = value.Trim().ToLowerInvariant();
        if (token.Length > 5 && token.EndsWith("ing", StringComparison.Ordinal))
            return token[..^3];
        if (token.Length > 4 && token.EndsWith("es", StringComparison.Ordinal))
            return token[..^2];
        if (token.Length > 3 && token.EndsWith('s'))
            return token[..^1];
        return token;
    }
}
