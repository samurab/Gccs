using System.Text;
using Gccs.Domain.Audit;
using Gccs.Domain.Cmmc;
using Gccs.Domain.Companies;
using Gccs.Domain.Compliance;
using Gccs.Domain.Common;
using Gccs.Domain.Contracts;
using Gccs.Domain.Evidence;
using Gccs.Domain.Identity;
using Gccs.Domain.Labor;
using Gccs.Domain.People;
using Gccs.Domain.Reports;
using Gccs.Domain.Tenancy;
using Gccs.Domain.Vendors;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gccs.Infrastructure.Persistence;

public sealed class GccsDbContext(DbContextOptions<GccsDbContext> options) : DbContext(options)
{
    public DbSet<TenantEntity> Tenants => Set<TenantEntity>();
    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<TenantMembershipEntity> TenantMemberships => Set<TenantMembershipEntity>();
    public DbSet<TenantInvitationEntity> TenantInvitations => Set<TenantInvitationEntity>();
    public DbSet<NoCuiAcknowledgementEntity> NoCuiAcknowledgements => Set<NoCuiAcknowledgementEntity>();
    public DbSet<NotificationPreferenceEntity> NotificationPreferences => Set<NotificationPreferenceEntity>();
    public DbSet<NotificationDeliveryEntity> NotificationDeliveries => Set<NotificationDeliveryEntity>();
    public DbSet<RoleEntity> Roles => Set<RoleEntity>();
    public DbSet<CompanyProfileEntity> CompanyProfiles => Set<CompanyProfileEntity>();
    public DbSet<ClauseEntity> Clauses => Set<ClauseEntity>();
    public DbSet<ObligationEntity> Obligations => Set<ObligationEntity>();
    public DbSet<ContractEntity> Contracts => Set<ContractEntity>();
    public DbSet<SolicitationEntity> Solicitations => Set<SolicitationEntity>();
    public DbSet<ComplianceTaskEntity> ComplianceTasks => Set<ComplianceTaskEntity>();
    public DbSet<EvidenceItemEntity> EvidenceItems => Set<EvidenceItemEntity>();
    public DbSet<EvidenceFileVersionEntity> EvidenceFileVersions => Set<EvidenceFileVersionEntity>();
    public DbSet<ControlEntity> Controls => Set<ControlEntity>();
    public DbSet<AssessmentEntity> Assessments => Set<AssessmentEntity>();
    public DbSet<ControlAssessmentEntity> ControlAssessments => Set<ControlAssessmentEntity>();
    public DbSet<PoamItemEntity> PoamItems => Set<PoamItemEntity>();
    public DbSet<AssetEntity> Assets => Set<AssetEntity>();
    public DbSet<SystemBoundaryEntity> SystemBoundaries => Set<SystemBoundaryEntity>();
    public DbSet<AnnualAffirmationEntity> AnnualAffirmations => Set<AnnualAffirmationEntity>();
    public DbSet<VendorEntity> Vendors => Set<VendorEntity>();
    public DbSet<SubcontractorEntity> Subcontractors => Set<SubcontractorEntity>();
    public DbSet<FlowDownClauseEntity> FlowDownClauses => Set<FlowDownClauseEntity>();
    public DbSet<SubcontractorEvidenceRequestEntity> SubcontractorEvidenceRequests => Set<SubcontractorEvidenceRequestEntity>();
    public DbSet<EmployeeEntity> Employees => Set<EmployeeEntity>();
    public DbSet<TrainingRecordEntity> TrainingRecords => Set<TrainingRecordEntity>();
    public DbSet<WageDeterminationEntity> WageDeterminations => Set<WageDeterminationEntity>();
    public DbSet<LaborClassificationEntity> LaborClassifications => Set<LaborClassificationEntity>();
    public DbSet<PayrollRecordEntity> PayrollRecords => Set<PayrollRecordEntity>();
    public DbSet<ReportEntity> Reports => Set<ReportEntity>();
    public DbSet<AuditLogEntryEntity> AuditLogEntries => Set<AuditLogEntryEntity>();
    public DbSet<MvpModuleEntity> MvpModules => Set<MvpModuleEntity>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<AffirmationStatus>().HaveConversion<string>().HaveMaxLength(64);
        configurationBuilder.Properties<AssessmentResult>().HaveConversion<string>().HaveMaxLength(64);
        configurationBuilder.Properties<AssessmentStatus>().HaveConversion<string>().HaveMaxLength(64);
        configurationBuilder.Properties<AssessmentType>().HaveConversion<string>().HaveMaxLength(64);
        configurationBuilder.Properties<AssetType>().HaveConversion<string>().HaveMaxLength(64);
        configurationBuilder.Properties<AuditAction>().HaveConversion<string>().HaveMaxLength(64);
        configurationBuilder.Properties<BoundaryStatus>().HaveConversion<string>().HaveMaxLength(64);
        configurationBuilder.Properties<CertificationStatus>().HaveConversion<string>().HaveMaxLength(64);
        configurationBuilder.Properties<CertificationType>().HaveConversion<string>().HaveMaxLength(64);
        configurationBuilder.Properties<ClauseSource>().HaveConversion<string>().HaveMaxLength(64);
        configurationBuilder.Properties<CmmcLevel>().HaveConversion<string>().HaveMaxLength(64);
        configurationBuilder.Properties<CompanyRange>().HaveConversion<string>().HaveMaxLength(64);
        configurationBuilder.Properties<ComplianceTaskStatus>().HaveConversion<string>().HaveMaxLength(64);
        configurationBuilder.Properties<ComplianceTaskType>().HaveConversion<string>().HaveMaxLength(64);
        configurationBuilder.Properties<ContractDocumentType>().HaveConversion<string>().HaveMaxLength(64);
        configurationBuilder.Properties<ContractKind>().HaveConversion<string>().HaveMaxLength(64);
        configurationBuilder.Properties<ContractorRelationship>().HaveConversion<string>().HaveMaxLength(64);
        configurationBuilder.Properties<ContractorRole>().HaveConversion<string>().HaveMaxLength(64);
        configurationBuilder.Properties<ContractStatus>().HaveConversion<string>().HaveMaxLength(64);
        configurationBuilder.Properties<ControlFramework>().HaveConversion<string>().HaveMaxLength(64);
        configurationBuilder.Properties<ControlImplementationStatus>().HaveConversion<string>().HaveMaxLength(64);
        configurationBuilder.Properties<DataHandlingPosture>().HaveConversion<string>().HaveMaxLength(64);
        configurationBuilder.Properties<DeliverableStatus>().HaveConversion<string>().HaveMaxLength(64);
        configurationBuilder.Properties<EmploymentStatus>().HaveConversion<string>().HaveMaxLength(64);
        configurationBuilder.Properties<EvidenceStatus>().HaveConversion<string>().HaveMaxLength(64);
        configurationBuilder.Properties<EvidenceType>().HaveConversion<string>().HaveMaxLength(64);
        configurationBuilder.Properties<ExtractionJobStatus>().HaveConversion<string>().HaveMaxLength(64);
        configurationBuilder.Properties<FlowDownStatus>().HaveConversion<string>().HaveMaxLength(64);
        configurationBuilder.Properties<MembershipStatus>().HaveConversion<string>().HaveMaxLength(64);
        configurationBuilder.Properties<Permission>().HaveConversion<string>().HaveMaxLength(64);
        configurationBuilder.Properties<PoamStatus>().HaveConversion<string>().HaveMaxLength(64);
        configurationBuilder.Properties<RecurrencePattern>().HaveConversion<string>().HaveMaxLength(64);
        configurationBuilder.Properties<ReportStatus>().HaveConversion<string>().HaveMaxLength(64);
        configurationBuilder.Properties<ReportType>().HaveConversion<string>().HaveMaxLength(64);
        configurationBuilder.Properties<ReviewState>().HaveConversion<string>().HaveMaxLength(64);
        configurationBuilder.Properties<RiskLevel>().HaveConversion<string>().HaveMaxLength(64);
        configurationBuilder.Properties<SubcontractorStatus>().HaveConversion<string>().HaveMaxLength(64);
        configurationBuilder.Properties<SubcontractorEvidenceRequestStatus>().HaveConversion<string>().HaveMaxLength(64);
        configurationBuilder.Properties<TenantDataPosture>().HaveConversion<string>().HaveMaxLength(64);
        configurationBuilder.Properties<TenantInvitationStatus>().HaveConversion<string>().HaveMaxLength(64);
        configurationBuilder.Properties<TenantStatus>().HaveConversion<string>().HaveMaxLength(64);
        configurationBuilder.Properties<TrainingStatus>().HaveConversion<string>().HaveMaxLength(64);
        configurationBuilder.Properties<TrainingType>().HaveConversion<string>().HaveMaxLength(64);
        configurationBuilder.Properties<UserStatus>().HaveConversion<string>().HaveMaxLength(64);
        configurationBuilder.Properties<VendorRiskLevel>().HaveConversion<string>().HaveMaxLength(64);
        configurationBuilder.Properties<VendorType>().HaveConversion<string>().HaveMaxLength(64);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("gccs");

