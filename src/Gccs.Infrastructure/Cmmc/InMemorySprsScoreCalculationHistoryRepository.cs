using Gccs.Application.Cmmc;

namespace Gccs.Infrastructure.Cmmc;

public sealed class InMemorySprsScoreCalculationHistoryRepository : ISprsScoreCalculationHistoryRepository
{
    private readonly List<SprsScoreCalculationDto> _calculations = [];

    public Task SaveAsync(SprsScoreCalculationDto calculation, CancellationToken cancellationToken = default)
    {
        _calculations.Add(calculation);
        return Task.CompletedTask;
    }
}
