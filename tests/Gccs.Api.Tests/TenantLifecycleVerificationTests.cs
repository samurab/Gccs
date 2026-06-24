using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Audit;
using Gccs.Application.Tenancy;
using Gccs.Domain.Audit;
using Gccs.Domain.Companies;
using Gccs.Domain.Compliance;
using Gccs.Domain.Contracts;
using Gccs.Domain.Identity;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Audit;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Gccs.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class TenantLifecycleVerificationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;

    public TenantLifecycleVerificationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TC_2_1_1_Tenant_creation_persists_required_metadata()
    {
        var actorUserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1");
        await using var factory = CreateFactory("tc-2-1-1");
        using var client = factory.CreateClient();
        using var request = CreateRequest(
            HttpMethod.Post,
            "/api/tenants",
            new CreateTenantRequest("TC-2.1.1 Acme Federal Services", TenantStatus.Trialing),
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa0"),
            actorUserId,
            Permission.ManageTenant);

        var response = await client.SendAsync(request);
        var createdTenant = await response.Content.ReadFromJsonAsync<TenantDto>(JsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(createdTenant);
        Assert.NotEqual(Guid.Empty, createdTenant.Id);
        Assert.Equal("TC-2.1.1 Acme Federal Services", createdTenant.DisplayName);
        Assert.Equal(TenantStatus.Trialing, createdTenant.Status);
        Assert.Equal(TenantDataPosture.NoCui, createdTenant.DataPosture);
        Assert.True(createdTenant.CreatedAt <= DateTimeOffset.UtcNow);
        Assert.Null(createdTenant.UpdatedAt);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var persistedTenant = await dbContext.Tenants.SingleAsync(candidate => candidate.Id == createdTenant.Id);

        Assert.Equal(createdTenant.Id, persistedTenant.Id);
        Assert.Equal(createdTenant.DisplayName, persistedTenant.Name);
        Assert.Equal(createdTenant.Status, persistedTenant.Status);
        Assert.Equal(createdTenant.DataPosture, persistedTenant.DataPosture);
        Assert.Equal(createdTenant.CreatedAt, persistedTenant.CreatedAt);
        Assert.Equal(createdTenant.UpdatedAt, persistedTenant.UpdatedAt);
        Assert.Equal(actorUserId, persistedTenant.CreatedByUserId);
    }

    [Fact]
    public async Task TC_2_1_2_Tenant_owned_sample_records_store_correct_tenant_id()
    {
        var tenantId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1");
        var userId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2");
        var roleId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb3");
        var companyProfileId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb4");
        var contractId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb5");
        var taskId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb6");
        await using var factory = CreateFactory("tc-2-1-2", dbContext =>
        {
            dbContext.Tenants.Add(CreateTenantEntity(tenantId, "TC-2.1.2 Tenant"));
            dbContext.Users.Add(new UserEntity
            {
                Id = userId,
                TenantId = tenantId,
                Email = "tc-2.1.2.owner@example.com",
                DisplayName = "TC-2.1.2 Owner",
                Status = UserStatus.Active,
                MfaEnabled = true,
                CreatedAt = DateTimeOffset.Parse("2026-06-13T12:00:00Z")
            });
            dbContext.Roles.Add(new RoleEntity
            {
                Id = roleId,
                TenantId = tenantId,
                Name = "TC-2.1.2 Compliance Manager",
                CreatedAt = DateTimeOffset.Parse("2026-06-13T12:00:00Z")
            });
            dbContext.CompanyProfiles.Add(new CompanyProfileEntity
            {
                Id = companyProfileId,
                TenantId = tenantId,
                LegalEntityName = "TC-2.1.2 Acme Federal Services LLC",
                Uei = "ABCDEF123456",
                CageCode = "1AB23",
                ContractorRole = ContractorRole.Subcontractor,
                ProductsAndServices = "Compliance readiness support",
                EmployeeRange = CompanyRange.Small,
                RevenueRange = CompanyRange.Small,
                ItEnvironmentDescription = "No-CUI MVP verification environment",
                DataHandlingPosture = DataHandlingPosture.FciOnly,
                CreatedAt = DateTimeOffset.Parse("2026-06-13T12:00:00Z")
            });
            dbContext.Contracts.Add(new ContractEntity
            {
                Id = contractId,
                TenantId = tenantId,
                ContractNumber = "TC-2.1.2-CONTRACT",
                Title = "Tenant-owned sample contract",
                AgencyOrPrimeName = "Sample Prime",
                Relationship = ContractorRelationship.Subcontractor,
                Kind = ContractKind.FixedPrice,
                Status = ContractStatus.Intake,
                PeriodOfPerformanceStart = new DateOnly(2026, 7, 1),
                PeriodOfPerformanceEnd = new DateOnly(2027, 6, 30),
                PlaceOfPerformance = "VA",
                CreatedAt = DateTimeOffset.Parse("2026-06-13T12:00:00Z")
            });
            dbContext.ComplianceTasks.Add(new ComplianceTaskEntity
            {
                Id = taskId,
                TenantId = tenantId,
                Title = "Verify tenant-owned task",
                Description = "Sample tenant-owned record for TC-2.1.2",
                Type = ComplianceTaskType.CalendarReminder,
                Status = ComplianceTaskStatus.Open,
                RiskLevel = RiskLevel.Medium,
                OwnerFunction = "Compliance",
                DueAt = new DateOnly(2026, 7, 15),
                CreatedAt = DateTimeOffset.Parse("2026-06-13T12:00:00Z")
            });
            dbContext.SaveChanges();
        });

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();

        Assert.Equal(tenantId, (await dbContext.Users.SingleAsync(candidate => candidate.Id == userId)).TenantId);
        Assert.Equal(tenantId, (await dbContext.Roles.SingleAsync(candidate => candidate.Id == roleId)).TenantId);
        Assert.Equal(tenantId, (await dbContext.CompanyProfiles.SingleAsync(candidate => candidate.Id == companyProfileId)).TenantId);
        Assert.Equal(tenantId, (await dbContext.Contracts.SingleAsync(candidate => candidate.Id == contractId)).TenantId);
        Assert.Equal(tenantId, (await dbContext.ComplianceTasks.SingleAsync(candidate => candidate.Id == taskId)).TenantId);
    }

    [Fact]
    public async Task TC_2_1_3_Cross_tenant_read_by_id_rejects_route_tenant_mismatch_without_data_leakage()
    {
        var tenantAId = Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc1");
        var tenantBId = Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc2");
        await using var factory = CreateFactory("tc-2-1-3", dbContext =>
        {
            dbContext.Tenants.AddRange(
                CreateTenantEntity(tenantAId, "TC-2.1.3 Tenant A"),
                CreateTenantEntity(tenantBId, "TC-2.1.3 Tenant B Secret Name"));
            dbContext.SaveChanges();
        });
        using var client = factory.CreateClient();
        using var request = CreateRequest(
            HttpMethod.Get,
            $"/api/tenants/{tenantBId}",
            tenantAId,
            Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc3"),
            Permission.ManageTenant);

        var response = await client.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("tenant_scope_mismatch", responseBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(tenantBId.ToString(), responseBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("TC-2.1.3 Tenant B Secret Name", responseBody, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TC_2_1_4_Tenant_status_change_audit_event_contains_before_and_after_status()
    {
        var actorUserId = Guid.Parse("dddddddd-dddd-dddd-dddd-ddddddddddd2");
        await using var factory = CreateFactory("tc-2-1-4");
        using var client = factory.CreateClient();
        var operationStartedAt = DateTimeOffset.UtcNow;
        using var createRequest = CreateRequest(
            HttpMethod.Post,
            "/api/tenants",
            new CreateTenantRequest("TC-2.1.4 Tenant"),
            Guid.Parse("dddddddd-dddd-dddd-dddd-ddddddddddd0"),
            actorUserId,
            Permission.ManageTenant);

        var createResponse = await client.SendAsync(createRequest);
        var createdTenant = await createResponse.Content.ReadFromJsonAsync<TenantDto>(JsonOptions);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.NotNull(createdTenant);

        using var request = CreateRequest(
            HttpMethod.Patch,
            $"/api/tenants/{createdTenant.Id}/status",
            new UpdateTenantStatusRequest(TenantStatus.Suspended),
            createdTenant.Id,
            actorUserId,
            Permission.ManageTenant);

        var response = await client.SendAsync(request);
        var updatedTenant = await response.Content.ReadFromJsonAsync<TenantDto>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(updatedTenant);
        Assert.Equal(TenantStatus.Suspended, updatedTenant.Status);
        Assert.NotNull(updatedTenant.UpdatedAt);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var auditEvents = await dbContext.AuditLogEntries
            .Where(candidate => candidate.EntityId == createdTenant.Id.ToString())
            .OrderBy(candidate => candidate.OccurredAt)
            .ToListAsync();
        var creationAuditEvent = Assert.Single(auditEvents, candidate => candidate.Action == AuditAction.Created);
        var statusAuditEvent = Assert.Single(auditEvents, candidate =>
            candidate.Action == AuditAction.Updated);
        var metadata = JsonSerializer.Deserialize<Dictionary<string, string>>(statusAuditEvent.MetadataJson) ?? [];

        Assert.Equal(createdTenant.Id, creationAuditEvent.TenantId);
        Assert.Equal(actorUserId, creationAuditEvent.ActorUserId);
        Assert.Equal(AuditAction.Created, creationAuditEvent.Action);
        Assert.Equal("Tenant", creationAuditEvent.EntityType);
        Assert.True(creationAuditEvent.OccurredAt >= operationStartedAt);

        Assert.Equal(createdTenant.Id, statusAuditEvent.TenantId);
        Assert.Equal(actorUserId, statusAuditEvent.ActorUserId);
        Assert.Equal(AuditAction.Updated, statusAuditEvent.Action);
        Assert.Equal("Tenant", statusAuditEvent.EntityType);
        Assert.True(statusAuditEvent.OccurredAt >= operationStartedAt);
        Assert.True(statusAuditEvent.OccurredAt <= DateTimeOffset.UtcNow);
        Assert.True(
            metadata.TryGetValue("beforeStatus", out var beforeStatus),
            $"Expected beforeStatus audit metadata. Actual metadata: {statusAuditEvent.MetadataJson}");
        Assert.True(
            metadata.TryGetValue("afterStatus", out var afterStatus),
            $"Expected afterStatus audit metadata. Actual metadata: {statusAuditEvent.MetadataJson}");
        Assert.Equal("Active", beforeStatus);
        Assert.Equal("Suspended", afterStatus);
    }

    [Fact]
    public async Task Story_2_1_server_side_rbac_and_standard_validation_errors_are_enforced()
    {
        var tenantId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeee1");
        await using var factory = CreateFactory("story-2-1-invariants", dbContext =>
        {
            dbContext.Tenants.Add(CreateTenantEntity(tenantId, "Story 2.1 Invariant Tenant"));
            dbContext.SaveChanges();
        });
        using var client = factory.CreateClient();
        using var unauthorizedCreateRequest = CreateRequest(
            HttpMethod.Post,
            "/api/tenants",
            new CreateTenantRequest("Unauthorized Tenant"),
            tenantId,
            Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeee2"),
            Permission.AuditorReadOnly);
        using var invalidCreateRequest = CreateRequest(
            HttpMethod.Post,
            "/api/tenants",
            new CreateTenantRequest("   "),
            tenantId,
            Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeee3"),
            Permission.ManageTenant);
        using var unauthorizedStatusRequest = CreateRequest(
            HttpMethod.Patch,
            $"/api/tenants/{tenantId}/status",
            new UpdateTenantStatusRequest(TenantStatus.Suspended),
            tenantId,
            Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeee4"),
            Permission.AuditorReadOnly);

        var unauthorizedCreateResponse = await client.SendAsync(unauthorizedCreateRequest);
        var invalidCreateResponse = await client.SendAsync(invalidCreateRequest);
        var invalidCreateBody = await invalidCreateResponse.Content.ReadAsStringAsync();
        var unauthorizedStatusResponse = await client.SendAsync(unauthorizedStatusRequest);

        Assert.Equal(HttpStatusCode.Forbidden, unauthorizedCreateResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, invalidCreateResponse.StatusCode);
        Assert.Contains("errors", invalidCreateBody, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Tenant display name is required", invalidCreateBody, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(HttpStatusCode.Forbidden, unauthorizedStatusResponse.StatusCode);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var unchangedTenant = await dbContext.Tenants.SingleAsync(candidate => candidate.Id == tenantId);

        Assert.Equal(TenantStatus.Active, unchangedTenant.Status);
        Assert.DoesNotContain(
            dbContext.AuditLogEntries,
            candidate => candidate.EntityId == tenantId.ToString() && candidate.Action == AuditAction.Updated);
    }

    private WebApplicationFactory<Program> CreateFactory(
        string databaseName,
        Action<GccsDbContext>? seed = null) =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.UseSetting("ConnectionStrings:GccsDatabase", string.Empty);
            builder.ConfigureServices(services =>
            {
                services.AddDbContext<GccsDbContext>(options => options.UseInMemoryDatabase(databaseName));
                services.AddScoped<TenantService>();
                services.AddScoped<ITenantRepository, EfTenantRepository>();
                services.AddScoped<IAuditEventWriter, EfAuditEventWriter>();

                using var provider = services.BuildServiceProvider();
                using var scope = provider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
                dbContext.Database.EnsureDeleted();
                dbContext.Database.EnsureCreated();
                seed?.Invoke(dbContext);
            });
        });

    private static HttpRequestMessage CreateRequest<TContent>(
        HttpMethod method,
        string requestUri,
        TContent? content,
        Guid tenantId,
        Guid userId,
        Permission permission)
    {
        var request = CreateRequest(method, requestUri, tenantId, userId, permission);
        if (content is not null)
        {
            request.Content = JsonContent.Create(content, options: JsonOptions);
        }

        return request;
    }

    private static HttpRequestMessage CreateRequest(
        HttpMethod method,
        string requestUri,
        Guid tenantId,
        Guid userId,
        Permission permission)
    {
        var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Add("X-Gccs-Dev-Auth", "true");
        request.Headers.Add("X-Gccs-Dev-Tenant", tenantId.ToString());
        request.Headers.Add("X-Gccs-Dev-User", userId.ToString());
        request.Headers.Add("X-Gccs-Dev-Permissions", permission.ToString());

        return request;
    }

    private static TenantEntity CreateTenantEntity(Guid tenantId, string name) =>
        new()
        {
            Id = tenantId,
            Name = name,
            Status = TenantStatus.Active,
            DataPosture = TenantDataPosture.NoCui,
            CreatedAt = DateTimeOffset.Parse("2026-06-13T12:00:00Z")
        };
}
