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

        return assessments.Select(ToDto).ToArray();
    }

    public async Task<CmmcAssessmentDto?> FindCurrentTenantAsync(Guid assessmentId, CancellationToken cancellationToken = default)
    {
        var assessment = await QueryCurrentTenant()
            .SingleOrDefaultAsync(candidate => candidate.Id == assessmentId, cancellationToken);
        return assessment is null ? null : ToDto(assessment);
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
        return ToDto(entity);
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
        return ToDto(entity);
    }

    public async Task<IReadOnlyList<CmmcControlStatusDto>?> ListControlStatusesAsync(
        Guid assessmentId,
        CancellationToken cancellationToken = default)
    {
        var exists = await dbContext.Assessments.AnyAsync(
            assessment => assessment.Id == assessmentId && assessment.TenantId == tenantContext.TenantId,
            cancellationToken);
        if (!exists)
        {
            return null;
        }

        var controls = await QueryCurrentTenantControlAssessments(assessmentId)
            .OrderBy(control => control.ControlId)
            .ToArrayAsync(cancellationToken);
        return controls.Select(ToDto).ToArray();
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
        control.AssessedByUserId = request.AssessedByUserId ?? actorUserId;
        control.AssessedAt = request.AssessedAt ?? DateOnly.FromDateTime(DateTime.UtcNow);
        assessment.UpdatedAt = DateTimeOffset.UtcNow;
        assessment.UpdatedByUserId = actorUserId;

        await dbContext.SaveChangesAsync(cancellationToken);
        var updated = await QueryCurrentTenantControlAssessments(assessmentId)
            .Where(candidate => candidate.ControlId == controlId)
            .SingleAsync(cancellationToken);
        return ToDto(updated);
    }

    private IQueryable<AssessmentEntity> QueryCurrentTenant() =>
        dbContext.Assessments
            .Include(assessment => assessment.Controls)
            .Where(assessment => assessment.TenantId == tenantContext.TenantId);

    private IQueryable<ControlAssessmentEntity> QueryCurrentTenantControlAssessments(Guid assessmentId) =>
        dbContext.ControlAssessments
            .Include(control => control.Control)
            .Include(control => control.Assessment)
            .Where(control =>
                control.AssessmentId == assessmentId &&
                control.Assessment != null &&
                control.Assessment.TenantId == tenantContext.TenantId);

    private static CmmcAssessmentDto ToDto(AssessmentEntity entity) =>
        new(
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
            CalculateSummary(entity.Controls),
            entity.CreatedAt,
            entity.UpdatedAt);

    private static CmmcControlStatusDto ToDto(ControlAssessmentEntity entity) =>
        new(
            entity.AssessmentId,
            entity.ControlId,
            entity.Control?.Title ?? entity.ControlId,
            entity.Control?.Family ?? string.Empty,
            entity.ImplementationStatus,
            entity.Result,
            ReadGuidArray(entity.EvidenceItemIdsJson),
            entity.AssessedByUserId,
            entity.AssessedAt,
            entity.Notes);

    private static ControlSummaryDto CalculateSummary(IEnumerable<ControlAssessmentEntity> controls)
    {
        var statuses = controls.Select(control => control.ImplementationStatus).ToArray();
        var total = statuses.Length;
        var implemented = statuses.Count(status => status == ControlImplementationStatus.Implemented);
        var notApplicable = statuses.Count(status => status == ControlImplementationStatus.NotApplicable);
        return new ControlSummaryDto(
            total,
            implemented,
            statuses.Count(status => status == ControlImplementationStatus.PartiallyImplemented),
            statuses.Count(status => status == ControlImplementationStatus.NotStarted),
            notApplicable,
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
