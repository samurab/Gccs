using System.Text.Json;
using Gccs.Application.Demo;
using Gccs.Domain.Cmmc;
using Gccs.Domain.Common;
using Gccs.Domain.Companies;
using Gccs.Domain.Compliance;
using Gccs.Domain.Contracts;
using Gccs.Domain.Evidence;
using Gccs.Domain.Reports;
using Gccs.Domain.Tenancy;
using Gccs.Domain.Vendors;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Demo;

public sealed class EfDemoTenantSeedRepository(GccsDbContext dbContext) : IDemoTenantSeedRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly Guid ContractId = Guid.Parse("1a030002-0000-4000-8000-000000000001");
    private static readonly Guid ContractDocumentId = Guid.Parse("1a030002-0000-4000-8000-000000000002");
    private static readonly Guid ExtractionJobId = Guid.Parse("1a030002-0000-4000-8000-000000000003");
    private static readonly Guid ClauseCandidateId = Guid.Parse("1a030002-0000-4000-8000-000000000004");
    private static readonly Guid ContractClauseId = Guid.Parse("1a030002-0000-4000-8000-000000000005");
    private static readonly Guid EvidenceId = Guid.Parse("1a030002-0000-4000-8000-000000000006");
    private static readonly Guid AssessmentId = Guid.Parse("1a030002-0000-4000-8000-000000000007");
    private static readonly Guid PoamId = Guid.Parse("1a030002-0000-4000-8000-000000000008");
    private static readonly Guid SubcontractorId = Guid.Parse("1a030002-0000-4000-8000-000000000009");
    private static readonly Guid FlowDownId = Guid.Parse("1a030002-0000-4000-8000-000000000010");
    private static readonly Guid SubcontractorEvidenceRequestId = Guid.Parse("1a030002-0000-4000-8000-000000000011");
    private static readonly Guid ReportId = Guid.Parse("1a030002-0000-4000-8000-000000000012");
    private static readonly Guid ExpertReviewItemId = Guid.Parse("1a030002-0000-4000-8000-000000000013");
    private static readonly Guid ClauseObligationMappingId = Guid.Parse("1a030002-0000-4000-8000-000000000014");
    private const string ClauseId = "demo-synthetic-far-52-204-21";
    private const string ObligationId = "demo-synthetic-cui-safeguarding";
    private const string ControlId = "DEMO.AC.L1-3.1.1";

    public async Task<TenantDataPosture?> GetTenantModeAsync(Guid tenantId, CancellationToken cancellationToken = default) =>
        await dbContext.Tenants
            .AsNoTracking()
            .Where(tenant => tenant.Id == tenantId)
            .Select(tenant => (TenantDataPosture?)tenant.DataPosture)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<DemoTenantSeedResult> SeedAsync(
        SyntheticDemoDatasetDefinition dataset,
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var created = 0;

        created += await AddIfMissingAsync(dbContext.Clauses, ClauseId, new ClauseEntity
        {
            Id = ClauseId,
            Source = "FAR 52.204-21",
            Number = "52.204-21",
            Title = "[Synthetic demo data] Basic safeguarding of covered contractor information systems",
            PlainEnglishSummary = "Synthetic demo clause for CUI-like safeguarding workflow training.",
            ApplicabilityLogic = "DemoSandbox synthetic CUI scenario only.",
            ClauseTextVersion = dataset.Metadata.Version,
            ClauseEffectiveAt = today,
            SourceHash = $"synthetic-demo:{dataset.Metadata.Version}",
            RequiredActionIdsJson = JsonSerializer.Serialize(new[] { "demo-apply-basic-safeguards" }, JsonOptions),
            UsuallyRequiresFlowDown = true,
            SourceName = "GCCS synthetic demo dataset",
            SourceUrl = "https://example.invalid/gccs/synthetic-demo",
            SourceLastReviewedAt = dataset.Metadata.ReviewedAt ?? today,
            SourceEffectiveAt = today,
            SourceConfidence = "synthetic-demo",
            SourceRequiresExpertReview = false,
            LastReviewedAt = dataset.Metadata.ReviewedAt ?? today,
            Confidence = "synthetic-demo",
            RequiresExpertReview = false,
            ReviewState = ReviewState.Published
        }, cancellationToken);

        created += await AddIfMissingAsync(dbContext.Obligations, ObligationId, new ObligationEntity
        {
            Id = ObligationId,
            Source = "GCCS Synthetic Demo",
            Title = "[Synthetic demo data] Maintain sample CUI safeguarding evidence",
            PlainEnglishSummary = "Synthetic obligation used to demonstrate CUI-style tasking without real controlled data.",
            TriggerCondition = "DemoSandbox tenant uses imported synthetic CUI workflow examples.",
            RequiredAction = "Review synthetic access control evidence, assign owner, and collect placeholder flow-down acknowledgement.",
            OwnerFunction = "Security",
            RiskLevel = RiskLevel.High,
            RequiresFlowDown = true,
            FlowDownRequirement = "Synthetic flow-down acknowledgement for demo subcontractor only.",
            ApplicabilityJson = JsonSerializer.Serialize(new { datasetVersion = dataset.Metadata.Version }, JsonOptions),
            EvidenceExamplesJson = JsonSerializer.Serialize(new[] { "Synthetic access control evidence" }, JsonOptions),
            SourceName = "GCCS synthetic demo dataset",
            SourceUrl = "https://example.invalid/gccs/synthetic-demo",
            SourceLastReviewedAt = dataset.Metadata.ReviewedAt ?? today,
            SourceEffectiveAt = today,
            SourceConfidence = "synthetic-demo",
            SourceRequiresExpertReview = false,
            LastReviewedAt = dataset.Metadata.ReviewedAt ?? today,
            Confidence = "synthetic-demo",
            RequiresExpertReview = false,
            ReviewState = ReviewState.Published
        }, cancellationToken);

        created += await AddIfMissingAsync(dbContext.ClauseObligationMappings, ClauseObligationMappingId, new ClauseObligationMappingEntity
        {
            Id = ClauseObligationMappingId,
            TenantId = null,
            ClauseId = ClauseId,
            ObligationId = ObligationId,
            TriggerCondition = "DemoSandbox synthetic CUI workflow",
            RequiredAction = "Use synthetic CUI examples for demo-only evidence, CMMC, reporting, and flow-down workflows.",
            SourceUrl = "https://example.invalid/gccs/synthetic-demo",
            Confidence = "synthetic-demo",
            RequiresExpertReview = false,
            ReviewState = ReviewState.Published,
            LastReviewedAt = dataset.Metadata.ReviewedAt ?? today,
            ReviewedByUserId = actorUserId,
            CreatedAt = now,
            CreatedByUserId = actorUserId
        }, cancellationToken);

        created += await AddIfMissingAsync(dbContext.Contracts, ContractId, new ContractEntity
        {
            Id = ContractId,
            TenantId = tenantId,
            ContractNumber = "DEMO-26-CUI-0001",
            Title = "[Synthetic demo data] CUI Handling Training Support Order",
            AgencyOrPrimeName = "Fictional Prime Training Office",
            Relationship = ContractorRelationship.Subcontractor,
            Kind = ContractKind.PurchaseOrder,
            Status = ContractStatus.Active,
            AwardedAt = new DateOnly(2026, 6, 1),
            PeriodOfPerformanceStart = new DateOnly(2026, 6, 1),
            PeriodOfPerformanceEnd = new DateOnly(2026, 12, 31),
            PlaceOfPerformance = "Synthetic remote support location",
            Description = $"Synthetic demo contract seeded from dataset {dataset.Metadata.Version}.",
            DataHandlingPosture = DataHandlingPosture.Cui,
            CreatedAt = now,
            CreatedByUserId = actorUserId
        }, cancellationToken);

        created += await AddIfMissingAsync(dbContext.Set<ContractDocumentEntity>(), ContractDocumentId, new ContractDocumentEntity
        {
            Id = ContractDocumentId,
            ContractId = ContractId,
            Type = ContractDocumentType.StatementOfWork,
            FileName = "synthetic-cui-training-sow.txt",
            ContentType = "text/plain",
            SizeBytes = 512,
            StorageUri = $"demo://synthetic-cui/{dataset.Metadata.Version}/contract-document",
            ValidationStatus = "accepted",
            MalwareScanStatus = "not-required-demo-seed",
            NoticeVersion = "synthetic-demo-seed",
            UploadedAt = now,
            UploadedByUserId = actorUserId,
            ContainsPotentialCui = true,
            Classification = ContentClassification.SyntheticCui,
            ClassificationSource = ContentClassificationSource.ImportedDemoSeed,
            ClassificationConfidence = 1m,
            ClassificationReviewedAt = now,
            ClassificationReason = $"Approved synthetic demo dataset {dataset.Metadata.Version}.",
            ClassificationIsApprovedDemoContent = true
        }, cancellationToken);

        created += await AddIfMissingAsync(dbContext.Set<ExtractionJobEntity>(), ExtractionJobId, new ExtractionJobEntity
        {
            Id = ExtractionJobId,
            TenantId = tenantId,
            SourceDocumentId = ContractDocumentId,
            RequestedByUserId = actorUserId,
            Status = ExtractionJobStatus.Completed,
            RequestedAt = now,
            StartedAt = now,
            CompletedAt = now,
            Classification = ContentClassification.SyntheticCui,
            ClassificationSource = ContentClassificationSource.ImportedDemoSeed,
            ClassificationConfidence = 1m,
            ClassificationReviewedAt = now,
            ClassificationReason = $"Approved synthetic extraction job from dataset {dataset.Metadata.Version}.",
            ClassificationIsApprovedDemoContent = true
        }, cancellationToken);

        created += await AddIfMissingAsync(dbContext.Set<ClauseCandidateEntity>(), ClauseCandidateId, new ClauseCandidateEntity
        {
            Id = ClauseCandidateId,
            TenantId = tenantId,
            ExtractionJobId = ExtractionJobId,
            SourceDocumentId = ContractDocumentId,
            NormalizedCitation = "FAR 52.204-21",
            RawExtractedText = "[Synthetic demo data] FAR 52.204-21 appears in the fictional training order.",
            DetectedTitle = "Basic Safeguarding of Covered Contractor Information Systems",
            Confidence = 0.99m,
            LocationMetadata = "synthetic-line:12",
            MatchMethod = "demo-seed",
            ClauseLibraryId = ClauseId,
            ReviewStatus = "accepted",
            ReviewedByUserId = actorUserId,
            ReviewedAt = now,
            DecisionReason = "Synthetic demo candidate accepted during seed.",
            CreatedAt = now
        }, cancellationToken);

        created += await AddIfMissingAsync(dbContext.Set<ContractClauseEntity>(), ContractClauseId, new ContractClauseEntity
        {
            Id = ContractClauseId,
            ContractId = ContractId,
            ClauseLibraryId = ClauseId,
            ClauseNumber = "52.204-21",
            Title = "[Synthetic demo data] Basic safeguarding flow-down",
            FullText = "Synthetic clause attachment for demo-only CUI handling workflow.",
            Source = ClauseSource.Local,
            SourceUrl = "https://example.invalid/gccs/synthetic-demo",
            AttachmentReason = "Seeded synthetic demo clause.",
            SourceDocumentReference = "synthetic-cui-training-sow.txt",
            RequiresFlowDown = true,
            LastReviewedAt = dataset.Metadata.ReviewedAt ?? today,
            ReviewedByUserId = actorUserId,
            Confidence = "synthetic-demo",
            ReviewState = ReviewState.Published,
            CreatedAt = now,
            CreatedByUserId = actorUserId
        }, cancellationToken);

        created += await AddLinkIfMissingAsync(dbContext.Set<ContractClauseObligationEntity>(), ContractClauseId, ObligationId, new ContractClauseObligationEntity
        {
            ContractClauseId = ContractClauseId,
            ObligationId = ObligationId
        }, cancellationToken);

        created += await AddIfMissingAsync(dbContext.EvidenceItems, EvidenceId, new EvidenceItemEntity
        {
            Id = EvidenceId,
            TenantId = tenantId,
            Name = "[Synthetic demo data] Access control evidence",
            Description = $"Synthetic evidence seeded from dataset {dataset.Metadata.Version}.",
            Type = EvidenceType.Policy,
            OwnerFunction = "Security",
            Status = EvidenceStatus.Approved,
            EffectiveAt = new DateOnly(2026, 6, 1),
            ExpiresAt = new DateOnly(2026, 12, 31),
            TagsJson = JsonSerializer.Serialize(new[] { "synthetic-demo", dataset.Metadata.Version }, JsonOptions),
            ApprovedByUserId = actorUserId,
            ApprovedAt = now,
            Classification = ContentClassification.SyntheticCui,
            ClassificationSource = ContentClassificationSource.ImportedDemoSeed,
            ClassificationConfidence = 1m,
            ClassificationReviewedAt = now,
            ClassificationReason = $"Approved synthetic evidence from dataset {dataset.Metadata.Version}.",
            ClassificationIsApprovedDemoContent = true,
            CreatedAt = now,
            CreatedByUserId = actorUserId
        }, cancellationToken);

        created += await AddLinkIfMissingAsync(dbContext.Set<EvidenceContractEntity>(), EvidenceId, ContractId, new EvidenceContractEntity { EvidenceItemId = EvidenceId, ContractId = ContractId }, cancellationToken);
        created += await AddLinkIfMissingAsync(dbContext.Set<EvidenceObligationEntity>(), EvidenceId, ObligationId, new EvidenceObligationEntity { EvidenceItemId = EvidenceId, ObligationId = ObligationId }, cancellationToken);
        created += await AddLinkIfMissingAsync(dbContext.Set<EvidenceControlEntity>(), EvidenceId, ControlId, new EvidenceControlEntity { EvidenceItemId = EvidenceId, ControlId = ControlId }, cancellationToken);

        created += await AddIfMissingAsync(dbContext.Controls, ControlId, new ControlEntity
        {
            Id = ControlId,
            Framework = ControlFramework.Cmmc,
            CmmcLevel = CmmcLevel.Level1,
            Family = "AC",
            Title = "[Synthetic demo data] Limit system access",
            Requirement = "Synthetic access control requirement for demo workflows.",
            AssessmentObjective = "Demonstrate linking synthetic evidence to a CMMC-style control.",
            EvidenceExamplesJson = JsonSerializer.Serialize(new[] { "Synthetic access control evidence" }, JsonOptions),
            SourceName = "GCCS synthetic demo dataset",
            SourceUrl = "https://example.invalid/gccs/synthetic-demo",
            SourceLastReviewedAt = dataset.Metadata.ReviewedAt ?? today,
            SourceConfidence = "synthetic-demo"
        }, cancellationToken);

        created += await AddIfMissingAsync(dbContext.Assessments, AssessmentId, new AssessmentEntity
        {
            Id = AssessmentId,
            TenantId = tenantId,
            Name = "[Synthetic demo data] Level 1 readiness check",
            Type = AssessmentType.Readiness,
            Level = CmmcLevel.Level1,
            Framework = "CMMC synthetic demo",
            Status = AssessmentStatus.InProgress,
            StartedAt = today,
            AffirmationDueAt = today.AddMonths(12),
            OwnerFunction = "Security",
            ContractIdsJson = JsonSerializer.Serialize(new[] { ContractId }, JsonOptions),
            CreatedAt = now,
            CreatedByUserId = actorUserId
        }, cancellationToken);

        created += await AddLinkIfMissingAsync(dbContext.ControlAssessments, AssessmentId, ControlId, new ControlAssessmentEntity
        {
            AssessmentId = AssessmentId,
            ControlId = ControlId,
            ImplementationStatus = ControlImplementationStatus.PartiallyImplemented,
            Result = AssessmentResult.NotMet,
            Notes = $"Synthetic demo control gap from dataset {dataset.Metadata.Version}.",
            EvidenceItemIdsJson = JsonSerializer.Serialize(new[] { EvidenceId }, JsonOptions),
            AssessedByUserId = actorUserId,
            AssessedAt = today,
            OwnerFunction = "Security"
        }, cancellationToken);

        created += await AddIfMissingAsync(dbContext.PoamItems, PoamId, new PoamItemEntity
        {
            Id = PoamId,
            TenantId = tenantId,
            AssessmentId = AssessmentId,
            ControlId = ControlId,
            Weakness = "[Synthetic demo data] Boundary narrative needs reviewer confirmation",
            PlannedRemediation = "Update the fictional system boundary narrative and reattach synthetic evidence.",
            RiskLevel = RiskLevel.Medium,
            Status = PoamStatus.Open,
            OwnerFunction = "Security",
            TargetCompletionAt = today.AddDays(30),
            CreatedAt = now,
            CreatedByUserId = actorUserId
        }, cancellationToken);

        created += await AddIfMissingAsync(dbContext.Subcontractors, SubcontractorId, new SubcontractorEntity
        {
            Id = SubcontractorId,
            TenantId = tenantId,
            Name = "[Synthetic demo data] Northstar Demo Components",
            Status = SubcontractorStatus.Active,
            RoleDescription = "Fictional supplier for synthetic flow-down demonstration.",
            SmallBusinessStatus = "Synthetic small business",
            NaicsCodesJson = JsonSerializer.Serialize(new[] { "541519" }, JsonOptions),
            CertificationsJson = "[]",
            CmmcStatus = "Synthetic Level 1 self-assessment",
            InsuranceExpiresAt = today.AddMonths(6),
            NdaStatus = "Synthetic acknowledged",
            WorkshareDescription = "Synthetic demo workshare only.",
            WorksharePercentage = 12.5m,
            HasFciAccess = true,
            HasCuiAccess = true,
            HasExportControlledAccess = false,
            RequiredCmmcLevel = "Level 1",
            ContactName = "Demo Contact",
            ContactEmail = "demo-subcontractor@example.invalid",
            OwnerFunction = "Contracts",
            CreatedAt = now,
            CreatedByUserId = actorUserId
        }, cancellationToken);

        created += await AddLinkIfMissingAsync(dbContext.Set<ContractSubcontractorEntity>(), ContractId, SubcontractorId, new ContractSubcontractorEntity { ContractId = ContractId, SubcontractorId = SubcontractorId }, cancellationToken);
        created += await AddLinkIfMissingAsync(dbContext.Set<SubcontractorEvidenceEntity>(), SubcontractorId, EvidenceId, new SubcontractorEvidenceEntity { SubcontractorId = SubcontractorId, EvidenceItemId = EvidenceId }, cancellationToken);

        created += await AddIfMissingAsync(dbContext.FlowDownClauses, FlowDownId, new FlowDownClauseEntity
        {
            Id = FlowDownId,
            SubcontractorId = SubcontractorId,
            ContractId = ContractId,
            ContractClauseId = ContractClauseId,
            ObligationId = ObligationId,
            ClauseNumber = "52.204-21",
            Title = "[Synthetic demo data] Flow-down acknowledgement",
            Status = FlowDownStatus.Acknowledged,
            SentAt = today,
            AcknowledgedAt = today,
            SignedEvidenceItemId = EvidenceId,
            CreatedAt = now,
            CreatedByUserId = actorUserId
        }, cancellationToken);

        created += await AddIfMissingAsync(dbContext.SubcontractorEvidenceRequests, SubcontractorEvidenceRequestId, new SubcontractorEvidenceRequestEntity
        {
            Id = SubcontractorEvidenceRequestId,
            TenantId = tenantId,
            SubcontractorId = SubcontractorId,
            RequestedItem = "[Synthetic demo data] Flow-down acknowledgement",
            RequestedEvidenceTypesJson = JsonSerializer.Serialize(new[] { "SignedFlowDown" }, JsonOptions),
            DueDate = today.AddDays(14),
            Status = SubcontractorEvidenceRequestStatus.Satisfied,
            OwnerFunction = "Contracts",
            RecipientName = "Demo Contact",
            RecipientEmail = "demo-subcontractor@example.invalid",
            ObligationId = ObligationId,
            RelatedFlowDownClauseId = FlowDownId,
            ReceivedEvidenceItemId = EvidenceId,
            CompletedAt = now,
            CreatedAt = now,
            CreatedByUserId = actorUserId
        }, cancellationToken);

        created += await AddIfMissingAsync(dbContext.Reports, ReportId, new ReportEntity
        {
            Id = ReportId,
            TenantId = tenantId,
            Type = ReportType.PrimeEvidencePackage,
            Title = "[Synthetic demo data] Prime evidence package",
            Status = ReportStatus.Complete,
            GeneratedAt = now,
            GeneratedByUserId = actorUserId,
            SnapshotJson = JsonSerializer.Serialize(new { dataset.Metadata.Version, ContractId, EvidenceId, SubcontractorId }, JsonOptions),
            ExportHtml = "<h1>Synthetic demo data</h1><p>Prime evidence package for demo only.</p>",
            Classification = ContentClassification.SyntheticCui,
            ClassificationSource = ContentClassificationSource.ImportedDemoSeed,
            ClassificationConfidence = 1m,
            ClassificationReviewedAt = now,
            ClassificationReason = $"Approved synthetic report from dataset {dataset.Metadata.Version}.",
            ClassificationIsApprovedDemoContent = true,
            CreatedAt = now,
            CreatedByUserId = actorUserId
        }, cancellationToken);

        created += await AddLinkIfMissingAsync(dbContext.Set<ReportContractEntity>(), ReportId, ContractId, new ReportContractEntity { ReportId = ReportId, ContractId = ContractId }, cancellationToken);
        created += await AddLinkIfMissingAsync(dbContext.Set<ReportObligationEntity>(), ReportId, ObligationId, new ReportObligationEntity { ReportId = ReportId, ObligationId = ObligationId }, cancellationToken);
        created += await AddLinkIfMissingAsync(dbContext.Set<ReportEvidenceEntity>(), ReportId, EvidenceId, new ReportEvidenceEntity { ReportId = ReportId, EvidenceItemId = EvidenceId }, cancellationToken);

        created += await AddIfMissingAsync(dbContext.ExpertReviewItems, ExpertReviewItemId, new ExpertReviewItemEntity
        {
            Id = ExpertReviewItemId,
            TenantId = tenantId,
            SourceType = "SyntheticDemoSeed",
            SourceId = ContractDocumentId,
            Reason = "[Synthetic demo data] Escalation example for reviewer workflow.",
            Priority = "medium",
            Topic = "Synthetic CUI demo escalation",
            Status = "open",
            CreatedByUserId = actorUserId,
            CreatedAt = now
        }, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
        return BuildResult("seed", dataset, created, 0);
    }

    public async Task<DemoTenantSeedResult> ResetAsync(
        SyntheticDemoDatasetDefinition dataset,
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var deleted = 0;
        deleted += await RemoveAsync(dbContext.Set<ReportEvidenceEntity>().Where(item => item.ReportId == ReportId), cancellationToken);
        deleted += await RemoveAsync(dbContext.Set<ReportObligationEntity>().Where(item => item.ReportId == ReportId), cancellationToken);
        deleted += await RemoveAsync(dbContext.Set<ReportContractEntity>().Where(item => item.ReportId == ReportId), cancellationToken);
        deleted += await RemoveAsync(dbContext.Set<SubcontractorEvidenceEntity>().Where(item => item.SubcontractorId == SubcontractorId || item.EvidenceItemId == EvidenceId), cancellationToken);
        deleted += await RemoveAsync(dbContext.Set<ContractSubcontractorEntity>().Where(item => item.ContractId == ContractId || item.SubcontractorId == SubcontractorId), cancellationToken);
        deleted += await RemoveAsync(dbContext.Set<EvidenceControlEntity>().Where(item => item.EvidenceItemId == EvidenceId || item.ControlId == ControlId), cancellationToken);
        deleted += await RemoveAsync(dbContext.Set<EvidenceObligationEntity>().Where(item => item.EvidenceItemId == EvidenceId || item.ObligationId == ObligationId), cancellationToken);
        deleted += await RemoveAsync(dbContext.Set<EvidenceContractEntity>().Where(item => item.EvidenceItemId == EvidenceId || item.ContractId == ContractId), cancellationToken);
        deleted += await RemoveAsync(dbContext.Set<ContractClauseObligationEntity>().Where(item => item.ContractClauseId == ContractClauseId || item.ObligationId == ObligationId), cancellationToken);
        deleted += await RemoveAsync(dbContext.ControlAssessments.Where(item => item.AssessmentId == AssessmentId || item.ControlId == ControlId), cancellationToken);
        deleted += await RemoveAsync(dbContext.PoamItems.Where(item => item.TenantId == tenantId && item.Id == PoamId), cancellationToken);
        deleted += await RemoveAsync(dbContext.SubcontractorEvidenceRequests.Where(item => item.TenantId == tenantId && item.Id == SubcontractorEvidenceRequestId), cancellationToken);
        deleted += await RemoveAsync(dbContext.FlowDownClauses.Where(item => item.Id == FlowDownId && item.Subcontractor != null && item.Subcontractor.TenantId == tenantId), cancellationToken);
        deleted += await RemoveAsync(dbContext.Reports.Where(item => item.TenantId == tenantId && item.Id == ReportId), cancellationToken);
        deleted += await RemoveAsync(dbContext.ExpertReviewItems.Where(item => item.TenantId == tenantId && item.Id == ExpertReviewItemId), cancellationToken);
        deleted += await RemoveAsync(dbContext.Assessments.Where(item => item.TenantId == tenantId && item.Id == AssessmentId), cancellationToken);
        deleted += await RemoveAsync(dbContext.Subcontractors.Where(item => item.TenantId == tenantId && item.Id == SubcontractorId), cancellationToken);
        deleted += await RemoveAsync(dbContext.EvidenceItems.Where(item => item.TenantId == tenantId && item.Id == EvidenceId), cancellationToken);
        deleted += await RemoveAsync(dbContext.Set<ClauseCandidateEntity>().Where(item => item.TenantId == tenantId && item.Id == ClauseCandidateId), cancellationToken);
        deleted += await RemoveAsync(dbContext.Set<ExtractionJobEntity>().Where(item => item.TenantId == tenantId && item.Id == ExtractionJobId), cancellationToken);
        deleted += await RemoveAsync(dbContext.Set<ContractDocumentEntity>().Where(item => item.Id == ContractDocumentId && item.Contract != null && item.Contract.TenantId == tenantId), cancellationToken);
        deleted += await RemoveAsync(dbContext.Set<ContractClauseEntity>().Where(item => item.ContractId == ContractId && item.Id == ContractClauseId), cancellationToken);
        deleted += await RemoveAsync(dbContext.Contracts.Where(item => item.TenantId == tenantId && item.Id == ContractId), cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
        return BuildResult("reset", dataset, 0, deleted);
    }

    private static DemoTenantSeedResult BuildResult(string action, SyntheticDemoDatasetDefinition dataset, int created, int deleted) =>
        new(
            true,
            action,
            dataset.Metadata.DatasetId,
            dataset.Metadata.Version,
            created,
            deleted,
            dataset.Records.Select(record => record.RecordType).Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).ToArray(),
            new Dictionary<string, string>
            {
                ["contractId"] = ContractId.ToString(),
                ["contractDocumentId"] = ContractDocumentId.ToString(),
                ["extractionJobId"] = ExtractionJobId.ToString(),
                ["obligationId"] = ObligationId,
                ["evidenceItemId"] = EvidenceId.ToString(),
                ["assessmentId"] = AssessmentId.ToString(),
                ["subcontractorId"] = SubcontractorId.ToString(),
                ["reportId"] = ReportId.ToString(),
                ["expertReviewItemId"] = ExpertReviewItemId.ToString()
            });

    private async Task<int> AddIfMissingAsync<TEntity, TKey>(DbSet<TEntity> set, TKey key, TEntity entity, CancellationToken cancellationToken)
        where TEntity : class
    {
        var exists = await set.FindAsync([key], cancellationToken) is not null;
        if (exists)
        {
            return 0;
        }

        set.Add(entity);
        return 1;
    }

    private async Task<int> AddLinkIfMissingAsync<TEntity, TLeft, TRight>(DbSet<TEntity> set, TLeft left, TRight right, TEntity entity, CancellationToken cancellationToken)
        where TEntity : class
    {
        var exists = await set.FindAsync([left!, right!], cancellationToken) is not null;
        if (exists)
        {
            return 0;
        }

        set.Add(entity);
        return 1;
    }

    private async Task<int> RemoveAsync<TEntity>(IQueryable<TEntity> query, CancellationToken cancellationToken)
        where TEntity : class
    {
        var items = await query.ToArrayAsync(cancellationToken);
        if (items.Length == 0)
        {
            return 0;
        }

        dbContext.RemoveRange(items);
        return items.Length;
    }
}
