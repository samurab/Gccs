using Gccs.Application.Security;
using Gccs.Application.Tenancy;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Tenancy;

public sealed class EfRegulatedTenantProvisioningRepository(
    GccsDbContext dbContext,
    ICurrentTenantContext tenantContext) : IRegulatedTenantProvisioningRepository
{
    public async Task<IReadOnlyList<RegulatedTenantProvisioningRequestDto>> ListCurrentTenantAsync(CancellationToken cancellationToken = default) =>
        await QueryRequests()
            .Where(request => request.TenantId == tenantContext.TenantId)
            .OrderByDescending(request => request.CreatedAt)
            .Select(request => ToDto(request))
            .ToListAsync(cancellationToken);

    public async Task<RegulatedTenantProvisioningRequestDto?> GetCurrentTenantAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        var request = await QueryRequests()
            .SingleOrDefaultAsync(candidate => candidate.Id == requestId && candidate.TenantId == tenantContext.TenantId, cancellationToken);

        return request is null ? null : ToDto(request);
    }

    public async Task<GovernmentCloudEnvironmentDto?> GetEnvironmentForCurrentTenantAsync(Guid environmentId, CancellationToken cancellationToken = default)
    {
        var environment = await dbContext.GovernmentCloudEnvironments
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == environmentId && candidate.TenantId == tenantContext.TenantId, cancellationToken);

        return environment is null
            ? null
            : new GovernmentCloudEnvironmentDto(
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

    public async Task<RegulatedTenantProvisioningRequestDto> CreateForCurrentTenantAsync(
        CreateRegulatedTenantProvisioningRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var entity = new RegulatedTenantProvisioningRequestEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantContext.TenantId,
            TenantName = request.TenantName.Trim(),
            CustomerType = request.CustomerType.Trim(),
            EnvironmentId = request.EnvironmentId,
            DataHandlingMode = request.DataHandlingMode,
            CuiApprovalComplete = request.CuiApprovalComplete,
            KeyPolicy = request.KeyPolicy.Trim(),
            SupportModel = request.SupportModel.Trim(),
            MigrationSource = request.MigrationSource.Trim(),
            Status = RegulatedProvisioningStatus.Requested,
            CreatedAt = now,
            CreatedByUserId = actorUserId
        };

        dbContext.RegulatedTenantProvisioningRequests.Add(entity);
        AddHistory(entity, null, entity.Status, actorUserId, "Provisioning request created.");
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public Task<RegulatedTenantProvisioningRequestDto?> MarkApprovalCompleteAsync(
        Guid requestId,
        RegulatedProvisioningApprovalArea area,
        string approverName,
        string notes,
        Guid actorUserId,
        CancellationToken cancellationToken = default) =>
        UpdateRequestAsync(
            requestId,
            actorUserId,
            request =>
            {
                var approval = request.Approvals.SingleOrDefault(candidate => candidate.Area == area);
                if (approval is null)
                {
                    approval = new RegulatedProvisioningApprovalEntity
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantContext.TenantId,
                        RequestId = request.Id,
                        Area = area
                    };
                    dbContext.RegulatedProvisioningApprovals.Add(approval);
                    request.Approvals.Add(approval);
                }

                approval.ApproverName = approverName;
                approval.Notes = notes;
                approval.ApprovedAt = DateTimeOffset.UtcNow;
                approval.ApprovedByUserId = actorUserId;
                if (request.Status is RegulatedProvisioningStatus.Requested && request.Approvals.Select(item => item.Area).Distinct().Count() >= 5)
                {
                    request.Status = RegulatedProvisioningStatus.Approved;
                }
            },
            "Approval completed.",
            cancellationToken);

    public Task<RegulatedTenantProvisioningRequestDto?> MarkChecklistItemCompleteAsync(
        Guid requestId,
        RegulatedProvisioningChecklistItem item,
        string completedByName,
        string evidenceReference,
        Guid actorUserId,
        CancellationToken cancellationToken = default) =>
        UpdateRequestAsync(
            requestId,
            actorUserId,
            request =>
            {
                var checklist = request.Checklist.SingleOrDefault(candidate => candidate.Item == item);
                if (checklist is null)
                {
                    checklist = new RegulatedProvisioningChecklistEntity
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantContext.TenantId,
                        RequestId = request.Id,
                        Item = item
                    };
                    dbContext.RegulatedProvisioningChecklist.Add(checklist);
                    request.Checklist.Add(checklist);
                }

                checklist.CompletedByName = completedByName;
                checklist.EvidenceReference = evidenceReference;
                checklist.CompletedAt = DateTimeOffset.UtcNow;
                checklist.CompletedByUserId = actorUserId;
            },
            "Checklist item completed.",
            cancellationToken);

    public Task<RegulatedTenantProvisioningRequestDto?> UpdateStatusAsync(
        Guid requestId,
        RegulatedProvisioningStatus status,
        Guid actorUserId,
        string historyNote,
        CancellationToken cancellationToken = default) =>
        UpdateRequestAsync(
            requestId,
            actorUserId,
            request => request.Status = status,
            historyNote,
            cancellationToken);

    public async Task<RegulatedTenantProvisioningRequestDto?> CreateTenantAndMarkReadyAsync(
        Guid requestId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var request = await QueryRequests()
            .SingleOrDefaultAsync(candidate => candidate.Id == requestId && candidate.TenantId == tenantContext.TenantId, cancellationToken);
        if (request is null)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        if (!request.ProvisionedTenantId.HasValue)
        {
            var tenant = new TenantEntity
            {
                Id = Guid.NewGuid(),
                Name = request.TenantName,
                Status = TenantStatus.Active,
                DataPosture = request.DataHandlingMode,
                CreatedAt = now,
                CreatedByUserId = actorUserId
            };
            dbContext.Tenants.Add(tenant);
            request.ProvisionedTenantId = tenant.Id;
        }

        var previous = request.Status;
        request.Status = RegulatedProvisioningStatus.Ready;
        request.UpdatedAt = now;
        request.UpdatedByUserId = actorUserId;
        AddHistory(request, previous, request.Status, actorUserId, "Provisioned tenant created.");
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(request);
    }

    public Task<RegulatedTenantProvisioningRequestDto?> RecordFailureAsync(
        Guid requestId,
        string failureReason,
        string rollbackDecision,
        string owner,
        Guid actorUserId,
        CancellationToken cancellationToken = default) =>
        UpdateRequestAsync(
            requestId,
            actorUserId,
            request =>
            {
                request.Status = RegulatedProvisioningStatus.Failed;
                request.FailureReason = failureReason;
                request.RollbackDecision = rollbackDecision;
                request.FailureOwner = owner;
            },
            "Provisioning failure recorded.",
            cancellationToken);

    private async Task<RegulatedTenantProvisioningRequestDto?> UpdateRequestAsync(
        Guid requestId,
        Guid actorUserId,
        Action<RegulatedTenantProvisioningRequestEntity> update,
        string historyNote,
        CancellationToken cancellationToken)
    {
        var request = await QueryRequests()
            .SingleOrDefaultAsync(candidate => candidate.Id == requestId && candidate.TenantId == tenantContext.TenantId, cancellationToken);
        if (request is null)
        {
            return null;
        }

        var previous = request.Status;
        update(request);
        request.UpdatedAt = DateTimeOffset.UtcNow;
        request.UpdatedByUserId = actorUserId;
        AddHistory(request, previous, request.Status, actorUserId, historyNote);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(request);
    }

    private void AddHistory(
        RegulatedTenantProvisioningRequestEntity request,
        RegulatedProvisioningStatus? previous,
        RegulatedProvisioningStatus next,
        Guid actorUserId,
        string note)
    {
        dbContext.RegulatedTenantProvisioningHistory.Add(new RegulatedTenantProvisioningHistoryEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantContext.TenantId,
            RequestId = request.Id,
            PreviousStatus = previous,
            NewStatus = next,
            ChangedAt = DateTimeOffset.UtcNow,
            ChangedByUserId = actorUserId,
            Note = note
        });
    }

    private IQueryable<RegulatedTenantProvisioningRequestEntity> QueryRequests() =>
        dbContext.RegulatedTenantProvisioningRequests
            .Include(request => request.Approvals)
            .Include(request => request.Checklist);

    private static RegulatedTenantProvisioningRequestDto ToDto(RegulatedTenantProvisioningRequestEntity request) =>
        new(
            request.Id,
            request.TenantId,
            request.TenantName,
            request.CustomerType,
            request.EnvironmentId,
            request.DataHandlingMode,
            request.CuiApprovalComplete,
            request.KeyPolicy,
            request.SupportModel,
            request.MigrationSource,
            request.Status,
            request.ProvisionedTenantId,
            request.FailureReason,
            request.RollbackDecision,
            request.FailureOwner,
            request.Approvals.Select(approval => approval.Area).Distinct().ToArray(),
            request.Checklist.Select(item => item.Item).Distinct().ToArray(),
            request.CreatedAt,
            request.UpdatedAt);
}
