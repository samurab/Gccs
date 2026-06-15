using System.Text.Json;
using Gccs.Application.Cmmc;
using Gccs.Application.Security;
using Gccs.Domain.Cmmc;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Cmmc;

public sealed class EfCmmcAssessmentRepository(
    GccsDbContext dbContext,
    ICurrentTenantContext tenantContext) : ICmmcAssessmentRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<CmmcAssessmentDto>> ListCurrentTenantAsync(CancellationToken cancellationToken = default)
    {
        var assessments = await QueryCurrentTenant()
            .OrderByDescending(assessment => assessment.StartedAt)
            .ThenBy(assessment => assessment.Name)
            .ToArrayAsync(cancellationToken);

        var results = new List<CmmcAssessmentDto>();
        foreach (var assessment in assessments)
        {
            results.Add(await ToDtoAsync(assessment, cancellationToken));
        }

        return results;
    }

    public async Task<CmmcAssessmentDto?> FindCurrentTenantAsync(Guid assessmentId, CancellationToken cancellationToken = default)
    {
        var assessment = await QueryCurrentTenant()
            .SingleOrDefaultAsync(candidate => candidate.Id == assessmentId, cancellationToken);
        return assessment is null ? null : await ToDtoAsync(assessment, cancellationToken);
    }

    public async Task<CmmcAssessmentDto> CreateCurrentTenantAsync(
        UpsertCmmcAssessmentRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var entity = new AssessmentEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantContext.TenantId,
            Name = request.Name,
            Type = request.Type,
            Level = request.Level,
            Framework = request.Framework,
            Status = request.Status,
            StartedAt = request.StartedAt,
            CompletedAt = request.CompletedAt,
            AffirmationDueAt = request.AffirmationDueAt,
            OwnerFunction = request.OwnerFunction,
            CompanyProfileId = request.CompanyProfileId,
            ContractIdsJson = JsonSerializer.Serialize(request.ContractIds, JsonOptions),
            CreatedAt = now,
            CreatedByUserId = actorUserId
        };

        dbContext.Assessments.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await ToDtoAsync(entity, cancellationToken);
    }

    public async Task<CmmcAssessmentDto?> UpdateCurrentTenantAsync(
        Guid assessmentId,
        UpsertCmmcAssessmentRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var entity = await QueryCurrentTenant()
            .SingleOrDefaultAsync(candidate => candidate.Id == assessmentId, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        entity.Name = request.Name;
        entity.Type = request.Type;
        entity.Level = request.Level;
        entity.Framework = request.Framework;
        entity.Status = request.Status;
        entity.StartedAt = request.StartedAt;
        entity.CompletedAt = request.CompletedAt;
        entity.AffirmationDueAt = request.AffirmationDueAt;
        entity.OwnerFunction = request.OwnerFunction;
        entity.CompanyProfileId = request.CompanyProfileId;
        entity.ContractIdsJson = JsonSerializer.Serialize(request.ContractIds, JsonOptions);
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        entity.UpdatedByUserId = actorUserId;

        await dbContext.SaveChangesAsync(cancellationToken);
        return await ToDtoAsync(entity, cancellationToken);
    }

    public async Task<IReadOnlyList<CmmcControlStatusDto>?> ListControlStatusesAsync(
        Guid assessmentId,
        CancellationToken cancellationToken = default)
    {
        var assessment = await QueryCurrentTenant()
            .SingleOrDefaultAsync(candidate => candidate.Id == assessmentId, cancellationToken);
        if (assessment is null)
        {
            return null;
        }

        var controls = await QueryControlsForLevel(assessment.Level)
            .OrderBy(control => control.Id)
            .ToArrayAsync(cancellationToken);
        var statuses = assessment.Controls.ToDictionary(control => control.ControlId, StringComparer.OrdinalIgnoreCase);
        return controls.Select(control => ToDto(control, statuses.GetValueOrDefault(control.Id), assessmentId)).ToArray();
    }

    public async Task<CmmcControlStatusDto?> UpsertControlStatusAsync(
        Guid assessmentId,
        string controlId,
        UpsertCmmcControlStatusRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var assessment = await QueryCurrentTenant()
            .SingleOrDefaultAsync(candidate => candidate.Id == assessmentId, cancellationToken);
        if (assessment is null)
        {
            return null;
        }

        var baselineControl = await QueryControlsForLevel(assessment.Level)
            .SingleOrDefaultAsync(candidate => candidate.Id == controlId, cancellationToken);
        if (baselineControl is null)
        {
            return null;
        }

        var control = await dbContext.ControlAssessments
            .SingleOrDefaultAsync(
                candidate => candidate.AssessmentId == assessmentId && candidate.ControlId == controlId,
                cancellationToken);

        if (control is null)
        {
            control = new ControlAssessmentEntity
            {
                AssessmentId = assessmentId,
                ControlId = controlId
            };
            dbContext.ControlAssessments.Add(control);
        }

        control.ImplementationStatus = request.Status;
        control.Result = request.Result;
        control.Notes = request.Notes ?? string.Empty;
        control.EvidenceItemIdsJson = JsonSerializer.Serialize(request.EvidenceItemIds, JsonOptions);
        control.TaskIdsJson = JsonSerializer.Serialize(request.TaskIds, JsonOptions);
        control.AssetIdsJson = JsonSerializer.Serialize(request.AssetIds, JsonOptions);
        control.PoamItemIdsJson = JsonSerializer.Serialize(request.PoamItemIds, JsonOptions);
        control.AssessedByUserId = request.AssessedByUserId ?? actorUserId;
        control.AssessedAt = request.AssessedAt ?? DateOnly.FromDateTime(DateTime.UtcNow);
        assessment.UpdatedAt = DateTimeOffset.UtcNow;
        assessment.UpdatedByUserId = actorUserId;

        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(baselineControl, control, assessmentId);
    }

    private IQueryable<AssessmentEntity> QueryCurrentTenant() =>
        dbContext.Assessments
            .Include(assessment => assessment.Controls)
            .Where(assessment => assessment.TenantId == tenantContext.TenantId);

    private IQueryable<ControlEntity> QueryControlsForLevel(CmmcLevel level)
    {
        var controls = dbContext.Controls.AsNoTracking();
        return level switch
        {
            CmmcLevel.Level1 => controls.Where(control => control.CmmcLevel == CmmcLevel.Level1),
            CmmcLevel.Level2 => controls.Where(control => control.CmmcLevel == CmmcLevel.Level1 || control.CmmcLevel == CmmcLevel.Level2),
            _ => controls
        };
    }

    private async Task<CmmcAssessmentDto> ToDtoAsync(AssessmentEntity entity, CancellationToken cancellationToken)
    {
        var scopedControlIds = await QueryControlsForLevel(entity.Level)
            .Select(control => control.Id)
            .ToArrayAsync(cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var poamStatuses = await dbContext.PoamItems
            .AsNoTracking()
            .Where(item => item.TenantId == tenantContext.TenantId && item.AssessmentId == entity.Id)
            .Select(item => new { item.Status, item.TargetCompletionAt })
            .ToArrayAsync(cancellationToken);
        var openPoamItems = poamStatuses
            .Where(item => item.Status is not PoamStatus.Closed and not PoamStatus.AcceptedRisk)
            .ToArray();
        return new CmmcAssessmentDto(
            entity.Id,
            entity.TenantId,
            entity.Name,
            entity.Type,
            entity.Level,
            entity.Framework,
            entity.Status,
            entity.StartedAt,
            entity.CompletedAt,
            entity.AffirmationDueAt,
            entity.OwnerFunction,
            entity.CompanyProfileId,
            ReadGuidArray(entity.ContractIdsJson),
            CalculateSummary(scopedControlIds, entity.Controls),
            openPoamItems.Length,
            openPoamItems.Count(item => item.TargetCompletionAt < today),
            entity.CreatedAt,
            entity.UpdatedAt);
    }

    private static CmmcControlStatusDto ToDto(
        ControlEntity baselineControl,
        ControlAssessmentEntity? status,
        Guid assessmentId) =>
        new(
            assessmentId,
            baselineControl.Id,
            baselineControl.Title,
            baselineControl.Family,
            baselineControl.Requirement,
            baselineControl.AssessmentObjective,
            baselineControl.SourceName,
            baselineControl.SourceUrl,
            baselineControl.SourceLastReviewedAt,
            baselineControl.SourceConfidence,
            status?.ImplementationStatus ?? ControlImplementationStatus.NotStarted,
            status?.Result ?? AssessmentResult.NotAssessed,
            ReadGuidArray(status?.EvidenceItemIdsJson ?? "[]"),
            ReadGuidArray(status?.TaskIdsJson ?? "[]"),
            ReadGuidArray(status?.AssetIdsJson ?? "[]"),
            ReadGuidArray(status?.PoamItemIdsJson ?? "[]"),
            status?.AssessedByUserId,
            status?.AssessedAt,
            status?.Notes ?? string.Empty);

    private static ControlSummaryDto CalculateSummary(
        IReadOnlyCollection<string> scopedControlIds,
        IEnumerable<ControlAssessmentEntity> controls)
    {
        var statusByControlId = controls.ToDictionary(control => control.ControlId, StringComparer.OrdinalIgnoreCase);
        var statuses = scopedControlIds
            .Select(controlId => statusByControlId.GetValueOrDefault(controlId)?.ImplementationStatus ?? ControlImplementationStatus.NotStarted)
            .ToArray();
        var total = statuses.Length;
        var implemented = statuses.Count(status => status == ControlImplementationStatus.Implemented);
        var notApplicable = statuses.Count(status => status == ControlImplementationStatus.NotApplicable);
        return new ControlSummaryDto(
            total,
            implemented,
            statuses.Count(status => status == ControlImplementationStatus.PartiallyImplemented),
            statuses.Count(status => status == ControlImplementationStatus.NotStarted),
            notApplicable,
            statuses.Count(status => status == ControlImplementationStatus.NeedsReview),
            total == 0 ? 0 : (int)Math.Round((implemented + notApplicable) * 100m / total));
    }

    private static IReadOnlyList<Guid> ReadGuidArray(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<Guid[]>(json, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
