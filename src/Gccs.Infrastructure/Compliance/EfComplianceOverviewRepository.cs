using System.Text.Json;
using Gccs.Application.Compliance;
using Gccs.Application.Security;
using Gccs.Domain.Audit;
using Gccs.Domain.Cmmc;
using Gccs.Domain.Evidence;
using Gccs.Domain.Identity;
using Gccs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Compliance;

public sealed class EfComplianceOverviewRepository(
    GccsDbContext dbContext,
    ICurrentTenantContext tenantContext) : IComplianceOverviewRepository
{
    private const int RecentAuditEventCount = 10;
    private const int MaxAlertsPerRule = 10;

    public async Task<ComplianceOverviewDto> GetCurrentTenantOverviewAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContext.TenantId;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var controlStatuses = await dbContext.ControlAssessments
            .AsNoTracking()
            .Where(control => control.Assessment != null && control.Assessment.TenantId == tenantId)
            .GroupBy(control => control.ImplementationStatus)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToArrayAsync(cancellationToken);

        var poamStatuses = await dbContext.PoamItems
            .AsNoTracking()
            .Where(item => item.TenantId == tenantId)
            .GroupBy(item => new
            {
                IsOpen = item.Status != PoamStatus.Closed && item.Status != PoamStatus.AcceptedRisk,
                IsOverdue = item.TargetCompletionAt < today && item.Status != PoamStatus.Closed && item.Status != PoamStatus.AcceptedRisk
            })
            .Select(group => new { group.Key.IsOpen, group.Key.IsOverdue, Count = group.Count() })
            .ToArrayAsync(cancellationToken);

        var evidenceItems = await dbContext.EvidenceItems
            .AsNoTracking()
            .CountAsync(item => item.TenantId == tenantId, cancellationToken);

        var recentAuditEvents = await dbContext.AuditLogEntries
            .AsNoTracking()
            .Where(entry => entry.TenantId == tenantId)
            .OrderByDescending(entry => entry.OccurredAt)
            .ThenByDescending(entry => entry.Id)
            .Take(RecentAuditEventCount)
            .Select(entry => new RecentAuditEventDto(
                entry.Id,
                entry.ActorUserId,
                entry.Action.ToString(),
                entry.EntityType,
                entry.EntityId,
                entry.OccurredAt,
                entry.CorrelationId,
                entry.Summary))
            .ToArrayAsync(cancellationToken);

        var alerts = await BuildAlertsAsync(tenantId, today, cancellationToken);
        var controlCountByStatus = controlStatuses.ToDictionary(item => item.Status, item => item.Count);

        return new ComplianceOverviewDto(
            tenantId,
            controlStatuses.Sum(item => item.Count),
            controlCountByStatus.GetValueOrDefault(ControlImplementationStatus.Implemented),
            controlCountByStatus.GetValueOrDefault(ControlImplementationStatus.PartiallyImplemented),
            controlCountByStatus.GetValueOrDefault(ControlImplementationStatus.NotStarted),
            poamStatuses.Where(item => item.IsOpen).Sum(item => item.Count),
            poamStatuses.Where(item => item.IsOverdue).Sum(item => item.Count),
            evidenceItems,
            recentAuditEvents)
        {
            Alerts = alerts
        };
    }

    private async Task<IReadOnlyList<ComplianceDashboardAlertDto>> BuildAlertsAsync(
        Guid tenantId,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        var alerts = new List<ComplianceDashboardAlertDto>();
        alerts.AddRange(await BuildOverduePoamAlertsAsync(tenantId, today, cancellationToken));
        alerts.AddRange(await BuildControlWithoutEvidenceAlertsAsync(tenantId, cancellationToken));
        alerts.AddRange(await BuildEvidenceReviewAlertsAsync(tenantId, cancellationToken));
        alerts.AddRange(await BuildFailedUploadAlertsAsync(tenantId, cancellationToken));
        alerts.AddRange(await BuildHighRiskRoleAlertsAsync(tenantId, cancellationToken));
        alerts.AddRange(await BuildCompletedControlMetadataAlertsAsync(tenantId, cancellationToken));

        return alerts
            .OrderByDescending(alert => SeverityRank(alert.Severity))
            .ThenByDescending(alert => alert.DetectedUtc)
            .ThenBy(alert => alert.AlertType, StringComparer.Ordinal)
            .ToArray();
    }

    private async Task<IReadOnlyList<ComplianceDashboardAlertDto>> BuildOverduePoamAlertsAsync(
        Guid tenantId,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        var rows = await dbContext.PoamItems
            .AsNoTracking()
            .Where(item =>
                item.TenantId == tenantId &&
                item.TargetCompletionAt < today &&
                item.Status != PoamStatus.Closed &&
                item.Status != PoamStatus.AcceptedRisk)
            .OrderBy(item => item.TargetCompletionAt)
            .ThenBy(item => item.Id)
            .Take(MaxAlertsPerRule)
            .Select(item => new
            {
                item.Id,
                item.ControlId,
                item.Weakness,
                item.RiskLevel,
                item.TargetCompletionAt,
                item.CreatedAt
            })
            .ToArrayAsync(cancellationToken);

        return rows
            .Select(item => Alert(
                "overdue_poam",
                item.RiskLevel is Gccs.Domain.Compliance.RiskLevel.Critical or Gccs.Domain.Compliance.RiskLevel.High ? "High" : "Medium",
                "Overdue POA&M",
                $"POA&M for {item.ControlId} is overdue: {item.Weakness}",
                "PoamItem",
                item.Id.ToString(),
                item.CreatedAt))
            .ToArray();
    }

    private async Task<IReadOnlyList<ComplianceDashboardAlertDto>> BuildControlWithoutEvidenceAlertsAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var rows = await dbContext.ControlAssessments
            .AsNoTracking()
            .Where(control =>
                control.Assessment != null &&
                control.Assessment.TenantId == tenantId &&
                control.ImplementationStatus != ControlImplementationStatus.NotApplicable &&
                (control.EvidenceItemIdsJson == "[]" || control.EvidenceItemIdsJson == string.Empty))
            .OrderBy(control => control.ControlId)
            .Take(MaxAlertsPerRule)
            .Select(control => new
            {
                control.AssessmentId,
                control.ControlId,
                control.ImplementationStatus
            })
            .ToArrayAsync(cancellationToken);

        return rows
            .Select(control => Alert(
                "control_without_evidence",
                control.ImplementationStatus == ControlImplementationStatus.Implemented ? "High" : "Medium",
                "Control missing evidence",
                $"Control {control.ControlId} has no linked evidence.",
                "ControlAssessment",
                $"{control.AssessmentId}:{control.ControlId}",
                DateTimeOffset.UtcNow))
            .ToArray();
    }

    private async Task<IReadOnlyList<ComplianceDashboardAlertDto>> BuildEvidenceReviewAlertsAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var rows = await dbContext.EvidenceItems
            .AsNoTracking()
            .Where(evidence =>
                evidence.TenantId == tenantId &&
                (evidence.Status == EvidenceStatus.Uploaded ||
                 evidence.Status == EvidenceStatus.Submitted ||
                 evidence.Status == EvidenceStatus.InReview ||
                 evidence.Status == EvidenceStatus.Rejected))
            .OrderByDescending(evidence => evidence.CreatedAt)
            .ThenByDescending(evidence => evidence.Id)
            .Take(MaxAlertsPerRule * 2)
            .Select(evidence => new
            {
                evidence.Id,
                evidence.Name,
                evidence.Status,
                evidence.CreatedAt
            })
            .ToArrayAsync(cancellationToken);

        return rows
            .Select(evidence =>
                evidence.Status == EvidenceStatus.Rejected
                    ? Alert(
                        "rejected_evidence",
                        "High",
                        "Evidence rejected",
                        $"Evidence '{evidence.Name}' was rejected and needs correction.",
                        "EvidenceItem",
                        evidence.Id.ToString(),
                        evidence.CreatedAt)
                    : Alert(
                        "evidence_pending_review",
                        "Medium",
                        "Evidence pending review",
                        $"Evidence '{evidence.Name}' is pending review.",
                        "EvidenceItem",
                        evidence.Id.ToString(),
                        evidence.CreatedAt))
            .ToArray();
    }

    private async Task<IReadOnlyList<ComplianceDashboardAlertDto>> BuildFailedUploadAlertsAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var rows = await dbContext.AuditLogEntries
            .AsNoTracking()
            .Where(audit =>
                audit.TenantId == tenantId &&
                audit.Action == AuditAction.Rejected &&
                (audit.EntityType.Contains("Evidence") ||
                 audit.EntityType.Contains("Upload") ||
                 audit.EntityType.Contains("ContractDocument")) &&
                (audit.Summary.Contains("upload") ||
                 audit.MetadataJson.Contains("upload")))
            .OrderByDescending(audit => audit.OccurredAt)
            .ThenByDescending(audit => audit.Id)
            .Take(MaxAlertsPerRule)
            .Select(audit => new
            {
                audit.Id,
                audit.EntityType,
                audit.EntityId,
                audit.OccurredAt,
                audit.Summary
            })
            .ToArrayAsync(cancellationToken);

        return rows
            .Select(audit => Alert(
                "failed_upload_attempt",
                "High",
                "Failed upload attempt",
                audit.Summary,
                audit.EntityType,
                string.IsNullOrWhiteSpace(audit.EntityId) ? audit.Id.ToString() : audit.EntityId,
                audit.OccurredAt))
            .ToArray();
    }

    private async Task<IReadOnlyList<ComplianceDashboardAlertDto>> BuildHighRiskRoleAlertsAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var highRiskRoles = new[] { RoleCatalog.Owner, RoleCatalog.Admin, RoleCatalog.Advisor };
        var rows = await dbContext.TenantMemberships
            .AsNoTracking()
            .Where(membership =>
                membership.TenantId == tenantId &&
                membership.Status == MembershipStatus.Active &&
                highRiskRoles.Contains(membership.RoleName))
            .OrderBy(membership => membership.RoleName)
            .ThenBy(membership => membership.UserId)
            .Take(MaxAlertsPerRule)
            .Select(membership => new
            {
                membership.Id,
                membership.UserId,
                membership.RoleName,
                membership.CreatedAt
            })
            .ToArrayAsync(cancellationToken);

        return rows
            .Select(membership => Alert(
                "high_risk_role",
                membership.RoleName == RoleCatalog.Owner ? "High" : "Medium",
                "High-risk role assigned",
                $"User {membership.UserId} has active {membership.RoleName} access.",
                "TenantMembership",
                membership.Id.ToString(),
                membership.CreatedAt))
            .ToArray();
    }

    private async Task<IReadOnlyList<ComplianceDashboardAlertDto>> BuildCompletedControlMetadataAlertsAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var rows = await dbContext.ControlAssessments
            .AsNoTracking()
            .Where(control =>
                control.Assessment != null &&
                control.Assessment.TenantId == tenantId &&
                (control.ImplementationStatus == ControlImplementationStatus.Implemented || control.Result == AssessmentResult.Met))
            .OrderBy(control => control.ControlId)
            .Take(MaxAlertsPerRule)
            .Select(control => new
            {
                control.AssessmentId,
                control.ControlId,
                control.ImplementationStatus,
                control.Result,
                control.Notes,
                control.AssessedByUserId,
                control.AssessedAt,
                control.EvidenceItemIdsJson
            })
            .ToArrayAsync(cancellationToken);

        return rows
            .Where(control =>
                control.ImplementationStatus != ControlImplementationStatus.Implemented ||
                control.Result != AssessmentResult.Met ||
                control.AssessedByUserId is null ||
                control.AssessedAt is null ||
                string.IsNullOrWhiteSpace(control.Notes) ||
                !HasJsonIds(control.EvidenceItemIdsJson))
            .Select(control => Alert(
                "complete_control_missing_review_metadata",
                "Critical",
                "Completed control needs review metadata",
                $"Control {control.ControlId} is marked complete without required evidence or review metadata.",
                "ControlAssessment",
                $"{control.AssessmentId}:{control.ControlId}",
                DateTimeOffset.UtcNow))
            .ToArray();
    }

    private static ComplianceDashboardAlertDto Alert(
        string alertType,
        string severity,
        string title,
        string message,
        string entityType,
        string entityId,
        DateTimeOffset detectedUtc) =>
        new(alertType, severity, title, message, entityType, entityId, detectedUtc);

    private static bool HasJsonIds(string? json)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "[]")
        {
            return false;
        }

        try
        {
            return JsonSerializer.Deserialize<Guid[]>(json)?.Length > 0;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static int SeverityRank(string severity) =>
        severity switch
        {
            "Critical" => 4,
            "High" => 3,
            "Medium" => 2,
            "Low" => 1,
            _ => 0
        };
}
