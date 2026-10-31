using Gccs.Application.Common;

namespace Gccs.Api.Tests;

// Only for isolated service tests; relational tests prove rollback semantics.
internal sealed class TestApplicationTransaction : IApplicationTransaction
{
    public Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default) => operation(cancellationToken);
}
