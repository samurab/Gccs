using Gccs.Application.Security;
using Gccs.Application.Tenancy;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Tenancy;

public sealed class EfGovernmentCloudEnvironmentRepository(
    GccsDbContext dbContext,
    ICurrentTenantContext tenantContext) : IGovernmentCloudEnvironmentRepository
{
    public async Task<IReadOnlyList<GovernmentCloudEnvironmentDto>> ListCurrentTenantAsync(CancellationToken cancellationToken = default) =>
        await dbContext.GovernmentCloudEnvironments
            .AsNoTracking()
            .Where(environment => environment.TenantId == tenantContext.TenantId)
            .OrderBy(environment => environment.Name)
            .Select(environment => ToDto(environment))
            .ToListAsync(cancellationToken);

    public async Task<GovernmentCloudEnvironmentDto?> GetCurrentTenantAsync(
        Guid environmentId,
        CancellationToken cancellationToken = default)
    {
        var environment = await dbContext.GovernmentCloudEnvironments
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate => candidate.Id == environmentId && candidate.TenantId == tenantContext.TenantId,
                cancellationToken);

        return environment is null ? null : ToDto(environment);
    }

    public async Task<GovernmentCloudEnvironmentDto> AddToCurrentTenantAsync(
        GovernmentCloudEnvironmentModel environment,
        Guid actorUserId,
        string historyNote,
        CancellationToken cancellationToken = default)
    {
        var entity = new GovernmentCloudEnvironmentEntity
        {
            Id = environment.Id,
            TenantId = tenantContext.TenantId,
            Name = environment.Name,
            EnvironmentType = environment.EnvironmentType,
            Region = environment.Region,
            Boundary = environment.Boundary,
            NetworkSegment = environment.NetworkSegment,
            StorageAccount = environment.StorageAccount,
            DatabaseService = environment.DatabaseService,
            KeyManagementService = environment.KeyManagementService,
            LoggingWorkspace = environment.LoggingWorkspace,
            BackupPolicy = environment.BackupPolicy,
            PrivateNetworkingEnabled = environment.PrivateNetworkingEnabled,
            StorageEncryptionEnabled = environment.StorageEncryptionEnabled,
            DatabaseEncryptionEnabled = environment.DatabaseEncryptionEnabled,
            CustomerManagedKeysEnabled = environment.CustomerManagedKeysEnabled,
            AuditLoggingEnabled = environment.AuditLoggingEnabled,
            ImmutableLoggingEnabled = environment.ImmutableLoggingEnabled,
            BackupEnabled = environment.BackupEnabled,
            RestoreTested = environment.RestoreTested,
            Status = environment.Status,
            CreatedAt = environment.CreatedAt,
            CreatedByUserId = actorUserId
        };
        dbContext.GovernmentCloudEnvironments.Add(entity);
        AddHistory(entity, null, entity.Status, null, null, actorUserId, historyNote);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<GovernmentCloudEnvironmentDto?> UpdateInCurrentTenantAsync(
        Guid environmentId,
        UpsertGovernmentCloudEnvironmentRequest request,
        Guid actorUserId,
        string historyNote,
        CancellationToken cancellationToken = default)
    {
        var environment = await dbContext.GovernmentCloudEnvironments
            .SingleOrDefaultAsync(candidate => candidate.Id == environmentId && candidate.TenantId == tenantContext.TenantId, cancellationToken);
        if (environment is null)
        {
            return null;
        }

        environment.Name = request.Name.Trim();
        environment.EnvironmentType = request.EnvironmentType;
        environment.Region = request.Region.Trim();
        environment.Boundary = request.Boundary.Trim();
        environment.NetworkSegment = request.NetworkSegment.Trim();
        environment.StorageAccount = request.StorageAccount.Trim();
        environment.DatabaseService = request.DatabaseService.Trim();
        environment.KeyManagementService = request.KeyManagementService.Trim();
        environment.LoggingWorkspace = request.LoggingWorkspace.Trim();
        environment.BackupPolicy = request.BackupPolicy.Trim();
        environment.PrivateNetworkingEnabled = request.PrivateNetworkingEnabled;
        environment.StorageEncryptionEnabled = request.StorageEncryptionEnabled;
        environment.DatabaseEncryptionEnabled = request.DatabaseEncryptionEnabled;
        environment.CustomerManagedKeysEnabled = request.CustomerManagedKeysEnabled;
        environment.AuditLoggingEnabled = request.AuditLoggingEnabled;
        environment.ImmutableLoggingEnabled = request.ImmutableLoggingEnabled;
        environment.BackupEnabled = request.BackupEnabled;
        environment.RestoreTested = request.RestoreTested;
        environment.UpdatedAt = DateTimeOffset.UtcNow;
        environment.UpdatedByUserId = actorUserId;

        AddHistory(environment, environment.Status, environment.Status, environment.ReviewerName, environment.ReviewNotes, actorUserId, historyNote);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(environment);
    }

    public async Task<GovernmentCloudEnvironmentDto?> UpdateStatusInCurrentTenantAsync(
        Guid environmentId,
        EnvironmentReadinessStatus status,
        string reviewerName,
        string reviewNotes,
        Guid actorUserId,
        string historyNote,
        CancellationToken cancellationToken = default)
    {
        var environment = await dbContext.GovernmentCloudEnvironments
            .SingleOrDefaultAsync(candidate => candidate.Id == environmentId && candidate.TenantId == tenantContext.TenantId, cancellationToken);
        if (environment is null)
        {
            return null;
        }

        var previous = environment.Status;
        environment.Status = status;
        environment.ReviewerName = reviewerName;
        environment.ReviewNotes = reviewNotes;
        environment.ReviewedAt = DateTimeOffset.UtcNow;
        environment.UpdatedAt = DateTimeOffset.UtcNow;
        environment.UpdatedByUserId = actorUserId;

        AddHistory(environment, previous, status, reviewerName, reviewNotes, actorUserId, historyNote);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(environment);
    }

    private void AddHistory(
        GovernmentCloudEnvironmentEntity environment,
        EnvironmentReadinessStatus? previousStatus,
        EnvironmentReadinessStatus newStatus,
        string? reviewerName,
        string? reviewNotes,
        Guid actorUserId,
        string historyNote)
    {
        dbContext.GovernmentCloudEnvironmentStatusHistory.Add(new GovernmentCloudEnvironmentStatusHistoryEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantContext.TenantId,
            EnvironmentId = environment.Id,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            ReviewerName = reviewerName,
            ReviewNotes = reviewNotes,
            ChangedAt = DateTimeOffset.UtcNow,
            ChangedByUserId = actorUserId,
            HistoryNote = historyNote
        });
    }

    private static GovernmentCloudEnvironmentDto ToDto(GovernmentCloudEnvironmentEntity environment) =>
        new(
            environment.Id,
            environment.TenantId,
            environment.Name,
            environment.EnvironmentType,
            environment.Region,
            environment.Boundary,
            environment.NetworkSegment,
            environment.StorageAccount,
            environment.DatabaseService,
            environment.KeyManagementService,
            environment.LoggingWorkspace,
            environment.BackupPolicy,
            environment.PrivateNetworkingEnabled,
            environment.StorageEncryptionEnabled,
            environment.DatabaseEncryptionEnabled,
            environment.CustomerManagedKeysEnabled,
            environment.AuditLoggingEnabled,
            environment.ImmutableLoggingEnabled,
            environment.BackupEnabled,
            environment.RestoreTested,
            environment.Status,
            environment.ReviewerName,
            environment.ReviewNotes,
            environment.ReviewedAt,
            environment.CreatedAt,
            environment.UpdatedAt);
}
