using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Tenancy;
using Gccs.Domain.Identity;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Gccs.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class PlatformCustomerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;

    public PlatformCustomerTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Read_only_customer_permission_lists_filters_pages_and_opens_operational_details()
    {
        var now = DateTimeOffset.UtcNow;
        await using var factory = CreateFactory("platform-customers-list", context =>
        {
            SeedCustomer(context, "Alpha Pilot", "PILOT-ALPHA", TenantOnboardingType.Pilot, SubscriptionPlan.PilotEvaluation, now.AddDays(7), now);
            SeedCustomer(context, "Bravo Paid", "PAID-BRAVO", TenantOnboardingType.Paid, SubscriptionPlan.CommercialStandard, null, now.AddMinutes(-1));
            SeedCustomer(context, "Charlie Pilot", "PILOT-CHARLIE", TenantOnboardingType.Pilot, SubscriptionPlan.PilotEvaluation, now.AddDays(30), now.AddMinutes(-2));
        });
        using var client = factory.CreateClient();

        using var listRequest = CreateRequest(
            HttpMethod.Get,
            "/api/platform/customers?page=1&pageSize=1&customerType=Pilot&sort=NameAscending",
            "ViewPlatformCustomers");
        var listResponse = await client.SendAsync(listRequest);
        var page = await listResponse.Content.ReadFromJsonAsync<PlatformCustomerPageDto>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        Assert.NotNull(page);
        Assert.Equal(2, page.TotalCount);
        Assert.Single(page.Items);
        Assert.Equal("Alpha Pilot", page.Items[0].DisplayName);
        Assert.Contains(PlatformCustomerAttention.PilotExpiring, page.Items[0].Attention);
        Assert.True(page.HasNextPage);

        using var detailRequest = CreateRequest(
            HttpMethod.Get,
            $"/api/platform/customers/{page.Items[0].TenantId}",
            "ViewPlatformCustomers");
        var detailResponse = await client.SendAsync(detailRequest);
        var detailBody = await detailResponse.Content.ReadAsStringAsync();
        var detail = JsonSerializer.Deserialize<PlatformCustomerDetailDto>(detailBody, JsonOptions);

        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
        Assert.NotNull(detail);
        Assert.Equal("pilot.owner@example.com", detail.Customer.OwnerEmail);
        Assert.DoesNotContain("invitationToken", detailBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token-hash", detailBody, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Read_only_customer_permission_cannot_mutate_subscription_or_onboarding_and_leaves_state_unchanged()
    {
        var now = DateTimeOffset.UtcNow;
        Guid tenantId = default;
        await using var factory = CreateFactory("platform-customers-read-only", context =>
            tenantId = SeedCustomer(context, "Read Only Pilot", "PILOT-READ", TenantOnboardingType.Pilot, SubscriptionPlan.PilotEvaluation, now.AddDays(10), now));
        using var client = factory.CreateClient();

        using var subscriptionRequest = CreateRequest(
            HttpMethod.Post,
            $"/api/platform/tenant-subscriptions/{tenantId}/cancel",
            "ViewPlatformCustomers");
        subscriptionRequest.Headers.Add("Idempotency-Key", "read-only-cancel");
        subscriptionRequest.Content = JsonContent.Create(new ChangePilotSubscriptionStatusRequest("Unauthorized mutation.", 1));
        Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(subscriptionRequest)).StatusCode);

        using var onboardingRequest = CreateRequest(
            HttpMethod.Post,
            "/api/platform/tenants",
            "ViewPlatformCustomers");
        onboardingRequest.Headers.Add("Idempotency-Key", "read-only-provision");
        onboardingRequest.Content = JsonContent.Create(new { });
        Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(onboardingRequest)).StatusCode);

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Equal(1, (await dbContext.TenantSubscriptions.SingleAsync()).Version);
        Assert.Empty(await dbContext.TenantSubscriptionTransitions.ToArrayAsync());
        Assert.Single(await dbContext.Tenants.ToArrayAsync());
    }

    [Fact]
    public async Task Customer_endpoints_fail_closed_for_tenant_permission_and_return_non_disclosing_not_found()
    {
        await using var factory = CreateFactory("platform-customers-denial", _ => { });
        using var client = factory.CreateClient();

        using var deniedRequest = CreateRequest(HttpMethod.Get, "/api/platform/customers?page=1&pageSize=25", null);
        deniedRequest.Headers.Add("X-Gccs-Dev-Permissions", "ManageTenant");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(deniedRequest)).StatusCode);

        using var missingRequest = CreateRequest(
            HttpMethod.Get,
            $"/api/platform/customers/{Guid.NewGuid()}",
            "ViewPlatformCustomers");
        var missingResponse = await client.SendAsync(missingRequest);
        Assert.Equal(HttpStatusCode.NotFound, missingResponse.StatusCode);
        Assert.DoesNotContain("tenant", await missingResponse.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Customer_query_rejects_unbounded_page_and_search_inputs()
    {
        await using var factory = CreateFactory("platform-customers-validation", _ => { });
        using var client = factory.CreateClient();

        using var pageRequest = CreateRequest(HttpMethod.Get, "/api/platform/customers?page=1&pageSize=101", "ViewPlatformCustomers");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.SendAsync(pageRequest)).StatusCode);

        using var searchRequest = CreateRequest(
            HttpMethod.Get,
            $"/api/platform/customers?page=1&pageSize=25&search={new string('a', 321)}",
            "ViewPlatformCustomers");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.SendAsync(searchRequest)).StatusCode);
    }

    [Theory]
    [InlineData("ManageTenantOnboarding", HttpStatusCode.Forbidden, 1)]
    [InlineData("ManageTenantSubscriptions", HttpStatusCode.OK, 2)]
    [InlineData("ProvisionTenants", HttpStatusCode.OK, 2)]
    public async Task Subscription_mutation_permission_matrix_is_server_authoritative(
        string permission,
        HttpStatusCode expectedStatus,
        long expectedVersion)
    {
        var now = DateTimeOffset.UtcNow;
        Guid tenantId = default;
        await using var factory = CreateFactory($"platform-customers-subscription-{permission}", context =>
            tenantId = SeedCustomer(context, "Permission Pilot", $"PILOT-{permission}", TenantOnboardingType.Pilot, SubscriptionPlan.PilotEvaluation, now.AddDays(10), now));
        using var client = factory.CreateClient();
        using var request = CreateRequest(
            HttpMethod.Post,
            $"/api/platform/tenant-subscriptions/{tenantId}/cancel",
            permission);
        request.Headers.Add("Idempotency-Key", $"cancel-{permission}");
        request.Content = JsonContent.Create(new ChangePilotSubscriptionStatusRequest("Permission matrix test.", 1));

        Assert.Equal(expectedStatus, (await client.SendAsync(request)).StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Equal(expectedVersion, (await dbContext.TenantSubscriptions.SingleAsync()).Version);
        Assert.Equal(expectedVersion == 2 ? 1 : 0, await dbContext.TenantSubscriptionTransitions.CountAsync());
    }

    [PostgresFact]
    [Trait("Category", "PostgresIntegration")]
    public async Task Customer_directory_query_filters_sorts_and_projects_on_postgresql()
    {
        var connectionString = Environment.GetEnvironmentVariable("GCCS_TEST_POSTGRES_CONNECTION") ??
            throw new InvalidOperationException("Set GCCS_TEST_POSTGRES_CONNECTION to run the PostgreSQL integration test.");
        var marker = $"PGCUSTOMER{Guid.NewGuid():N}";
        var now = DateTimeOffset.UtcNow;
        var tenantIds = new List<Guid>();
        var options = new DbContextOptionsBuilder<GccsDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        await using var dbContext = new GccsDbContext(options);
        await dbContext.Database.MigrateAsync();
        tenantIds.Add(SeedCustomer(dbContext, $"{marker} Alpha", $"{marker}-A", TenantOnboardingType.Pilot, SubscriptionPlan.PilotEvaluation, now.AddDays(5), now.AddMinutes(-2)));
        tenantIds.Add(SeedCustomer(dbContext, $"{marker} Bravo", $"{marker}-B", TenantOnboardingType.Pilot, SubscriptionPlan.PilotEvaluation, now.AddDays(10), now.AddMinutes(-1)));
        await dbContext.SaveChangesAsync();

        try
        {
            var repository = new EfPlatformCustomerRepository(dbContext);
            var result = await repository.ListAsync(new PlatformCustomerQuery(
                1,
                25,
                marker.ToLowerInvariant(),
                TenantOnboardingType.Pilot,
                null,
                TenantOnboardingStatus.Active,
                SubscriptionStatus.Active,
                PlatformCustomerAttention.PilotExpiring,
                PlatformCustomerSort.UpdatedDescending,
                now));

            Assert.Equal(2, result.TotalCount);
            Assert.Equal($"{marker} Bravo", result.Items[0].DisplayName);
            Assert.All(result.Items, item => Assert.Contains(marker, item.DisplayName, StringComparison.Ordinal));
        }
        finally
        {
            await dbContext.PlatformTenantOnboardings.Where(item => tenantIds.Contains(item.TenantId)).ExecuteDeleteAsync();
            await dbContext.TenantSubscriptions.Where(item => tenantIds.Contains(item.TenantId)).ExecuteDeleteAsync();
            await dbContext.TenantInvitations.Where(item => tenantIds.Contains(item.TenantId)).ExecuteDeleteAsync();
            await dbContext.Tenants.Where(item => tenantIds.Contains(item.Id)).ExecuteDeleteAsync();
        }
    }

    private WebApplicationFactory<Program> CreateFactory(string databaseName, Action<GccsDbContext> seed) =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.UseSetting("ConnectionStrings:GccsDatabase", string.Empty);
            builder.UseSetting("Security:DevelopmentAuth:DefaultPlatformPermissions", string.Empty);
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<GccsDbContext>();
                services.RemoveAll<DbContextOptions<GccsDbContext>>();
                services.AddDbContext<GccsDbContext>(options => options.UseInMemoryDatabase(databaseName));
                services.RemoveAll<IPlatformCustomerRepository>();
                services.AddScoped<IPlatformCustomerRepository, EfPlatformCustomerRepository>();
                services.RemoveAll<ITenantSubscriptionRepository>();
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

    private static Guid SeedCustomer(
        GccsDbContext context,
        string name,
        string customerReference,
        TenantOnboardingType type,
        SubscriptionPlan plan,
        DateTimeOffset? endsAt,
        DateTimeOffset createdAt)
    {
        var tenantId = Guid.NewGuid();
        var invitationId = Guid.NewGuid();
        context.Tenants.Add(new TenantEntity
        {
            Id = tenantId,
            Name = name,
            Status = type == TenantOnboardingType.Pilot ? TenantStatus.Trialing : TenantStatus.Active,
            DataPosture = TenantDataPosture.NoCui,
            TrialEndsAt = endsAt is null ? null : DateOnly.FromDateTime(endsAt.Value.UtcDateTime),
            CreatedAt = createdAt
        });
        context.TenantInvitations.Add(new TenantInvitationEntity
        {
            Id = invitationId,
            TenantId = tenantId,
            Email = "pilot.owner@example.com",
            RoleName = RoleCatalog.Owner,
            InvitationTokenHash = $"token-hash-must-not-leak-{Guid.NewGuid():N}",
            Status = TenantInvitationStatus.Accepted,
            DeliveryStatus = InvitationDeliveryStatus.Sent,
            ExpiresAt = createdAt.AddDays(7),
            NotificationSentAt = createdAt.AddMinutes(1),
            AcceptedAt = createdAt.AddMinutes(2),
            NotificationPlaceholder = "accepted",
            CreatedAt = createdAt
        });
        context.PlatformTenantOnboardings.Add(new PlatformTenantOnboardingEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            InvitationId = invitationId,
            IdempotencyKey = Guid.NewGuid().ToString(),
            RequestFingerprint = Guid.NewGuid().ToString("N"),
            OnboardingType = type,
            Status = TenantOnboardingStatus.Active,
            CustomerReference = customerReference,
            OwnerEmail = "pilot.owner@example.com",
            OwnerDisplayName = "Pilot Owner",
            PlanCode = plan == SubscriptionPlan.PilotEvaluation ? null : "COMMERCIAL-STANDARD",
            SubscriptionReference = plan == SubscriptionPlan.PilotEvaluation ? null : $"SUB-{customerReference}",
            CommercialApprovalConfirmed = plan != SubscriptionPlan.PilotEvaluation,
            SetupReason = "Synthetic customer directory test.",
            CreatedAt = createdAt
        });
        context.TenantSubscriptions.Add(new TenantSubscriptionEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TenantKind = TenantKind.ContractorWorkspace,
            Plan = plan,
            PlanCode = plan == SubscriptionPlan.PilotEvaluation ? "PILOT-EVALUATION" : "COMMERCIAL-STANDARD",
            Status = SubscriptionStatus.Active,
            StartsAt = createdAt,
            EndsAt = endsAt,
            GraceEndsAt = endsAt?.AddDays(7),
            ExternalCustomerReference = customerReference,
            ExternalSubscriptionReference = plan == SubscriptionPlan.PilotEvaluation ? null : $"SUB-{customerReference}",
            StatusReason = "Synthetic customer directory test.",
            Version = 1,
            CreatedAt = createdAt
        });
        return tenantId;
    }

    private static HttpRequestMessage CreateRequest(HttpMethod method, string path, string? platformPermission)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Add("X-Gccs-Dev-Auth", "true");
        request.Headers.Add("X-Gccs-Dev-Tenant", "none");
        request.Headers.Add("X-Gccs-Dev-User", "61616161-6161-6161-6161-616161616161");
        if (platformPermission is not null)
        {
            request.Headers.Add("X-Gccs-Dev-Platform-Permissions", platformPermission);
        }
        return request;
    }
}
