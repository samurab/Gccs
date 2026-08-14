using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Tenancy;
using Gccs.Application.Identity;
using Gccs.Domain.Audit;
using Gccs.Domain.Identity;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Gccs.Infrastructure.Identity;
using Gccs.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class TenantSubscriptionLifecycleTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly Guid TenantId = Guid.Parse("91919191-9191-9191-9191-919191919191");
    private static readonly Guid UserId = Guid.Parse("92929292-9292-9292-9292-929292929292");
    private static readonly Guid OperatorId = Guid.Parse("93939393-9393-9393-9393-939393939393");
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;

    public TenantSubscriptionLifecycleTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public void Pending_and_malformed_grace_subscriptions_fail_closed()
    {
        var now = DateTimeOffset.Parse("2026-08-13T12:00:00Z");
        var pending = new TenantSubscription(
            Guid.NewGuid(), TenantId, TenantKind.ContractorWorkspace, SubscriptionPlan.PilotEvaluation,
            "PILOT-EVALUATION", SubscriptionStatus.Pending, now.AddDays(-1), now.AddDays(10), now.AddDays(17),
            "PILOT-LIFECYCLE", null, "Pending activation.", 1);
        var malformedGrace = pending with { Status = SubscriptionStatus.GracePeriod, GraceEndsAt = null };
        var malformedActive = pending with { Status = SubscriptionStatus.Active, EndsAt = null };

        Assert.Equal(SubscriptionAccessLevel.Denied, pending.AccessLevel(now));
        Assert.Equal(SubscriptionStatus.Expired, malformedGrace.EffectiveStatus(now));
        Assert.Equal(SubscriptionAccessLevel.Denied, malformedGrace.AccessLevel(now));
        Assert.Equal(SubscriptionAccessLevel.Denied, malformedActive.AccessLevel(now));
    }

    [Fact]
    public async Task Boundary_time_enforcement_allows_reads_during_grace_and_denies_all_access_after_grace()
    {
        var clock = new MutableTimeProvider(DateTimeOffset.Parse("2026-08-13T12:00:00Z"));
        await using var factory = CreateFactory("subscription-boundaries", clock, dbContext =>
            SeedActivePilot(dbContext, clock.GetUtcNow(), clock.GetUtcNow().AddHours(1), clock.GetUtcNow().AddHours(2)));
        using var client = factory.CreateClient();

        var activeRead = await SendTenantAsync(client, HttpMethod.Get, "/api/me/access");
        Assert.Equal(HttpStatusCode.OK, activeRead.StatusCode);

        clock.Advance(TimeSpan.FromHours(1));
        var graceRead = await SendTenantAsync(client, HttpMethod.Get, "/api/me/access");
        var graceWrite = await SendTenantAsync(client, HttpMethod.Post, "/api/no-cui-acknowledgement", new { });
        Assert.Equal(HttpStatusCode.OK, graceRead.StatusCode);
        var graceWriteBody = await graceWrite.Content.ReadAsStringAsync();
        Assert.True(graceWrite.StatusCode == HttpStatusCode.Forbidden, graceWriteBody);
        Assert.Contains("subscription_read_only", graceWriteBody, StringComparison.OrdinalIgnoreCase);

        clock.Advance(TimeSpan.FromHours(1));
        var expiredRead = await SendTenantAsync(client, HttpMethod.Get, "/api/me/access");
        Assert.Equal(HttpStatusCode.Forbidden, expiredRead.StatusCode);
        Assert.Contains("subscription_inactive", await expiredRead.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Extend_expire_and_convert_are_audited_versioned_and_preserve_no_cui_posture()
    {
        var clock = new MutableTimeProvider(DateTimeOffset.Parse("2026-08-13T12:00:00Z"));
        await using var factory = CreateFactory("subscription-transitions", clock, dbContext =>
            SeedActivePilot(dbContext, clock.GetUtcNow(), clock.GetUtcNow().AddDays(10), clock.GetUtcNow().AddDays(17)));
        using var client = factory.CreateClient();

        var extended = await PostPlatformAsync<ExtendPilotSubscriptionRequest>(
            client,
            $"/api/platform/tenant-subscriptions/{TenantId}/extend",
            new ExtendPilotSubscriptionRequest(new DateOnly(2026, 9, 1), "Customer approved an evaluation extension.", 1),
            "extend-pilot-001");
        Assert.Equal(HttpStatusCode.OK, extended.StatusCode);
        var extendedDto = await extended.Content.ReadFromJsonAsync<TenantSubscriptionDto>(JsonOptions);
        Assert.NotNull(extendedDto);
        Assert.Equal(2, extendedDto.Version);
        Assert.Equal(SubscriptionStatus.Active, extendedDto.EffectiveStatus);

        var replay = await PostPlatformAsync<ExtendPilotSubscriptionRequest>(
            client,
            $"/api/platform/tenant-subscriptions/{TenantId}/extend",
            new ExtendPilotSubscriptionRequest(new DateOnly(2026, 9, 1), "Customer approved an evaluation extension.", 1),
            "extend-pilot-001");
        var replayDto = await replay.Content.ReadFromJsonAsync<TenantSubscriptionDto>(JsonOptions);
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        Assert.NotNull(replayDto);
        Assert.True(replayDto.IsReplay);
        Assert.Equal(2, replayDto.Version);

        var mismatchedReplay = await PostPlatformAsync<ExtendPilotSubscriptionRequest>(
            client,
            $"/api/platform/tenant-subscriptions/{TenantId}/extend",
            new ExtendPilotSubscriptionRequest(new DateOnly(2026, 9, 2), "Different input must not reuse the key.", 1),
            "extend-pilot-001");
        Assert.Equal(HttpStatusCode.Conflict, mismatchedReplay.StatusCode);

        var stale = await PostPlatformAsync<ChangePilotSubscriptionStatusRequest>(
            client,
            $"/api/platform/tenant-subscriptions/{TenantId}/cancel",
            new ChangePilotSubscriptionStatusRequest("Stale cancellation must fail.", 1));
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);

        var expired = await PostPlatformAsync<ChangePilotSubscriptionStatusRequest>(
            client,
            $"/api/platform/tenant-subscriptions/{TenantId}/expire",
            new ChangePilotSubscriptionStatusRequest("Evaluation concluded without immediate conversion.", 2));
        Assert.Equal(HttpStatusCode.OK, expired.StatusCode);
        var expiredDto = await expired.Content.ReadFromJsonAsync<TenantSubscriptionDto>(JsonOptions);
        Assert.NotNull(expiredDto);
        Assert.Equal(SubscriptionStatus.GracePeriod, expiredDto.EffectiveStatus);
        Assert.Equal(SubscriptionAccessLevel.ReadOnly, expiredDto.AccessLevel);

        var converted = await PostPlatformAsync<ConvertPilotSubscriptionRequest>(
            client,
            $"/api/platform/tenant-subscriptions/{TenantId}/convert",
            new ConvertPilotSubscriptionRequest(
                "COMMERCIAL-STANDARD",
                "SUB-CONVERTED-001",
                "Commercial approval recorded in the billing system of record.",
                3));
        Assert.Equal(HttpStatusCode.OK, converted.StatusCode);
        var convertedDto = await converted.Content.ReadFromJsonAsync<TenantSubscriptionDto>(JsonOptions);
        Assert.NotNull(convertedDto);
        Assert.Equal(SubscriptionPlan.CommercialStandard, convertedDto.Plan);
        Assert.Equal(SubscriptionStatus.Converted, convertedDto.EffectiveStatus);
        Assert.Equal(SubscriptionAccessLevel.Full, convertedDto.AccessLevel);
        Assert.Null(convertedDto.EndsAt);

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Equal(TenantDataPosture.NoCui, (await dbContext.Tenants.SingleAsync()).DataPosture);
        Assert.Equal(3, await dbContext.AuditLogEntries.CountAsync(item => item.EntityType == "TenantSubscription"));
        Assert.Equal(4, (await dbContext.TenantSubscriptions.SingleAsync()).Version);
        Assert.Equal(3, await dbContext.TenantSubscriptionTransitions.CountAsync());
        var transition = await dbContext.TenantSubscriptionTransitions.FirstAsync();
        transition.Transition = "Tampered";
        await Assert.ThrowsAsync<InvalidOperationException>(() => dbContext.SaveChangesAsync());
    }

    [Fact]
    public async Task Cancel_denies_workspace_access_and_repeated_or_unauthorized_mutations_change_nothing()
    {
        var clock = new MutableTimeProvider(DateTimeOffset.Parse("2026-08-13T12:00:00Z"));
        await using var factory = CreateFactory("subscription-cancel", clock, dbContext =>
            SeedActivePilot(dbContext, clock.GetUtcNow(), clock.GetUtcNow().AddDays(10), clock.GetUtcNow().AddDays(17)));
        using var client = factory.CreateClient();

        using var unauthorized = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/platform/tenant-subscriptions/{TenantId}/cancel");
        AddTenantHeaders(unauthorized);
        unauthorized.Content = JsonContent.Create(new ChangePilotSubscriptionStatusRequest("Customer cannot cancel platform subscription.", 1), options: JsonOptions);
        var denied = await client.SendAsync(unauthorized);
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);

        var cancelled = await PostPlatformAsync<ChangePilotSubscriptionStatusRequest>(
            client,
            $"/api/platform/tenant-subscriptions/{TenantId}/cancel",
            new ChangePilotSubscriptionStatusRequest("Pilot cancelled by the platform operator.", 1));
        Assert.Equal(HttpStatusCode.OK, cancelled.StatusCode);

        var repeated = await PostPlatformAsync<ChangePilotSubscriptionStatusRequest>(
            client,
            $"/api/platform/tenant-subscriptions/{TenantId}/cancel",
            new ChangePilotSubscriptionStatusRequest("Repeated cancellation.", 2));
        Assert.Equal(HttpStatusCode.Conflict, repeated.StatusCode);

        var workspaceRead = await SendTenantAsync(client, HttpMethod.Get, "/api/me/access");
        Assert.True(
            workspaceRead.StatusCode == HttpStatusCode.Forbidden,
            await workspaceRead.Content.ReadAsStringAsync());

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var subscription = await dbContext.TenantSubscriptions.SingleAsync();
        Assert.Equal(SubscriptionStatus.Cancelled, subscription.Status);
        Assert.Equal(2, subscription.Version);
        Assert.Single(await dbContext.AuditLogEntries.Where(item => item.EntityType == "TenantSubscription").ToArrayAsync());
    }

    [Fact]
    public async Task Unknown_tenant_returns_not_found_without_disclosing_another_subscription()
    {
        var clock = new MutableTimeProvider(DateTimeOffset.Parse("2026-08-13T12:00:00Z"));
        await using var factory = CreateFactory("subscription-not-found", clock, dbContext =>
            SeedActivePilot(dbContext, clock.GetUtcNow(), clock.GetUtcNow().AddDays(10), clock.GetUtcNow().AddDays(17)));
        using var client = factory.CreateClient();
        var unknownTenantId = Guid.Parse("94949494-9494-9494-9494-949494949494");

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/platform/tenant-subscriptions/{unknownTenantId}");
        AddPlatformHeaders(request);
        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.DoesNotContain(TenantId.ToString(), body, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("GET", "")]
    [InlineData("POST", "/extend")]
    [InlineData("POST", "/expire")]
    [InlineData("POST", "/cancel")]
    [InlineData("POST", "/convert")]
    public async Task Customer_tenant_role_cannot_invoke_any_platform_subscription_operation(
        string method,
        string suffix)
    {
        var clock = new MutableTimeProvider(DateTimeOffset.Parse("2026-08-13T12:00:00Z"));
        await using var factory = CreateFactory($"subscription-rbac-{suffix.Replace("/", string.Empty)}-{method}", clock, dbContext =>
            SeedActivePilot(dbContext, clock.GetUtcNow(), clock.GetUtcNow().AddDays(10), clock.GetUtcNow().AddDays(17)));
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(
            new HttpMethod(method),
            $"/api/platform/tenant-subscriptions/{TenantId}{suffix}");
        AddTenantHeaders(request);
        if (method == "POST")
        {
            request.Content = JsonContent.Create(new { });
        }

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Equal(1, (await dbContext.TenantSubscriptions.SingleAsync()).Version);
        Assert.Empty(await dbContext.AuditLogEntries.ToArrayAsync());
    }

    private WebApplicationFactory<Program> CreateFactory(
        string databaseName,
        MutableTimeProvider clock,
        Action<GccsDbContext> seed) =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.UseSetting("ConnectionStrings:GccsDatabase", string.Empty);
            builder.UseSetting("Security:MembershipAuthorization:Enforce", "true");
            builder.UseSetting("Security:DevelopmentAuth:DefaultPlatformPermissions", string.Empty);
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<TimeProvider>();
                services.AddSingleton<TimeProvider>(clock);
                services.AddDbContext<GccsDbContext>(options => options.UseInMemoryDatabase(databaseName));
                services.AddScoped<ITenantMembershipRepository, EfTenantMembershipRepository>();
                services.AddScoped<ITenantSubscriptionRepository, EfTenantSubscriptionRepository>();

                using var provider = services.BuildServiceProvider();
                using var scope = provider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
                dbContext.Database.EnsureDeleted();
                dbContext.Database.EnsureCreated();
                seed(dbContext);
                dbContext.SaveChanges();
            });
        });

    private static void SeedActivePilot(
        GccsDbContext dbContext,
        DateTimeOffset now,
        DateTimeOffset endsAt,
        DateTimeOffset graceEndsAt)
    {
        dbContext.Tenants.Add(new TenantEntity
        {
            Id = TenantId,
            Name = "Lifecycle Pilot",
            Status = TenantStatus.Trialing,
            DataPosture = TenantDataPosture.NoCui,
            TrialEndsAt = DateOnly.FromDateTime(endsAt.UtcDateTime),
            CreatedAt = now
        });
        dbContext.Users.Add(new UserEntity
        {
            Id = UserId,
            TenantId = TenantId,
            Email = "pilot.owner@example.com",
            DisplayName = "Pilot Owner",
            Status = UserStatus.Active,
            MfaEnabled = true,
            CreatedAt = now
        });
        dbContext.TenantMemberships.Add(new TenantMembershipEntity
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
            UserId = UserId,
            Status = MembershipStatus.Active,
            RoleName = RoleCatalog.Owner,
            CreatedAt = now
        });
        dbContext.TenantSubscriptions.Add(new TenantSubscriptionEntity
        {
            Id = Guid.Parse("95959595-9595-9595-9595-959595959595"),
            TenantId = TenantId,
            TenantKind = TenantKind.ContractorWorkspace,
            Plan = SubscriptionPlan.PilotEvaluation,
            PlanCode = "PILOT-EVALUATION",
            Status = SubscriptionStatus.Active,
            StartsAt = now.AddDays(-1),
            EndsAt = endsAt,
            GraceEndsAt = graceEndsAt,
            ExternalCustomerReference = "PILOT-LIFECYCLE",
            StatusReason = "Lifecycle test pilot.",
            Version = 1,
            CreatedAt = now
        });
    }

    private static async Task<HttpResponseMessage> PostPlatformAsync<T>(
        HttpClient client,
        string path,
        T body,
        string? idempotencyKey = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path);
        AddPlatformHeaders(request);
        request.Headers.Add("Idempotency-Key", idempotencyKey ?? Guid.NewGuid().ToString());
        request.Content = JsonContent.Create(body, options: JsonOptions);
        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> SendTenantAsync(
        HttpClient client,
        HttpMethod method,
        string path,
        object? body = null)
    {
        using var request = new HttpRequestMessage(method, path);
        AddTenantHeaders(request);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: JsonOptions);
        }
        return await client.SendAsync(request);
    }

    private static void AddPlatformHeaders(HttpRequestMessage request)
    {
        request.Headers.Add("X-Gccs-Dev-Auth", "true");
        request.Headers.Add("X-Gccs-Dev-Tenant", "none");
        request.Headers.Add("X-Gccs-Dev-User", OperatorId.ToString());
        request.Headers.Add("X-Gccs-Dev-Platform-Permissions", "ProvisionTenants");
    }

    private static void AddTenantHeaders(HttpRequestMessage request)
    {
        request.Headers.Add("X-Gccs-Dev-Auth", "true");
        request.Headers.Add("X-Gccs-Dev-Tenant", TenantId.ToString());
        request.Headers.Add("X-Gccs-Dev-User", UserId.ToString());
    }

    private sealed class MutableTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        private DateTimeOffset _utcNow = utcNow;
        public override DateTimeOffset GetUtcNow() => _utcNow;
        public void Advance(TimeSpan duration) => _utcNow = _utcNow.Add(duration);
    }
}
