using System.Text.Json;
using Gccs.Application.Cmmc;
using Gccs.Application.Security;
using Gccs.Domain.Cmmc;
using Gccs.Domain.Compliance;
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

    public async Task<IReadOnlyList<CmmcControlLibraryDto>> ListControlLibraryAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Controls
            .AsNoTracking()
            .OrderBy(control => control.Id)
            .Select(control => new CmmcControlLibraryDto(
                control.Id,
                control.Title,
                control.Family,
                control.CmmcLevel,
                control.Requirement,
                control.AssessmentObjective,
                control.SourceName,
                control.SourceUrl,
                control.SourceLastReviewedAt,
                control.SourceConfidence))
            .ToArrayAsync(cancellationToken);

    public async Task<CmmcAssessmentDto?> FindCurrentTenantAsync(Guid assessmentId, CancellationToken cancellationToken = default)
    {
        var assessment = await QueryCurrentTenant()
            .AsNoTracking()
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
            .Include(candidate => candidate.History)
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
        control.ImplementationDetails = request.ImplementationDetails ?? string.Empty;
        control.IsInherited = request.IsInherited;
        control.InheritedFrom = request.InheritedFrom;
        control.EspResponsible = request.EspResponsible;
        control.EspName = request.EspName;
        control.ResponsibilityType = request.ResponsibilityType;
        control.OwnerFunction = request.OwnerFunction ?? "Security";
        control.ResponsibilityProvider = request.ResponsibilityProvider;
        control.ResponsibilityNotes = request.ResponsibilityNotes ?? string.Empty;
        control.AssessedByUserId = request.AssessedByUserId ?? actorUserId;
        control.AssessedAt = request.AssessedAt ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var history = new ControlAssessmentHistoryEntity
        {
            Id = Guid.NewGuid(),
            AssessmentId = assessmentId,
            ControlId = controlId,
            Status = control.ImplementationStatus,
            Result = control.Result,
            Notes = control.Notes,
            ChangedByUserId = actorUserId,
            ChangedAt = DateTimeOffset.UtcNow
        };
        dbContext.ControlAssessmentHistory.Add(history);
        control.History.Add(history);
        assessment.UpdatedAt = DateTimeOffset.UtcNow;
        assessment.UpdatedByUserId = actorUserId;

        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(baselineControl, control, assessmentId);
    }

    public async Task<IReadOnlyList<CmmcResponsibilityMatrixRowDto>?> GetResponsibilityMatrixAsync(
        Guid assessmentId,
        CancellationToken cancellationToken = default)
    {
        var assessment = await QueryCurrentTenant()
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == assessmentId, cancellationToken);
        if (assessment is null)
        {
            return null;
        }

        var controls = await QueryControlsForLevel(assessment.Level)
            .OrderBy(control => control.Family)
            .ThenBy(control => control.Id)
            .ToArrayAsync(cancellationToken);
        var statuses = assessment.Controls.ToDictionary(control => control.ControlId, StringComparer.OrdinalIgnoreCase);
        var controlIds = controls.Select(control => control.Id).ToArray();
        var evidenceRequests = await dbContext.EvidenceRequests
            .AsNoTracking()
            .Where(request =>
                request.TenantId == tenantContext.TenantId &&
                request.RelatedRecordType == "Control" &&
                controlIds.Contains(request.RelatedRecordId))
            .ToArrayAsync(cancellationToken);
        var evidenceStatusByControl = evidenceRequests
            .GroupBy(request => request.RelatedRecordId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
            group => group.Key,
            group => SummarizeEvidenceStatus(group.Select(request => request.Status).ToArray()),
            StringComparer.OrdinalIgnoreCase);

        return controls
            .Select(control =>
            {
                statuses.TryGetValue(control.Id, out var status);
                return new CmmcResponsibilityMatrixRowDto(
                    assessmentId,
                    control.Id,
                    control.Title,
                    control.Family,
                    status?.ResponsibilityType ?? ControlResponsibilityType.Organization,
                    string.IsNullOrWhiteSpace(status?.OwnerFunction) ? "Security" : status.OwnerFunction,
                    status?.ResponsibilityProvider,
                    evidenceStatusByControl.GetValueOrDefault(control.Id, "NoRequests"),
                    status?.ResponsibilityNotes ?? string.Empty);
            })
            .OrderBy(row => row.Family, StringComparer.OrdinalIgnoreCase)
            .ThenBy(row => row.ResponsibilityType)
            .ThenBy(row => row.ControlId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public async Task<string?> ExportResponsibilityMatrixCsvAsync(
        Guid assessmentId,
        CancellationToken cancellationToken = default)
    {
        var rows = await GetResponsibilityMatrixAsync(assessmentId, cancellationToken);
        if (rows is null)
        {
            return null;
        }

        var lines = new List<string>
        {
            "Control,Family,Title,Responsibility Type,Owner,Provider,Evidence Status,Notes"
        };
        lines.AddRange(rows.Select(row => string.Join(
            ",",
            Csv(row.ControlId),
            Csv(row.Family),
            Csv(row.Title),
            Csv(row.ResponsibilityType.ToString()),
            Csv(row.OwnerFunction),
            Csv(row.Provider ?? string.Empty),
            Csv(row.EvidenceStatus),
            Csv(row.Notes))));

        return string.Join(Environment.NewLine, lines);
    }

    public async Task<IReadOnlyList<CmmcReadinessGapDto>?> GetReadinessGapsAsync(
        Guid assessmentId,
        CancellationToken cancellationToken = default)
    {
        var assessment = await QueryCurrentTenant()
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == assessmentId, cancellationToken);
        if (assessment is null)
        {
            return null;
        }

        var controls = await QueryControlsForLevel(assessment.Level)
            .OrderBy(control => control.Family)
            .ThenBy(control => control.Id)
            .ToArrayAsync(cancellationToken);
        var statuses = assessment.Controls.ToDictionary(control => control.ControlId, StringComparer.OrdinalIgnoreCase);
        var controlIds = controls.Select(control => control.Id).ToArray();
        var evidenceRequests = await dbContext.EvidenceRequests
            .AsNoTracking()
            .Where(request =>
                request.TenantId == tenantContext.TenantId &&
                request.RelatedRecordType == "Control" &&
                controlIds.Contains(request.RelatedRecordId))
            .ToArrayAsync(cancellationToken);
        var evidenceStatusByControl = evidenceRequests
            .GroupBy(request => request.RelatedRecordId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => SummarizeEvidenceStatus(group.Select(request => request.Status).ToArray()),
                StringComparer.OrdinalIgnoreCase);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var poamItems = await dbContext.PoamItems
            .AsNoTracking()
            .Where(item => item.TenantId == tenantContext.TenantId && item.AssessmentId == assessmentId && controlIds.Contains(item.ControlId))
            .ToArrayAsync(cancellationToken);
        var poamByControl = poamItems
            .GroupBy(item => item.ControlId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(item => item.TargetCompletionAt < today && item.Status is not PoamStatus.Closed and not PoamStatus.AcceptedRisk)
                    .ThenByDescending(item => item.RiskLevel)
                    .ThenBy(item => item.TargetCompletionAt)
                    .First(),
                StringComparer.OrdinalIgnoreCase);

        return controls
            .Select(control =>
            {
                statuses.TryGetValue(control.Id, out var status);
                var evidenceStatus = evidenceStatusByControl.GetValueOrDefault(control.Id, "NoRequests");
                poamByControl.TryGetValue(control.Id, out var poam);
                var isOverdue = poam is not null &&
                    poam.TargetCompletionAt < today &&
                    poam.Status is not PoamStatus.Closed and not PoamStatus.AcceptedRisk;
                var hasObjectiveCoverage = !string.IsNullOrWhiteSpace(status?.ImplementationDetails) ||
                    ReadGuidArray(status?.EvidenceItemIdsJson ?? "[]").Count > 0 ||
                    string.Equals(evidenceStatus, "Accepted", StringComparison.OrdinalIgnoreCase);
                var isCuiRelevant = control.CmmcLevel is CmmcLevel.Level2 or CmmcLevel.Level3;
                var reasons = BuildGapReasonCodes(status, evidenceStatus, poam, isOverdue, isCuiRelevant, hasObjectiveCoverage);
                var priority = CalculateGapPriority(status, evidenceStatus, poam, isOverdue, isCuiRelevant, hasObjectiveCoverage);

                return new CmmcReadinessGapDto(
                    assessmentId,
                    control.Id,
                    control.Title,
                    control.Family,
                    priority,
                    reasons,
                    status?.ImplementationStatus ?? ControlImplementationStatus.NotStarted,
                    status?.Result ?? AssessmentResult.NotAssessed,
                    evidenceStatus,
                    poam?.RiskLevel,
                    poam?.TargetCompletionAt,
                    isOverdue,
                    isCuiRelevant,
                    status?.IsInherited ?? false,
                    hasObjectiveCoverage);
            })
            .OrderBy(row => GapPriorityRank(row.Priority))
            .ThenBy(row => row.Family, StringComparer.OrdinalIgnoreCase)
            .ThenBy(row => row.ControlId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private IQueryable<AssessmentEntity> QueryCurrentTenant() =>
        dbContext.Assessments
            .Include(assessment => assessment.Controls)
                .ThenInclude(control => control.History)
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
            status?.Notes ?? string.Empty,
            status?.ImplementationDetails ?? string.Empty,
            status?.IsInherited ?? false,
            status?.InheritedFrom,
            status?.EspResponsible ?? false,
            status?.EspName,
            status?.ResponsibilityType ?? ControlResponsibilityType.Organization,
            string.IsNullOrWhiteSpace(status?.OwnerFunction) ? "Security" : status.OwnerFunction,
            status?.ResponsibilityProvider,
            status?.ResponsibilityNotes ?? string.Empty,
            status?.History
                .OrderByDescending(history => history.ChangedAt)
                .Select(history => new CmmcControlStatusHistoryDto(
                    history.Id,
                    history.Status,
                    history.Result,
                    history.ChangedByUserId,
                    history.ChangedAt,
                    history.Notes))
                .ToArray() ?? []);

    private static string SummarizeEvidenceStatus(IReadOnlyCollection<string> statuses)
    {
        if (statuses.Contains("Accepted", StringComparer.OrdinalIgnoreCase))
        {
            return "Accepted";
        }

        if (statuses.Contains("Submitted", StringComparer.OrdinalIgnoreCase))
        {
            return "Submitted";
        }

        if (statuses.Contains("Returned", StringComparer.OrdinalIgnoreCase))
        {
            return "Returned";
        }

        if (statuses.Contains("Open", StringComparer.OrdinalIgnoreCase))
        {
            return "Open";
        }

        return statuses.Count == 0 ? "NoRequests" : string.Join("/", statuses.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(status => status));
    }

    private static string Csv(string value)
    {
        var escaped = value.Replace("\"", "\"\"", StringComparison.Ordinal);
        return escaped.Contains(',') || escaped.Contains('"') || escaped.Contains('\n') || escaped.Contains('\r')
            ? $"\"{escaped}\""
            : escaped;
    }

    private static IReadOnlyList<string> BuildGapReasonCodes(
        ControlAssessmentEntity? status,
        string evidenceStatus,
        PoamItemEntity? poam,
        bool isPoamOverdue,
        bool isCuiRelevant,
        bool hasAssessmentObjectiveCoverage)
    {
        var reasons = new List<string>();
        var controlStatus = status?.ImplementationStatus ?? ControlImplementationStatus.NotStarted;
        var result = status?.Result ?? AssessmentResult.NotAssessed;

        if (controlStatus is ControlImplementationStatus.NeedsReview || result is AssessmentResult.NotAssessed)
        {
            reasons.Add("needs-review");
        }

        if (controlStatus is ControlImplementationStatus.NotStarted or ControlImplementationStatus.PartiallyImplemented || result == AssessmentResult.NotMet)
        {
            reasons.Add("control-not-implemented");
        }

        if (!string.Equals(evidenceStatus, "Accepted", StringComparison.OrdinalIgnoreCase))
        {
            reasons.Add(evidenceStatus == "NoRequests" ? "evidence-missing" : "evidence-not-accepted");
        }

        if (poam?.RiskLevel is RiskLevel.High or RiskLevel.Critical)
        {
            reasons.Add("poam-high-risk");
        }

        if (isPoamOverdue)
        {
            reasons.Add("poam-overdue");
        }

        if (isCuiRelevant)
        {
            reasons.Add("cui-relevant");
        }

        if (status?.IsInherited == true)
        {
            reasons.Add("inherited-control");
        }

        if (!hasAssessmentObjectiveCoverage)
        {
            reasons.Add("objective-coverage-missing");
        }

        return reasons.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static CmmcGapPriority CalculateGapPriority(
        ControlAssessmentEntity? status,
        string evidenceStatus,
        PoamItemEntity? poam,
        bool isPoamOverdue,
        bool isCuiRelevant,
        bool hasAssessmentObjectiveCoverage)
    {
        var controlStatus = status?.ImplementationStatus ?? ControlImplementationStatus.NotStarted;
        var result = status?.Result ?? AssessmentResult.NotAssessed;
        if (controlStatus == ControlImplementationStatus.NeedsReview || result == AssessmentResult.NotAssessed)
        {
            return CmmcGapPriority.NeedsReview;
        }

        if ((result == AssessmentResult.NotMet || controlStatus == ControlImplementationStatus.NotStarted) &&
            isCuiRelevant &&
            (!string.Equals(evidenceStatus, "Accepted", StringComparison.OrdinalIgnoreCase) || isPoamOverdue || poam?.RiskLevel == RiskLevel.Critical))
        {
            return CmmcGapPriority.Critical;
        }

        if (controlStatus == ControlImplementationStatus.PartiallyImplemented ||
            result == AssessmentResult.NotMet ||
            isPoamOverdue ||
            poam?.RiskLevel is RiskLevel.High or RiskLevel.Critical)
        {
            return CmmcGapPriority.High;
        }

        if (!hasAssessmentObjectiveCoverage || !string.Equals(evidenceStatus, "Accepted", StringComparison.OrdinalIgnoreCase))
        {
            return CmmcGapPriority.Medium;
        }

        return CmmcGapPriority.Low;
    }

    private static int GapPriorityRank(CmmcGapPriority priority) =>
        priority switch
        {
            CmmcGapPriority.Critical => 0,
            CmmcGapPriority.High => 1,
            CmmcGapPriority.Medium => 2,
            CmmcGapPriority.NeedsReview => 3,
            CmmcGapPriority.Low => 4,
            _ => 5
        };

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
