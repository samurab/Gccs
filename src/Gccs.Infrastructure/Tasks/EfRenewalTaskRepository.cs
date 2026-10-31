using System.Text.Json;
using Gccs.Application.Security;
using Gccs.Application.Tasks;
using Gccs.Domain.Cmmc;
using Gccs.Domain.Common;
using Gccs.Domain.Companies;
using Gccs.Domain.Compliance;
using Gccs.Domain.Evidence;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Tasks;

public sealed class EfRenewalTaskRepository(
    GccsDbContext dbContext,
    ICurrentTenantContext tenantContext) : IRenewalTaskRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<RenewalTaskGenerationResult> GenerateForCurrentTenantAsync(
        int leadTimeDays,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var candidates = await LoadCandidatesAsync(leadTimeDays, cancellationToken);
        var items = new List<RenewalTaskGenerationItem>();
        var createdCount = 0;
        var skippedCount = 0;

        foreach (var candidate in candidates)
        {
            var duplicateExists = await dbContext.ComplianceTasks.AnyAsync(
                task =>
                    task.TenantId == tenantContext.TenantId &&
                    task.Type == candidate.TaskType &&
                    task.DueAt == candidate.ReminderDueAt &&
                    task.ControlId == candidate.ControlId &&
                    task.EvidenceItemId == candidate.EvidenceItemId,
                cancellationToken);

            if (duplicateExists)
            {
                skippedCount++;
                items.Add(candidate.ToItem(null, created: false));
                continue;
            }

            var task = new ComplianceTaskEntity
            {
                Id = Guid.NewGuid(),
                TenantId = tenantContext.TenantId,
                Title = candidate.Title,
                Description = candidate.Description,
                Type = candidate.TaskType,
                Status = ComplianceTaskStatus.Open,
                RiskLevel = candidate.RiskLevel,
                OwnerFunction = candidate.OwnerFunction,
                DueAt = candidate.ReminderDueAt,
                EvidenceItemId = candidate.EvidenceItemId,
                ControlId = candidate.ControlId,
                CreatedAt = now,
                CreatedByUserId = actorUserId
            };

            dbContext.ComplianceTasks.Add(task);
            createdCount++;
            items.Add(candidate.ToItem(task.Id, created: true));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return new RenewalTaskGenerationResult(leadTimeDays, createdCount, skippedCount, items);
    }

    private async Task<IReadOnlyList<RenewalCandidate>> LoadCandidatesAsync(int leadTimeDays, CancellationToken cancellationToken)
    {
        var candidates = new List<RenewalCandidate>();
        var profile = await dbContext.CompanyProfiles
            .AsNoTracking()
            .Include(company => company.Certifications)
            .SingleOrDefaultAsync(company => company.TenantId == tenantContext.TenantId, cancellationToken);

        if (profile?.SamRegistrationExpiresAt is { } samExpiresAt)
        {
            candidates.Add(new RenewalCandidate(
                "sam_registration",
                profile.Id.ToString(),
                "Renew SAM registration",
                $"SAM registration expires on {samExpiresAt:yyyy-MM-dd}.",
                ComplianceTaskType.Renewal,
                RiskLevel.High,
                "Compliance",
                samExpiresAt,
                samExpiresAt.AddDays(-leadTimeDays),
                "company-profile",
                profile.Id.ToString(),
                null,
                $"company-profile:{profile.Id}:sam"));
        }

        if (profile is not null)
        {
            foreach (var certification in profile.Certifications.Where(certification =>
                certification.ExpiresAt.HasValue &&
                certification.Status is not CertificationStatus.Expired and not CertificationStatus.Revoked))
            {
                var expiresAt = certification.ExpiresAt!.Value;
                candidates.Add(new RenewalCandidate(
                    "certification",
                    certification.Id.ToString(),
                    $"Renew {FormatCertificationType(certification.Type)} certification",
                    $"{FormatCertificationType(certification.Type)} certification issued by {certification.Issuer} expires on {expiresAt:yyyy-MM-dd}.",
                    ComplianceTaskType.Renewal,
                    RiskLevel.Medium,
                    "Compliance",
                    expiresAt,
                    expiresAt.AddDays(-leadTimeDays),
                    "certification",
                    certification.Id.ToString(),
                    null,
                    $"certification:{certification.Id}"));
            }
        }

        var evidenceItems = await dbContext.EvidenceItems
            .AsNoTracking()
            .Where(evidence => evidence.TenantId == tenantContext.TenantId && evidence.ExpiresAt.HasValue)
            .ToArrayAsync(cancellationToken);

        foreach (var evidence in evidenceItems)
        {
            var tags = ReadTags(evidence.TagsJson);
            var sourceDueAt = evidence.ExpiresAt!.Value;
            var (sourceType, title, taskType, riskLevel, owner) = ClassifyEvidence(evidence, tags);
            candidates.Add(new RenewalCandidate(
                sourceType,
                evidence.Id.ToString(),
                title,
                $"{evidence.Name} expires or needs review on {sourceDueAt:yyyy-MM-dd}.",
                taskType,
                riskLevel,
                owner,
                sourceDueAt,
                sourceDueAt.AddDays(-leadTimeDays),
                "evidence",
                evidence.Id.ToString(),
                evidence.Id,
                null));
        }

        var affirmations = await dbContext.AnnualAffirmations
            .AsNoTracking()
            .Where(affirmation =>
                affirmation.TenantId == tenantContext.TenantId &&
                affirmation.Status != AffirmationStatus.Submitted &&
                affirmation.Status != AffirmationStatus.NotRequired)
            .ToArrayAsync(cancellationToken);

        foreach (var affirmation in affirmations)
        {
            candidates.Add(new RenewalCandidate(
                "cmmc_affirmation",
                affirmation.Id.ToString(),
                $"Complete {FormatCmmcLevel(affirmation.Level)} annual affirmation",
                $"CMMC {FormatCmmcLevel(affirmation.Level)} affirmation is due on {affirmation.DueAt:yyyy-MM-dd}.",
                ComplianceTaskType.Renewal,
                RiskLevel.High,
                "Security",
                affirmation.DueAt,
                affirmation.DueAt.AddDays(-leadTimeDays),
                "cmmc-affirmation",
                affirmation.Id.ToString(),
                null,
                $"cmmc-affirmation:{affirmation.Id}"));
        }

        return candidates
            .GroupBy(candidate => $"{candidate.TaskType}:{candidate.ReminderDueAt}:{candidate.LinkedEntityType}:{candidate.LinkedEntityId}", StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(candidate => candidate.ReminderDueAt)
            .ThenBy(candidate => candidate.Title)
            .ToArray();
    }

    private static (string SourceType, string Title, ComplianceTaskType TaskType, RiskLevel RiskLevel, string OwnerFunction) ClassifyEvidence(
        EvidenceItemEntity evidence,
        IReadOnlyList<string> tags)
    {
        var searchable = string.Join(" ", tags.Append(evidence.Name)).ToLowerInvariant();
        if (evidence.Type == EvidenceType.Policy || searchable.Contains("policy", StringComparison.OrdinalIgnoreCase))
        {
            return ("policy_review", $"Review policy evidence: {evidence.Name}", ComplianceTaskType.PolicyReview, RiskLevel.Medium, "Compliance");
        }

        if (searchable.Contains("insurance", StringComparison.OrdinalIgnoreCase))
        {
            return ("insurance", $"Renew insurance evidence: {evidence.Name}", ComplianceTaskType.Renewal, RiskLevel.Medium, "Contracts");
        }

        return ("evidence_expiration", $"Renew evidence: {evidence.Name}", ComplianceTaskType.Renewal, RiskLevel.Medium, "Compliance");
    }

    private static IReadOnlyList<string> ReadTags(string tagsJson)
    {
        try
        {
            return JsonSerializer.Deserialize<string[]>(tagsJson, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static string FormatCertificationType(CertificationType certificationType) =>
        certificationType switch
        {
            CertificationType.EightA => "8(a)",
            CertificationType.Wosb => "WOSB",
            CertificationType.Edwosb => "EDWOSB",
            CertificationType.HubZone => "HUBZone",
            CertificationType.Sdvosb => "SDVOSB",
            CertificationType.Sdb => "SDB",
            _ => "custom"
        };

    private static string FormatCmmcLevel(CmmcLevel level) =>
        level switch
        {
            CmmcLevel.Level1 => "Level 1",
            CmmcLevel.Level2 => "Level 2",
            CmmcLevel.Level3 => "Level 3",
            _ => level.ToString()
        };

    private sealed record RenewalCandidate(
        string SourceType,
        string SourceId,
        string Title,
        string Description,
        ComplianceTaskType TaskType,
        RiskLevel RiskLevel,
        string OwnerFunction,
        DateOnly SourceDueAt,
        DateOnly ReminderDueAt,
        string LinkedEntityType,
        string? LinkedEntityId,
        Guid? EvidenceItemId,
        string? ControlId)
    {
        public RenewalTaskGenerationItem ToItem(Guid? taskId, bool created) =>
            new(
                taskId,
                SourceType,
                SourceId,
                Title,
                TaskType,
                RiskLevel,
                SourceDueAt,
                ReminderDueAt,
                LinkedEntityType,
                LinkedEntityId,
                created);
    }
}