        ConfigureCore(modelBuilder);
        ConfigureComplianceContent(modelBuilder);
        ConfigureContracts(modelBuilder);
        ConfigureEvidence(modelBuilder);
        ConfigureCmmc(modelBuilder);
        ConfigureVendors(modelBuilder);
        ConfigurePeopleAndLabor(modelBuilder);
        ConfigureReports(modelBuilder);
        ConfigureAudit(modelBuilder);

        ConfigureTenantForeignKeys(modelBuilder);
        ApplyPostgresConventions(modelBuilder);
    }

    private static void ConfigureCore(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TenantEntity>(entity =>
        {
            entity.ToTable("tenants");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(240).IsRequired();
            ConfigureAuditColumns(entity);
        });

        modelBuilder.Entity<UserEntity>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.TenantId, x.Email }).IsUnique();
            entity.Property(x => x.Email).HasMaxLength(320).IsRequired();
            entity.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
            entity.HasOne(x => x.Tenant).WithMany(x => x.Users).HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            ConfigureAuditColumns(entity);
        });

        modelBuilder.Entity<TenantMembershipEntity>(entity =>
        {
            entity.ToTable("tenant_memberships");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.TenantId, x.UserId }).IsUnique();
            entity.HasIndex(x => new { x.TenantId, x.Status });
            entity.Property(x => x.RoleName).HasMaxLength(120).IsRequired();
            entity.HasOne(x => x.Tenant).WithMany(x => x.Memberships).HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.User).WithMany(x => x.Memberships).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            ConfigureAuditColumns(entity);
        });

        modelBuilder.Entity<TenantInvitationEntity>(entity =>
        {
            entity.ToTable("tenant_invitations");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.InvitationToken).IsUnique();
            entity.HasIndex(x => new { x.TenantId, x.Status, x.ExpiresAt });
            entity.HasIndex(x => new { x.TenantId, x.Email, x.Status });
            entity.Property(x => x.Email).HasMaxLength(320).IsRequired();
            entity.Property(x => x.RoleName).HasMaxLength(120).IsRequired();
            entity.Property(x => x.InvitationToken).HasMaxLength(128).IsRequired();
            entity.Property(x => x.NotificationPlaceholder).HasMaxLength(600).IsRequired();
            entity.HasOne(x => x.Tenant).WithMany(x => x.Invitations).HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            ConfigureAuditColumns(entity);
        });

        modelBuilder.Entity<NoCuiAcknowledgementEntity>(entity =>
        {
            entity.ToTable("no_cui_acknowledgements");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.TenantId, x.UserId, x.NoticeVersion }).IsUnique();
            entity.Property(x => x.NoticeVersion).HasMaxLength(80).IsRequired();
            entity.Property(x => x.NoticeCopy).HasMaxLength(1000).IsRequired();
            ConfigureAuditColumns(entity);
        });

        modelBuilder.Entity<NotificationPreferenceEntity>(entity =>
        {
            entity.ToTable("notification_preferences");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.TenantId, x.UserId }).IsUnique();
            entity.Property(x => x.RoleName).HasMaxLength(120).IsRequired();
            ConfigureAuditColumns(entity);
        });

        modelBuilder.Entity<NotificationDeliveryEntity>(entity =>
        {
            entity.ToTable("notification_deliveries");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.TenantId, x.SourceTaskId, x.Category, x.UserId }).IsUnique();
            entity.HasIndex(x => new { x.TenantId, x.AttemptedAt });
            entity.Property(x => x.Category).HasMaxLength(80).IsRequired();
            entity.Property(x => x.SourceType).HasMaxLength(120).IsRequired();
            entity.Property(x => x.LinkUrl).HasMaxLength(400).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Placeholder).HasMaxLength(800).IsRequired();
            entity.Property(x => x.FailureMessage).HasMaxLength(800);
            ConfigureAuditColumns(entity);
        });

        modelBuilder.Entity<RoleEntity>(entity =>
        {
            entity.ToTable("roles");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.TenantId, x.Name }).IsUnique();
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.HasOne(x => x.Tenant).WithMany(x => x.Roles).HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            ConfigureAuditColumns(entity);
        });

        modelBuilder.Entity<UserRoleEntity>(entity =>
        {
            entity.ToTable("user_roles");
            entity.HasKey(x => new { x.UserId, x.RoleId });
            entity.HasOne(x => x.User).WithMany(x => x.UserRoles).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Role).WithMany(x => x.UserRoles).HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RolePermissionEntity>(entity =>
        {
            entity.ToTable("role_permissions");
            entity.HasKey(x => new { x.RoleId, x.Permission });
            entity.HasOne(x => x.Role).WithMany(x => x.Permissions).HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CompanyProfileEntity>(entity =>
        {
            entity.ToTable("company_profiles");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.TenantId).IsUnique();
            entity.HasIndex(x => x.Uei);
            entity.Property(x => x.LegalEntityName).HasMaxLength(240).IsRequired();
            entity.Property(x => x.Uei).HasMaxLength(32);
            entity.Property(x => x.CageCode).HasMaxLength(16);
            entity.Property(x => x.KeySystemsJson).HasColumnType("jsonb");
            entity.Property(x => x.AgencyCustomersJson).HasColumnType("jsonb");
            entity.HasOne(x => x.Tenant).WithOne().HasForeignKey<CompanyProfileEntity>(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            ConfigureAuditColumns(entity);
        });

        modelBuilder.Entity<CompanyNaicsCodeEntity>(entity =>
        {
            entity.ToTable("company_naics_codes");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.CompanyProfileId, x.Code }).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(12).IsRequired();
            entity.Property(x => x.Title).HasMaxLength(240).IsRequired();
            entity.HasOne(x => x.CompanyProfile).WithMany(x => x.NaicsCodes).HasForeignKey(x => x.CompanyProfileId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CompanyCertificationEntity>(entity =>
        {
            entity.ToTable("company_certifications");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.CompanyProfileId, x.Type });
            entity.Property(x => x.Issuer).HasMaxLength(180).IsRequired();
            entity.Property(x => x.ReferenceNumber).HasMaxLength(120);
            entity.HasOne(x => x.CompanyProfile).WithMany(x => x.Certifications).HasForeignKey(x => x.CompanyProfileId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CompanyLocationEntity>(entity =>
        {
            entity.ToTable("company_locations");
            entity.HasKey(x => x.Id);
            entity.HasOne(x => x.CompanyProfile).WithMany(x => x.Locations).HasForeignKey(x => x.CompanyProfileId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ComplianceTaskEntity>(entity =>
        {
            entity.ToTable("compliance_tasks");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.TenantId, x.Status, x.DueAt });
            entity.HasIndex(x => new { x.TenantId, x.ContractId });
            entity.HasIndex(x => new { x.TenantId, x.ObligationId });
            entity.Property(x => x.Title).HasMaxLength(240).IsRequired();
            entity.Property(x => x.OwnerFunction).HasMaxLength(120).IsRequired();
            ConfigureAuditColumns(entity);
        });
    }

    private static void ConfigureComplianceContent(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ClauseEntity>(entity =>
        {
            entity.ToTable("clauses");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.TenantId, x.Source, x.Number }).IsUnique();
            entity.HasIndex(x => new { x.TenantId, x.ReviewState });
            entity.Property(x => x.RequiredActionIdsJson).HasColumnType("jsonb");
            entity.Property(x => x.ClauseTextVersion).HasMaxLength(120).HasDefaultValue("current").IsRequired();
            entity.Property(x => x.SourceHash).HasMaxLength(128);
            entity.Property(x => x.SupersededByClauseId).HasMaxLength(120);
            entity.Property(x => x.ReviewState).HasDefaultValue(ReviewState.Draft);
        });

        modelBuilder.Entity<ObligationEntity>(entity =>
        {
            entity.ToTable("obligations");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Source);
            entity.Property(x => x.ApplicabilityJson).HasColumnType("jsonb");
            entity.Property(x => x.EvidenceExamplesJson).HasColumnType("jsonb");
            entity.Property(x => x.Confidence).HasDefaultValue("unknown");
            entity.Property(x => x.ReviewState).HasDefaultValue(ReviewState.Draft);
        });

        modelBuilder.Entity<MvpModuleEntity>(entity =>
        {
            entity.ToTable("mvp_modules");
            entity.HasKey(x => x.Key);
        });
    }

    private static void ConfigureContracts(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ContractEntity>(entity =>
        {
            entity.ToTable("contracts");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.TenantId, x.ContractNumber }).IsUnique();
            entity.HasIndex(x => new { x.TenantId, x.Status });
            entity.Property(x => x.ContractNumber).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Title).HasMaxLength(300).IsRequired();
            entity.Property(x => x.AgencyOrPrimeName).HasMaxLength(240).IsRequired();
            entity.Property(x => x.PlaceOfPerformance).HasMaxLength(240).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(1200);
            ConfigureAuditColumns(entity);
        });

        modelBuilder.Entity<SolicitationEntity>(entity =>
        {
            entity.ToTable("solicitations");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.TenantId, x.SolicitationNumber }).IsUnique();
            ConfigureAuditColumns(entity);
        });

        modelBuilder.Entity<ContractDocumentEntity>(entity =>
        {
            entity.ToTable("contract_documents");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.ContractId, x.Type });
            entity.Property(x => x.FileName).HasMaxLength(300).IsRequired();
            entity.Property(x => x.ContentType).HasMaxLength(160).IsRequired();
            entity.Property(x => x.ValidationStatus).HasMaxLength(80).IsRequired();
            entity.Property(x => x.MalwareScanStatus).HasMaxLength(80).IsRequired();
            entity.Property(x => x.NoticeVersion).HasMaxLength(80).IsRequired();
            entity.HasOne(x => x.Contract).WithMany(x => x.Documents).HasForeignKey(x => x.ContractId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ExtractionJobEntity>(entity =>
        {
            entity.ToTable("extraction_jobs");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.TenantId, x.Status, x.RequestedAt });
            entity.HasIndex(x => x.SourceDocumentId);
            entity.Property(x => x.FailureReason).HasMaxLength(1000);
            entity.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.SourceDocument).WithMany(x => x.ExtractionJobs).HasForeignKey(x => x.SourceDocumentId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ClauseCandidateEntity>(entity =>
        {
            entity.ToTable("clause_candidates");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.TenantId, x.SourceDocumentId });
            entity.HasIndex(x => new { x.ExtractionJobId, x.NormalizedCitation });
            entity.Property(x => x.NormalizedCitation).HasMaxLength(120).IsRequired();
            entity.Property(x => x.RawExtractedText).HasMaxLength(2000).IsRequired();
            entity.Property(x => x.DetectedTitle).HasMaxLength(300);
            entity.Property(x => x.Confidence).HasPrecision(5, 4);
            entity.Property(x => x.LocationMetadata).HasMaxLength(300).IsRequired();
            entity.Property(x => x.MatchMethod).HasMaxLength(80).IsRequired();
            entity.Property(x => x.ClauseLibraryId).HasMaxLength(160);
            entity.Property(x => x.ReviewStatus).HasMaxLength(80).IsRequired();
            entity.Property(x => x.DecisionNote).HasMaxLength(1000);
            entity.Property(x => x.DecisionReason).HasMaxLength(600);
            entity.HasOne(x => x.ExtractionJob).WithMany(x => x.Candidates).HasForeignKey(x => x.ExtractionJobId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.SourceDocument).WithMany().HasForeignKey(x => x.SourceDocumentId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ContractClauseEntity>(entity =>
        {
            entity.ToTable("contract_clauses");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.ContractId, x.ClauseNumber });
            entity.HasIndex(x => new { x.ContractId, x.ClauseLibraryId, x.RemovedAt });
            entity.Property(x => x.ClauseLibraryId).HasMaxLength(160).IsRequired();
            entity.Property(x => x.SourceUrl).HasMaxLength(600).IsRequired();
            entity.Property(x => x.AttachmentReason).HasMaxLength(600).IsRequired();
            entity.Property(x => x.SourceDocumentReference).HasMaxLength(300);
            entity.Property(x => x.RemovalReason).HasMaxLength(600);
            entity.Property(x => x.SourceHash).HasMaxLength(128);
            entity.Property(x => x.ReviewState).HasDefaultValue(ReviewState.Draft);
            entity.HasOne(x => x.Contract).WithMany(x => x.Clauses).HasForeignKey(x => x.ContractId).OnDelete(DeleteBehavior.Cascade);
            ConfigureAuditColumns(entity);
        });

        modelBuilder.Entity<ContractClauseObligationEntity>(entity =>
        {
            entity.ToTable("contract_clause_obligations");
            entity.HasKey(x => new { x.ContractClauseId, x.ObligationId });
            entity.HasOne(x => x.ContractClause).WithMany(x => x.Obligations).HasForeignKey(x => x.ContractClauseId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Obligation).WithMany(x => x.ContractClauses).HasForeignKey(x => x.ObligationId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ContractDeliverableEntity>(entity =>
        {
            entity.ToTable("contract_deliverables");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.ContractId, x.DueAt });
            entity.HasOne(x => x.Contract).WithMany(x => x.Deliverables).HasForeignKey(x => x.ContractId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ContractReportingDeadlineEntity>(entity =>
        {
            entity.ToTable("contract_reporting_deadlines");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.ContractId, x.DueAt });
            entity.Property(x => x.SourceClauseNumbersJson).HasColumnType("jsonb");
            entity.HasOne(x => x.Contract).WithMany(x => x.ReportingDeadlines).HasForeignKey(x => x.ContractId).OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureEvidence(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EvidenceItemEntity>(entity =>
        {
            entity.ToTable("evidence_items");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.TenantId, x.Status });
            entity.HasIndex(x => new { x.TenantId, x.ExpiresAt });
            entity.Property(x => x.Name).HasMaxLength(240).IsRequired();
            entity.Property(x => x.OwnerFunction).HasMaxLength(120).IsRequired();
            entity.Property(x => x.OriginalFileName).HasMaxLength(240);
            entity.Property(x => x.ContentType).HasMaxLength(160);
            entity.Property(x => x.UploadValidationStatus).HasMaxLength(80);
            entity.Property(x => x.MalwareScanStatus).HasMaxLength(80);
            entity.Property(x => x.TagsJson).HasColumnType("jsonb");
            ConfigureAuditColumns(entity);
        });

        modelBuilder.Entity<EvidenceObligationEntity>(entity =>
        {
            entity.ToTable("evidence_obligations");
            entity.HasKey(x => new { x.EvidenceItemId, x.ObligationId });
            entity.HasOne(x => x.EvidenceItem).WithMany(x => x.Obligations).HasForeignKey(x => x.EvidenceItemId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Obligation).WithMany(x => x.EvidenceItems).HasForeignKey(x => x.ObligationId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EvidenceContractEntity>(entity =>
        {
            entity.ToTable("evidence_contracts");
            entity.HasKey(x => new { x.EvidenceItemId, x.ContractId });
            entity.HasOne(x => x.EvidenceItem).WithMany(x => x.Contracts).HasForeignKey(x => x.EvidenceItemId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Contract).WithMany().HasForeignKey(x => x.ContractId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EvidenceControlEntity>(entity =>
        {
            entity.ToTable("evidence_controls");
            entity.HasKey(x => new { x.EvidenceItemId, x.ControlId });
            entity.HasOne(x => x.EvidenceItem).WithMany(x => x.Controls).HasForeignKey(x => x.EvidenceItemId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Control).WithMany().HasForeignKey(x => x.ControlId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EvidenceVendorEntity>(entity =>
        {
            entity.ToTable("evidence_vendors");
            entity.HasKey(x => new { x.EvidenceItemId, x.VendorId });
            entity.HasOne(x => x.EvidenceItem).WithMany(x => x.Vendors).HasForeignKey(x => x.EvidenceItemId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Vendor).WithMany().HasForeignKey(x => x.VendorId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EvidenceEmployeeEntity>(entity =>
        {
            entity.ToTable("evidence_employees");
            entity.HasKey(x => new { x.EvidenceItemId, x.EmployeeId });
            entity.HasOne(x => x.EvidenceItem).WithMany(x => x.Employees).HasForeignKey(x => x.EvidenceItemId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EvidenceFileVersionEntity>(entity =>
        {
            entity.ToTable("evidence_file_versions");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.EvidenceItemId, x.VersionNumber }).IsUnique();
            entity.Property(x => x.FileName).HasMaxLength(240).IsRequired();
            entity.Property(x => x.ContentType).HasMaxLength(160).IsRequired();
            entity.Property(x => x.ValidationStatus).HasMaxLength(80).IsRequired();
            entity.Property(x => x.MalwareScanStatus).HasMaxLength(80).IsRequired();
            entity.HasOne(x => x.EvidenceItem).WithMany(x => x.FileVersions).HasForeignKey(x => x.EvidenceItemId).OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureCmmc(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ControlEntity>(entity =>
        {
            entity.ToTable("controls");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.Framework, x.CmmcLevel });
            entity.Property(x => x.EvidenceExamplesJson).HasColumnType("jsonb");
        });

        modelBuilder.Entity<AssessmentEntity>(entity =>
        {
            entity.ToTable("assessments");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.TenantId, x.Status, x.Level });
            entity.Property(x => x.Name).HasMaxLength(240).HasDefaultValue("CMMC readiness assessment").IsRequired();
            entity.Property(x => x.Framework).HasMaxLength(120).HasDefaultValue("CMMC").IsRequired();
            entity.Property(x => x.OwnerFunction).HasMaxLength(120).HasDefaultValue("Compliance").IsRequired();
            entity.Property(x => x.ContractIdsJson).HasColumnType("jsonb");
            ConfigureAuditColumns(entity);
        });

        modelBuilder.Entity<ControlAssessmentEntity>(entity =>
        {
            entity.ToTable("control_assessments");
            entity.HasKey(x => new { x.AssessmentId, x.ControlId });
            entity.Property(x => x.EvidenceItemIdsJson).HasColumnType("jsonb");
            entity.Property(x => x.TaskIdsJson).HasColumnType("jsonb");
            entity.Property(x => x.AssetIdsJson).HasColumnType("jsonb");
            entity.Property(x => x.PoamItemIdsJson).HasColumnType("jsonb");
            entity.HasOne(x => x.Assessment).WithMany(x => x.Controls).HasForeignKey(x => x.AssessmentId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Control).WithMany().HasForeignKey(x => x.ControlId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PoamItemEntity>(entity =>
        {
            entity.ToTable("poam_items");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.TenantId, x.Status, x.TargetCompletionAt });
            entity.HasIndex(x => new { x.AssessmentId, x.ControlId });
            entity.Property(x => x.OwnerFunction).HasMaxLength(120).HasDefaultValue("Security").IsRequired();
            ConfigureAuditColumns(entity);
        });

        modelBuilder.Entity<PoamEvidenceEntity>(entity =>
        {
            entity.ToTable("poam_evidence");
            entity.HasKey(x => new { x.PoamItemId, x.EvidenceItemId });
            entity.HasOne(x => x.PoamItem).WithMany(x => x.EvidenceItems).HasForeignKey(x => x.PoamItemId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.EvidenceItem).WithMany().HasForeignKey(x => x.EvidenceItemId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AssetEntity>(entity =>
        {
            entity.ToTable("assets");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.TenantId, x.SystemBoundaryId });
            entity.Property(x => x.TagsJson).HasColumnType("jsonb");
            ConfigureAuditColumns(entity);
        });

        modelBuilder.Entity<SystemBoundaryEntity>(entity =>
        {
            entity.ToTable("system_boundaries");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.TenantId, x.Status });
            ConfigureAuditColumns(entity);
        });

        modelBuilder.Entity<SystemBoundaryAssetEntity>(entity =>
        {
            entity.ToTable("system_boundary_assets");
            entity.HasKey(x => new { x.SystemBoundaryId, x.AssetId });
            entity.HasOne(x => x.SystemBoundary).WithMany(x => x.Assets).HasForeignKey(x => x.SystemBoundaryId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Asset).WithMany().HasForeignKey(x => x.AssetId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SystemBoundaryExternalServiceProviderEntity>(entity =>
        {
            entity.ToTable("system_boundary_external_service_providers");
            entity.HasKey(x => new { x.SystemBoundaryId, x.VendorId });
            entity.HasOne(x => x.SystemBoundary).WithMany(x => x.ExternalServiceProviders).HasForeignKey(x => x.SystemBoundaryId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Vendor).WithMany().HasForeignKey(x => x.VendorId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SystemBoundaryEvidenceEntity>(entity =>
        {
            entity.ToTable("system_boundary_evidence");
            entity.HasKey(x => new { x.SystemBoundaryId, x.EvidenceItemId });
            entity.HasOne(x => x.SystemBoundary).WithMany(x => x.EvidenceItems).HasForeignKey(x => x.SystemBoundaryId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.EvidenceItem).WithMany().HasForeignKey(x => x.EvidenceItemId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AnnualAffirmationEntity>(entity =>
        {
            entity.ToTable("annual_affirmations");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.TenantId, x.Status, x.DueAt });
            entity.Property(x => x.EvidenceItemIdsJson).HasColumnType("jsonb");
            ConfigureAuditColumns(entity);
        });
    }

    private static void ConfigureVendors(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<VendorEntity>(entity =>
        {
            entity.ToTable("vendors");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.TenantId, x.Type });
            entity.Property(x => x.Name).HasMaxLength(240).IsRequired();
            ConfigureAuditColumns(entity);
        });

        modelBuilder.Entity<SubcontractorEntity>(entity =>
        {
            entity.ToTable("subcontractors");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.TenantId, x.Name });
            entity.HasIndex(x => new { x.TenantId, x.Uei });
            entity.Property(x => x.RoleDescription).HasMaxLength(160).HasDefaultValue("").IsRequired();
            entity.Property(x => x.SmallBusinessStatus).HasMaxLength(120).HasDefaultValue("Unknown").IsRequired();
            entity.Property(x => x.CmmcStatus).HasMaxLength(120).HasDefaultValue("Unknown").IsRequired();
            entity.Property(x => x.NdaStatus).HasMaxLength(120).HasDefaultValue("NotOnFile").IsRequired();
            entity.Property(x => x.WorksharePercentage).HasPrecision(5, 2);
            ConfigureAuditColumns(entity);
        });

        modelBuilder.Entity<FlowDownClauseEntity>(entity =>
        {
            entity.ToTable("flow_down_clauses");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.SubcontractorId, x.ClauseNumber });
            entity.HasIndex(x => new { x.SubcontractorId, x.ContractId });
            entity.HasIndex(x => new { x.ContractId, x.ClauseNumber });
            entity.Property(x => x.ObligationId).HasMaxLength(160);
            entity.HasOne(x => x.Subcontractor).WithMany(x => x.FlowDownClauses).HasForeignKey(x => x.SubcontractorId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Contract).WithMany().HasForeignKey(x => x.ContractId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ContractClause).WithMany().HasForeignKey(x => x.ContractClauseId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Obligation).WithMany().HasForeignKey(x => x.ObligationId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.SignedEvidenceItem).WithMany().HasForeignKey(x => x.SignedEvidenceItemId).OnDelete(DeleteBehavior.Restrict);
            ConfigureAuditColumns(entity);
        });

        modelBuilder.Entity<ContractSubcontractorEntity>(entity =>
        {
            entity.ToTable("contract_subcontractors");
            entity.HasKey(x => new { x.ContractId, x.SubcontractorId });
            entity.HasOne(x => x.Contract).WithMany(x => x.Subcontractors).HasForeignKey(x => x.ContractId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Subcontractor).WithMany(x => x.Contracts).HasForeignKey(x => x.SubcontractorId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SubcontractorEvidenceEntity>(entity =>
        {
            entity.ToTable("subcontractor_evidence");
            entity.HasKey(x => new { x.SubcontractorId, x.EvidenceItemId });
            entity.HasOne(x => x.Subcontractor).WithMany(x => x.EvidenceItems).HasForeignKey(x => x.SubcontractorId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.EvidenceItem).WithMany().HasForeignKey(x => x.EvidenceItemId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SubcontractorEvidenceRequestEntity>(entity =>
        {
            entity.ToTable("subcontractor_evidence_requests");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.TenantId, x.Status, x.DueDate });
            entity.HasIndex(x => new { x.SubcontractorId, x.DueDate });
            entity.Property(x => x.RequestedItem).HasMaxLength(300).IsRequired();
            entity.Property(x => x.RequestedEvidenceTypesJson).HasColumnType("jsonb");
            entity.Property(x => x.RecipientName).HasMaxLength(160);
            entity.Property(x => x.RecipientEmail).HasMaxLength(320);
            entity.Property(x => x.ObligationId).HasMaxLength(160);
            entity.HasOne(x => x.Subcontractor).WithMany(x => x.EvidenceRequests).HasForeignKey(x => x.SubcontractorId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Obligation).WithMany().HasForeignKey(x => x.ObligationId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.RelatedFlowDownClause).WithMany().HasForeignKey(x => x.RelatedFlowDownClauseId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ReceivedEvidenceItem).WithMany().HasForeignKey(x => x.ReceivedEvidenceItemId).OnDelete(DeleteBehavior.Restrict);
            ConfigureAuditColumns(entity);
        });
    }

    private static void ConfigurePeopleAndLabor(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EmployeeEntity>(entity =>
        {
            entity.ToTable("employees");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.TenantId, x.EmployeeNumber }).IsUnique();
            entity.HasIndex(x => new { x.TenantId, x.Email });
            ConfigureAuditColumns(entity);
        });

        modelBuilder.Entity<TrainingRecordEntity>(entity =>
        {
            entity.ToTable("training_records");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.TenantId, x.EmployeeId, x.Status });
            entity.HasIndex(x => new { x.TenantId, x.ExpiresAt });
            ConfigureAuditColumns(entity);
        });

        modelBuilder.Entity<WageDeterminationEntity>(entity =>
        {
            entity.ToTable("wage_determinations");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.TenantId, x.DeterminationNumber, x.Revision }).IsUnique();
            ConfigureAuditColumns(entity);
        });

        modelBuilder.Entity<LaborCategoryRateEntity>(entity =>
        {
            entity.ToTable("labor_category_rates");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.HourlyWage).HasPrecision(12, 2);
            entity.Property(x => x.FringeBenefitRate).HasPrecision(12, 2);
            entity.HasOne(x => x.WageDetermination).WithMany(x => x.Rates).HasForeignKey(x => x.WageDeterminationId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LaborClassificationEntity>(entity =>
        {
            entity.ToTable("labor_classifications");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.TenantId, x.EmployeeId, x.ContractId });
            ConfigureAuditColumns(entity);
        });

        modelBuilder.Entity<PayrollRecordEntity>(entity =>
        {
            entity.ToTable("payroll_records");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.TenantId, x.EmployeeId, x.PeriodStart, x.PeriodEnd });
            entity.Property(x => x.HoursWorked).HasPrecision(10, 2);
            entity.Property(x => x.WagePaid).HasPrecision(12, 2);
            entity.Property(x => x.FringePaid).HasPrecision(12, 2);
            ConfigureAuditColumns(entity);
        });
    }

    private static void ConfigureReports(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ReportEntity>(entity =>
        {
            entity.ToTable("reports");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.TenantId, x.Type, x.Status });
            entity.Property(x => x.SnapshotJson).HasColumnType("jsonb");
            ConfigureAuditColumns(entity);
        });

        modelBuilder.Entity<ReportContractEntity>(entity =>
        {
            entity.ToTable("report_contracts");
            entity.HasKey(x => new { x.ReportId, x.ContractId });
        });

        modelBuilder.Entity<ReportObligationEntity>(entity =>
        {
            entity.ToTable("report_obligations");
            entity.HasKey(x => new { x.ReportId, x.ObligationId });
        });

        modelBuilder.Entity<ReportEvidenceEntity>(entity =>
        {
            entity.ToTable("report_evidence");
            entity.HasKey(x => new { x.ReportId, x.EvidenceItemId });
        });
    }

    private static void ConfigureAudit(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLogEntryEntity>(entity =>
        {
            entity.ToTable("audit_log_entries");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.TenantId, x.OccurredAt });
            entity.HasIndex(x => new { x.TenantId, x.EntityType, x.EntityId });
            entity.Property(x => x.CorrelationId).HasMaxLength(120);
            entity.Property(x => x.MetadataJson).HasColumnType("jsonb");
        });
    }

    private static void ConfigureAuditColumns<T>(EntityTypeBuilder<T> entity)
        where T : AuditedEntity
    {
        entity.Property(x => x.CreatedAt).IsRequired();
        entity.HasIndex(x => new { x.CreatedAt, x.UpdatedAt });
    }

    private static void ConfigureTenantForeignKeys(ModelBuilder modelBuilder)
    {
        var tenantScopedTypes = modelBuilder.Model.GetEntityTypes()
            .Where(entityType =>
                entityType.ClrType != typeof(TenantEntity) &&
                entityType.ClrType != typeof(AuditLogEntryEntity) &&
                entityType.ClrType != typeof(NoCuiAcknowledgementEntity) &&
                entityType.FindProperty(nameof(UserEntity.TenantId)) is not null &&
                !entityType.GetForeignKeys().Any(foreignKey =>
                    foreignKey.PrincipalEntityType.ClrType == typeof(TenantEntity)))
            .Select(entityType => entityType.ClrType)
            .ToArray();

        foreach (var clrType in tenantScopedTypes)
        {
            modelBuilder.Entity(clrType)
                .HasOne(typeof(TenantEntity))
                .WithMany()
                .HasForeignKey(nameof(UserEntity.TenantId))
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    private static void ApplyPostgresConventions(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                property.SetColumnName(ToSnakeCase(property.Name));

                if (property.ClrType == typeof(string) && property.Name.EndsWith("Json", StringComparison.Ordinal))
                {
                    property.SetColumnType("jsonb");
                }
            }
        }
    }

    private static string ToSnakeCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var builder = new StringBuilder(value.Length + 8);
        for (var i = 0; i < value.Length; i++)
        {
            var current = value[i];
            if (char.IsUpper(current) && i > 0)
            {
                builder.Append('_');
            }

            builder.Append(char.ToLowerInvariant(current));
        }

        return builder.ToString();
    }
}
