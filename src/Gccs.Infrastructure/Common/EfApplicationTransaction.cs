using Gccs.Application.Common;
using Gccs.Application.Tenancy;
using Gccs.Domain.Audit;
using Gccs.Infrastructure.Persistence.Models;
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
        catch (Exception exception)
        {
            // Policy rejections describe a denied attempt, not a committed mutation.
            // Keep only those events after rolling the entire business transaction back.
            var rejections = exception is TenantDataHandlingModeRestrictedException or CuiReadyApprovalChecklistValidationException
                ? dbContext.ChangeTracker.Entries<AuditLogEntryEntity>()
                    .Where(entry => entry.Entity.Action == AuditAction.Rejected &&
                        entry.Entity.EntityType is "TenantDataHandlingModePolicy" or "CuiReadyApprovalChecklist" or "TenantDataHandlingMode")
                    .Select(entry => entry.Entity).ToArray()
                : [];
            await transaction.RollbackAsync(CancellationToken.None);
            await transaction.DisposeAsync();
            dbContext.ChangeTracker.Clear();
            foreach (var rejection in rejections)
            {
                // Previously committed tracked events are not replayed.
                if (!await dbContext.AuditLogEntries.AsNoTracking().AnyAsync(
                        entry => entry.Id == rejection.Id, CancellationToken.None))
                    dbContext.AuditLogEntries.Add(rejection);
            }
            if (rejections.Length > 0) await dbContext.SaveChangesAsync(CancellationToken.None);
            throw;
        }
    }
}
