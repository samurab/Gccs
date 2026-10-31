using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Audit;
using Gccs.Application.Security;
using Gccs.Application.Subcontractors;
using Gccs.Domain.Identity;
using Gccs.Domain.Tenancy;
using Gccs.Domain.Vendors;
using Gccs.Infrastructure.Audit;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Gccs.Infrastructure.Subcontractors;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class ExpandedSubcontractorProfileTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;

    public ExpandedSubcontractorProfileTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TC_24_1_1_and_TC_24_1_2_Create_and_update_expanded_fields_with_completeness()
    {
        var tenantId = Guid.Parse("24124124-1241-2412-4124-1241241241a1");
        await using var factory = CreateFactory("tc-24-1-1", dbContext => SeedTenant(dbContext, tenantId));
        using var client = factory.CreateClient();
        using var create = CreateRequest(HttpMethod.Post, "/api/subcontractors", Request("Expanded Sub"), tenantId, Permission.ManageSubcontractors);

        var createResponse = await client.SendAsync(create);
        var created = await createResponse.Content.ReadFromJsonAsync<SubcontractorDto>(JsonOptions);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.NotNull(created);
        Assert.Equal("123456789012", created.Uei);
        Assert.Contains("541511", created.NaicsCodes);
        Assert.Contains("WOSB", created.Certifications);
        Assert.Equal("Supplier manager", created.OwnerFunction);
        Assert.True(created.IsComplete);
        Assert.Equal(100, created.CompletionPercentage);

        using var update = CreateRequest(
            HttpMethod.Put,
            $"/api/subcontractors/{created.Id}",
            Request("Expanded Sub Updated") with { HasCuiAccess = false, HasExportControlledAccess = false },
            tenantId,
            Permission.ManageSubcontractors);
        var updateResponse = await client.SendAsync(update);
        var updated = await updateResponse.Content.ReadFromJsonAsync<SubcontractorDto>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        Assert.NotNull(updated);
        Assert.False(updated.HasCuiAccess);
        Assert.False(updated.HasExportControlledAccess);
    }

    [Fact]
    public async Task TC_24_1_3_and_TC_24_1_4_Filters_are_tenant_scoped_and_surface_expiring_insurance()
    {
        var tenantAId = Guid.Parse("24124124-1241-2412-4124-1241241241a2");
        var tenantBId = Guid.Parse("24124124-1241-2412-4124-1241241241b2");
        await using var factory = CreateFactory("tc-24-1-3", dbContext =>
        {
            SeedTenant(dbContext, tenantAId);
            SeedTenant(dbContext, tenantBId);
            SeedSubcontractor(dbContext, tenantAId, "Expiring", DateOnly.FromDateTime(DateTime.UtcNow).AddDays(10), "Supplier manager");
            SeedSubcontractor(dbContext, tenantAId, "Later", DateOnly.FromDateTime(DateTime.UtcNow).AddDays(120), "Other owner");
            SeedSubcontractor(dbContext, tenantBId, "Other tenant", DateOnly.FromDateTime(DateTime.UtcNow).AddDays(10), "Supplier manager");
        });
        using var client = factory.CreateClient();

        using var expiring = CreateRequest<object?>(HttpMethod.Get, "/api/subcontractors?expiringInsuranceOnly=true", null, tenantAId, Permission.ViewSubcontractors);
        using var owner = CreateRequest<object?>(HttpMethod.Get, "/api/subcontractors?owner=Supplier", null, tenantAId, Permission.ViewSubcontractors);
        var expiringResponse = await client.SendAsync(expiring);
        var ownerResponse = await client.SendAsync(owner);

        var expiringItems = await expiringResponse.Content.ReadFromJsonAsync<SubcontractorDto[]>(JsonOptions) ?? [];
        var ownerItems = await ownerResponse.Content.ReadFromJsonAsync<SubcontractorDto[]>(JsonOptions) ?? [];
        Assert.Equal(HttpStatusCode.OK, expiringResponse.StatusCode);
        Assert.Single(expiringItems);
        Assert.Equal("Expiring", expiringItems.Single().Name);
        Assert.Single(ownerItems);
        Assert.DoesNotContain(ownerItems, item => item.Name == "Other tenant");
    }

    [Fact]
    public async Task TC_24_1_5_Sensitive_field_changes_are_audit_logged()
    {
        var tenantId = Guid.Parse("24124124-1241-2412-4124-1241241241a5");
        await using var factory = CreateFactory("tc-24-1-5", dbContext => SeedTenant(dbContext, tenantId));
        using var client = factory.CreateClient();
        using var create = CreateRequest(HttpMethod.Post, "/api/subcontractors", Request("Audit Sub"), tenantId, Permission.ManageSubcontractors);
        var createResponse = await client.SendAsync(create);
        var created = await createResponse.Content.ReadFromJsonAsync<SubcontractorDto>(JsonOptions);
        Assert.NotNull(created);

        using var update = CreateRequest(
            HttpMethod.Put,
            $"/api/subcontractors/{created.Id}",
            Request("Audit Sub") with { HasCuiAccess = false, HasExportControlledAccess = false },
            tenantId,
            Permission.ManageSubcontractors);
        await client.SendAsync(update);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var audits = await dbContext.AuditLogEntries.Where(entry => entry.TenantId == tenantId && entry.EntityType == "Subcontractor").ToArrayAsync();
        Assert.Contains(audits, audit => audit.MetadataJson.Contains("hasCuiAccess", StringComparison.Ordinal));
        Assert.Contains(audits, audit => audit.MetadataJson.Contains("hasExportControlledAccess", StringComparison.Ordinal));
    }

    private WebApplicationFactory<Program> CreateFactory(string databaseName, Action<GccsDbContext>? seed = null) =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.UseSetting("ConnectionStrings:GccsDatabase", string.Empty);
            builder.ConfigureServices(services =>
            {
                services.AddDbContext<GccsDbContext>(options => options.UseInMemoryDatabase(databaseName));
                services.AddScoped<SubcontractorService>();
                services.AddScoped<ISubcontractorRepository, EfSubcontractorRepository>();
                services.AddScoped<IAuditEventWriter, EfAuditEventWriter>();

                using var provider = services.BuildServiceProvider();
                using var scope = provider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
                dbContext.Database.EnsureDeleted();
                dbContext.Database.EnsureCreated();
                seed?.Invoke(dbContext);
                dbContext.SaveChanges();
            });
        });

    private static UpsertSubcontractorRequest Request(string name) =>
        new(
            name,
            "123456789012",
            "1ABC2",
            SubcontractorStatus.Active,
            "Performs IT services",
            "Small",
            "Level 2 ready",
            DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30),
            "OnFile",
            "50 percent workshare",
            50m,
            true,
            true,
            true,
            "Level2",
            "Pat Contact",
            "pat@example.test",
            "555-0100",
            "Contracts Manager",
            [],
            ["541511"],
            ["WOSB"],
            "Supplier manager");

    private static HttpRequestMessage CreateRequest<TContent>(HttpMethod method, string requestUri, TContent content, Guid tenantId, Permission permission)
    {
        var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Add("X-Gccs-Dev-Auth", "true");
        request.Headers.Add("X-Gccs-Dev-Tenant", tenantId.ToString());
        request.Headers.Add("X-Gccs-Dev-User", Guid.NewGuid().ToString());
        request.Headers.Add("X-Gccs-Dev-Permissions", permission.ToString());
        if (content is not null)
        {
            request.Content = JsonContent.Create(content, options: JsonOptions);
        }

        return request;
    }

    private static void SeedTenant(GccsDbContext dbContext, Guid tenantId)
    {
        dbContext.Tenants.Add(new TenantEntity
        {
            Id = tenantId,
            Name = $"Tenant {tenantId:N}",
            Status = TenantStatus.Active,
            DataPosture = TenantDataPosture.NoCui,
            CreatedAt = DateTimeOffset.UtcNow
        });
    }

    private static void SeedSubcontractor(GccsDbContext dbContext, Guid tenantId, string name, DateOnly insuranceExpiresAt, string owner)
    {
        dbContext.Subcontractors.Add(new SubcontractorEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            Status = SubcontractorStatus.Active,
            RoleDescription = "Services",
            SmallBusinessStatus = "Small",
            CmmcStatus = "Ready",
            InsuranceExpiresAt = insuranceExpiresAt,
            NdaStatus = "OnFile",
            OwnerFunction = owner,
            CreatedAt = DateTimeOffset.UtcNow
        });
    }
}
