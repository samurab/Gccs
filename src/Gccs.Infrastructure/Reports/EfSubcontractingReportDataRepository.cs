using Gccs.Application.Reports;
using Gccs.Application.Security;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Gccs.Infrastructure.Reports;

public sealed class EfSubcontractingReportDataRepository(
    GccsDbContext dbContext,
    ICurrentTenantContext tenantContext) : ISubcontractingReportDataRepository
{
    public async Task<SubcontractingReportDataRowDto> CreateAsync(
        SubcontractingReportDataRowRequest request,
        SprSchemaReferenceDto? schema,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var entity = new SubcontractingReportDataRowEntity
        {
            Id = Guid.NewGuid(), TenantId = tenantContext.TenantId, ReviewStatus = SubcontractingReportDataReviewStatus.Draft,
            Version = 1, CreatedAt = now, CreatedByUserId = actorUserId
        };
        Apply(entity, request, schema);
        SyncEvidence(entity, request.SupportingEvidenceItemIds);
        dbContext.SubcontractingReportDataRows.Add(entity);
        await SaveAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<SubcontractingReportDataRowDto?> UpdateAsync(
        Guid rowId,
        SubcontractingReportDataRowRequest request,
        SprSchemaReferenceDto? schema,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var entity = await QueryCurrentTenant().SingleOrDefaultAsync(row => row.Id == rowId, cancellationToken);
        if (entity is null) return null;
        Apply(entity, request, schema);
        SyncEvidence(entity, request.SupportingEvidenceItemIds);
        entity.ReviewStatus = SubcontractingReportDataReviewStatus.PendingReview;
        entity.ReviewedByUserId = null; entity.ReviewedAt = null; entity.ReviewerNotes = null;
        entity.Version++; entity.UpdatedAt = DateTimeOffset.UtcNow; entity.UpdatedByUserId = actorUserId;
        await SaveAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<SubcontractingReportDataRowDto?> FindCurrentTenantAsync(Guid rowId, CancellationToken cancellationToken = default)
    {
        var entity = await QueryCurrentTenant().AsNoTracking().SingleOrDefaultAsync(row => row.Id == rowId, cancellationToken);
        return entity is null ? null : ToDto(entity);
    }

    public async Task<IReadOnlyList<SubcontractingReportDataRowDto>> ListCurrentTenantAsync(
        SubcontractingReportDataQuery query,
        CancellationToken cancellationToken = default)
    {
        var rows = QueryCurrentTenant().AsNoTracking();
        if (query.ContractId is not null) rows = rows.Where(row => row.ContractId == query.ContractId);
        if (query.ReportType is not null) rows = rows.Where(row => row.ReportType == query.ReportType);
        if (query.ReportPeriodStart is not null) rows = rows.Where(row => row.ReportPeriodStart == query.ReportPeriodStart);
        if (query.ReportPeriodEnd is not null) rows = rows.Where(row => row.ReportPeriodEnd == query.ReportPeriodEnd);
        return (await rows.OrderByDescending(row => row.UpdatedAt ?? row.CreatedAt).ThenBy(row => row.SubcontractorId)
            .ToArrayAsync(cancellationToken)).Select(ToDto).ToArray();
    }

    public async Task<SubcontractingReportDataRowDto?> UpdateReviewStatusAsync(
        Guid rowId,
        SubcontractingReportDataReviewStatus status,
        string? reviewerNotes,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var entity = await QueryCurrentTenant().SingleOrDefaultAsync(row => row.Id == rowId, cancellationToken);
        if (entity is null) return null;
        entity.ReviewStatus = status; entity.ReviewerNotes = reviewerNotes; entity.ReviewedByUserId = actorUserId;
        entity.ReviewedAt = DateTimeOffset.UtcNow; entity.Version++; entity.UpdatedAt = entity.ReviewedAt; entity.UpdatedByUserId = actorUserId;
        await SaveAsync(cancellationToken);
        return ToDto(entity);
    }

    public Task<bool> ExistsDuplicateCurrentTenantAsync(
        SubcontractingReportDataRowRequest request,
        Guid? existingRowId,
        CancellationToken cancellationToken = default)
    {
        var socioeconomicKey = Key(request.SocioeconomicCategory); var planKey = Key(request.PlanCategory);
        return dbContext.SubcontractingReportDataRows.AsNoTracking().AnyAsync(row =>
            row.TenantId == tenantContext.TenantId && row.Id != existingRowId && row.ContractId == request.ContractId &&
            row.SubcontractorId == request.SubcontractorId && row.ReportType == request.ReportType &&
            row.ReportPeriodStart == request.ReportPeriodStart && row.ReportPeriodEnd == request.ReportPeriodEnd &&
            row.RowPeriodStart == request.RowPeriodStart && row.RowPeriodEnd == request.RowPeriodEnd &&
            row.SocioeconomicCategoryKey == socioeconomicKey && row.PlanCategoryKey == planKey, cancellationToken);
    }

    public async Task<IReadOnlyDictionary<string, string[]>> ValidateReferencesCurrentTenantAsync(
        SubcontractingReportDataRowRequest request,
        CancellationToken cancellationToken = default)
    {
        var errors = new Dictionary<string, string[]>();
        var contractExists = await ContractExistsCurrentTenantAsync(request.ContractId, cancellationToken);
        if (!contractExists) errors["contractId"] = ["The contract was not found."];
        var subcontractorExists = await dbContext.Subcontractors.AsNoTracking().AnyAsync(
            item => item.TenantId == tenantContext.TenantId && item.Id == request.SubcontractorId, cancellationToken);
        if (!subcontractorExists) errors["subcontractorId"] = ["The subcontractor was not found."];
        if (contractExists && subcontractorExists && !await dbContext.Set<ContractSubcontractorEntity>().AsNoTracking().AnyAsync(
                link => link.ContractId == request.ContractId && link.SubcontractorId == request.SubcontractorId, cancellationToken))
            errors["subcontractorId"] = ["The subcontractor is not linked to this contract."];
        if (contractExists && !await dbContext.EsrsApplicabilities.AsNoTracking().AnyAsync(item =>
                item.TenantId == tenantContext.TenantId && item.ContractId == request.ContractId && item.ReportType == request.ReportType &&
                item.PeriodStart == request.ReportPeriodStart && item.PeriodEnd == request.ReportPeriodEnd, cancellationToken))
            errors["reportPeriod"] = ["The report period does not match an active subcontracting plan reporting obligation for this contract."];
        if (contractExists && request.PrimeContractPiid is not null)
        {
            var contractPiid = await dbContext.Contracts.AsNoTracking()
                .Where(item => item.TenantId == tenantContext.TenantId && item.Id == request.ContractId)
                .Select(item => item.ContractNumber).SingleAsync(cancellationToken);
            if (!string.Equals(contractPiid, request.PrimeContractPiid, StringComparison.OrdinalIgnoreCase))
                errors["primeContractPiid"] = ["Prime contract PIID must match the selected contract number."];
        }
        if (subcontractorExists && request.ReportingEntityUei is not null)
        {
            var expectedUei = request.ReportingRole == SprReportingRole.Subcontractor
                ? await dbContext.Subcontractors.AsNoTracking().Where(item => item.TenantId == tenantContext.TenantId && item.Id == request.SubcontractorId)
                    .Select(item => item.Uei).SingleAsync(cancellationToken)
                : await dbContext.CompanyProfiles.AsNoTracking().Where(item => item.TenantId == tenantContext.TenantId)
                    .Select(item => item.Uei).SingleOrDefaultAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(expectedUei) || !string.Equals(expectedUei, request.ReportingEntityUei, StringComparison.OrdinalIgnoreCase))
                errors["reportingEntityUei"] = ["Reporting entity UEI must match the tenant profile or selected subcontractor for the reporting role."];
        }
        if (request.SupportingEvidenceItemIds.Count > 0)
        {
            var validEvidenceIds = await dbContext.EvidenceItems.AsNoTracking()
                .Where(item => item.TenantId == tenantContext.TenantId && !item.IsUseBlocked && request.SupportingEvidenceItemIds.Contains(item.Id))
                .Select(item => item.Id).ToArrayAsync(cancellationToken);
            var invalid = request.SupportingEvidenceItemIds.Except(validEvidenceIds).ToArray();
            if (invalid.Length > 0) errors["supportingEvidenceItemIds"] = ["One or more evidence records were not found or are blocked from use."];
        }
        return errors;
    }

    public Task<bool> ContractExistsCurrentTenantAsync(Guid contractId, CancellationToken cancellationToken = default) =>
        dbContext.Contracts.AsNoTracking().AnyAsync(item => item.TenantId == tenantContext.TenantId && item.Id == contractId, cancellationToken);

    public async Task<SprRemediationValuesDto> GetRemediationSuggestionCurrentTenantAsync(
        SubcontractingReportDataRowDto row,
        CancellationToken cancellationToken = default)
    {
        var primeContractPiid = await dbContext.Contracts.AsNoTracking()
            .Where(item => item.TenantId == tenantContext.TenantId && item.Id == row.ContractId)
            .Select(item => item.ContractNumber).SingleOrDefaultAsync(cancellationToken);
        var reportingEntityUei = row.ReportingRole == SprReportingRole.Subcontractor
            ? await dbContext.Subcontractors.AsNoTracking()
                .Where(item => item.TenantId == tenantContext.TenantId && item.Id == row.SubcontractorId)
                .Select(item => item.Uei).SingleOrDefaultAsync(cancellationToken)
            : await dbContext.CompanyProfiles.AsNoTracking()
                .Where(item => item.TenantId == tenantContext.TenantId)
                .Select(item => item.Uei).SingleOrDefaultAsync(cancellationToken);
        return new(reportingEntityUei, primeContractPiid);
    }

    private IQueryable<SubcontractingReportDataRowEntity> QueryCurrentTenant() =>
        dbContext.SubcontractingReportDataRows.Include(row => row.EvidenceLinks)
            .Where(row => row.TenantId == tenantContext.TenantId);

    private static void Apply(SubcontractingReportDataRowEntity entity, SubcontractingReportDataRowRequest request, SprSchemaReferenceDto? schema)
    {
        entity.ContractId = request.ContractId; entity.SubcontractorId = request.SubcontractorId; entity.ReportType = request.ReportType;
        entity.ReportPeriodStart = request.ReportPeriodStart; entity.ReportPeriodEnd = request.ReportPeriodEnd;
        entity.RowPeriodStart = request.RowPeriodStart; entity.RowPeriodEnd = request.RowPeriodEnd;
        entity.SocioeconomicCategory = request.SocioeconomicCategory; entity.SocioeconomicCategoryKey = Key(request.SocioeconomicCategory);
        entity.PlanCategory = request.PlanCategory; entity.PlanCategoryKey = Key(request.PlanCategory);
        entity.Amount = request.Amount; entity.SourceReference = request.SourceReference!;
        entity.ReportingRole = request.ReportingRole; entity.ReportingFiscalYear = request.ReportingFiscalYear;
        entity.ReportingPeriod = request.ReportingPeriod; entity.ReportingEntityUei = request.ReportingEntityUei;
        entity.PrimeContractPiid = request.PrimeContractPiid; entity.SubcontractNumber = request.SubcontractNumber;
        entity.SprEligibilityConfirmed = request.SprEligibilityConfirmed; entity.SprEligibilityBasis = request.SprEligibilityBasis;
        entity.SprSchemaProfileId = schema?.Id; entity.SprSchemaVersion = schema?.Version;
        entity.SprSchemaSourceUrl = schema?.SourceUrl; entity.SprSchemaDefinitionSha256 = schema?.DefinitionSha256;
    }

    private static string Key(string value) => value.Trim().ToUpperInvariant();

    private void SyncEvidence(SubcontractingReportDataRowEntity entity, IReadOnlyList<Guid> evidenceIds)
    {
        var expected = evidenceIds.ToHashSet();
        foreach (var link in entity.EvidenceLinks.Where(link => !expected.Contains(link.EvidenceItemId)).ToArray())
            dbContext.SubcontractingReportDataEvidence.Remove(link);
        foreach (var id in expected.Where(id => entity.EvidenceLinks.All(link => link.EvidenceItemId != id)))
            entity.EvidenceLinks.Add(new SubcontractingReportDataEvidenceEntity { TenantId = tenantContext.TenantId, ReportDataRowId = entity.Id, EvidenceItemId = id });
    }

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        try { await dbContext.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException)
        {
            throw new SubcontractingReportDataValidationException(new Dictionary<string, string[]> { ["version"] = ["The report data row changed. Reload it and try again."] });
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new SubcontractingReportDataValidationException(new Dictionary<string, string[]> { ["duplicate"] = ["A duplicate report data row could not be saved."] });
        }
    }

    private static SubcontractingReportDataRowDto ToDto(SubcontractingReportDataRowEntity entity) => new(
        entity.Id, entity.TenantId, entity.ContractId, entity.SubcontractorId, entity.ReportType,
        entity.ReportPeriodStart, entity.ReportPeriodEnd, entity.RowPeriodStart, entity.RowPeriodEnd,
        entity.SocioeconomicCategory, entity.PlanCategory, entity.Amount,
        entity.EvidenceLinks.Select(link => link.EvidenceItemId).Order().ToArray(), entity.SourceReference,
        entity.ReviewStatus, entity.ReviewedByUserId, entity.ReviewedAt, entity.ReviewerNotes,
        entity.Version, entity.CreatedAt, entity.UpdatedAt, entity.ReportingRole, entity.ReportingFiscalYear,
        entity.ReportingPeriod, entity.ReportingEntityUei, entity.PrimeContractPiid, entity.SubcontractNumber,
        entity.SprEligibilityConfirmed, entity.SprEligibilityBasis, entity.SprSchemaProfileId, entity.SprSchemaVersion,
        entity.SprSchemaSourceUrl, entity.SprSchemaDefinitionSha256);
}
