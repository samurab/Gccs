using Gccs.Application.Ai;

namespace Gccs.Infrastructure.Ai;

public sealed class InMemoryAiRetrievalSourceRepository : IAiRetrievalSourceRepository
{
    private readonly List<AiRetrievalSourceDto> _sources = [];

    public Task<IReadOnlyList<AiRetrievalSourceDto>> ListSourcesAsync(Guid tenantId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<AiRetrievalSourceDto>>(_sources.ToArray());

    public void Seed(params AiRetrievalSourceDto[] sources)
    {
        _sources.AddRange(sources);
    }
}
