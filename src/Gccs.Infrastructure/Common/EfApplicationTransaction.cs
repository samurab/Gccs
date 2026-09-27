using System.Data;
using System.Runtime.ExceptionServices;
using Gccs.Application.Common;
using Gccs.Domain.Audit;
using Gccs.Infrastructure.Persistence.Models;
using Gccs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Gccs.Infrastructure.Common;

public sealed class EfApplicationTransaction(IServiceProvider serviceProvider) : IApplicationTransaction
{
    public async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
        => await ExecuteCoreAsync(operation, null, cancellationToken);

    public async Task<T> ExecuteSerializableAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
        => await ExecuteCoreAsync(operation, IsolationLevel.Serializable, cancellationToken);

    private async Task<T> ExecuteCoreAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        IsolationLevel? isolationLevel,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);
        var dbContext = serviceProvider.GetService<GccsDbContext>();

        if (dbContext is null ||
            !dbContext.Database.IsRelational() ||
            dbContext.Database.CurrentTransaction is not null)
        {
            return await operation(cancellationToken);
        }

        var preexistingAuditIds = dbContext.ChangeTracker.Entries<AuditLogEntryEntity>()
            .Select(entry => entry.Entity.Id)
            .ToHashSet();
        Exception failure;
        AuditLogEntryEntity[] rejections;
        await using (var transaction = isolationLevel.HasValue
            ? await dbContext.Database.BeginTransactionAsync(isolationLevel.Value, cancellationToken)
            : await dbContext.Database.BeginTransactionAsync(cancellationToken))
        {
            try
            {
                var result = await operation(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return result;
            }
            catch (Exception exception)
            {
                failure = exception;
                // Rejected attempts are audit evidence, not committed business state. Preserve
                // every rejection event after rolling back the failed mutation transaction.
                rejections = dbContext.ChangeTracker.Entries<AuditLogEntryEntity>()
                    .Where(entry => entry.Entity.Action == AuditAction.Rejected &&
                        !preexistingAuditIds.Contains(entry.Entity.Id))
                    .Select(entry => entry.Entity)
                    .ToArray();
                try
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                }
                catch (InvalidOperationException) when (IsSerializationFailure(exception))
                {
                    // PostgreSQL has already aborted and completed the transaction. Preserve the
                    // original serialization failure instead of replacing it with rollback noise.
                }
            }
        }

        dbContext.ChangeTracker.Clear();
        foreach (var rejection in rejections)
        {
            // Previously committed tracked events are not replayed.
            if (!await dbContext.AuditLogEntries.AsNoTracking().AnyAsync(
                    entry => entry.Id == rejection.Id, CancellationToken.None))
                dbContext.AuditLogEntries.Add(rejection);
        }
        if (rejections.Length > 0) await dbContext.SaveChangesAsync(CancellationToken.None);

        if (IsSerializationFailure(failure))
            throw new ApplicationConcurrencyException("The operation conflicted with another transaction.", failure);

        ExceptionDispatchInfo.Capture(failure).Throw();
        throw new InvalidOperationException("Unreachable transaction failure path.");
    }

    private static bool IsSerializationFailure(Exception exception) =>
        exception is PostgresException { SqlState: PostgresErrorCodes.SerializationFailure } ||
        exception.InnerException is not null && IsSerializationFailure(exception.InnerException);
}
