using Gccs.Application.Cmmc;

namespace Gccs.Infrastructure.Cmmc;

public sealed class InMemorySprsScoreCalculationHistoryRepository : ISprsScoreCalculationHistoryRepository
{
    private readonly List<SprsScoreCalculationDto> _calculations = [];
    private readonly object _sync = new();

    public Task SaveAsync(SprsScoreCalculationDto calculation, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            _calculations.Add(calculation);
        }
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<SprsScoreCalculationDto>?> ListCurrentTenantAsync(
        Guid assessmentId,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            return Task.FromResult<IReadOnlyList<SprsScoreCalculationDto>?>(_calculations
                .Where(calculation => calculation.AssessmentId == assessmentId)
                .OrderByDescending(calculation => calculation.GeneratedAt)
                .Take(50)
                .ToArray());
        }
    }
}
