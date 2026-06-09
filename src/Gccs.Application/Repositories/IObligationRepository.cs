using Gccs.Domain.Compliance;

namespace Gccs.Application.Repositories;

public interface IObligationRepository
{
    Task<IReadOnlyList<Obligation>> ListAsync(CancellationToken cancellationToken = default);

    Task<Obligation?> FindByIdAsync(string id, CancellationToken cancellationToken = default);
}
