using System.Data;
using System.Text.Json;
using Gccs.Application.Audit;
using Gccs.Application.Marketing;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Gccs.Infrastructure.Marketing;

public sealed class EfDemoFollowUpRepository(
    GccsDbContext dbContext,
    IAuditRequestMetadata requestMetadata) : IDemoFollowUpRepository
{
    public async Task<DemoFollowUpQueueWriteResult> QueueRequestAsync(
        DemoFollowUpQueueCommand command,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        if (!await dbContext.DemoRequests.AsNoTracking()
            .AnyAsync(request => request.Id == command.DemoRequestId, cancellationToken))
        {
            await transaction.RollbackAsync(cancellationToken);
            return new DemoFollowUpQueueWriteResult(DemoFollowUpQueueDisposition.DemoRequestNotFound);
        }

        var pending = await dbContext.DemoFollowUpRequests
            .SingleOrDefaultAsync(request =>
                request.DemoRequestId == command.DemoRequestId &&
                request.Status == DemoFollowUpCatalog.Pending,
                cancellationToken);
        if (pending is not null && pending.ExpiresAt > command.RequestedAt)
        {
            await transaction.RollbackAsync(cancellationToken);
            return new DemoFollowUpQueueWriteResult(
                DemoFollowUpQueueDisposition.AlreadyPending,
                pending.Id,
                pending.ExpiresAt);
        }

        if (pending is not null)
        {
            pending.Status = DemoFollowUpCatalog.Expired;
            pending.UpdatedAt = command.RequestedAt;
        }

        dbContext.DemoFollowUpRequests.Add(new DemoFollowUpRequestEntity
        {
            Id = command.FollowUpRequestId,
            DemoRequestId = command.DemoRequestId,
            TokenHash = command.TokenHash,
            Status = DemoFollowUpCatalog.Pending,
            TemplateVersion = command.TemplateVersion,
            NoCuiNoticeVersion = command.NoCuiNoticeVersion,
            ExpiresAt = command.ExpiresAt,
            RequestedByUserId = command.RequestedByUserId,
            RequestedAt = command.RequestedAt,
            UpdatedAt = command.RequestedAt
        });
        dbContext.DemoRequestDeliveries.Add(new DemoRequestDeliveryEntity
        {
            Id = Guid.NewGuid(),
            DemoRequestId = command.DemoRequestId,
            DemoFollowUpRequestId = command.FollowUpRequestId,
            DeliveryKind = $"DemoFollowUpRequested:{command.FollowUpRequestId:N}",
            Status = "Queued",
            RequestedByUserId = command.RequestedByUserId,
            CreatedAt = command.RequestedAt,
            UpdatedAt = command.RequestedAt
        });

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new DemoFollowUpQueueWriteResult(
                DemoFollowUpQueueDisposition.Queued,
                command.FollowUpRequestId,
                command.ExpiresAt);
        }
        catch (Exception exception) when (HasPostgresState(
            exception,
            PostgresErrorCodes.UniqueViolation,
            PostgresErrorCodes.SerializationFailure))
        {
            await transaction.DisposeAsync();
            dbContext.ChangeTracker.Clear();
            var concurrent = await dbContext.DemoFollowUpRequests.AsNoTracking()
                .Where(request =>
                    request.DemoRequestId == command.DemoRequestId &&
                    request.Status == DemoFollowUpCatalog.Pending &&
                    request.ExpiresAt > command.RequestedAt)
                .Select(request => new { request.Id, request.ExpiresAt })
                .SingleOrDefaultAsync(cancellationToken);
            if (concurrent is not null)
            {
                return new DemoFollowUpQueueWriteResult(
                    DemoFollowUpQueueDisposition.AlreadyPending,
                    concurrent.Id,
                    concurrent.ExpiresAt);
            }

            throw new InvalidOperationException("The follow-up request could not be queued because concurrent state changed.");
        }
    }

    public Task<DemoFollowUpAccessRecord?> GetAccessAsync(
        Guid followUpRequestId,
        string tokenHash,
        CancellationToken cancellationToken = default) =>
        dbContext.DemoFollowUpRequests.AsNoTracking()
            .Where(request => request.Id == followUpRequestId && request.TokenHash == tokenHash)
            .Select(request => new DemoFollowUpAccessRecord(
                request.Id,
                request.DemoRequestId,
                request.Status,
                request.ExpiresAt,
                request.RequestedAt,
                request.RespondedAt))
            .SingleOrDefaultAsync(cancellationToken);

    public Task<DemoFollowUpPreviewRecord?> GetPreviewAsync(
        Guid demoRequestId,
        Guid followUpRequestId,
        CancellationToken cancellationToken = default) =>
        dbContext.DemoFollowUpRequests.AsNoTracking()
            .Where(request => request.Id == followUpRequestId && request.DemoRequestId == demoRequestId)
            .Select(request => new DemoFollowUpPreviewRecord(
                request.Id,
                request.DemoRequestId,
                request.TokenHash,
                request.Status,
                request.ExpiresAt))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<DemoFollowUpSubmissionDisposition> SubmitResponseAsync(
        string tokenHash,
        DemoFollowUpResponseCommand command,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        var request = await dbContext.DemoFollowUpRequests.SingleOrDefaultAsync(
            item => item.Id == command.FollowUpRequestId && item.TokenHash == tokenHash,
            cancellationToken);
        if (request is null || request.DemoRequestId != command.DemoRequestId)
        {
            await transaction.RollbackAsync(cancellationToken);
            return DemoFollowUpSubmissionDisposition.Invalid;
        }

        if (request.Status == DemoFollowUpCatalog.Responded)
        {
            await transaction.RollbackAsync(cancellationToken);
            return DemoFollowUpSubmissionDisposition.AlreadyResponded;
        }

        if (request.Status == DemoFollowUpCatalog.Expired || request.ExpiresAt <= command.SubmittedAt)
        {
            if (request.Status != DemoFollowUpCatalog.Expired)
            {
                request.Status = DemoFollowUpCatalog.Expired;
                request.UpdatedAt = command.SubmittedAt;
                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            else
            {
                await transaction.RollbackAsync(cancellationToken);
            }

            return DemoFollowUpSubmissionDisposition.Expired;
        }

        if (request.Status != DemoFollowUpCatalog.Pending)
        {
            await transaction.RollbackAsync(cancellationToken);
            return DemoFollowUpSubmissionDisposition.Invalid;
        }

        dbContext.DemoFollowUpResponses.Add(new DemoFollowUpResponseEntity
        {
            Id = command.ResponseId,
            DemoFollowUpRequestId = command.FollowUpRequestId,
            DemoRequestId = command.DemoRequestId,
            WorkflowsJson = JsonSerializer.Serialize(command.Workflows),
            OtherWorkflow = command.OtherWorkflow,
            Goals = command.Goals,
            Challenges = command.Challenges,
            CurrentProcess = command.CurrentProcess,
            AdditionalContext = command.AdditionalContext,
            NoCuiConfirmed = true,
            NoCuiNoticeVersion = command.NoCuiNoticeVersion,
            SubmittedAt = command.SubmittedAt,
            IpAddress = Truncate(requestMetadata.IpAddress, 120),
            UserAgent = Truncate(requestMetadata.UserAgent, 500),
            CorrelationId = Truncate(requestMetadata.CorrelationId, 120)
        });
        request.Status = DemoFollowUpCatalog.Responded;
        request.RespondedAt = command.SubmittedAt;
        request.UpdatedAt = command.SubmittedAt;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return DemoFollowUpSubmissionDisposition.Accepted;
        }
        catch (Exception exception) when (HasPostgresState(
            exception,
            PostgresErrorCodes.UniqueViolation,
            PostgresErrorCodes.SerializationFailure))
        {
            await transaction.DisposeAsync();
            dbContext.ChangeTracker.Clear();
            var state = await dbContext.DemoFollowUpRequests.AsNoTracking()
                .Where(item => item.Id == command.FollowUpRequestId && item.TokenHash == tokenHash)
                .Select(item => new { item.Status, item.ExpiresAt })
                .SingleOrDefaultAsync(cancellationToken);
            if (state is null) return DemoFollowUpSubmissionDisposition.Invalid;
            if (state.Status == DemoFollowUpCatalog.Responded)
                return DemoFollowUpSubmissionDisposition.AlreadyResponded;
            if (state.Status == DemoFollowUpCatalog.Expired || state.ExpiresAt <= command.SubmittedAt)
                return DemoFollowUpSubmissionDisposition.Expired;
            throw new InvalidOperationException("The follow-up response could not be saved because concurrent state changed.");
        }
    }

    private static string Truncate(string value, int maximumLength) =>
        value.Length <= maximumLength ? value : value[..maximumLength];

    private static bool HasPostgresState(Exception exception, params string[] states)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException postgresException &&
                states.Contains(postgresException.SqlState, StringComparer.Ordinal)) return true;
        }

        return false;
    }
}
