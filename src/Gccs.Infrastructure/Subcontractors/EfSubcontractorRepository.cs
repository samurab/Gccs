using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Security;
using Gccs.Application.Subcontractors;
using Gccs.Domain.Evidence;
using Gccs.Domain.Vendors;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Subcontractors;

public sealed class EfSubcontractorRepository(
    GccsDbContext dbContext,
    ICurrentTenantContext tenantContext) : ISubcontractorRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<IReadOnlyList<SubcontractorDto>> ListCurrentTenantAsync(
        SubcontractorListQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var items = QueryCurrentTenant();
        if (!string.IsNullOrWhiteSpace(query?.Status) &&
            Enum.TryParse<SubcontractorStatus>(query.Status, true, out var status))
        {
            items = items.Where(subcontractor => subcontractor.Status == status);
        }

        if (query?.ExpiringInsuranceOnly == true)
        {
            var soon = today.AddDays(60);
            items = items.Where(subcontractor => subcontractor.InsuranceExpiresAt != null && subcontractor.InsuranceExpiresAt <= soon);
        }

        if (!string.IsNullOrWhiteSpace(query?.Owner))
        {
            items = items.Where(subcontractor => subcontractor.OwnerFunction != null && subcontractor.OwnerFunction.Contains(query.Owner));
        }

        var subcontractors = await items.OrderBy(subcontractor => subcontractor.Name).ToArrayAsync(cancellationToken);
        return subcontractors.Select(ToDto).ToArray();
    }

    public async Task<SubcontractorDto?> FindCurrentTenantAsync(Guid subcontractorId, CancellationToken cancellationToken = default)
    {
        var subcontractor = await QueryCurrentTenant()
            .SingleOrDefaultAsync(candidate => candidate.Id == subcontractorId, cancellationToken);
        return subcontractor is null ? null : ToDto(subcontractor);
    }

    public async Task<SubcontractorDto> CreateAsync(
        UpsertSubcontractorRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var entity = new SubcontractorEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantContext.TenantId,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByUserId = actorUserId
        };

        Apply(entity, request);
        SyncContractLinks(entity, request.ContractIds);
        dbContext.Subcontractors.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<SubcontractorDto?> UpdateAsync(
        Guid subcontractorId,
        UpsertSubcontractorRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var entity = await QueryCurrentTenant()
            .SingleOrDefaultAsync(candidate => candidate.Id == subcontractorId, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        Apply(entity, request);
        SyncContractLinks(entity, request.ContractIds);
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        entity.UpdatedByUserId = actorUserId;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<SubcontractorDto?> ApplySamDataAsync(
        Guid subcontractorId,
        ApplySubcontractorEntityLookupRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var entity = await QueryCurrentTenant()
            .SingleOrDefaultAsync(candidate => candidate.Id == subcontractorId, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        var selected = request.SelectedFields.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (selected.Contains("name"))
        {
            entity.Name = request.Result.LegalBusinessName;
        }

        if (selected.Contains("uei"))
        {
            entity.Uei = request.Result.Uei;
        }

        if (selected.Contains("cageCode"))
        {
            entity.CageCode = request.Result.CageCode;
        }

        entity.SamRegistrationStatus = request.Result.RegistrationStatus;
        entity.SamRegistrationExpiresAt = request.Result.SamRegistrationExpiresAt;
        entity.SamSource = request.Result.Source;
        entity.SamRetrievedAt = request.Result.RetrievedAt;
        entity.SamNaicsJson = JsonSerializer.Serialize(request.Result.NaicsCodes, JsonOptions);
        entity.SamExclusionStatus = request.Result.ExclusionStatus;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        entity.UpdatedByUserId = actorUserId;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<IReadOnlyList<SubcontractorFlowDownDto>?> ListFlowDownsAsync(
        Guid subcontractorId,
        Guid? contractId,
        CancellationToken cancellationToken = default)
    {
        var subcontractorExists = await dbContext.Subcontractors
            .AnyAsync(candidate => candidate.Id == subcontractorId && candidate.TenantId == tenantContext.TenantId, cancellationToken);
        if (!subcontractorExists)
        {
            return null;
        }

        var query = dbContext.FlowDownClauses
            .AsNoTracking()
            .Where(flowDown => flowDown.SubcontractorId == subcontractorId);
        if (contractId is not null)
        {
            query = query.Where(flowDown => flowDown.ContractId == contractId);
        }

        var flowDowns = await query
            .OrderBy(flowDown => flowDown.ClauseNumber)
            .ThenBy(flowDown => flowDown.Title)
            .ToArrayAsync(cancellationToken);
        return flowDowns.Select(ToDto).ToArray();
    }

    public async Task<SubcontractorFlowDownDto?> CreateFlowDownAsync(
        Guid subcontractorId,
        UpsertSubcontractorFlowDownRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var subcontractorExists = await dbContext.Subcontractors
            .AnyAsync(candidate => candidate.Id == subcontractorId && candidate.TenantId == tenantContext.TenantId, cancellationToken);
        if (!subcontractorExists)
        {
            return null;
        }

        var contractId = await ResolveAndValidateContractReferencesAsync(request, cancellationToken);
        await ValidateSignedEvidenceAsync(request.SignedEvidenceItemId, cancellationToken);

        var entity = new FlowDownClauseEntity
        {
            Id = Guid.NewGuid(),
            SubcontractorId = subcontractorId,
            ContractId = contractId,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByUserId = actorUserId
        };
        Apply(entity, request);
        dbContext.FlowDownClauses.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<SubcontractorFlowDownDto?> UpdateFlowDownAsync(
        Guid subcontractorId,
        Guid flowDownId,
        UpsertSubcontractorFlowDownRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.FlowDownClauses
            .Include(flowDown => flowDown.Subcontractor)
            .SingleOrDefaultAsync(
                candidate => candidate.Id == flowDownId &&
                    candidate.SubcontractorId == subcontractorId &&
                    candidate.Subcontractor != null &&
                    candidate.Subcontractor.TenantId == tenantContext.TenantId,
                cancellationToken);
        if (entity is null)
        {
            return null;
        }

        var contractId = await ResolveAndValidateContractReferencesAsync(request, cancellationToken);
        await ValidateSignedEvidenceAsync(request.SignedEvidenceItemId, cancellationToken);
        Apply(entity, request);
        entity.ContractId = contractId;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        entity.UpdatedByUserId = actorUserId;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<IReadOnlyList<SubcontractorEvidenceRequestDto>?> ListEvidenceRequestsAsync(
        Guid subcontractorId,
        CancellationToken cancellationToken = default)
    {
        var subcontractorExists = await dbContext.Subcontractors
            .AnyAsync(candidate => candidate.Id == subcontractorId && candidate.TenantId == tenantContext.TenantId, cancellationToken);
        if (!subcontractorExists)
        {
            return null;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var requests = await dbContext.SubcontractorEvidenceRequests
            .AsNoTracking()
            .Where(request => request.SubcontractorId == subcontractorId && request.TenantId == tenantContext.TenantId)
            .OrderBy(request => request.DueDate)
            .ThenByDescending(request => request.CreatedAt)
            .ToArrayAsync(cancellationToken);
        return requests.Select(request => ToDto(request, today)).ToArray();
    }

    public async Task<SubcontractorEvidenceRequestDto?> CreateEvidenceRequestAsync(
        Guid subcontractorId,
        UpsertSubcontractorEvidenceRequestRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var subcontractorExists = await dbContext.Subcontractors
            .AnyAsync(candidate => candidate.Id == subcontractorId && candidate.TenantId == tenantContext.TenantId, cancellationToken);
        if (!subcontractorExists)
        {
            return null;
        }

        await ValidateEvidenceRequestReferencesAsync(subcontractorId, request, cancellationToken);

        var entity = new SubcontractorEvidenceRequestEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantContext.TenantId,
            SubcontractorId = subcontractorId,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByUserId = actorUserId
        };
        Apply(entity, request, actorUserId);
        dbContext.SubcontractorEvidenceRequests.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<SubcontractorEvidenceRequestDto?> UpdateEvidenceRequestAsync(
        Guid subcontractorId,
        Guid evidenceRequestId,
        UpsertSubcontractorEvidenceRequestRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.SubcontractorEvidenceRequests.SingleOrDefaultAsync(
            candidate => candidate.Id == evidenceRequestId &&
                candidate.SubcontractorId == subcontractorId &&
                candidate.TenantId == tenantContext.TenantId,
            cancellationToken);
        if (entity is null)
        {
            return null;
        }

        await ValidateEvidenceRequestReferencesAsync(subcontractorId, request, cancellationToken);
        Apply(entity, request, actorUserId);
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        entity.UpdatedByUserId = actorUserId;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    private IQueryable<SubcontractorEntity> QueryCurrentTenant() =>
        dbContext.Subcontractors
            .Include(subcontractor => subcontractor.Contracts)
            .Where(subcontractor => subcontractor.TenantId == tenantContext.TenantId);

    private static void Apply(SubcontractorEntity entity, UpsertSubcontractorRequest request)
    {
        entity.Name = request.Name;
        entity.Uei = request.Uei;
        entity.CageCode = request.CageCode;
        entity.Status = request.Status;
        entity.RoleDescription = request.RoleDescription;
        entity.SmallBusinessStatus = request.SmallBusinessStatus;
        entity.NaicsCodesJson = JsonSerializer.Serialize(request.NaicsCodes ?? [], JsonOptions);
        entity.CertificationsJson = JsonSerializer.Serialize(request.Certifications ?? [], JsonOptions);
        entity.CmmcStatus = request.CmmcStatus;
        entity.InsuranceExpiresAt = request.InsuranceExpiresAt;
        entity.NdaStatus = request.NdaStatus;
        entity.WorkshareDescription = request.WorkshareDescription;
        entity.WorksharePercentage = request.WorksharePercentage;
        entity.HasFciAccess = request.HasFciAccess;
        entity.HasCuiAccess = request.HasCuiAccess;
        entity.HasExportControlledAccess = request.HasExportControlledAccess;
        entity.RequiredCmmcLevel = request.RequiredCmmcLevel;
        entity.ContactName = request.ContactName;
        entity.ContactEmail = request.ContactEmail;
        entity.ContactPhone = request.ContactPhone;
        entity.ContactTitle = request.ContactTitle;
        entity.OwnerFunction = request.OwnerFunction;
    }

    private async Task<Guid?> ResolveAndValidateContractReferencesAsync(
        UpsertSubcontractorFlowDownRequest request,
        CancellationToken cancellationToken)
    {
        Guid? contractId = request.ContractId;
        if (request.ContractClauseId is not null)
        {
            var clause = await dbContext.Set<ContractClauseEntity>()
                .AsNoTracking()
                .Include(candidate => candidate.Contract)
                .SingleOrDefaultAsync(candidate => candidate.Id == request.ContractClauseId, cancellationToken);
            if (clause?.Contract is null || clause.Contract.TenantId != tenantContext.TenantId)
            {
                throw new SubcontractorValidationException("Contract clause was not found for the current tenant.");
            }

            if (contractId is not null && contractId != clause.ContractId)
            {
                throw new SubcontractorValidationException("Contract clause does not belong to the requested contract.");
            }

            contractId = clause.ContractId;

            if (request.ObligationId is not null)
            {
                var clauseHasObligation = await dbContext.Set<ContractClauseObligationEntity>()
                    .AnyAsync(
                        link => link.ContractClauseId == request.ContractClauseId && link.ObligationId == request.ObligationId,
                        cancellationToken);
                if (!clauseHasObligation)
                {
                    throw new SubcontractorValidationException("Obligation is not assigned to the requested contract clause.");
                }
            }
        }

        if (contractId is not null)
        {
            var contractExists = await dbContext.Contracts
                .AnyAsync(contract => contract.Id == contractId && contract.TenantId == tenantContext.TenantId, cancellationToken);
            if (!contractExists)
            {
                throw new SubcontractorValidationException("Contract was not found for the current tenant.");
            }
        }

        if (request.ObligationId is not null && request.ContractClauseId is null)
        {
            var obligationExists = await dbContext.Obligations
                .AnyAsync(obligation => obligation.Id == request.ObligationId, cancellationToken);
            if (!obligationExists)
            {
                throw new SubcontractorValidationException("Obligation was not found.");
            }
        }

        return contractId;
    }

    private async Task ValidateSignedEvidenceAsync(Guid? signedEvidenceItemId, CancellationToken cancellationToken)
    {
        if (signedEvidenceItemId is null)
        {
            return;
        }

        var evidence = await dbContext.EvidenceItems
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == signedEvidenceItemId, cancellationToken);
        if (evidence is null || evidence.TenantId != tenantContext.TenantId)
        {
            throw new SubcontractorValidationException("Signed evidence was not found for the current tenant.");
        }

        if (evidence.Status != EvidenceStatus.Approved || evidence.Type != EvidenceType.SignedFlowDown)
        {
            throw new SubcontractorValidationException("Signed evidence must be an approved signed flow-down evidence item.");
        }
    }

    private async Task ValidateEvidenceRequestReferencesAsync(
        Guid subcontractorId,
        UpsertSubcontractorEvidenceRequestRequest request,
        CancellationToken cancellationToken)
    {
        if (request.ObligationId is not null)
        {
            var obligationExists = await dbContext.Obligations
                .AnyAsync(obligation => obligation.Id == request.ObligationId, cancellationToken);
            if (!obligationExists)
            {
                throw new SubcontractorValidationException("Obligation was not found.");
            }
        }

        if (request.RelatedFlowDownClauseId is not null)
        {
            var flowDownExists = await dbContext.FlowDownClauses
                .Include(flowDown => flowDown.Subcontractor)
                .AnyAsync(flowDown =>
                    flowDown.Id == request.RelatedFlowDownClauseId &&
                    flowDown.SubcontractorId == subcontractorId &&
                    flowDown.Subcontractor != null &&
                    flowDown.Subcontractor.TenantId == tenantContext.TenantId,
                    cancellationToken);
            if (!flowDownExists)
            {
                throw new SubcontractorValidationException("Related flow-down was not found for the subcontractor.");
            }
        }

        if (request.ReceivedEvidenceItemId is not null)
        {
            var evidence = await dbContext.EvidenceItems
                .AsNoTracking()
                .SingleOrDefaultAsync(candidate => candidate.Id == request.ReceivedEvidenceItemId, cancellationToken);
            if (evidence is null || evidence.TenantId != tenantContext.TenantId)
            {
                throw new SubcontractorValidationException("Received evidence was not found for the current tenant.");
            }
        }
    }

    private static void Apply(FlowDownClauseEntity entity, UpsertSubcontractorFlowDownRequest request)
    {
        entity.ContractClauseId = request.ContractClauseId;
        entity.ObligationId = request.ObligationId;
        entity.ClauseNumber = request.ClauseNumber;
        entity.Title = request.Title;
        entity.Status = request.Status;
        entity.SentAt = request.SentAt;
        entity.AcknowledgedAt = request.AcknowledgedAt;
        entity.SignedAt = request.SignedAt;
        entity.WaivedAt = request.WaivedAt;
        entity.SignedEvidenceItemId = request.SignedEvidenceItemId;
    }

    private void Apply(SubcontractorEvidenceRequestEntity entity, UpsertSubcontractorEvidenceRequestRequest request, Guid actorUserId)
    {
        entity.RequestedItem = request.RequestedItem;
        entity.RequestedEvidenceTypesJson = JsonSerializer.Serialize(request.RequestedEvidenceTypes, JsonOptions);
        entity.DueDate = request.DueDate;
        entity.Status = request.Status;
        entity.RecipientName = request.RecipientName;
        entity.RecipientEmail = request.RecipientEmail;
        entity.ObligationId = request.ObligationId;
        entity.RelatedFlowDownClauseId = request.RelatedFlowDownClauseId;
        entity.ReceivedEvidenceItemId = request.ReceivedEvidenceItemId;
        entity.CompletedAt = request.Status == SubcontractorEvidenceRequestStatus.Satisfied
            ? entity.CompletedAt ?? DateTimeOffset.UtcNow
            : null;

        if (request.ReceivedEvidenceItemId is not null)
        {
            var linkExists = dbContext.Set<SubcontractorEvidenceEntity>()
                .Any(link => link.SubcontractorId == entity.SubcontractorId && link.EvidenceItemId == request.ReceivedEvidenceItemId);
            if (!linkExists)
            {
                dbContext.Set<SubcontractorEvidenceEntity>().Add(new SubcontractorEvidenceEntity
                {
                    SubcontractorId = entity.SubcontractorId,
                    EvidenceItemId = request.ReceivedEvidenceItemId.Value
                });
            }
        }
    }

    private static void SyncContractLinks(SubcontractorEntity entity, IReadOnlyList<Guid> contractIds)
    {
        entity.Contracts.Clear();
        foreach (var contractId in contractIds)
        {
            entity.Contracts.Add(new ContractSubcontractorEntity
            {
                ContractId = contractId,
                SubcontractorId = entity.Id
            });
        }
    }

    private static SubcontractorDto ToDto(SubcontractorEntity entity) =>
        new(
            entity.Id,
            entity.TenantId,
            entity.Name,
            entity.Uei,
            entity.CageCode,
            entity.SamRegistrationStatus,
            entity.SamRegistrationExpiresAt,
            entity.SamSource,
            entity.SamRetrievedAt,
            ReadSamNaics(entity.SamNaicsJson),
            entity.SamExclusionStatus,
            entity.Status,
            entity.RoleDescription,
            entity.SmallBusinessStatus,
            ReadStringArray(entity.NaicsCodesJson),
            ReadStringArray(entity.CertificationsJson),
            entity.CmmcStatus,
            entity.InsuranceExpiresAt,
            entity.NdaStatus,
            entity.WorkshareDescription,
            entity.WorksharePercentage,
            entity.HasFciAccess,
            entity.HasCuiAccess,
            entity.HasExportControlledAccess,
            entity.RequiredCmmcLevel,
            entity.ContactName,
            entity.ContactEmail,
            entity.ContactPhone,
            entity.ContactTitle,
            entity.OwnerFunction,
            CalculateCompletion(entity),
            CalculateCompletion(entity) == 100,
            entity.Contracts.Select(link => link.ContractId).OrderBy(id => id).ToArray(),
            entity.CreatedAt,
            entity.UpdatedAt);

    private static SubcontractorFlowDownDto ToDto(FlowDownClauseEntity entity) =>
        new(
            entity.Id,
            entity.SubcontractorId,
            entity.ContractId,
            entity.ContractClauseId,
            entity.ObligationId,
            entity.ClauseNumber,
            entity.Title,
            entity.Status,
            entity.SentAt,
            entity.AcknowledgedAt,
            entity.SignedAt,
            entity.WaivedAt,
            entity.SignedEvidenceItemId,
            entity.CreatedAt,
            entity.UpdatedAt);

    private static SubcontractorEvidenceRequestDto ToDto(SubcontractorEvidenceRequestEntity entity) =>
        ToDto(entity, DateOnly.FromDateTime(DateTime.UtcNow));

    private static SubcontractorEvidenceRequestDto ToDto(SubcontractorEvidenceRequestEntity entity, DateOnly today) =>
        new(
            entity.Id,
            entity.TenantId,
            entity.SubcontractorId,
            entity.RequestedItem,
            DeserializeEvidenceTypes(entity.RequestedEvidenceTypesJson),
            entity.DueDate,
            entity.Status,
            entity.RecipientName,
            entity.RecipientEmail,
            entity.ObligationId,
            entity.RelatedFlowDownClauseId,
            entity.ReceivedEvidenceItemId,
            entity.CompletedAt,
            IsEvidenceRequestOverdue(entity, today),
            entity.CreatedAt,
            entity.UpdatedAt);

    private static bool IsEvidenceRequestOverdue(SubcontractorEvidenceRequestEntity entity, DateOnly today) =>
        entity.DueDate < today &&
        entity.Status is not SubcontractorEvidenceRequestStatus.Satisfied and not SubcontractorEvidenceRequestStatus.Cancelled;

    private static IReadOnlyList<EvidenceType> DeserializeEvidenceTypes(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<EvidenceType[]>(json, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static IReadOnlyList<SubcontractorSamNaicsCodeDto> ReadSamNaics(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<SubcontractorSamNaicsCodeDto[]>(json, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static IReadOnlyList<string> ReadStringArray(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<string[]>(json, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static int CalculateCompletion(SubcontractorEntity entity)
    {
        var completed = 0;
        const int total = 10;
        completed += string.IsNullOrWhiteSpace(entity.Name) ? 0 : 1;
        completed += string.IsNullOrWhiteSpace(entity.Uei) ? 0 : 1;
        completed += string.IsNullOrWhiteSpace(entity.CageCode) ? 0 : 1;
        completed += string.IsNullOrWhiteSpace(entity.RoleDescription) ? 0 : 1;
        completed += string.IsNullOrWhiteSpace(entity.SmallBusinessStatus) ? 0 : 1;
        completed += ReadStringArray(entity.NaicsCodesJson).Count == 0 ? 0 : 1;
        completed += ReadStringArray(entity.CertificationsJson).Count == 0 ? 0 : 1;
        completed += string.IsNullOrWhiteSpace(entity.CmmcStatus) ? 0 : 1;
        completed += entity.InsuranceExpiresAt is null ? 0 : 1;
        completed += string.IsNullOrWhiteSpace(entity.OwnerFunction) ? 0 : 1;
        return (int)Math.Round(completed / (double)total * 100, MidpointRounding.AwayFromZero);
    }
}
