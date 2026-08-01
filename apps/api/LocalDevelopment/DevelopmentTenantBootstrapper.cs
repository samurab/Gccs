using System.Text.Json;
using Gccs.Application.Tenancy;
using Gccs.Domain.Audit;
using Gccs.Domain.Cmmc;
using Gccs.Domain.Common;
using Gccs.Domain.Compliance;
using Gccs.Domain.Evidence;
using Gccs.Domain.Identity;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Api.LocalDevelopment;

public sealed class DevelopmentTenantBootstrapper(
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    IWebHostEnvironment environment,
    ILogger<DevelopmentTenantBootstrapper> logger) : IHostedService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly DateTimeOffset SeededAt = new(2026, 6, 15, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly SeededDate = new(2026, 6, 15);
    private static readonly DateOnly PoamDueDate = new(2026, 7, 15);
    private static readonly DateOnly ExpirationDate = new(2027, 6, 15);

    private static readonly LocalSeedTenant Alpha = new(
        Guid.Parse("11111111-1111-1111-1111-111111111111"),
        "Tenant Alpha",
        "alpha",
        Guid.Parse("22222222-2222-2222-2222-222222222222"),
        Guid.Parse("22222222-2222-2222-2222-222222222223"),
        Guid.Parse("22222222-2222-2222-2222-222222222224"),
        Guid.Parse("22222222-2222-2222-2222-222222222225"),
        Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"),
        Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2"),
        Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa3"),
        Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa4"),
        Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa5"));

    private static readonly LocalSeedTenant Beta = new(
        Guid.Parse("11111111-1111-1111-1111-111111111112"),
        "Tenant Beta",
        "beta",
        Guid.Parse("22222222-2222-2222-2222-222222222232"),
        Guid.Parse("22222222-2222-2222-2222-222222222233"),
        Guid.Parse("22222222-2222-2222-2222-222222222234"),
        Guid.Parse("22222222-2222-2222-2222-222222222235"),
        Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1"),
        Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2"),
        Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb3"),
        Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb4"),
        Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb5"));

    private static readonly LocalSeedTenant Northstar = new(
        Guid.Parse("11111111-1111-1111-1111-111111111113"),
        "Northstar Precision Systems",
        "northstar",
        Guid.Parse("22222222-2222-2222-2222-222222222242"),
        Guid.Parse("22222222-2222-2222-2222-222222222243"),
        Guid.Parse("22222222-2222-2222-2222-222222222244"),
        Guid.Parse("22222222-2222-2222-2222-222222222245"),
        Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc1"),
        Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc2"),
        Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc3"),
        Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc4"),
        Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc5"),
        IsMarketingDemo: true,
        Users:
        [
            new(Guid.Parse("22222222-2222-2222-2222-222222222242"), "TenantAdmin", RoleCatalog.Admin, "alex.morgan.northstar@example.com", "Alex Morgan"),
            new(Guid.Parse("22222222-2222-2222-2222-222222222243"), "ComplianceManager", RoleCatalog.ComplianceManager, "priya.shah.northstar@example.com", "Priya Shah"),
            new(Guid.Parse("22222222-2222-2222-2222-222222222244"), "Contributor", RoleCatalog.Contributor, "daniel.brooks.northstar@example.com", "Daniel Brooks"),
            new(Guid.Parse("22222222-2222-2222-2222-222222222245"), "ReadOnlyAuditor", RoleCatalog.Auditor, "elena.ortiz.northstar@example.com", "Elena Ortiz")
        ]);

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
        {
            logger.LogInformation("Development seed data skipped outside Development environment.");
            return;
        }

        var seedDataEnabled =
            configuration.GetValue("LocalDevelopment:SeedData:Enabled", false) ||
            (configuration.GetValue("LocalDependencies:Enabled", false) &&
             configuration.GetValue("LocalDependencies:SeedData:Enabled", false));
        if (!seedDataEnabled)
        {
            return;
        }

        var developmentAuthEnabled = configuration.GetValue("Security:DevelopmentAuth:Enabled", true);
        if (!developmentAuthEnabled)
        {
            return;
        }

        var tenantIdValue = configuration.GetValue(
            "Security:DevelopmentAuth:DefaultTenantId",
            "11111111-1111-1111-1111-111111111111");
        var userIdValue = configuration.GetValue(
            "Security:DevelopmentAuth:DefaultUserId",
            "22222222-2222-2222-2222-222222222222");

        if (!Guid.TryParse(tenantIdValue, out var tenantId) || !Guid.TryParse(userIdValue, out var userId))
        {
            logger.LogWarning("Development tenant bootstrap skipped because development auth IDs are not valid GUIDs.");
            return;
        }

        try
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();

            if (!await dbContext.Database.CanConnectAsync(cancellationToken))
            {
                logger.LogWarning("Development tenant bootstrap skipped because the database is not reachable.");
                return;
            }

            var marketingDemoEnabled = configuration.GetValue("MarketingDemo:Enabled", false);
            if (marketingDemoEnabled)
            {
                ValidateMarketingDemoPreflight(dbContext, Northstar);
            }

            EnsureControls(dbContext);
            EnsureTenantSeed(dbContext, Alpha);
            EnsureTenantSeed(dbContext, Beta);
            if (marketingDemoEnabled)
            {
                EnsureTenantSeed(dbContext, Northstar);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation(
                "Development seed data ensured for Tenant Alpha {AlphaTenantId} and Tenant Beta {BetaTenantId}. Default development auth tenant={DefaultTenantId}, user={DefaultUserId}.",
                Alpha.TenantId,
                Beta.TenantId,
                tenantId,
                userId);
            if (marketingDemoEnabled)
            {
                logger.LogInformation(
                    "FeDril marketing demonstration data ensured for Northstar Precision Systems {NorthstarTenantId}.",
                    Northstar.TenantId);
            }
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Development tenant bootstrap skipped because tenant creation failed.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static void EnsureTenantSeed(GccsDbContext dbContext, LocalSeedTenant seed)
    {
        EnsureTenant(dbContext, seed);
        EnsureModeHistory(dbContext, seed);
        EnsureUsersAndRoles(dbContext, seed);
        EnsureEvidence(dbContext, seed);
        EnsureAssessment(dbContext, seed);
        EnsurePoam(dbContext, seed);
        EnsureAuditLogs(dbContext, seed);
        EnsureChecklist(dbContext, seed);
    }

    private static void ValidateMarketingDemoPreflight(GccsDbContext dbContext, LocalSeedTenant seed)
    {
        if (!seed.IsMarketingDemo || seed.Users is null)
        {
            throw new InvalidOperationException("Marketing demonstration preflight requires an explicit marketing seed definition.");
        }

        ValidateExistingIdentifier(
            dbContext.Tenants.AsNoTracking().SingleOrDefault(tenant => tenant.Id == seed.TenantId),
            tenant =>
                tenant.Name == seed.Name &&
                tenant.Status == TenantStatus.Active &&
                tenant.DataPosture == TenantDataPosture.NoCui &&
                tenant.TrialEndsAt == null &&
                tenant.CreatedAt == SeededAt &&
                tenant.CreatedByUserId == seed.AdminUserId,
            "tenant",
            seed.TenantId);

        foreach (var user in seed.Users)
        {
            var expectedEmail = user.Email ?? $"{seed.Slug}.{user.Label.ToLowerInvariant()}@gccs.local";
            var expectedDisplayName = user.DisplayName ?? $"{seed.Name} {user.Label}";
            ValidateExistingIdentifier(
                dbContext.Users.AsNoTracking().SingleOrDefault(candidate => candidate.Id == user.UserId),
                candidate =>
                    candidate.TenantId == seed.TenantId &&
                    string.Equals(candidate.Email, expectedEmail, StringComparison.OrdinalIgnoreCase) &&
                    candidate.DisplayName == expectedDisplayName &&
                    candidate.Status == UserStatus.Active &&
                    candidate.MfaEnabled &&
                    candidate.CreatedAt == SeededAt &&
                    candidate.CreatedByUserId == seed.AdminUserId,
                "user",
                user.UserId);

            var userWithExpectedEmail = dbContext.Users
                .AsNoTracking()
                .Where(candidate => candidate.TenantId == seed.TenantId)
                .AsEnumerable()
                .SingleOrDefault(candidate => string.Equals(candidate.Email, expectedEmail, StringComparison.OrdinalIgnoreCase));
            if (userWithExpectedEmail is not null && userWithExpectedEmail.Id != user.UserId)
            {
                ThrowMarketingDemoCollision("user email", userWithExpectedEmail.Id);
            }

            var roleId = SeedGuid(seed.Prefix, RoleOffset(user.RoleName));
            ValidateExistingIdentifier(
                dbContext.Roles.AsNoTracking().SingleOrDefault(role => role.Id == roleId),
                role =>
                    role.TenantId == seed.TenantId &&
                    role.Name == user.RoleName &&
                    role.CreatedAt == SeededAt &&
                    role.CreatedByUserId == seed.AdminUserId,
                "role",
                roleId);

            var roleWithExpectedName = dbContext.Roles
                .AsNoTracking()
                .SingleOrDefault(role => role.TenantId == seed.TenantId && role.Name == user.RoleName);
            if (roleWithExpectedName is not null && roleWithExpectedName.Id != roleId)
            {
                ThrowMarketingDemoCollision("role name", roleWithExpectedName.Id);
            }

            var membershipId = SeedGuid(seed.Prefix, MembershipOffset(user.Label));
            ValidateExistingIdentifier(
                dbContext.TenantMemberships.AsNoTracking().SingleOrDefault(membership => membership.Id == membershipId),
                membership =>
                    membership.TenantId == seed.TenantId &&
                    membership.UserId == user.UserId &&
                    membership.Status == MembershipStatus.Active &&
                    membership.RoleName == user.RoleName &&
                    membership.CreatedAt == SeededAt &&
                    membership.CreatedByUserId == seed.AdminUserId,
                "tenant membership",
                membershipId);

            var existingMembership = dbContext.TenantMemberships
                .AsNoTracking()
                .SingleOrDefault(membership => membership.TenantId == seed.TenantId && membership.UserId == user.UserId);
            if (existingMembership is not null && existingMembership.Id != membershipId)
            {
                ThrowMarketingDemoCollision("tenant membership", existingMembership.Id);
            }
        }

        var modeHistoryId = SeedGuid(seed.Prefix, 11);
        var expectedModeHistory = dbContext.TenantDataHandlingModeHistory
            .AsNoTracking()
            .SingleOrDefault(history => history.Id == modeHistoryId);
        ValidateExistingIdentifier(
            expectedModeHistory,
            history =>
                history.TenantId == seed.TenantId &&
                history.PreviousMode == null &&
                history.NewMode == TenantDataPosture.NoCui &&
                history.ActorUserId == seed.AdminUserId &&
                history.ChangedAt == SeededAt &&
                history.Reason == $"FeDril marketing demonstration seed created {seed.Name} as a No-CUI tenant." &&
                history.ApprovalRecordReference == "fedril-marketing-demo-seed",
            "data-handling mode history",
            modeHistoryId);

        if (expectedModeHistory is null &&
            dbContext.TenantDataHandlingModeHistory.AsNoTracking().Any(history => history.TenantId == seed.TenantId))
        {
            ThrowMarketingDemoCollision("data-handling mode history", modeHistoryId);
        }

        ValidateExistingIdentifier(
            dbContext.EvidenceItems.AsNoTracking().SingleOrDefault(evidence => evidence.Id == seed.EvidenceItemId),
            evidence =>
                evidence.TenantId == seed.TenantId &&
                evidence.Name == "Northstar quarterly access review summary" &&
                evidence.OriginalFileName == "northstar-quarterly-access-review-summary.pdf" &&
                evidence.StorageUri == null &&
                evidence.UploadValidationStatus == "metadata-only" &&
                evidence.MalwareScanStatus == "not-applicable-metadata-only" &&
                evidence.Classification == ContentClassification.Unclassified &&
                evidence.ClassificationIsApprovedDemoContent &&
                evidence.CreatedAt == SeededAt &&
                evidence.CreatedByUserId == seed.ContributorUserId,
            "evidence item",
            seed.EvidenceItemId);

        ValidateExistingIdentifier(
            dbContext.Assessments.AsNoTracking().SingleOrDefault(assessment => assessment.Id == seed.AssessmentId),
            assessment =>
                assessment.TenantId == seed.TenantId &&
                assessment.Name == $"{seed.Name} Level 1 readiness" &&
                assessment.Type == AssessmentType.Readiness &&
                assessment.Level == CmmcLevel.Level1 &&
                assessment.Framework == "CMMC Level 1 / FAR 52.204-21" &&
                assessment.CreatedAt == SeededAt &&
                assessment.CreatedByUserId == seed.ComplianceManagerUserId,
            "assessment",
            seed.AssessmentId);

        ValidateExistingIdentifier(
            dbContext.PoamItems.AsNoTracking().SingleOrDefault(poam => poam.Id == seed.PoamItemId),
            poam =>
                poam.TenantId == seed.TenantId &&
                poam.AssessmentId == seed.AssessmentId &&
                poam.ControlId == "IA.L1-3.5.1" &&
                poam.Weakness == "Quarterly privileged-access review evidence is incomplete" &&
                poam.CreatedAt == SeededAt &&
                poam.CreatedByUserId == seed.ComplianceManagerUserId,
            "POA&M item",
            seed.PoamItemId);

        ValidateExistingIdentifier(
            dbContext.AuditLogEntries.AsNoTracking().SingleOrDefault(audit => audit.Id == seed.AuditLogId),
            audit =>
                audit.TenantId == seed.TenantId &&
                audit.ActorUserId == seed.ComplianceManagerUserId &&
                audit.Action == AuditAction.Created &&
                audit.EntityType == "MarketingDemoSeed" &&
                audit.EntityId == seed.TenantId.ToString() &&
                audit.OccurredAt == SeededAt &&
                audit.CorrelationId == $"fedril-demo-seed-{seed.Slug}",
            "audit log entry",
            seed.AuditLogId);

        ValidateExistingIdentifier(
            dbContext.CuiReadyApprovalChecklists.AsNoTracking().SingleOrDefault(checklist => checklist.Id == seed.ChecklistId),
            checklist =>
                checklist.TenantId == seed.TenantId &&
                checklist.Version == 1 &&
                checklist.State == CuiReadyChecklistState.Draft &&
                checklist.CreatedAt == SeededAt &&
                checklist.CreatedByUserId == seed.AdminUserId,
            "CUI-ready approval checklist",
            seed.ChecklistId);

        ValidateExistingIdentifier(
            dbContext.CuiReadyApprovalChecklistItems.AsNoTracking().SingleOrDefault(item => item.Id == SeedGuid(seed.Prefix, 401)),
            item =>
                item.ChecklistId == seed.ChecklistId &&
                item.ItemKey == "data-handling-notice" &&
                item.Section == "Data handling notice",
            "CUI-ready approval checklist item",
            SeedGuid(seed.Prefix, 401));
        ValidateExistingIdentifier(
            dbContext.CuiReadyApprovalChecklistItems.AsNoTracking().SingleOrDefault(item => item.Id == SeedGuid(seed.Prefix, 402)),
            item =>
                item.ChecklistId == seed.ChecklistId &&
                item.ItemKey == "audit-logging" &&
                item.Section == "Audit logging",
            "CUI-ready approval checklist item",
            SeedGuid(seed.Prefix, 402));
    }

    private static void ValidateExistingIdentifier<TEntity>(
        TEntity? existing,
        Func<TEntity, bool> isExpectedRecord,
        string entityType,
        Guid identifier)
        where TEntity : class
    {
        if (existing is not null && !isExpectedRecord(existing))
        {
            ThrowMarketingDemoCollision(entityType, identifier);
        }
    }

    private static void ThrowMarketingDemoCollision(string entityType, Guid identifier) =>
        throw new InvalidOperationException(
            $"FeDril marketing demonstration seed identifier collision for {entityType} '{identifier}'.");

    private static void EnsureTenant(GccsDbContext dbContext, LocalSeedTenant seed)
    {
        var tenant = dbContext.Tenants.Local.SingleOrDefault(item => item.Id == seed.TenantId) ??
            dbContext.Tenants.SingleOrDefault(item => item.Id == seed.TenantId);
        if (tenant is null)
        {
            dbContext.Tenants.Add(new TenantEntity
            {
                Id = seed.TenantId,
                Name = seed.Name,
                Status = TenantStatus.Active,
                DataPosture = TenantDataPosture.NoCui,
                TrialEndsAt = null,
                CreatedAt = SeededAt,
                CreatedByUserId = seed.AdminUserId
            });
            return;
        }

        if (seed.IsMarketingDemo)
        {
            return;
        }

        tenant.Name = seed.Name;
        tenant.Status = TenantStatus.Active;
        tenant.DataPosture = TenantDataPosture.NoCui;
    }

    private static void EnsureModeHistory(GccsDbContext dbContext, LocalSeedTenant seed)
    {
        if (dbContext.TenantDataHandlingModeHistory.Any(history => history.TenantId == seed.TenantId))
        {
            return;
        }

        dbContext.TenantDataHandlingModeHistory.Add(new TenantDataHandlingModeHistoryEntity
        {
            Id = SeedGuid(seed.Prefix, 11),
            TenantId = seed.TenantId,
            PreviousMode = null,
            NewMode = TenantDataPosture.NoCui,
            ActorUserId = seed.AdminUserId,
            ChangedAt = SeededAt,
            Reason = seed.IsMarketingDemo
                ? $"FeDril marketing demonstration seed created {seed.Name} as a No-CUI tenant."
                : $"Local development seed created {seed.Name} as a No-CUI tenant.",
            ApprovalRecordReference = seed.IsMarketingDemo ? "fedril-marketing-demo-seed" : "local-development-seed"
        });
    }

    private static void EnsureUsersAndRoles(GccsDbContext dbContext, LocalSeedTenant seed)
    {
        var users = seed.Users ??
        [
            new LocalSeedUser(seed.AdminUserId, "TenantAdmin", RoleCatalog.Admin),
            new LocalSeedUser(seed.ComplianceManagerUserId, "ComplianceManager", RoleCatalog.ComplianceManager),
            new LocalSeedUser(seed.ContributorUserId, "Contributor", RoleCatalog.Contributor),
            new LocalSeedUser(seed.AuditorUserId, "ReadOnlyAuditor", RoleCatalog.Auditor)
        ];

        foreach (var user in users)
        {
            EnsureUser(dbContext, seed, user);
            var role = EnsureRole(dbContext, seed, user.RoleName);
            EnsureMembership(dbContext, seed, user);
            EnsureUserRole(dbContext, user.UserId, role.Id);
        }
    }

    private static void EnsureUser(GccsDbContext dbContext, LocalSeedTenant seed, LocalSeedUser user)
    {
        if (dbContext.Users.Any(item => item.Id == user.UserId))
        {
            return;
        }

        dbContext.Users.Add(new UserEntity
        {
            Id = user.UserId,
            TenantId = seed.TenantId,
            Email = user.Email ?? $"{seed.Slug}.{user.Label.ToLowerInvariant()}@gccs.local",
            DisplayName = user.DisplayName ?? $"{seed.Name} {user.Label}",
            Status = UserStatus.Active,
            MfaEnabled = true,
            LastSignedInAt = SeededAt,
            CreatedAt = SeededAt,
            CreatedByUserId = seed.AdminUserId
        });
    }

    private static RoleEntity EnsureRole(GccsDbContext dbContext, LocalSeedTenant seed, string roleName)
    {
        var role = dbContext.Roles.Local.SingleOrDefault(item => item.TenantId == seed.TenantId && item.Name == roleName) ??
            dbContext.Roles.SingleOrDefault(item => item.TenantId == seed.TenantId && item.Name == roleName);
        if (role is null)
        {
            role = new RoleEntity
            {
                Id = SeedGuid(seed.Prefix, RoleOffset(roleName)),
                TenantId = seed.TenantId,
                Name = roleName,
                CreatedAt = SeededAt,
                CreatedByUserId = seed.AdminUserId
            };
            dbContext.Roles.Add(role);
        }

        foreach (var permission in RoleCatalog.GetPermissions(roleName))
        {
            var exists = dbContext.Set<RolePermissionEntity>().Local.Any(item => item.RoleId == role.Id && item.Permission == permission) ||
                dbContext.Set<RolePermissionEntity>().Any(item => item.RoleId == role.Id && item.Permission == permission);
            if (!exists)
            {
                dbContext.Set<RolePermissionEntity>().Add(new RolePermissionEntity
                {
                    RoleId = role.Id,
                    Permission = permission
                });
            }
        }

        return role;
    }

    private static void EnsureMembership(GccsDbContext dbContext, LocalSeedTenant seed, LocalSeedUser user)
    {
        var membership = dbContext.TenantMemberships.Local.SingleOrDefault(item => item.TenantId == seed.TenantId && item.UserId == user.UserId) ??
            dbContext.TenantMemberships.SingleOrDefault(item => item.TenantId == seed.TenantId && item.UserId == user.UserId);
        if (membership is null)
        {
            dbContext.TenantMemberships.Add(new TenantMembershipEntity
            {
                Id = SeedGuid(seed.Prefix, MembershipOffset(user.Label)),
                TenantId = seed.TenantId,
                UserId = user.UserId,
                Status = MembershipStatus.Active,
                RoleName = user.RoleName,
                LastAccessedAt = SeededAt,
                CreatedAt = SeededAt,
                CreatedByUserId = seed.AdminUserId
            });
            return;
        }

        if (seed.IsMarketingDemo)
        {
            return;
        }

        membership.Status = MembershipStatus.Active;
        membership.RoleName = user.RoleName;
    }

    private static void EnsureUserRole(GccsDbContext dbContext, Guid userId, Guid roleId)
    {
        var exists = dbContext.Set<UserRoleEntity>().Local.Any(item => item.UserId == userId && item.RoleId == roleId) ||
            dbContext.Set<UserRoleEntity>().Any(item => item.UserId == userId && item.RoleId == roleId);
        if (!exists)
        {
            dbContext.Set<UserRoleEntity>().Add(new UserRoleEntity { UserId = userId, RoleId = roleId });
        }
    }

    private static void EnsureControls(GccsDbContext dbContext)
    {
        AddControlIfMissing(
            dbContext,
            "AC.L1-3.1.1",
            "Access Control",
            "Limit system access to authorized users, processes, and devices.",
            "Determine whether authorized access is identified and enforced.",
            ["Access control policy", "User access review"]);
        AddControlIfMissing(
            dbContext,
            "IA.L1-3.5.1",
            "Identification and Authentication",
            "Identify information system users, processes, and devices.",
            "Determine whether identities are uniquely assigned and managed.",
            ["Identity provider configuration", "MFA screenshot"]);
    }

    private static void AddControlIfMissing(
        GccsDbContext dbContext,
        string controlId,
        string family,
        string requirement,
        string objective,
        IReadOnlyList<string> examples)
    {
        if (dbContext.Controls.Any(control => control.Id == controlId))
        {
            return;
        }

        dbContext.Controls.Add(new ControlEntity
        {
            Id = controlId,
            Framework = ControlFramework.Cmmc,
            CmmcLevel = CmmcLevel.Level1,
            Family = family,
            Title = controlId.Contains("3.1", StringComparison.Ordinal) ? "Authorized access control" : "Identity management",
            Requirement = requirement,
            AssessmentObjective = objective,
            EvidenceExamplesJson = JsonSerializer.Serialize(examples, JsonOptions),
            SourceName = "CMMC Level 1 local development seed",
            SourceUrl = "https://dodcio.defense.gov/CMMC/Resources-Documentation/",
            SourceLastReviewedAt = SeededDate,
            SourceEffectiveAt = SeededDate,
            SourceConfidence = "high",
            SourceRequiresExpertReview = false
        });
    }

    private static void EnsureEvidence(GccsDbContext dbContext, LocalSeedTenant seed)
    {
        if (dbContext.EvidenceItems.Any(evidence => evidence.Id == seed.EvidenceItemId))
        {
            return;
        }

        dbContext.EvidenceItems.Add(new EvidenceItemEntity
        {
            Id = seed.EvidenceItemId,
            TenantId = seed.TenantId,
            Name = seed.IsMarketingDemo
                ? "Northstar quarterly access review summary"
                : $"{seed.Name} access control policy",
            Description = seed.IsMarketingDemo
                ? "Fictional, non-sensitive FeDril demonstration evidence metadata. No file content is stored."
                : "Synthetic No-CUI local development evidence metadata. No file content is stored.",
            Type = EvidenceType.Policy,
            OwnerFunction = "Security",
            Status = EvidenceStatus.Approved,
            StorageUri = seed.IsMarketingDemo ? null : $"local-dev://{seed.Slug}/evidence/access-control-policy.pdf",
            OriginalFileName = seed.IsMarketingDemo
                ? "northstar-quarterly-access-review-summary.pdf"
                : $"{seed.Slug}-access-control-policy.pdf",
            ContentType = "application/pdf",
            SizeBytes = seed.IsMarketingDemo ? 18432 : 42000,
            UploadValidationStatus = seed.IsMarketingDemo ? "metadata-only" : "accepted",
            MalwareScanStatus = seed.IsMarketingDemo ? "not-applicable-metadata-only" : "clean",
            EffectiveAt = SeededDate,
            ExpiresAt = ExpirationDate,
            TagsJson = JsonSerializer.Serialize(
                seed.IsMarketingDemo
                    ? new[] { "fictional-demo", "northstar", "no-cui", "access-review" }
                    : new[] { "local-dev", seed.Slug, "no-cui", "access-control" },
                JsonOptions),
            ApprovedByUserId = seed.ComplianceManagerUserId,
            ApprovedAt = SeededAt,
            Classification = ContentClassification.Unclassified,
            ClassificationSource = ContentClassificationSource.UserSelected,
            ClassificationConfidence = 1.0m,
            ClassificationReviewedByUserId = seed.ComplianceManagerUserId,
            ClassificationReviewedAt = SeededAt,
            ClassificationReason = seed.IsMarketingDemo
                ? "Fictional, non-sensitive No-CUI evidence metadata for a FeDril marketing demonstration."
                : "Synthetic local development No-CUI evidence metadata.",
            ClassificationIsApprovedDemoContent = true,
            CreatedAt = SeededAt,
            CreatedByUserId = seed.ContributorUserId
        });
        dbContext.Set<EvidenceControlEntity>().Add(new EvidenceControlEntity
        {
            EvidenceItemId = seed.EvidenceItemId,
            ControlId = "AC.L1-3.1.1"
        });
    }

    private static void EnsureAssessment(GccsDbContext dbContext, LocalSeedTenant seed)
    {
        if (!dbContext.Assessments.Any(assessment => assessment.Id == seed.AssessmentId))
        {
            dbContext.Assessments.Add(new AssessmentEntity
            {
                Id = seed.AssessmentId,
                TenantId = seed.TenantId,
                Name = $"{seed.Name} Level 1 readiness",
                Type = AssessmentType.Readiness,
                Level = CmmcLevel.Level1,
                Framework = "CMMC Level 1 / FAR 52.204-21",
                Status = AssessmentStatus.InProgress,
                StartedAt = SeededDate,
                AffirmationDueAt = ExpirationDate,
                OwnerFunction = "Security",
                ContractIdsJson = "[]",
                CreatedAt = SeededAt,
                CreatedByUserId = seed.ComplianceManagerUserId
            });
        }

        EnsureControlAssessment(
            dbContext,
            seed,
            "AC.L1-3.1.1",
            ControlImplementationStatus.Implemented,
            AssessmentResult.Met,
            [seed.EvidenceItemId],
            seed.IsMarketingDemo
                ? "Reviewed fictional demonstration evidence supports this readiness record."
                : "Reviewed local-development evidence supports this control.",
            seed.ComplianceManagerUserId,
            SeededDate);
        EnsureControlAssessment(
            dbContext,
            seed,
            "IA.L1-3.5.1",
            ControlImplementationStatus.PartiallyImplemented,
            AssessmentResult.NotMet,
            [],
            seed.IsMarketingDemo
                ? "Fictional demonstration data leaves an identity evidence gap for remediation tracking."
                : "Local-development seed leaves an identity evidence gap for POA&M testing.",
            null,
            null);
    }

    private static void EnsureControlAssessment(
        GccsDbContext dbContext,
        LocalSeedTenant seed,
        string controlId,
        ControlImplementationStatus status,
        AssessmentResult result,
        IReadOnlyList<Guid> evidenceItemIds,
        string notes,
        Guid? assessedByUserId,
        DateOnly? assessedAt)
    {
        var control = dbContext.ControlAssessments.Local.SingleOrDefault(item => item.AssessmentId == seed.AssessmentId && item.ControlId == controlId) ??
            dbContext.ControlAssessments.SingleOrDefault(item => item.AssessmentId == seed.AssessmentId && item.ControlId == controlId);
        if (control is null)
        {
            dbContext.ControlAssessments.Add(new ControlAssessmentEntity
            {
                AssessmentId = seed.AssessmentId,
                ControlId = controlId,
                ImplementationStatus = status,
                Result = result,
                EvidenceItemIdsJson = JsonSerializer.Serialize(evidenceItemIds, JsonOptions),
                TaskIdsJson = "[]",
                AssetIdsJson = "[]",
                PoamItemIdsJson = controlId == "IA.L1-3.5.1"
                    ? JsonSerializer.Serialize(new[] { seed.PoamItemId }, JsonOptions)
                    : "[]",
                Notes = notes,
                AssessedByUserId = assessedByUserId,
                AssessedAt = assessedAt,
                OwnerFunction = "Security"
            });
        }
    }

    private static void EnsurePoam(GccsDbContext dbContext, LocalSeedTenant seed)
    {
        if (dbContext.PoamItems.Any(poam => poam.Id == seed.PoamItemId))
        {
            return;
        }

        dbContext.PoamItems.Add(new PoamItemEntity
        {
            Id = seed.PoamItemId,
            TenantId = seed.TenantId,
            AssessmentId = seed.AssessmentId,
            ControlId = "IA.L1-3.5.1",
            Weakness = seed.IsMarketingDemo
                ? "Quarterly privileged-access review evidence is incomplete"
                : $"{seed.Name} identity evidence gap",
            PlannedRemediation = seed.IsMarketingDemo
                ? "Complete the fictional access review, document the reviewer decision, and associate the approved evidence metadata."
                : "Collect synthetic identity provider configuration evidence for local workflow testing.",
            RiskLevel = seed.IsMarketingDemo ? RiskLevel.High : RiskLevel.Medium,
            Status = PoamStatus.Open,
            OwnerUserId = seed.ComplianceManagerUserId,
            OwnerFunction = "Security",
            TargetCompletionAt = PoamDueDate,
            CreatedAt = SeededAt,
            CreatedByUserId = seed.ComplianceManagerUserId
        });
    }

    private static void EnsureAuditLogs(GccsDbContext dbContext, LocalSeedTenant seed)
    {
        if (dbContext.AuditLogEntries.Any(audit => audit.Id == seed.AuditLogId))
        {
            return;
        }

        dbContext.AuditLogEntries.Add(new AuditLogEntryEntity
        {
            Id = seed.AuditLogId,
            TenantId = seed.TenantId,
            ActorUserId = seed.ComplianceManagerUserId,
            Action = AuditAction.Created,
            EntityType = seed.IsMarketingDemo ? "MarketingDemoSeed" : "LocalDevelopmentSeed",
            EntityId = seed.TenantId.ToString(),
            OccurredAt = SeededAt,
            IpAddress = "127.0.0.1",
            UserAgent = seed.IsMarketingDemo ? "FeDril marketing demonstration seed" : "Gccs local development seed",
            CorrelationId = seed.IsMarketingDemo ? $"fedril-demo-seed-{seed.Slug}" : $"local-dev-seed-{seed.Slug}",
            Summary = seed.IsMarketingDemo
                ? $"Fictional FeDril demonstration data was seeded for {seed.Name}."
                : $"{seed.Name} local development data was seeded.",
            OldValue = null,
            NewValue = JsonSerializer.Serialize(new { seed.TenantId, seed.Name }, JsonOptions),
            MetadataJson = JsonSerializer.Serialize(new Dictionary<string, string>
            {
                ["seed"] = seed.IsMarketingDemo ? "fedril-marketing-demo" : "local-development",
                ["tenant"] = seed.Name,
                ["dataPosture"] = TenantDataPosture.NoCui.ToString()
            }, JsonOptions)
        });
    }

    private static void EnsureChecklist(GccsDbContext dbContext, LocalSeedTenant seed)
    {
        if (dbContext.CuiReadyApprovalChecklists.Any(checklist => checklist.Id == seed.ChecklistId))
        {
            return;
        }

        dbContext.CuiReadyApprovalChecklists.Add(new CuiReadyApprovalChecklistEntity
        {
            Id = seed.ChecklistId,
            TenantId = seed.TenantId,
            Version = 1,
            State = CuiReadyChecklistState.Draft,
            CreatedAt = SeededAt,
            CreatedByUserId = seed.AdminUserId
        });
        dbContext.CuiReadyApprovalChecklistItems.AddRange(
            new CuiReadyApprovalChecklistItemEntity
            {
                Id = SeedGuid(seed.Prefix, 401),
                ChecklistId = seed.ChecklistId,
                ItemKey = "data-handling-notice",
                Section = "Data handling notice",
                Description = "Synthetic local-development checklist item for No-CUI/CUI-ready approval workflow testing.",
                IsRequired = true,
                Status = CuiReadyChecklistItemStatus.InProgress,
                Owner = "Security",
                Notes = "Local development seed only. Not an approval to store CUI."
            },
            new CuiReadyApprovalChecklistItemEntity
            {
                Id = SeedGuid(seed.Prefix, 402),
                ChecklistId = seed.ChecklistId,
                ItemKey = "audit-logging",
                Section = "Audit logging",
                Description = "Verify audit events are visible for the current tenant only.",
                IsRequired = true,
                Status = CuiReadyChecklistItemStatus.NotStarted,
                Owner = "Compliance",
                Notes = "Synthetic local development checklist item."
            });
    }

    private static Guid SeedGuid(string prefix, int suffix) =>
        Guid.Parse($"{prefix}{suffix:D12}");

    private static int RoleOffset(string roleName) =>
        roleName switch
        {
            RoleCatalog.Admin => 101,
            RoleCatalog.ComplianceManager => 102,
            RoleCatalog.Contributor => 103,
            RoleCatalog.Auditor => 104,
            _ => 199
        };

    private static int MembershipOffset(string label) =>
        label switch
        {
            "TenantAdmin" => 201,
            "ComplianceManager" => 202,
            "Contributor" => 203,
            "ReadOnlyAuditor" => 204,
            _ => 299
        };

    private sealed record LocalSeedTenant(
        Guid TenantId,
        string Name,
        string Slug,
        Guid AdminUserId,
        Guid ComplianceManagerUserId,
        Guid ContributorUserId,
        Guid AuditorUserId,
        Guid AssessmentId,
        Guid EvidenceItemId,
        Guid PoamItemId,
        Guid AuditLogId,
        Guid ChecklistId,
        bool IsMarketingDemo = false,
        IReadOnlyList<LocalSeedUser>? Users = null)
    {
        public string Prefix => Slug switch
        {
            "alpha" => "aaaaaaaa-aaaa-aaaa-aaaa-",
            "beta" => "bbbbbbbb-bbbb-bbbb-bbbb-",
            "northstar" => "cccccccc-cccc-cccc-cccc-",
            _ => throw new InvalidOperationException($"Unsupported local seed tenant slug '{Slug}'.")
        };
    }

    private sealed record LocalSeedUser(
        Guid UserId,
        string Label,
        string RoleName,
        string? Email = null,
        string? DisplayName = null);
}
