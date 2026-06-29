using System.Text;
using System.Text.Json;
using Gccs.Application.Common;
using Gccs.Application.Reports;
using Gccs.Application.Security;
using Gccs.Application.Tenancy;
using Gccs.Domain.Cmmc;
using Gccs.Domain.Common;
using Gccs.Domain.Compliance;
using Gccs.Domain.Evidence;
using Gccs.Domain.Reports;
using Gccs.Domain.Vendors;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Reports;

public sealed class EfReportRepository(
    GccsDbContext dbContext,
    ICurrentTenantContext tenantContext,
    TenantDataHandlingModePolicyService dataHandlingModePolicy) : IReportRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private const string MvpReportDisclaimer =
        "GCCS MVP report for workflow tracking only. This is not legal advice, a certification decision, an assessor determination, a contracting-officer determination, or government endorsement.";

    public async Task<IReadOnlyList<ApprovedEvidencePackageDto>> ListApprovedEvidencePackagesAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var reports = await dbContext.Reports
            .AsNoTracking()
            .Where(report =>
                report.TenantId == tenantId &&
                report.Type == ReportType.PrimeEvidencePackage &&
                report.Status == ReportStatus.Complete)
            .OrderByDescending(report => report.GeneratedAt)
            .Select(report => new
            {
                report.Id,
                report.TenantId,
                report.Type,
                report.Title,
                report.Status,
                report.GeneratedAt,
                report.GeneratedByUserId
            })
            .ToListAsync(cancellationToken);

        var reportIds = reports.Select(report => report.Id).ToArray();
        var evidenceByReportId = await dbContext.Set<ReportEvidenceEntity>()
            .AsNoTracking()
            .Where(link => reportIds.Contains(link.ReportId))
            .Join(
                dbContext.EvidenceItems.AsNoTracking().Where(evidence =>
                    evidence.TenantId == tenantId &&
                    evidence.Status == EvidenceStatus.Approved),
                link => link.EvidenceItemId,
                evidence => evidence.Id,
                (link, evidence) => new
                {
                    link.ReportId,
                    Item = new ApprovedEvidencePackageItemDto(
                        evidence.Id,
                        evidence.Name,
                        evidence.Type,
                        evidence.Status,
                        evidence.ApprovedAt,
                        evidence.ApprovedByUserId)
                })
            .ToListAsync(cancellationToken);

        var evidenceLookup = evidenceByReportId
            .GroupBy(item => item.ReportId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<ApprovedEvidencePackageItemDto>)group.Select(item => item.Item).ToArray());

        return reports
            .Select(report => new ApprovedEvidencePackageDto(
                report.Id,
                report.TenantId,
                report.Type,
                report.Title,
                report.Status,
                report.GeneratedAt,
                report.GeneratedByUserId,
                evidenceLookup.GetValueOrDefault(report.Id, [])))
            .ToArray();
    }

    public async Task<EvidencePackageReportDto> GenerateEvidencePackageAsync(
        EvidencePackageGenerateRequest request,
        Guid actorUserId,
        bool includeDraftOrRejectedEvidence,
        CancellationToken cancellationToken = default)
    {
        var generatedAt = DateTimeOffset.UtcNow;
        var obligationIds = NormalizeStrings(request.ObligationIds);
        var contractIds = NormalizeGuids(request.ContractIds);
        var controlIds = NormalizeStrings(request.ControlIds);
        var subcontractorIds = NormalizeGuids(request.SubcontractorIds);
        var allowedStatuses = includeDraftOrRejectedEvidence
            ? new[] { EvidenceStatus.Approved, EvidenceStatus.Draft, EvidenceStatus.Rejected }
            : [EvidenceStatus.Approved];

        var evidenceItems = await dbContext.EvidenceItems
            .AsNoTracking()
            .Include(evidence => evidence.Obligations)
            .Include(evidence => evidence.Contracts)
            .Include(evidence => evidence.Controls)
            .Where(evidence =>
                evidence.TenantId == tenantContext.TenantId &&
                allowedStatuses.Contains(evidence.Status) &&
                (
                    (obligationIds.Count > 0 && evidence.Obligations.Any(link => obligationIds.Contains(link.ObligationId))) ||
                    (contractIds.Count > 0 && evidence.Contracts.Any(link => contractIds.Contains(link.ContractId))) ||
                    (controlIds.Count > 0 && evidence.Controls.Any(link => controlIds.Contains(link.ControlId))) ||
                    (subcontractorIds.Count > 0 && dbContext.Set<SubcontractorEvidenceEntity>().Any(link =>
                        link.EvidenceItemId == evidence.Id &&
                        subcontractorIds.Contains(link.SubcontractorId)))))
            .OrderBy(evidence => evidence.Name)
            .ThenBy(evidence => evidence.Id)
            .ToArrayAsync(cancellationToken);

        var evidenceIds = evidenceItems.Select(evidence => evidence.Id).ToArray();
        foreach (var evidence in evidenceItems)
        {
            await EnsureEvidenceAllowedForReportAsync(evidence, actorUserId, cancellationToken);
            ContentClassificationPolicy.EnsureProcessable(evidence.Classification, "Evidence package generation");
        }

        var subcontractorLinks = await dbContext.Set<SubcontractorEvidenceEntity>()
            .AsNoTracking()
            .Where(link => evidenceIds.Contains(link.EvidenceItemId))
            .ToListAsync(cancellationToken);
        var subcontractorsByEvidenceId = subcontractorLinks
            .GroupBy(link => link.EvidenceItemId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<Guid>)group.Select(link => link.SubcontractorId).Distinct().Order().ToArray());
        var title = string.IsNullOrWhiteSpace(request.Title)
            ? "Evidence package"
            : request.Title.Trim();
        var scope = new EvidencePackageScopeDto(
            obligationIds,
            contractIds,
            controlIds,
            subcontractorIds,
            includeDraftOrRejectedEvidence);
        var manifestItems = evidenceItems
            .Select(evidence => new EvidencePackageManifestItemDto(
                evidence.Id,
                evidence.Name,
                evidence.Type,
                evidence.Status,
                evidence.ApprovedAt,
                evidence.ApprovedByUserId,
                evidence.Obligations.Select(link => link.ObligationId).Distinct().Order(StringComparer.Ordinal).ToArray(),
                evidence.Contracts.Select(link => link.ContractId).Distinct().Order().ToArray(),
                evidence.Controls.Select(link => link.ControlId).Distinct().Order(StringComparer.Ordinal).ToArray(),
                subcontractorsByEvidenceId.GetValueOrDefault(evidence.Id, []),
                generatedAt))
            .ToArray();
        var manifest = new EvidencePackageManifestDto(title, generatedAt, scope, manifestItems);
        var entity = new ReportEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantContext.TenantId,
            Type = ReportType.PrimeEvidencePackage,
            Title = title,
            Status = ReportStatus.Complete,
            GeneratedAt = generatedAt,
            GeneratedByUserId = actorUserId,
            SnapshotJson = JsonSerializer.Serialize(manifest, JsonOptions),
            ExportHtml = BuildHtml(manifest),
            CreatedAt = generatedAt,
            CreatedByUserId = actorUserId
        };
        entity.Contracts = contractIds
            .Select(contractId => new ReportContractEntity
            {
                ReportId = entity.Id,
                ContractId = contractId
            })
            .ToArray();
        entity.Obligations = obligationIds
            .Select(obligationId => new ReportObligationEntity
            {
                ReportId = entity.Id,
                ObligationId = obligationId
            })
            .ToArray();
        entity.EvidenceItems = evidenceIds
            .Select(evidenceItemId => new ReportEvidenceEntity
            {
                ReportId = entity.Id,
                EvidenceItemId = evidenceItemId
            })
            .ToArray();

        dbContext.Reports.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(entity, manifest);
    }

    public async Task<EvidencePackageReportDto?> GetEvidencePackageAsync(
        Guid reportId,
        CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Reports
            .AsNoTracking()
            .SingleOrDefaultAsync(
                report =>
                    report.Id == reportId &&
                    report.TenantId == tenantContext.TenantId &&
                    report.Type == ReportType.PrimeEvidencePackage,
                cancellationToken);
        if (entity is null)
        {
            return null;
        }

        var manifest = JsonSerializer.Deserialize<EvidencePackageManifestDto>(entity.SnapshotJson, JsonOptions) ??
            new EvidencePackageManifestDto(
                entity.Title,
                entity.GeneratedAt,
                new EvidencePackageScopeDto([], [], [], [], false),
                []);
        return ToDto(entity, manifest);
    }

    public async Task<SubcontractorComplianceReportDto> GenerateSubcontractorComplianceReportAsync(
        Guid? contractId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var generatedAt = DateTimeOffset.UtcNow;
        var today = DateOnly.FromDateTime(generatedAt.UtcDateTime);
        Guid[]? scopedSubcontractorIds = null;
        if (contractId.HasValue)
        {
            scopedSubcontractorIds = await dbContext.Set<ContractSubcontractorEntity>()
                .AsNoTracking()
                .Join(
                    dbContext.Contracts.AsNoTracking().Where(contract => contract.TenantId == tenantContext.TenantId),
                    link => link.ContractId,
                    contract => contract.Id,
                    (link, contract) => new { link.ContractId, link.SubcontractorId })
                .Where(link => link.ContractId == contractId.Value)
                .Select(link => link.SubcontractorId)
                .Distinct()
                .ToArrayAsync(cancellationToken);
        }

        var subcontractors = await dbContext.Subcontractors
            .AsNoTracking()
            .Where(subcontractor =>
                subcontractor.TenantId == tenantContext.TenantId &&
                (scopedSubcontractorIds == null || scopedSubcontractorIds.Contains(subcontractor.Id)))
            .OrderBy(subcontractor => subcontractor.Name)
            .ToArrayAsync(cancellationToken);
        var subcontractorIds = subcontractors.Select(subcontractor => subcontractor.Id).ToArray();
        var flowDowns = await dbContext.FlowDownClauses
            .AsNoTracking()
            .Where(flowDown =>
                subcontractorIds.Contains(flowDown.SubcontractorId) &&
                (!contractId.HasValue || flowDown.ContractId == contractId.Value))
            .OrderBy(flowDown => flowDown.ClauseNumber)
            .ToArrayAsync(cancellationToken);
        var flowDownIds = flowDowns.Select(flowDown => flowDown.Id).ToArray();
        var evidenceRequests = await dbContext.SubcontractorEvidenceRequests
            .AsNoTracking()
            .Where(request =>
                request.TenantId == tenantContext.TenantId &&
                subcontractorIds.Contains(request.SubcontractorId) &&
                (!contractId.HasValue ||
                    (request.RelatedFlowDownClauseId.HasValue && flowDownIds.Contains(request.RelatedFlowDownClauseId.Value))))
            .OrderBy(request => request.DueDate)
            .ToArrayAsync(cancellationToken);
        var flowDownsBySubcontractor = flowDowns
            .GroupBy(flowDown => flowDown.SubcontractorId)
            .ToDictionary(group => group.Key, group => group.ToArray());
        var requestsBySubcontractor = evidenceRequests
            .GroupBy(request => request.SubcontractorId)
            .ToDictionary(group => group.Key, group => group.ToArray());
        var rows = subcontractors
            .Select(subcontractor =>
            {
                var subcontractorFlowDowns = flowDownsBySubcontractor.GetValueOrDefault(subcontractor.Id, []);
                var subcontractorRequests = requestsBySubcontractor.GetValueOrDefault(subcontractor.Id, []);
                var requestDtos = subcontractorRequests
                    .Select(request =>
                    {
                        var isTerminal = request.Status is SubcontractorEvidenceRequestStatus.Satisfied or SubcontractorEvidenceRequestStatus.Cancelled;
                        var isMissing = request.ReceivedEvidenceItemId is null && !isTerminal;
                        var isOverdue = !isTerminal && (request.Status == SubcontractorEvidenceRequestStatus.Overdue || request.DueDate < today);
                        return new SubcontractorReportEvidenceRequestDto(
                            request.Id,
                            request.RequestedItem,
                            request.DueDate,
                            request.Status,
                            request.RelatedFlowDownClauseId,
                            isMissing,
                            isOverdue);
                    })
                    .ToArray();
                return new SubcontractorComplianceRowDto(
                    subcontractor.Id,
                    subcontractor.Name,
                    subcontractor.Status,
                    subcontractor.CmmcStatus,
                    subcontractor.InsuranceExpiresAt,
                    subcontractor.NdaStatus,
                    subcontractorFlowDowns
                        .Select(flowDown => new SubcontractorReportFlowDownDto(
                            flowDown.Id,
                            flowDown.ContractId,
                            flowDown.ClauseNumber,
                            flowDown.Title,
                            flowDown.Status))
                        .ToArray(),
                    requestDtos,
                    requestDtos.Any(request => request.IsMissing),
                    requestDtos.Any(request => request.IsOverdue));
            })
            .ToArray();
        var openFlowDowns = rows.Sum(row => row.FlowDowns.Count(flowDown =>
            flowDown.Status is not FlowDownStatus.Signed and not FlowDownStatus.Waived and not FlowDownStatus.NotApplicable));
        var snapshot = new SubcontractorComplianceSnapshotDto(
            contractId,
            generatedAt,
            rows.Length,
            rows.Sum(row => row.EvidenceRequests.Count(request => request.IsMissing)),
            rows.Sum(row => row.EvidenceRequests.Count(request => request.IsOverdue)),
            openFlowDowns,
            rows);
        var exportCsv = BuildCsv(snapshot);
        var entity = new ReportEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantContext.TenantId,
            Type = ReportType.SubcontractorCompliance,
            Title = contractId.HasValue ? "Subcontractor compliance report - contract" : "Subcontractor compliance report",
            Status = ReportStatus.Complete,
            GeneratedAt = generatedAt,
            GeneratedByUserId = actorUserId,
            SnapshotJson = JsonSerializer.Serialize(snapshot, JsonOptions),
            ExportHtml = exportCsv,
            CreatedAt = generatedAt,
            CreatedByUserId = actorUserId
        };
        if (contractId.HasValue)
        {
            entity.Contracts = [new ReportContractEntity { ReportId = entity.Id, ContractId = contractId.Value }];
        }

        dbContext.Reports.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(entity, snapshot);
    }

    public async Task<ComplianceStatusReportDto> GenerateComplianceStatusReportAsync(
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var generatedAt = DateTimeOffset.UtcNow;
        var today = DateOnly.FromDateTime(generatedAt.UtcDateTime);

        var tenantObligationIds = await dbContext.Set<ContractClauseObligationEntity>()
            .AsNoTracking()
            .Join(
                dbContext.Set<ContractClauseEntity>().AsNoTracking(),
                link => link.ContractClauseId,
                clause => clause.Id,
                (link, clause) => new { link.ObligationId, clause.ContractId })
            .Join(
                dbContext.Contracts.AsNoTracking().Where(contract => contract.TenantId == tenantContext.TenantId),
                item => item.ContractId,
                contract => contract.Id,
                (item, _) => item.ObligationId)
            .Distinct()
            .ToArrayAsync(cancellationToken);

        var obligations = await dbContext.Obligations
            .AsNoTracking()
            .Where(obligation => tenantObligationIds.Contains(obligation.Id))
            .ToArrayAsync(cancellationToken);

        var tasks = await dbContext.ComplianceTasks
            .AsNoTracking()
            .Where(task => task.TenantId == tenantContext.TenantId)
            .ToArrayAsync(cancellationToken);

        var evidenceStatusCounts = await dbContext.EvidenceItems
            .AsNoTracking()
            .Where(evidence => evidence.TenantId == tenantContext.TenantId)
            .GroupBy(evidence => evidence.Status)
            .Select(group => new { Status = group.Key.ToString(), Count = group.Count() })
            .ToDictionaryAsync(group => group.Status, group => group.Count, cancellationToken);

        var assessments = await dbContext.Assessments
            .AsNoTracking()
            .Include(assessment => assessment.Controls)
            .Where(assessment => assessment.TenantId == tenantContext.TenantId)
            .ToArrayAsync(cancellationToken);

        var openFlowDownGaps = await dbContext.FlowDownClauses
            .AsNoTracking()
            .Include(flowDown => flowDown.Subcontractor)
            .CountAsync(flowDown =>
                flowDown.Subcontractor != null &&
                flowDown.Subcontractor.TenantId == tenantContext.TenantId &&
                flowDown.Status != FlowDownStatus.Signed &&
                flowDown.Status != FlowDownStatus.Waived &&
                flowDown.Status != FlowDownStatus.NotApplicable,
                cancellationToken);

        var openEvidenceRequestGaps = await dbContext.SubcontractorEvidenceRequests
            .AsNoTracking()
            .CountAsync(request =>
                request.TenantId == tenantContext.TenantId &&
                request.DueDate < today &&
                request.Status != SubcontractorEvidenceRequestStatus.Satisfied &&
                request.Status != SubcontractorEvidenceRequestStatus.Cancelled,
                cancellationToken);

        var overdueTasks = tasks.Count(task =>
            task.DueAt.HasValue &&
            task.DueAt.Value < today &&
            task.Status is not ComplianceTaskStatus.Done and not ComplianceTaskStatus.Canceled);

        var obligationStatusCounts = tasks
            .Where(task => !string.IsNullOrWhiteSpace(task.ObligationId))
            .GroupBy(task => task.Status.ToString())
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);

        var highRiskObligations = obligations
            .Where(obligation => obligation.RiskLevel is RiskLevel.High or RiskLevel.Critical)
            .ToArray();
        var highRiskItems = highRiskObligations
            .Select(obligation => $"Obligation: {obligation.Title}")
            .Concat(tasks
                .Where(task => task.RiskLevel is RiskLevel.High or RiskLevel.Critical)
                .Select(task => $"Task: {task.Title}"))
            .Take(20)
            .ToArray();

        var controls = assessments.SelectMany(assessment => assessment.Controls).ToArray();
        var snapshot = new ComplianceStatusReportSnapshotDto(
            generatedAt,
            obligations.Length,
            highRiskObligations.Length,
            overdueTasks,
            obligationStatusCounts,
            evidenceStatusCounts,
            assessments.Length,
            controls.Count(control => control.ImplementationStatus == ControlImplementationStatus.Implemented),
            controls.Length,
            openFlowDownGaps + openEvidenceRequestGaps,
            highRiskItems);
        var exportHtml = BuildHtml(snapshot);

        var entity = new ReportEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantContext.TenantId,
            Type = ReportType.ComplianceStatus,
            Title = "Compliance status report",
            Status = ReportStatus.Complete,
            GeneratedAt = generatedAt,
            GeneratedByUserId = actorUserId,
            SnapshotJson = JsonSerializer.Serialize(snapshot, JsonOptions),
            ExportHtml = exportHtml,
            CreatedAt = generatedAt,
            CreatedByUserId = actorUserId
        };

        entity.Obligations = tenantObligationIds
            .Select(obligationId => new ReportObligationEntity
            {
                ReportId = entity.Id,
                ObligationId = obligationId
            })
            .ToArray();

        dbContext.Reports.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(entity, snapshot);
    }

    public async Task<CmmcReadinessReportDto?> GenerateCmmcReadinessReportAsync(
        Guid assessmentId,
        Guid actorUserId,
        bool includeEvidenceLinks,
        CancellationToken cancellationToken = default)
    {
        var assessment = await dbContext.Assessments
            .AsNoTracking()
            .Include(candidate => candidate.Controls)
            .SingleOrDefaultAsync(
                candidate => candidate.Id == assessmentId && candidate.TenantId == tenantContext.TenantId,
                cancellationToken);
        if (assessment is null)
        {
            return null;
        }

        var generatedAt = DateTimeOffset.UtcNow;
        var tenantName = await dbContext.Tenants
            .AsNoTracking()
            .Where(tenant => tenant.Id == tenantContext.TenantId)
            .Select(tenant => tenant.Name)
            .SingleOrDefaultAsync(cancellationToken) ?? "Current tenant";
        var scopedControls = await QueryControlsForLevel(assessment.Level)
            .OrderBy(control => control.Family)
            .ThenBy(control => control.Id)
            .ToArrayAsync(cancellationToken);
        var statusLookup = assessment.Controls.ToDictionary(control => control.ControlId, StringComparer.OrdinalIgnoreCase);
        var controlRows = scopedControls.Select(control =>
        {
            statusLookup.TryGetValue(control.Id, out var status);
            return new CmmcControlReportRow(
                control,
                status?.ImplementationStatus ?? ControlImplementationStatus.NotStarted,
                status?.Result ?? AssessmentResult.NotAssessed,
                ReadGuidArray(status?.EvidenceItemIdsJson ?? "[]"));
        }).ToArray();
        var evidenceStatusByControl = await BuildEvidenceStatusByControlAsync(controlRows, cancellationToken);
        var progress = controlRows
            .GroupBy(row => row.Control.Family)
            .Select(group => new CmmcFamilyProgressDto(
                group.Key,
                group.Count(),
                group.Count(row => row.Status == ControlImplementationStatus.Implemented),
                group.Count(row => row.Status == ControlImplementationStatus.PartiallyImplemented),
                group.Count(row => row.Status == ControlImplementationStatus.NotStarted),
                group.Count(row => row.Status == ControlImplementationStatus.NeedsReview),
                group.Count(row => row.Status == ControlImplementationStatus.NotApplicable)))
            .OrderBy(item => item.Family)
            .ToArray();
        var controlStatuses = controlRows
            .Select(row => new CmmcReportControlStatusDto(
                row.Control.Id,
                row.Control.Family,
                row.Control.Title,
                row.Status,
                row.Result,
                evidenceStatusByControl.GetValueOrDefault(row.Control.Id, "Missing"),
                row.Control.SourceName,
                row.Control.SourceUrl,
                row.Control.SourceLastReviewedAt))
            .ToArray();
        var gaps = controlRows
            .Where(row => row.Status is ControlImplementationStatus.NotStarted or ControlImplementationStatus.PartiallyImplemented or ControlImplementationStatus.NeedsReview)
            .Select(row => new CmmcControlGapDto(
                row.Control.Id,
                row.Control.Family,
                row.Control.Title,
                row.Status,
                row.Result))
            .ToArray();
        var prioritizedGaps = controlRows
            .Select(row =>
            {
                statusLookup.TryGetValue(row.Control.Id, out var status);
                var evidenceStatus = evidenceStatusByControl.GetValueOrDefault(row.Control.Id, "Missing");
                var priority = CalculateReportGapPriority(row, evidenceStatus);
                return new CmmcReportGapPriorityDto(
                    row.Control.Id,
                    row.Control.Family,
                    row.Control.Title,
                    priority,
                    BuildReportGapReasons(row, evidenceStatus, status),
                    evidenceStatus);
            })
            .Where(row => row.Priority != CmmcGapPriority.Low)
            .OrderBy(row => ReportGapPriorityRank(row.Priority))
            .ThenBy(row => row.Family)
            .ThenBy(row => row.ControlId)
            .ToArray();
        var poamItems = await dbContext.PoamItems
            .AsNoTracking()
            .Where(item =>
                item.TenantId == tenantContext.TenantId &&
                item.AssessmentId == assessment.Id &&
                item.Status != PoamStatus.Closed &&
                item.Status != PoamStatus.AcceptedRisk)
            .OrderBy(item => item.TargetCompletionAt)
            .Select(item => new CmmcReportPoamItemDto(
                item.Id,
                item.ControlId,
                item.Weakness,
                item.RiskLevel,
                item.Status,
                item.TargetCompletionAt))
            .ToArrayAsync(cancellationToken);
        var evidenceLinks = includeEvidenceLinks
            ? await BuildEvidenceLinksAsync(controlRows, cancellationToken)
            : [];
        var responsibilityMatrix = controlRows
            .Select(row =>
            {
                statusLookup.TryGetValue(row.Control.Id, out var status);
                return new CmmcReportResponsibilityRowDto(
                    row.Control.Id,
                    row.Control.Family,
                    status?.ResponsibilityType ?? ControlResponsibilityType.Organization,
                    string.IsNullOrWhiteSpace(status?.OwnerFunction) ? assessment.OwnerFunction : status.OwnerFunction,
                    status?.ResponsibilityProvider,
                    status?.ResponsibilityNotes ?? string.Empty);
            })
            .ToArray();
        var sourceReferences = controlRows
            .Select(row => new CmmcReportSourceReferenceDto(
                row.Control.Id,
                row.Control.SourceName,
                row.Control.SourceUrl,
                row.Control.SourceLastReviewedAt,
                row.Control.SourceConfidence))
            .ToArray();
        var affirmations = await dbContext.AnnualAffirmations
            .AsNoTracking()
            .Where(affirmation => affirmation.TenantId == tenantContext.TenantId)
            .OrderBy(affirmation => affirmation.DueAt)
            .Select(affirmation => new CmmcReportAffirmationDto(
                affirmation.Id,
                affirmation.Level,
                affirmation.DueAt,
                affirmation.SubmittedAt,
                affirmation.Status))
            .ToArrayAsync(cancellationToken);
        var historyCount = await dbContext.Reports
            .AsNoTracking()
            .CountAsync(
                report => report.TenantId == tenantContext.TenantId && report.Type == ReportType.CmmcReadiness,
                cancellationToken) + 1;
        var snapshot = new CmmcReadinessSnapshotDto(
            assessment.Id,
            assessment.Name,
            tenantName,
            assessment.Level,
            assessment.Framework,
            generatedAt,
            actorUserId,
            progress,
            controlStatuses,
            gaps,
            prioritizedGaps,
            poamItems,
            evidenceLinks,
            responsibilityMatrix,
            sourceReferences,
            affirmations,
            historyCount);
        var exportHtml = BuildHtml(snapshot);
        var entity = new ReportEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantContext.TenantId,
            Type = ReportType.CmmcReadiness,
            Title = $"CMMC readiness report - {assessment.Name}",
            Status = ReportStatus.Complete,
            GeneratedAt = generatedAt,
            GeneratedByUserId = actorUserId,
            SnapshotJson = JsonSerializer.Serialize(snapshot, JsonOptions),
            ExportHtml = exportHtml,
            CreatedAt = generatedAt,
            CreatedByUserId = actorUserId
        };

        dbContext.Reports.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new CmmcReadinessReportDto(
            entity.Id,
            entity.TenantId,
            entity.Type,
            entity.Status,
            entity.Title,
            entity.GeneratedAt,
            entity.GeneratedByUserId,
            snapshot,
            entity.ExportHtml);
    }

    private static ComplianceStatusReportDto ToDto(ReportEntity entity, ComplianceStatusReportSnapshotDto snapshot) =>
        new(
            entity.Id,
            entity.TenantId,
            entity.Type,
            entity.Status,
            entity.Title,
            entity.GeneratedAt,
            entity.GeneratedByUserId,
            snapshot,
            entity.ExportHtml);

    private static EvidencePackageReportDto ToDto(ReportEntity entity, EvidencePackageManifestDto manifest) =>
        new(
            entity.Id,
            entity.TenantId,
            entity.Type,
            entity.Status,
            entity.Title,
            entity.GeneratedAt,
            entity.GeneratedByUserId,
            manifest,
            entity.ExportHtml);

    private static SubcontractorComplianceReportDto ToDto(ReportEntity entity, SubcontractorComplianceSnapshotDto snapshot) =>
        new(
            entity.Id,
            entity.TenantId,
            entity.Type,
            entity.Status,
            entity.Title,
            entity.GeneratedAt,
            entity.GeneratedByUserId,
            snapshot,
            entity.ExportHtml);

    private Task EnsureEvidenceAllowedForReportAsync(
        EvidenceItemEntity evidence,
        Guid actorUserId,
        CancellationToken cancellationToken) =>
        dataHandlingModePolicy.EnsureAllowedAsync(
            new TenantDataHandlingModePolicyRequest(
                TenantDataHandlingWorkflow.Report,
                ContainsRealCui: evidence.Classification is ContentClassification.Cui,
                ContainsSyntheticCui: evidence.Classification is ContentClassification.SyntheticCui,
                ClassificationConfirmed: evidence.Classification is not ContentClassification.Unknown,
                ApprovalChecksPassed: evidence.Classification is not ContentClassification.SyntheticCui ||
                    (evidence.ClassificationIsApprovedDemoContent &&
                     evidence.ClassificationSource is ContentClassificationSource.ImportedDemoSeed),
                EntityType: "EvidenceItem",
                EntityId: evidence.Id.ToString()),
            actorUserId,
            cancellationToken);

    private static string BuildHtml(ComplianceStatusReportSnapshotDto snapshot)
    {
        var html = new StringBuilder();
        html.Append("<!doctype html><html><head><meta charset=\"utf-8\"><title>Compliance status report</title></head><body>");
        html.Append("<h1>Compliance status report</h1>");
        html.Append("<p>").Append(MvpReportDisclaimer).Append("</p>");
        html.Append("<p>Generated at ").Append(snapshot.GeneratedAt.ToString("O")).Append("</p>");
        html.Append("<ul>");
        html.Append("<li>Total obligations: ").Append(snapshot.TotalObligations).Append("</li>");
        html.Append("<li>High-risk obligations: ").Append(snapshot.HighRiskObligations).Append("</li>");
        html.Append("<li>Overdue tasks: ").Append(snapshot.OverdueTasks).Append("</li>");
        html.Append("<li>CMMC controls: ").Append(snapshot.CmmcControlsImplemented).Append(" of ").Append(snapshot.CmmcControlsTotal).Append("</li>");
        html.Append("<li>Subcontractor gaps: ").Append(snapshot.SubcontractorGaps).Append("</li>");
        html.Append("</ul>");
        html.Append("</body></html>");
        return html.ToString();
    }

    private static string BuildHtml(CmmcReadinessSnapshotDto snapshot)
    {
        var html = new StringBuilder();
        html.Append("<!doctype html><html><head><meta charset=\"utf-8\"><title>CMMC readiness report</title></head><body>");
        html.Append("<h1>CMMC readiness report</h1>");
        html.Append("<p>Draft readiness tracking only. This report is not an official assessment determination.</p>");
        html.Append("<p>Tenant: ").Append(snapshot.TenantName).Append("</p>");
        html.Append("<p>Target level: ").Append(snapshot.TargetLevel).Append("</p>");
        html.Append("<p>Control version: ").Append(snapshot.ControlVersion).Append("</p>");
        html.Append("<p>Reviewer: ").Append(snapshot.ReviewerUserId).Append("</p>");
        html.Append("<p>Generated at ").Append(snapshot.GeneratedAt.ToString("O")).Append("</p>");
        html.Append("<h2>Control status and evidence</h2><ul>");
        foreach (var control in snapshot.ControlStatuses)
        {
            html.Append("<li>")
                .Append(control.ControlId)
                .Append(" - ")
                .Append(control.Status)
                .Append(" - evidence ")
                .Append(control.EvidenceStatus)
                .Append("</li>");
        }

        html.Append("</ul>");
        html.Append("<h2>Prioritized gaps</h2><ul>");
        foreach (var gap in snapshot.PrioritizedGaps)
        {
            html.Append("<li>")
                .Append(gap.ControlId)
                .Append(" - ")
                .Append(gap.Priority)
                .Append(" - ")
                .Append(string.Join("|", gap.ReasonCodes))
                .Append("</li>");
        }

        html.Append("</ul>");
        html.Append("<h2>Responsibility matrix</h2><ul>");
        foreach (var row in snapshot.ResponsibilityMatrix)
        {
            html.Append("<li>")
                .Append(row.ControlId)
                .Append(" - ")
                .Append(row.ResponsibilityType)
                .Append(" - ")
                .Append(row.OwnerFunction)
                .Append(" - ")
                .Append(row.Provider ?? "Internal")
                .Append("</li>");
        }

        html.Append("</ul>");
        html.Append("<h2>Source references</h2><ul>");
        foreach (var source in snapshot.SourceReferences)
        {
            html.Append("<li>")
                .Append(source.ControlId)
                .Append(" - ")
                .Append(source.SourceName)
                .Append(" - ")
                .Append(source.SourceUrl)
                .Append(" - reviewed ")
                .Append(source.LastReviewedAt.ToString("O"))
                .Append("</li>");
        }

        html.Append("</ul>");
        html.Append("<p>Open gaps: ").Append(snapshot.OpenGaps.Count).Append("</p>");
        html.Append("<p>Open POA&M items: ").Append(snapshot.OpenPoamItems.Count).Append("</p>");
        html.Append("</body></html>");
        return html.ToString();
    }

    private static string BuildHtml(EvidencePackageManifestDto manifest)
    {
        var html = new StringBuilder();
        html.Append("<!doctype html><html><head><meta charset=\"utf-8\"><title>Evidence package</title></head><body>");
        html.Append("<h1>").Append(manifest.Title).Append("</h1>");
        html.Append("<p>").Append(MvpReportDisclaimer).Append("</p>");
        html.Append("<p>Generated at ").Append(manifest.GeneratedAt.ToString("O")).Append("</p>");
        html.Append("<p>Evidence items: ").Append(manifest.Items.Count).Append("</p>");
        html.Append("<ul>");
        foreach (var item in manifest.Items)
        {
            html.Append("<li>")
                .Append(item.Title)
                .Append(" - ")
                .Append(item.Type)
                .Append(" - ")
                .Append(item.Status)
                .Append("</li>");
        }

        html.Append("</ul>");
        html.Append("</body></html>");
        return html.ToString();
    }

    private static string BuildCsv(SubcontractorComplianceSnapshotDto snapshot)
    {
        var csv = new StringBuilder();
        csv.AppendLine("subcontractorId,name,status,cmmcStatus,insuranceExpiresAt,ndaStatus,flowDownStatuses,missingEvidenceRequests,overdueEvidenceRequests");
        foreach (var row in snapshot.Rows)
        {
            csv.AppendLine(string.Join(
                ',',
                EscapeCsv(row.SubcontractorId.ToString()),
                EscapeCsv(row.Name),
                EscapeCsv(row.Status.ToString()),
                EscapeCsv(row.CmmcStatus),
                EscapeCsv(row.InsuranceExpiresAt?.ToString("O") ?? string.Empty),
                EscapeCsv(row.NdaStatus),
                EscapeCsv(string.Join('|', row.FlowDowns.Select(flowDown => $"{flowDown.ClauseNumber}:{flowDown.Status}"))),
                row.EvidenceRequests.Count(request => request.IsMissing).ToString(),
                row.EvidenceRequests.Count(request => request.IsOverdue).ToString()));
        }

        return csv.ToString();
    }

    private static string EscapeCsv(string value) =>
        value.Contains(',') || value.Contains('"') || value.Contains('\n')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;

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

    private async Task<IReadOnlyList<CmmcReportEvidenceLinkDto>> BuildEvidenceLinksAsync(
        IEnumerable<CmmcControlReportRow> controlRows,
        CancellationToken cancellationToken)
    {
        var pairs = controlRows
            .SelectMany(row => row.EvidenceItemIds.Select(id => new { EvidenceItemId = id, row.Control.Id }))
            .ToArray();
        var evidenceIds = pairs.Select(pair => pair.EvidenceItemId).Distinct().ToArray();
        var evidence = await dbContext.EvidenceItems
            .AsNoTracking()
            .Where(item => item.TenantId == tenantContext.TenantId && evidenceIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, item => item.Name, cancellationToken);
        return pairs
            .Where(pair => evidence.ContainsKey(pair.EvidenceItemId))
            .Select(pair => new CmmcReportEvidenceLinkDto(pair.EvidenceItemId, evidence[pair.EvidenceItemId], pair.Id))
            .ToArray();
    }

    private async Task<IReadOnlyDictionary<string, string>> BuildEvidenceStatusByControlAsync(
        IEnumerable<CmmcControlReportRow> controlRows,
        CancellationToken cancellationToken)
    {
        var pairs = controlRows
            .SelectMany(row => row.EvidenceItemIds.Select(id => new { EvidenceItemId = id, row.Control.Id }))
            .ToArray();
        var evidenceIds = pairs.Select(pair => pair.EvidenceItemId).Distinct().ToArray();
        if (evidenceIds.Length == 0)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        var evidenceStatuses = await dbContext.EvidenceItems
            .AsNoTracking()
            .Where(item => item.TenantId == tenantContext.TenantId && evidenceIds.Contains(item.Id))
            .Select(item => new { item.Id, item.Status })
            .ToDictionaryAsync(item => item.Id, item => item.Status.ToString(), cancellationToken);
        return pairs
            .Where(pair => evidenceStatuses.ContainsKey(pair.EvidenceItemId))
            .GroupBy(pair => pair.Id, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => SummarizeReportEvidenceStatus(group.Select(pair => evidenceStatuses[pair.EvidenceItemId]).ToArray()),
                StringComparer.OrdinalIgnoreCase);
    }

    private static string SummarizeReportEvidenceStatus(IReadOnlyCollection<string> statuses)
    {
        if (statuses.Contains("Approved", StringComparer.OrdinalIgnoreCase))
        {
            return "Approved";
        }

        if (statuses.Contains("Submitted", StringComparer.OrdinalIgnoreCase))
        {
            return "Submitted";
        }

        if (statuses.Contains("Draft", StringComparer.OrdinalIgnoreCase))
        {
            return "Draft";
        }

        return statuses.Count == 0 ? "Missing" : string.Join("/", statuses.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(status => status));
    }

    private static CmmcGapPriority CalculateReportGapPriority(CmmcControlReportRow row, string evidenceStatus)
    {
        if (row.Status == ControlImplementationStatus.NeedsReview || row.Result == AssessmentResult.NotAssessed)
        {
            return CmmcGapPriority.NeedsReview;
        }

        if ((row.Status == ControlImplementationStatus.NotStarted || row.Result == AssessmentResult.NotMet) &&
            row.Control.CmmcLevel is CmmcLevel.Level2 or CmmcLevel.Level3 &&
            !string.Equals(evidenceStatus, "Approved", StringComparison.OrdinalIgnoreCase))
        {
            return CmmcGapPriority.Critical;
        }

        if (row.Status == ControlImplementationStatus.PartiallyImplemented || row.Result == AssessmentResult.NotMet)
        {
            return CmmcGapPriority.High;
        }

        if (!string.Equals(evidenceStatus, "Approved", StringComparison.OrdinalIgnoreCase))
        {
            return CmmcGapPriority.Medium;
        }

        return CmmcGapPriority.Low;
    }

    private static IReadOnlyList<string> BuildReportGapReasons(CmmcControlReportRow row, string evidenceStatus, ControlAssessmentEntity? status)
    {
        var reasons = new List<string>();
        if (row.Status is ControlImplementationStatus.NeedsReview || row.Result is AssessmentResult.NotAssessed)
        {
            reasons.Add("needs-review");
        }

        if (row.Status is ControlImplementationStatus.NotStarted or ControlImplementationStatus.PartiallyImplemented || row.Result == AssessmentResult.NotMet)
        {
            reasons.Add("control-status-gap");
        }

        if (!string.Equals(evidenceStatus, "Approved", StringComparison.OrdinalIgnoreCase))
        {
            reasons.Add("evidence-status-gap");
        }

        if (row.Control.CmmcLevel is CmmcLevel.Level2 or CmmcLevel.Level3)
        {
            reasons.Add("cui-relevant");
        }

        if (status?.IsInherited == true)
        {
            reasons.Add("inherited-control");
        }

        return reasons.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static int ReportGapPriorityRank(CmmcGapPriority priority) =>
        priority switch
        {
            CmmcGapPriority.Critical => 0,
            CmmcGapPriority.High => 1,
            CmmcGapPriority.Medium => 2,
            CmmcGapPriority.NeedsReview => 3,
            CmmcGapPriority.Low => 4,
            _ => 5
        };

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

    private static IReadOnlyList<string> NormalizeStrings(IEnumerable<string>? values) =>
        values?
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.Ordinal)
            .ToArray() ?? [];

    private static IReadOnlyList<Guid> NormalizeGuids(IEnumerable<Guid>? values) =>
        values?
            .Where(value => value != Guid.Empty)
            .Distinct()
            .Order()
            .ToArray() ?? [];

    private sealed record CmmcControlReportRow(
        ControlEntity Control,
        ControlImplementationStatus Status,
        AssessmentResult Result,
        IReadOnlyList<Guid> EvidenceItemIds);
}
