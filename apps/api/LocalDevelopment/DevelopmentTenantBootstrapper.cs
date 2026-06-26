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

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
        {
            logger.LogInformation("Development seed data skipped outside Development environment.");
            return;
        }

        var seedDataEnabled = configuration.GetValue("LocalDevelopment:SeedData:Enabled", false);
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

            EnsureControls(dbContext);
            EnsureTenantSeed(dbContext, Alpha);
            EnsureTenantSeed(dbContext, Beta);

            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation(
                "Development seed data ensured for Tenant Alpha {AlphaTenantId} and Tenant Beta {BetaTenantId}. Default development auth tenant={DefaultTenantId}, user={DefaultUserId}.",
                Alpha.TenantId,
                Beta.TenantId,
                tenantId,
                userId);
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
            Reason = $"Local development seed created {seed.Name} as a No-CUI tenant.",
            ApprovalRecordReference = "local-development-seed"
        });
    }

    private static void EnsureUsersAndRoles(GccsDbContext dbContext, LocalSeedTenant seed)
    {
        var users = new[]
        {
            new LocalSeedUser(seed.AdminUserId, "TenantAdmin", RoleCatalog.Admin),
            new LocalSeedUser(seed.ComplianceManagerUserId, "ComplianceManager", RoleCatalog.ComplianceManager),
            new LocalSeedUser(seed.ContributorUserId, "Contributor", RoleCatalog.Contributor),
            new LocalSeedUser(seed.AuditorUserId, "ReadOnlyAuditor", RoleCatalog.Auditor)
        };

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
            Email = $"{seed.Slug}.{user.Label.ToLowerInvariant()}@gccs.local",
            DisplayName = $"{seed.Name} {user.Label}",
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
            Name = $"{seed.Name} access control policy",
            Description = "Synthetic No-CUI local development evidence metadata. No file content is stored.",
            Type = EvidenceType.Policy,
            OwnerFunction = "Security",
            Status = EvidenceStatus.Approved,
            StorageUri = $"local-dev://{seed.Slug}/evidence/access-control-policy.pdf",
            OriginalFileName = $"{seed.Slug}-access-control-policy.pdf",
            ContentType = "application/pdf",
            SizeBytes = 42000,
            UploadValidationStatus = "accepted",
            MalwareScanStatus = "clean",
            EffectiveAt = SeededDate,
            ExpiresAt = ExpirationDate,
            TagsJson = JsonSerializer.Serialize(new[] { "local-dev", seed.Slug, "no-cui", "access-control" }, JsonOptions),
            ApprovedByUserId = seed.ComplianceManagerUserId,
            ApprovedAt = SeededAt,
            Classification = ContentClassification.Unclassified,
            ClassificationSource = ContentClassificationSource.UserSelected,
            ClassificationConfidence = 1.0m,
            ClassificationReviewedByUserId = seed.ComplianceManagerUserId,
            ClassificationReviewedAt = SeededAt,
            ClassificationReason = "Synthetic local development No-CUI evidence metadata.",
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
            "Reviewed local-development evidence supports this control.",
            seed.ComplianceManagerUserId,
            SeededDate);
        EnsureControlAssessment(
            dbContext,
            seed,
            "IA.L1-3.5.1",
            ControlImplementationStatus.PartiallyImplemented,
            AssessmentResult.NotMet,
            [],
            "Local-development seed leaves an identity evidence gap for POA&M testing.",
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
            Weakness = $"{seed.Name} identity evidence gap",
            PlannedRemediation = "Collect synthetic identity provider configuration evidence for local workflow testing.",
            RiskLevel = RiskLevel.Medium,
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
            EntityType = "LocalDevelopmentSeed",
            EntityId = seed.TenantId.ToString(),
            OccurredAt = SeededAt,
            IpAddress = "127.0.0.1",
            UserAgent = "Gccs local development seed",
            CorrelationId = $"local-dev-seed-{seed.Slug}",
            Summary = $"{seed.Name} local development data was seeded.",
            OldValue = null,
            NewValue = JsonSerializer.Serialize(new { seed.TenantId, seed.Name }, JsonOptions),
            MetadataJson = JsonSerializer.Serialize(new Dictionary<string, string>
            {
                ["seed"] = "local-development",
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
        Guid ChecklistId)
    {
        public string Prefix => Slug == "alpha"
            ? "aaaaaaaa-aaaa-aaaa-aaaa-"
            : "bbbbbbbb-bbbb-bbbb-bbbb-";
    }

    private sealed record LocalSeedUser(Guid UserId, string Label, string RoleName);
}
