using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Security.Cryptography;
using System.Text;
using Gccs.Application.Audit;
using Gccs.Application.Identity;
using Gccs.Application.Tenancy;
using Gccs.Domain.Audit;
using Gccs.Domain.Identity;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Audit;
using Gccs.Infrastructure.Identity;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class PlatformTenantProvisioningTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;

    public PlatformTenantProvisioningTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Platform_operator_creates_pending_no_cui_tenant_and_owner_invitation_without_user_activation()
    {
        await using var factory = CreateFactory("platform-provision-success");
        using var client = factory.CreateClient();
        using var request = CreateProvisionRequest(
            PilotRequest(),
            "provision-pilot-003",
            includePlatformPermission: true);

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PlatformTenantProvisioningResultDto>(body, JsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(result);
        Assert.Equal(TenantStatus.PendingActivation, result.TenantStatus);
        Assert.Equal(TenantOnboardingStatus.PendingOwnerAcceptance, result.OnboardingStatus);
        Assert.Equal(TenantDataPosture.NoCui, result.DataHandlingMode);
        Assert.Equal(TenantInvitationStatus.Pending, result.InvitationStatus);
        Assert.DoesNotContain("invitationToken", body, StringComparison.OrdinalIgnoreCase);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.False(await dbContext.Users.AnyAsync());
        Assert.False(await dbContext.TenantMemberships.AnyAsync());
        Assert.True(await dbContext.Roles.AnyAsync(role => role.TenantId == result.TenantId && role.Name == RoleCatalog.Owner));
        Assert.True(await dbContext.TenantDataHandlingModeHistory.AnyAsync(history =>
            history.TenantId == result.TenantId && history.NewMode == TenantDataPosture.NoCui));
        var subscription = await dbContext.TenantSubscriptions.SingleAsync(item => item.TenantId == result.TenantId);
        Assert.Equal(SubscriptionPlan.PilotEvaluation, subscription.Plan);
        Assert.Equal(SubscriptionStatus.Pending, subscription.Status);
        Assert.Equal("PILOT-EVALUATION", subscription.PlanCode);
        Assert.Equal(3, await dbContext.AuditLogEntries.CountAsync(audit => audit.TenantId == result.TenantId));
    }

    [Fact]
    public async Task Customer_manage_tenant_permission_cannot_provision_platform_tenant()
    {
        await using var factory = CreateFactory("platform-provision-rbac");
        using var client = factory.CreateClient();
        using var request = CreateProvisionRequest(
            PilotRequest(),
            "provision-blocked",
            includePlatformPermission: false);
        request.Headers.Add("X-Gccs-Dev-Permissions", Permission.ManageTenant.ToString());

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        Assert.False(await scope.ServiceProvider.GetRequiredService<GccsDbContext>().PlatformTenantOnboardings.AnyAsync());
    }

    [Fact]
    public async Task Platform_operator_cancels_pending_onboarding_atomically_and_preserves_audit_history()
    {
        await using var factory = CreateFactory("platform-cancel-success");
        using var client = factory.CreateClient();
        using var provisionRequest = CreateProvisionRequest(PilotRequest(), "cancel-pilot", true);
        var provisionResponse = await client.SendAsync(provisionRequest);
        var provisioned = await provisionResponse.Content.ReadFromJsonAsync<PlatformTenantProvisioningResultDto>(JsonOptions);
        Assert.NotNull(provisioned);

        using var cancelRequest = CreateCancelRequest(
            provisioned.OnboardingId,
            "Duplicate pilot onboarding created during operator validation.",
            includePlatformPermission: true);
        var cancelResponse = await client.SendAsync(cancelRequest);
        var cancelled = await cancelResponse.Content.ReadFromJsonAsync<PlatformTenantProvisioningResultDto>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, cancelResponse.StatusCode);
        Assert.NotNull(cancelled);
        Assert.Equal(TenantOnboardingStatus.Cancelled, cancelled.OnboardingStatus);
        Assert.Equal(TenantStatus.Archived, cancelled.TenantStatus);
        Assert.Equal(TenantInvitationStatus.Revoked, cancelled.InvitationStatus);
        Assert.Equal(InvitationDeliveryStatus.Cancelled, cancelled.InvitationDeliveryStatus);
        Assert.Equal("Duplicate pilot onboarding created during operator validation.", cancelled.CancellationReason);
        Assert.NotNull(cancelled.CancelledAt);
        Assert.Equal(Guid.Parse("71717171-7171-7171-7171-717171717171"), cancelled.CancelledByUserId);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var onboarding = await dbContext.PlatformTenantOnboardings.SingleAsync();
        var tenant = await dbContext.Tenants.SingleAsync();
        var invitation = await dbContext.TenantInvitations.SingleAsync();
        Assert.Equal(TenantOnboardingStatus.Cancelled, onboarding.Status);
        Assert.Equal(TenantStatus.Archived, tenant.Status);
        Assert.Equal(TenantInvitationStatus.Revoked, invitation.Status);
        Assert.Equal(InvitationDeliveryStatus.Cancelled, invitation.DeliveryStatus);
        Assert.Null(invitation.InvitationTokenHash);
        Assert.Null(invitation.NextDeliveryAttemptAt);
        Assert.Null(invitation.DeliveryLeaseUntil);

        var deliveryRepository = new EfInvitationDeliveryRepository(dbContext);
        Assert.Null(await deliveryRepository.TryClaimNextAsync(
            DateTimeOffset.UtcNow,
            TimeSpan.FromMinutes(5)));

        var cancellationAudits = await dbContext.AuditLogEntries
            .Where(audit => audit.TenantId == provisioned.TenantId && audit.Action == AuditAction.Archived)
            .ToArrayAsync();
        Assert.Equal(2, cancellationAudits.Length);
        Assert.All(cancellationAudits, audit =>
        {
            Assert.Equal(Guid.Parse("71717171-7171-7171-7171-717171717171"), audit.ActorUserId);
            Assert.Contains("Duplicate pilot onboarding", audit.MetadataJson, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task Customer_manage_tenant_permission_cannot_cancel_platform_onboarding()
    {
        await using var factory = CreateFactory("platform-cancel-rbac");
        using var client = factory.CreateClient();
        using var provisionRequest = CreateProvisionRequest(PilotRequest(), "cancel-rbac", true);
        var provisionResponse = await client.SendAsync(provisionRequest);
        var provisioned = await provisionResponse.Content.ReadFromJsonAsync<PlatformTenantProvisioningResultDto>(JsonOptions);
        Assert.NotNull(provisioned);

        using var cancelRequest = CreateCancelRequest(
            provisioned.OnboardingId,
            "Unauthorized customer cancellation attempt.",
            includePlatformPermission: false);
        cancelRequest.Headers.Add("X-Gccs-Dev-Permissions", Permission.ManageTenant.ToString());
        var response = await client.SendAsync(cancelRequest);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Equal(TenantOnboardingStatus.PendingOwnerAcceptance, (await dbContext.PlatformTenantOnboardings.SingleAsync()).Status);
        Assert.Equal(TenantInvitationStatus.Pending, (await dbContext.TenantInvitations.SingleAsync()).Status);
    }

    [Fact]
    public async Task Platform_operator_corrects_pending_owner_by_revoking_and_replacing_invitation_atomically()
    {
        await using var factory = CreateFactory("platform-correct-owner-success");
        using var client = factory.CreateClient();
        using var provisionRequest = CreateProvisionRequest(PilotRequest(), "correct-owner-success", true);
        var provisionResponse = await client.SendAsync(provisionRequest);
        var provisioned = await provisionResponse.Content.ReadFromJsonAsync<PlatformTenantProvisioningResultDto>(JsonOptions);
        Assert.NotNull(provisioned);

        const string previousToken = "previous-owner-invitation-token";
        using (var setupScope = factory.Services.CreateScope())
        {
            var setupDbContext = setupScope.ServiceProvider.GetRequiredService<GccsDbContext>();
            var invitation = await setupDbContext.TenantInvitations.SingleAsync();
            invitation.InvitationTokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(previousToken)));
            invitation.DeliveryStatus = InvitationDeliveryStatus.Sent;
            invitation.DeliveryAttemptCount = 2;
            invitation.NotificationSentAt = DateTimeOffset.UtcNow;
            invitation.DeliveryProviderMessageId = "provider-message-id";
            await setupDbContext.SaveChangesAsync();
        }

        using var correctionRequest = CreateOwnerCorrectionRequest(
            provisioned.OnboardingId,
            provisioned.InvitationId,
            " Correct.Owner@Example.com ",
            "Correct the designated Owner address before activation.",
            includePlatformPermission: true);
        var correctionResponse = await client.SendAsync(correctionRequest);
        var corrected = await correctionResponse.Content.ReadFromJsonAsync<PlatformTenantProvisioningResultDto>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, correctionResponse.StatusCode);
        Assert.NotNull(corrected);
        Assert.Equal("correct.owner@example.com", corrected.OwnerEmail);
        Assert.NotEqual(provisioned.InvitationId, corrected.InvitationId);
        Assert.Equal(TenantInvitationStatus.Pending, corrected.InvitationStatus);
        Assert.Equal(InvitationDeliveryStatus.Queued, corrected.InvitationDeliveryStatus);

        using var verificationScope = factory.Services.CreateScope();
        var dbContext = verificationScope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var onboarding = await dbContext.PlatformTenantOnboardings.SingleAsync();
        var invitations = await dbContext.TenantInvitations.OrderBy(item => item.CreatedAt).ToArrayAsync();
        Assert.Equal("correct.owner@example.com", onboarding.OwnerEmail);
        Assert.Equal(corrected.InvitationId, onboarding.InvitationId);
        Assert.Equal(2, invitations.Length);

        var previousInvitation = Assert.Single(invitations, item => item.Id == provisioned.InvitationId);
        Assert.Equal(TenantInvitationStatus.Revoked, previousInvitation.Status);
        Assert.Equal(InvitationDeliveryStatus.Sent, previousInvitation.DeliveryStatus);
        Assert.Null(previousInvitation.InvitationTokenHash);
        Assert.Equal(2, previousInvitation.DeliveryAttemptCount);

        var replacementInvitation = Assert.Single(invitations, item => item.Id == corrected.InvitationId);
        Assert.Equal("correct.owner@example.com", replacementInvitation.Email);
        Assert.Equal(TenantInvitationStatus.Pending, replacementInvitation.Status);
        Assert.Equal(InvitationDeliveryStatus.Queued, replacementInvitation.DeliveryStatus);
        Assert.Equal(0, replacementInvitation.DeliveryAttemptCount);
        Assert.Null(replacementInvitation.InvitationTokenHash);
        Assert.NotNull(replacementInvitation.NextDeliveryAttemptAt);
        Assert.Equal(6, await dbContext.AuditLogEntries.CountAsync(item => item.TenantId == provisioned.TenantId));

        using var oldLinkRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/invitations/{previousToken}");
        oldLinkRequest.Headers.Add("X-Gccs-Dev-Auth", "true");
        oldLinkRequest.Headers.Add("X-Gccs-Dev-Tenant", "none");
        oldLinkRequest.Headers.Add("X-Gccs-Dev-User", "81818181-8181-8181-8181-818181818181");
        oldLinkRequest.Headers.Add("X-Gccs-Dev-Email", "pilot.owner@example.com");
        Assert.Equal(HttpStatusCode.NotFound, (await client.SendAsync(oldLinkRequest)).StatusCode);

        Assert.True(replacementInvitation.NextDeliveryAttemptAt <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Customer_manage_tenant_permission_cannot_correct_owner_invitation()
    {
        await using var factory = CreateFactory("platform-correct-owner-rbac");
        using var client = factory.CreateClient();
        using var provisionRequest = CreateProvisionRequest(PilotRequest(), "correct-owner-rbac", true);
        var provisioned = await (await client.SendAsync(provisionRequest)).Content
            .ReadFromJsonAsync<PlatformTenantProvisioningResultDto>(JsonOptions);
        Assert.NotNull(provisioned);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var auditCount = await dbContext.AuditLogEntries.CountAsync();

        using var correctionRequest = CreateOwnerCorrectionRequest(
            provisioned.OnboardingId,
            provisioned.InvitationId,
            "attacker@example.com",
            "Unauthorized correction attempt.",
            includePlatformPermission: false);
        correctionRequest.Headers.Add("X-Gccs-Dev-Permissions", Permission.ManageTenant.ToString());
        var response = await client.SendAsync(correctionRequest);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        dbContext.ChangeTracker.Clear();
        Assert.Equal("pilot.owner@example.com", (await dbContext.PlatformTenantOnboardings.SingleAsync()).OwnerEmail);
        Assert.Equal("pilot.owner@example.com", (await dbContext.TenantInvitations.SingleAsync()).Email);
        Assert.Equal(auditCount, await dbContext.AuditLogEntries.CountAsync());
    }

    [Fact]
    public async Task Platform_operator_revokes_processing_invitation_when_correcting_owner()
    {
        await using var factory = CreateFactory("platform-correct-owner-processing");
        using var client = factory.CreateClient();
        using var provisionRequest = CreateProvisionRequest(PilotRequest(), "correct-owner-processing", true);
        var provisioned = await (await client.SendAsync(provisionRequest)).Content
            .ReadFromJsonAsync<PlatformTenantProvisioningResultDto>(JsonOptions);
        Assert.NotNull(provisioned);

        using (var setupScope = factory.Services.CreateScope())
        {
            var setupDbContext = setupScope.ServiceProvider.GetRequiredService<GccsDbContext>();
            var invitation = await setupDbContext.TenantInvitations.SingleAsync();
            invitation.DeliveryStatus = InvitationDeliveryStatus.Processing;
            invitation.DeliveryLeaseUntil = DateTimeOffset.UtcNow.AddMinutes(5);
            invitation.InvitationTokenHash = Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes("processing-owner-invitation-token")));
            await setupDbContext.SaveChangesAsync();
        }

        using var correctionRequest = CreateOwnerCorrectionRequest(
            provisioned.OnboardingId,
            provisioned.InvitationId,
            "correct.owner@example.com",
            "Attempt correction during active delivery.",
            includePlatformPermission: true);
        var response = await client.SendAsync(correctionRequest);
        var corrected = await response.Content.ReadFromJsonAsync<PlatformTenantProvisioningResultDto>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(corrected);
        using var verificationScope = factory.Services.CreateScope();
        var dbContext = verificationScope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Equal("correct.owner@example.com", (await dbContext.PlatformTenantOnboardings.SingleAsync()).OwnerEmail);
        var invitations = await dbContext.TenantInvitations.ToArrayAsync();
        Assert.Equal(2, invitations.Length);
        var previous = Assert.Single(invitations, item => item.Id == provisioned.InvitationId);
        Assert.Equal(TenantInvitationStatus.Revoked, previous.Status);
        Assert.Equal(InvitationDeliveryStatus.Cancelled, previous.DeliveryStatus);
        Assert.Null(previous.InvitationTokenHash);
        var replacement = Assert.Single(invitations, item => item.Id == corrected.InvitationId);
        Assert.Equal(TenantInvitationStatus.Pending, replacement.Status);
        Assert.Equal(InvitationDeliveryStatus.Queued, replacement.DeliveryStatus);
        Assert.Equal(6, await dbContext.AuditLogEntries.CountAsync());
    }

    [Fact]
    public async Task Platform_operator_cannot_correct_owner_after_activation()
    {
        await using var factory = CreateFactory("platform-correct-owner-active");
        using var client = factory.CreateClient();
        using var provisionRequest = CreateProvisionRequest(PilotRequest(), "correct-owner-active", true);
        var provisioned = await (await client.SendAsync(provisionRequest)).Content
            .ReadFromJsonAsync<PlatformTenantProvisioningResultDto>(JsonOptions);
        Assert.NotNull(provisioned);

        using (var setupScope = factory.Services.CreateScope())
        {
            var setupDbContext = setupScope.ServiceProvider.GetRequiredService<GccsDbContext>();
            (await setupDbContext.PlatformTenantOnboardings.SingleAsync()).Status = TenantOnboardingStatus.Active;
            (await setupDbContext.Tenants.SingleAsync()).Status = TenantStatus.Trialing;
            (await setupDbContext.TenantInvitations.SingleAsync()).Status = TenantInvitationStatus.Accepted;
            await setupDbContext.SaveChangesAsync();
        }

        using var correctionRequest = CreateOwnerCorrectionRequest(
            provisioned.OnboardingId,
            provisioned.InvitationId,
            "correct.owner@example.com",
            "Attempt correction after activation.",
            includePlatformPermission: true);
        var response = await client.SendAsync(correctionRequest);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        using var verificationScope = factory.Services.CreateScope();
        var dbContext = verificationScope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Equal("pilot.owner@example.com", (await dbContext.PlatformTenantOnboardings.SingleAsync()).OwnerEmail);
        Assert.Single(await dbContext.TenantInvitations.ToArrayAsync());
        Assert.Equal(3, await dbContext.AuditLogEntries.CountAsync());
    }

    [Fact]
    public async Task Stale_owner_correction_cannot_replace_a_different_current_recipient()
    {
        await using var factory = CreateFactory("platform-correct-owner-stale");
        using var client = factory.CreateClient();
        using var provisionRequest = CreateProvisionRequest(PilotRequest(), "correct-owner-stale", true);
        var provisioned = await (await client.SendAsync(provisionRequest)).Content
            .ReadFromJsonAsync<PlatformTenantProvisioningResultDto>(JsonOptions);
        Assert.NotNull(provisioned);

        using var correctionRequest = CreateOwnerCorrectionRequest(
            provisioned.OnboardingId,
            Guid.NewGuid(),
            "attacker@example.com",
            "Stale correction request.",
            includePlatformPermission: true);
        var response = await client.SendAsync(correctionRequest);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        using var verificationScope = factory.Services.CreateScope();
        var dbContext = verificationScope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Equal("pilot.owner@example.com", (await dbContext.PlatformTenantOnboardings.SingleAsync()).OwnerEmail);
        Assert.Single(await dbContext.TenantInvitations.ToArrayAsync());
        Assert.Equal(3, await dbContext.AuditLogEntries.CountAsync());
    }

    [Fact]
    public async Task Repeating_owner_correction_for_replacement_target_is_idempotent()
    {
        await using var factory = CreateFactory("platform-correct-owner-replay");
        using var client = factory.CreateClient();
        using var provisionRequest = CreateProvisionRequest(PilotRequest(), "correct-owner-replay", true);
        var provisioned = await (await client.SendAsync(provisionRequest)).Content
            .ReadFromJsonAsync<PlatformTenantProvisioningResultDto>(JsonOptions);
        Assert.NotNull(provisioned);

        using var firstRequest = CreateOwnerCorrectionRequest(
            provisioned.OnboardingId,
            provisioned.InvitationId,
            "correct.owner@example.com",
            "Correct the designated Owner address.",
            includePlatformPermission: true);
        var first = await (await client.SendAsync(firstRequest)).Content
            .ReadFromJsonAsync<PlatformTenantProvisioningResultDto>(JsonOptions);
        Assert.NotNull(first);

        using var replayRequest = CreateOwnerCorrectionRequest(
            provisioned.OnboardingId,
            provisioned.InvitationId,
            "correct.owner@example.com",
            "Correct the designated Owner address.",
            includePlatformPermission: true);
        var replayResponse = await client.SendAsync(replayRequest);
        var replay = await replayResponse.Content.ReadFromJsonAsync<PlatformTenantProvisioningResultDto>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, replayResponse.StatusCode);
        Assert.NotNull(replay);
        Assert.Equal(first.InvitationId, replay.InvitationId);
        using var verificationScope = factory.Services.CreateScope();
        var dbContext = verificationScope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Equal(2, await dbContext.TenantInvitations.CountAsync());
        Assert.Equal(6, await dbContext.AuditLogEntries.CountAsync());
    }

    [Fact]
    public async Task Platform_operator_cannot_cancel_an_activated_onboarding()
    {
        await using var factory = CreateFactory("platform-cancel-active");
        using var client = factory.CreateClient();
        using var provisionRequest = CreateProvisionRequest(PilotRequest(), "cancel-active", true);
        var provisionResponse = await client.SendAsync(provisionRequest);
        var provisioned = await provisionResponse.Content.ReadFromJsonAsync<PlatformTenantProvisioningResultDto>(JsonOptions);
        Assert.NotNull(provisioned);

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
            (await dbContext.PlatformTenantOnboardings.SingleAsync()).Status = TenantOnboardingStatus.Active;
            (await dbContext.Tenants.SingleAsync()).Status = TenantStatus.Trialing;
            (await dbContext.TenantInvitations.SingleAsync()).Status = TenantInvitationStatus.Accepted;
            await dbContext.SaveChangesAsync();
        }

        using var cancelRequest = CreateCancelRequest(
            provisioned.OnboardingId,
            "Cancellation after activation must be rejected.",
            includePlatformPermission: true);
        var response = await client.SendAsync(cancelRequest);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Platform_operator_lists_only_requested_onboarding_status_with_bounded_paging()
    {
        await using var factory = CreateFactory("platform-list-status");
        using var client = factory.CreateClient();
        using var firstRequest = CreateProvisionRequest(PilotRequest(), "list-first", true);
        using var secondRequest = CreateProvisionRequest(
            PilotRequest() with { CustomerReference = "PILOT-004", DisplayName = "Second Pilot" },
            "list-second",
            true);
        var firstResponse = await client.SendAsync(firstRequest);
        var secondResponse = await client.SendAsync(secondRequest);
        var first = await firstResponse.Content.ReadFromJsonAsync<PlatformTenantProvisioningResultDto>(JsonOptions);
        Assert.NotNull(first);
        Assert.Equal(HttpStatusCode.Created, secondResponse.StatusCode);

        using var cancelRequest = CreateCancelRequest(first.OnboardingId, "Duplicate list test onboarding.", true);
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(cancelRequest)).StatusCode);

        using var listRequest = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/platform/tenant-onboardings?page=1&pageSize=25&status=PendingOwnerAcceptance");
        AddPlatformHeaders(listRequest, true);
        var listResponse = await client.SendAsync(listRequest);
        var page = await listResponse.Content.ReadFromJsonAsync<PlatformTenantOnboardingPageDto>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        Assert.NotNull(page);
        Assert.Equal(1, page.TotalCount);
        Assert.Single(page.Items);
        Assert.Equal("PILOT-004", page.Items[0].CustomerReference);
    }

    [Fact]
    public async Task Platform_access_returns_the_server_authoritative_pilot_trial_date_window()
    {
        var clock = new FixedTimeProvider(new DateTimeOffset(2026, 8, 17, 23, 30, 0, TimeSpan.Zero));
        await using var factory = CreateFactory("platform-pilot-date-rules", timeProvider: clock);
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/platform/me/access");
        AddPlatformHeaders(request, includePlatformPermission: true);

        using var response = await client.SendAsync(request);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Disabled", body.RootElement.GetProperty("invitationDeliveryMode").GetString());
        var rules = body.RootElement.GetProperty("pilotTrialDateRules");
        Assert.Equal("2026-08-18", rules.GetProperty("minimumEndsOn").GetString());
        Assert.Equal("2026-11-15", rules.GetProperty("maximumEndsOn").GetString());
        Assert.Equal(90, rules.GetProperty("maximumPilotDays").GetInt32());
    }

    [Fact]
    public async Task Same_idempotency_key_and_payload_returns_original_tenant_without_duplicate()
    {
        await using var factory = CreateFactory("platform-provision-idempotent");
        using var client = factory.CreateClient();

        using var firstRequest = CreateProvisionRequest(PilotRequest(), "stable-pilot-request", true);
        using var secondRequest = CreateProvisionRequest(PilotRequest(), "stable-pilot-request", true);
        var firstResponse = await client.SendAsync(firstRequest);
        var secondResponse = await client.SendAsync(secondRequest);
        var first = await firstResponse.Content.ReadFromJsonAsync<PlatformTenantProvisioningResultDto>(JsonOptions);
        var second = await secondResponse.Content.ReadFromJsonAsync<PlatformTenantProvisioningResultDto>(JsonOptions);

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(first.TenantId, second.TenantId);
        Assert.True(second.IsReplay);

        using var scope = factory.Services.CreateScope();
        Assert.Equal(1, await scope.ServiceProvider.GetRequiredService<GccsDbContext>().Tenants.CountAsync());
    }

    [Fact]
    public async Task Reusing_idempotency_key_with_different_payload_returns_conflict()
    {
        await using var factory = CreateFactory("platform-provision-conflict");
        using var client = factory.CreateClient();
        using var firstRequest = CreateProvisionRequest(PilotRequest(), "conflicting-request", true);
        using var conflictingRequest = CreateProvisionRequest(
            PilotRequest() with { DisplayName = "Different Workspace" },
            "conflicting-request",
            true);

        var firstResponse = await client.SendAsync(firstRequest);
        var conflictResponse = await client.SendAsync(conflictingRequest);

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, conflictResponse.StatusCode);
    }

    [Fact]
    public async Task Paid_tenant_requires_subscription_fields_and_commercial_confirmation()
    {
        await using var factory = CreateFactory("platform-provision-paid-validation");
        using var client = factory.CreateClient();
        var invalidPaidRequest = PilotRequest() with
        {
            OnboardingType = TenantOnboardingType.Paid.ToString(),
            TrialEndsAt = null
        };
        using var request = CreateProvisionRequest(invalidPaidRequest, "invalid-paid", true);

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("plan code and subscription reference", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Invited_owner_can_accept_without_existing_membership_and_activate_pilot()
    {
        await using var factory = CreateFactory("platform-owner-accept", enforceMembership: true);
        using var client = factory.CreateClient();
        using var provisionRequest = CreateProvisionRequest(PilotRequest(), "activate-pilot", true);
        var provisionResponse = await client.SendAsync(provisionRequest);
        var provisioned = await provisionResponse.Content.ReadFromJsonAsync<PlatformTenantProvisioningResultDto>(JsonOptions);
        Assert.NotNull(provisioned);

        const string invitationToken = "platform-owner-activation-token";
        using (var scope = factory.Services.CreateScope())
        {
            var tokenDbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
            var invitation = await tokenDbContext.TenantInvitations.SingleAsync(candidate => candidate.Id == provisioned.InvitationId);
            invitation.InvitationTokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(invitationToken)));
            await tokenDbContext.SaveChangesAsync();
        }

        var ownerUserId = Guid.Parse("81818181-8181-8181-8181-818181818181");
        using var acceptRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/invitations/{invitationToken}/accept");
        acceptRequest.Headers.Add("X-Gccs-Dev-Auth", "true");
        acceptRequest.Headers.Add("X-Gccs-Dev-Tenant", "none");
        acceptRequest.Headers.Add("X-Gccs-Dev-User", ownerUserId.ToString());
        acceptRequest.Headers.Add("X-Gccs-Dev-Email", "pilot.owner@example.com");
        acceptRequest.Content = JsonContent.Create(new AcceptTenantInvitationRequest("Pilot Owner"), options: JsonOptions);

        var acceptResponse = await client.SendAsync(acceptRequest);

        Assert.True(
            acceptResponse.StatusCode == HttpStatusCode.OK,
            $"Expected 200 OK but received {(int)acceptResponse.StatusCode}: {await acceptResponse.Content.ReadAsStringAsync()}");
        using var verificationScope = factory.Services.CreateScope();
        var dbContext = verificationScope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Equal(TenantStatus.Trialing, (await dbContext.Tenants.SingleAsync()).Status);
        Assert.Equal(TenantOnboardingStatus.Active, (await dbContext.PlatformTenantOnboardings.SingleAsync()).Status);
        Assert.Equal(SubscriptionStatus.Active, (await dbContext.TenantSubscriptions.SingleAsync()).Status);
        Assert.True(await dbContext.TenantMemberships.AnyAsync(membership =>
            membership.TenantId == provisioned.TenantId &&
            membership.UserId == ownerUserId &&
            membership.RoleName == RoleCatalog.Owner));
        Assert.Contains(await dbContext.AuditLogEntries.ToArrayAsync(), audit =>
            audit.EntityType == "PlatformTenantOnboarding" && audit.Action == AuditAction.Updated);
        Assert.Contains(await dbContext.AuditLogEntries.ToArrayAsync(), audit =>
            audit.EntityType == "TenantSubscription" && audit.Action == AuditAction.Updated);
    }

    [Fact]
    public async Task Pilot_end_date_must_be_future_and_within_configured_maximum_without_partial_writes()
    {
        await using var factory = CreateFactory("platform-pilot-date-validation");
        using var client = factory.CreateClient();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        using var pastRequest = CreateProvisionRequest(
            PilotRequest() with { TrialEndsAt = today },
            "pilot-past-date",
            true);
        using var excessiveRequest = CreateProvisionRequest(
            PilotRequest() with { CustomerReference = "PILOT-LONG", TrialEndsAt = today.AddDays(91) },
            "pilot-excessive-date",
            true);

        Assert.Equal(HttpStatusCode.BadRequest, (await client.SendAsync(pastRequest)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.SendAsync(excessiveRequest)).StatusCode);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Empty(await dbContext.Tenants.ToArrayAsync());
        Assert.Empty(await dbContext.TenantSubscriptions.ToArrayAsync());
        Assert.Empty(await dbContext.AuditLogEntries.ToArrayAsync());
    }

    private WebApplicationFactory<Program> CreateFactory(
        string databaseName,
        bool enforceMembership = false,
        TimeProvider? timeProvider = null) =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.UseSetting("InvitationDelivery:Enabled", "false");
            builder.UseSetting("ConnectionStrings:GccsDatabase", string.Empty);
            builder.UseSetting("Security:DevelopmentAuth:DefaultPlatformPermissions", string.Empty);
            builder.UseSetting("Security:MembershipAuthorization:Enforce", enforceMembership.ToString());
            builder.ConfigureServices(services =>
            {
                services.AddDbContext<GccsDbContext>(options => options.UseInMemoryDatabase(databaseName));
                services.AddScoped<PlatformTenantProvisioningService>();
                services.AddScoped<IPlatformTenantProvisioningRepository, EfPlatformTenantProvisioningRepository>();
                services.AddScoped<TenantInvitationService>();
                services.AddScoped<ITenantInvitationRepository, EfTenantInvitationRepository>();
                services.AddScoped<IAuditEventWriter, EfAuditEventWriter>();
                if (timeProvider is not null)
                {
                    services.AddSingleton<TimeProvider>(timeProvider);
                }

                using var provider = services.BuildServiceProvider();
                using var scope = provider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
                dbContext.Database.EnsureDeleted();
                dbContext.Database.EnsureCreated();
            });
        });

    private static HttpRequestMessage CreateProvisionRequest(
        PlatformTenantProvisioningRequest requestBody,
        string idempotencyKey,
        bool includePlatformPermission)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/platform/tenants");
        request.Headers.Add("X-Gccs-Dev-Auth", "true");
        request.Headers.Add("X-Gccs-Dev-Tenant", "none");
        request.Headers.Add("X-Gccs-Dev-User", "71717171-7171-7171-7171-717171717171");
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        if (includePlatformPermission)
        {
            request.Headers.Add("X-Gccs-Dev-Platform-Permissions", "ProvisionTenants");
        }

        request.Content = JsonContent.Create(requestBody, options: JsonOptions);
        return request;
    }

    private static HttpRequestMessage CreateCancelRequest(
        Guid onboardingId,
        string reason,
        bool includePlatformPermission)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/platform/tenant-onboardings/{onboardingId}/cancel");
        AddPlatformHeaders(request, includePlatformPermission);
        request.Content = JsonContent.Create(
            new CancelPlatformTenantOnboardingRequest(reason),
            options: JsonOptions);
        return request;
    }

    private static HttpRequestMessage CreateOwnerCorrectionRequest(
        Guid onboardingId,
        Guid expectedInvitationId,
        string newOwnerEmail,
        string reason,
        bool includePlatformPermission)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/platform/tenant-onboardings/{onboardingId}/owner-invitation/correct");
        AddPlatformHeaders(request, includePlatformPermission);
        request.Content = JsonContent.Create(
            new CorrectPlatformOwnerInvitationRequest(expectedInvitationId, newOwnerEmail, reason),
            options: JsonOptions);
        return request;
    }

    private static void AddPlatformHeaders(HttpRequestMessage request, bool includePlatformPermission)
    {
        request.Headers.Add("X-Gccs-Dev-Auth", "true");
        request.Headers.Add("X-Gccs-Dev-Tenant", "none");
        request.Headers.Add("X-Gccs-Dev-User", "71717171-7171-7171-7171-717171717171");
        if (includePlatformPermission)
        {
            request.Headers.Add("X-Gccs-Dev-Platform-Permissions", "ProvisionTenants");
        }
    }

    private static PlatformTenantProvisioningRequest PilotRequest() =>
        new(
            TenantOnboardingType.Pilot.ToString(),
            "PILOT-003",
            "Aegis Pilot Workspace",
            "pilot.owner@example.com",
            "Pilot Owner",
            DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30),
            null,
            null,
            "Provision approved No-CUI pilot PILOT-003.",
            true,
            false);

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
