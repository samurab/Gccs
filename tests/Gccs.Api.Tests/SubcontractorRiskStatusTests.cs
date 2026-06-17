using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Audit;
using Gccs.Application.Security;
using Gccs.Application.Subcontractors;
using Gccs.Domain.Evidence;
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

public sealed class SubcontractorRiskStatusTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;

    public SubcontractorRiskStatusTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TC_24_2_1_and_TC_24_2_2_Risk_status_and_drivers_are_visible_to_authorized_users()
    {
        var tenantId = Guid.Parse("24224224-2242-2422-4224-2242242242a1");
        var otherTenantId = Guid.Parse("24224224-2242-2422-4224-2242242242b1");
        await using var factory = CreateFactory("tc-24-2-1", dbContext =>
        {
            SeedTenant(dbContext, tenantId);
            SeedTenant(dbContext, otherTenantId);
            dbContext.Subcontractors.Add(CreateSubcontractor(tenantId, "Low Risk Supplier"));
            var highRisk = CreateSubcontractor(tenantId, "High Risk Supplier");
            highRisk.InsuranceExpiresAt = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);
            highRisk.SamRegistrationStatus = "Inactive";
            dbContext.Subcontractors.Add(highRisk);
            dbContext.SubcontractorEvidenceRequests.Add(new SubcontractorEvidenceRequestEntity
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                SubcontractorId = highRisk.Id,
                RequestedItem = "Updated insurance certificate",
                RequestedEvidenceTypesJson = JsonSerializer.Serialize(new[] { EvidenceType.VendorAttestation }, JsonOptions),
                DueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-2),
                Status = SubcontractorEvidenceRequestStatus.Sent,
                CreatedAt = DateTimeOffset.UtcNow
            });
            dbContext.Subcontractors.Add(CreateSubcontractor(otherTenantId, "Other Tenant Risk"));
        });
        using var client = factory.CreateClient();

        var items = await ListSubcontractorsAsync(client, tenantId);

        var lowRisk = Assert.Single(items, item => item.Name == "Low Risk Supplier");
        Assert.Equal("Low", lowRisk.RiskStatus);
        Assert.Contains(lowRisk.RiskDrivers, driver => driver.Contains("No elevated", StringComparison.OrdinalIgnoreCase));

        var high = Assert.Single(items, item => item.Name == "High Risk Supplier");
        Assert.Equal("High", high.RiskStatus);
        Assert.Contains(high.RiskDrivers, driver => driver.Contains("Insurance is expired", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(high.RiskDrivers, driver => driver.Contains("SAM registration", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(high.RiskDrivers, driver => driver.Contains("evidence requests are overdue", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(items, item => item.Name == "Other Tenant Risk");
    }

    [Fact]
    public async Task TC_24_2_3_Updating_risk_inputs_recalculates_status()
    {
        var tenantId = Guid.Parse("24224224-2242-2422-4224-2242242242a3");
        await using var factory = CreateFactory("tc-24-2-3", dbContext =>
        {
            SeedTenant(dbContext, tenantId);
            var subcontractor = CreateSubcontractor(tenantId, "Recalculated Supplier");
            subcontractor.InsuranceExpiresAt = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(15);
            subcontractor.NdaStatus = "Unsigned";
            dbContext.Subcontractors.Add(subcontractor);
        });
        using var client = factory.CreateClient();
        var before = Assert.Single(await ListSubcontractorsAsync(client, tenantId));
        Assert.Equal("Medium", before.RiskStatus);

        using var update = CreateRequest(
            HttpMethod.Put,
            $"/api/subcontractors/{before.Id}",
            UpsertRequest("Recalculated Supplier") with
            {
                InsuranceExpiresAt = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(120),
                NdaStatus = "Signed"
            },
            tenantId,
            Permission.ManageSubcontractors);
        var updateResponse = await client.SendAsync(update);
        var updated = await updateResponse.Content.ReadFromJsonAsync<SubcontractorDto>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        Assert.NotNull(updated);
        Assert.Equal("Low", updated.RiskStatus);
        Assert.Contains(updated.RiskDrivers, driver => driver.Contains("No elevated", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task TC_24_2_4_Missing_or_unknown_data_produces_needs_review()
    {
        var tenantId = Guid.Parse("24224224-2242-2422-4224-2242242242a4");
        await using var factory = CreateFactory("tc-24-2-4", dbContext =>
        {
            SeedTenant(dbContext, tenantId);
            var subcontractor = CreateSubcontractor(tenantId, "Needs Review Supplier");
            subcontractor.InsuranceExpiresAt = null;
            subcontractor.NdaStatus = "Unknown";
            subcontractor.HasCuiAccess = true;
            subcontractor.CmmcStatus = "Unknown";
            dbContext.Subcontractors.Add(subcontractor);
        });
        using var client = factory.CreateClient();

        var item = Assert.Single(await ListSubcontractorsAsync(client, tenantId));

        Assert.Equal("NeedsReview", item.RiskStatus);
        Assert.Contains(item.RiskDrivers, driver => driver.Contains("Insurance expiration is missing", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(item.RiskDrivers, driver => driver.Contains("NDA status is unknown", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(item.RiskDrivers, driver => driver.Contains("CMMC status is unknown", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void TC_24_2_5_Risk_inputs_are_documented()
    {
        var rulesPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "docs",
            "subcontractor-risk-rules.md"));
        var rules = File.ReadAllText(rulesPath);

        Assert.Contains("insurance", rules, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("NDA", rules, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CMMC", rules, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("SAM", rules, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Flow-down", rules, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Evidence requests", rules, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<SubcontractorDto[]> ListSubcontractorsAsync(HttpClient client, Guid tenantId)
    {
        using var request = CreateRequest<object?>(HttpMethod.Get, "/api/subcontractors", null, tenantId, Permission.ViewSubcontractors);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<SubcontractorDto[]>(JsonOptions) ?? [];
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

    private static HttpRequestMessage CreateRequest<TContent>(
        HttpMethod method,
        string requestUri,
        TContent content,
        Guid tenantId,
        Permission permission)
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

    private static SubcontractorEntity CreateSubcontractor(Guid tenantId, string name) =>
        new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            Uei = "123456789012",
            CageCode = "1ABC2",
            SamRegistrationStatus = "Active",
            Status = SubcontractorStatus.Active,
            RoleDescription = "Specialty services",
            SmallBusinessStatus = "Small",
            NaicsCodesJson = JsonSerializer.Serialize(new[] { "541511" }, JsonOptions),
            CertificationsJson = JsonSerializer.Serialize(new[] { "WOSB" }, JsonOptions),
            CmmcStatus = "Level 2 ready",
            InsuranceExpiresAt = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(120),
            NdaStatus = "OnFile",
            WorkshareDescription = "Forty percent workshare",
            WorksharePercentage = 40m,
            HasFciAccess = true,
            HasCuiAccess = false,
            HasExportControlledAccess = false,
            OwnerFunction = "Supplier manager",
            CreatedAt = DateTimeOffset.UtcNow
        };

    private static UpsertSubcontractorRequest UpsertRequest(string name) =>
        new(
            name,
            "123456789012",
            "1ABC2",
            SubcontractorStatus.Active,
            "Specialty services",
            "Small",
            "Level 2 ready",
            DateOnly.FromDateTime(DateTime.UtcNow).AddDays(120),
            "OnFile",
            "Forty percent workshare",
            40m,
            true,
            false,
            false,
            "Level2",
            "Pat Contact",
            "pat@example.test",
            "555-0100",
            "Contracts Manager",
            [],
            ["541511"],
            ["WOSB"],
            "Supplier manager");
}
