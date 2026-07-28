using Gccs.Application.Common;
using Gccs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Gccs.Infrastructure.Common;

public sealed class EfApplicationTransaction(IServiceProvider serviceProvider) : IApplicationTransaction
{
    public async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        var dbContext = serviceProvider.GetService<GccsDbContext>();

        if (dbContext is null ||
            !dbContext.Database.IsRelational() ||
            dbContext.Database.CurrentTransaction is not null)
        {
            return await operation(cancellationToken);
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var result = await operation(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            dbContext.ChangeTracker.Clear();
            throw;
        }
    }
}
