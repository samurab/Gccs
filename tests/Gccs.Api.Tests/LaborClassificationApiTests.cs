using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Labor;
using Gccs.Application.Audit;
using Gccs.Domain.Audit;
using Gccs.Domain.Contracts;
using Gccs.Domain.Identity;
using Gccs.Domain.People;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Gccs.Infrastructure.Audit;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class LaborClassificationApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
    private readonly WebApplicationFactory<Program> factory;

    public LaborClassificationApiTests(WebApplicationFactory<Program> factory) => this.factory = factory;

    [Fact]
    public async Task TC_32_2_1_3_5_Durable_routes_redact_sensitive_fields_and_audit_mutations()
    {
        var ids = Ids.Create();
        await using var app = CreateFactory(nameof(TC_32_2_1_3_5_Durable_routes_redact_sensitive_fields_and_audit_mutations), ids);
        using var client = app.CreateClient();

        var category = await CreateCategoryAsync(client, ids, "Help Desk Technician II");
        using var deniedEmployees = await client.SendAsync(Request(HttpMethod.Get,
            "/api/labor-classification/employees", null, ids, Permission.ViewContracts));
        Assert.Equal(HttpStatusCode.Forbidden, deniedEmployees.StatusCode);
        using var allowedEmployees = await client.SendAsync(Request(HttpMethod.Get,
            "/api/labor-classification/employees", null, ids, Permission.ViewSensitiveEmployeeData));
        Assert.Equal(HttpStatusCode.OK, allowedEmployees.StatusCode);
        Assert.Equal("Taylor Employee", (await allowedEmployees.Content.ReadFromJsonAsync<LaborEmployeeOptionDto[]>(JsonOptions))![1].Name);
        using var assignmentResponse = await client.SendAsync(Request(HttpMethod.Post,
            $"/api/contracts/{ids.ContractId}/labor-assignments", Assignment(ids, category.Id), ids,
            Permission.ManageContracts));
        Assert.Equal(HttpStatusCode.Created, assignmentResponse.StatusCode);
        var redacted = await assignmentResponse.Content.ReadFromJsonAsync<LaborEmployeeAssignmentViewDto>(JsonOptions);
        Assert.Null(redacted!.EmployeeName);
        Assert.Null(redacted.EmployeeEmail);
        Assert.Equal(ids.EmployeeId, redacted.EmployeeId);

        using var hrResponse = await client.SendAsync(Request(HttpMethod.Get,
            $"/api/contracts/{ids.ContractId}/labor-assignments/{redacted.Id}", null, ids,
            Permission.ViewContracts, Permission.ViewSensitiveEmployeeData));
        var hrView = await hrResponse.Content.ReadFromJsonAsync<LaborEmployeeAssignmentViewDto>(JsonOptions);
        Assert.Equal("Taylor Employee", hrView!.EmployeeName);
        Assert.Equal("taylor@example.test", hrView.EmployeeEmail);

        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Single(await db.LaborCategories.Where(x => x.TenantId == ids.TenantId).ToArrayAsync());
        var persistedAssignment = Assert.Single(await db.LaborEmployeeAssignments.Where(x => x.TenantId == ids.TenantId).ToArrayAsync());
        Assert.Equal(LaborAssignmentStatus.Active, persistedAssignment.Status);
        var audit = await db.AuditLogEntries.Where(x => x.TenantId == ids.TenantId &&
            (x.EntityType == "LaborCategory" || x.EntityType == "LaborEmployeeAssignment")).ToArrayAsync();
        Assert.Equal(2, audit.Length);
        Assert.All(audit, item => Assert.DoesNotContain("Taylor Employee", JsonSerializer.Serialize(item)));
    }

    [Fact]
    public async Task TC_32_2_2_Invalid_and_cross_tenant_assignments_fail_without_writes_or_audit()
    {
        var ids = Ids.Create();
        await using var app = CreateFactory(nameof(TC_32_2_2_Invalid_and_cross_tenant_assignments_fail_without_writes_or_audit), ids);
        using var client = app.CreateClient();
        var category = await CreateCategoryAsync(client, ids, "Help Desk Technician II");
        var existingAssignment = await CreateAssignmentAsync(client, ids, category.Id);

        using var wrongContractMutation = await client.SendAsync(Request(HttpMethod.Post,
            $"/api/contracts/{ids.OtherContractId}/labor-assignments/{existingAssignment.Id}/deactivate",
            null, ids, Permission.ManageContracts));
        Assert.Equal(HttpStatusCode.NotFound, wrongContractMutation.StatusCode);

        using var activeCategoryDeactivate = await client.SendAsync(Request(HttpMethod.Post,
            $"/api/contracts/{ids.ContractId}/labor-categories/{category.Id}/deactivate", null, ids, Permission.ManageContracts));
        Assert.Equal(HttpStatusCode.BadRequest, activeCategoryDeactivate.StatusCode);

        using var missingSource = await client.SendAsync(Request(HttpMethod.Post,
            $"/api/contracts/{ids.ContractId}/labor-assignments", Assignment(ids, category.Id) with
            { EmployeeId = ids.SecondEmployeeId, SourceReference = " " }, ids, Permission.ManageContracts));
        Assert.Equal(HttpStatusCode.BadRequest, missingSource.StatusCode);

        using var overlap = await client.SendAsync(Request(HttpMethod.Post,
            $"/api/contracts/{ids.ContractId}/labor-assignments", Assignment(ids, category.Id), ids, Permission.ManageContracts));
        Assert.Equal(HttpStatusCode.BadRequest, overlap.StatusCode);

        var inactiveCategory = await CreateCategoryAsync(client, ids, "Inactive technician");
        using var deactivate = await client.SendAsync(Request(HttpMethod.Post,
            $"/api/contracts/{ids.ContractId}/labor-categories/{inactiveCategory.Id}/deactivate", null, ids, Permission.ManageContracts));
        Assert.Equal(HttpStatusCode.OK, deactivate.StatusCode);
        using var inactive = await client.SendAsync(Request(HttpMethod.Post,
            $"/api/contracts/{ids.ContractId}/labor-assignments", Assignment(ids, inactiveCategory.Id) with { EmployeeId = ids.SecondEmployeeId }, ids,
            Permission.ManageContracts));
        Assert.Equal(HttpStatusCode.BadRequest, inactive.StatusCode);

        using var foreign = await client.SendAsync(Request(HttpMethod.Get,
            $"/api/contracts/{ids.ContractId}/labor-assignments", null, ids with { TenantId = ids.OtherTenantId }, Permission.ViewContracts));
        Assert.Equal(HttpStatusCode.OK, foreign.StatusCode);
        Assert.Empty((await foreign.Content.ReadFromJsonAsync<LaborEmployeeAssignmentViewDto[]>(JsonOptions))!);

        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var unchangedAssignment = Assert.Single(await db.LaborEmployeeAssignments.Where(x => x.TenantId == ids.TenantId).ToArrayAsync());
        Assert.Equal(LaborAssignmentStatus.Active, unchangedAssignment.Status);
        Assert.Equal(4, await db.AuditLogEntries.CountAsync(x => x.TenantId == ids.TenantId &&
            (x.EntityType == "LaborCategory" || x.EntityType == "LaborEmployeeAssignment")));
    }

    [Fact]
    public async Task TC_32_2_4_Reclassification_preserves_append_only_history()
    {
        var ids = Ids.Create();
        await using var app = CreateFactory(nameof(TC_32_2_4_Reclassification_preserves_append_only_history), ids);
        using var client = app.CreateClient();
        var original = await CreateCategoryAsync(client, ids, "Help Desk Technician II");
        var next = await CreateCategoryAsync(client, ids, "Network Technician III");
        var assignment = await CreateAssignmentAsync(client, ids, original.Id);

        using var futureCategoryResponse = await client.SendAsync(Request(HttpMethod.Post,
            $"/api/contracts/{ids.ContractId}/labor-categories",
            Category(ids, "Future technician") with { EffectiveStart = new DateOnly(2026, 7, 1) }, ids,
            Permission.ManageContracts));
        var futureCategory = await futureCategoryResponse.Content.ReadFromJsonAsync<LaborCategoryDto>(JsonOptions);
        using var invalidReclassification = await client.SendAsync(Request(HttpMethod.Post,
            $"/api/contracts/{ids.ContractId}/labor-assignments/{assignment.Id}/reclassify",
            new LaborReclassificationRequest(futureCategory!.Id, "Premature change."), ids, Permission.ManageContracts));
        Assert.Equal(HttpStatusCode.BadRequest, invalidReclassification.StatusCode);

        using var response = await client.SendAsync(Request(HttpMethod.Post,
            $"/api/contracts/{ids.ContractId}/labor-assignments/{assignment.Id}/reclassify",
            new LaborReclassificationRequest(next.Id, "Promotion and revised wage-determination mapping."), ids,
            Permission.ManageContracts, Permission.ViewSensitiveEmployeeData));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<LaborEmployeeAssignmentViewDto>(JsonOptions);
        var history = Assert.Single(updated!.History);
        Assert.Equal(original.Id, history.PriorCategoryId);
        Assert.Equal(next.Id, history.NewCategoryId);
        Assert.Equal(ids.UserId, history.ActorUserId);
        Assert.Equal("Promotion and revised wage-determination mapping.", history.Reason);
        Assert.Equal(LaborClassificationReviewStatus.PendingReview, updated.ReviewStatus);

        using var reviewResponse = await client.SendAsync(Request(HttpMethod.Post,
            $"/api/contracts/{ids.ContractId}/labor-assignments/{assignment.Id}/review",
            new LaborClassificationReviewRequest(LaborClassificationReviewStatus.Reviewed, "HR verified the source mapping."), ids,
            Permission.ManageContracts, Permission.ViewSensitiveEmployeeData));
        Assert.Equal(HttpStatusCode.OK, reviewResponse.StatusCode);
        var reviewed = await reviewResponse.Content.ReadFromJsonAsync<LaborEmployeeAssignmentViewDto>(JsonOptions);
        Assert.Equal(LaborClassificationReviewStatus.Reviewed, reviewed!.ReviewStatus);
        Assert.Equal(ids.UserId, reviewed.ReviewedByUserId);
        Assert.NotNull(reviewed.ReviewedAt);

        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Single(await db.LaborClassificationHistory.Where(x => x.TenantId == ids.TenantId).ToArrayAsync());
        Assert.Equal(6, await db.AuditLogEntries.CountAsync(x => x.TenantId == ids.TenantId &&
            (x.EntityType == "LaborCategory" || x.EntityType == "LaborEmployeeAssignment")));
    }

    private WebApplicationFactory<Program> CreateFactory(string databaseName, Ids ids) =>
        factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.UseSetting("ConnectionStrings:GccsDatabase", string.Empty);
            builder.ConfigureServices(services =>
            {
                services.AddDbContext<GccsDbContext>(options => options.UseInMemoryDatabase(databaseName)
                    .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning)));
                services.AddScoped<IAuditEventWriter, EfAuditEventWriter>();
                using var provider = services.BuildServiceProvider();
                using var scope = provider.CreateScope();
                Seed(scope.ServiceProvider.GetRequiredService<GccsDbContext>(), ids);
            });
        });

    private static void Seed(GccsDbContext db, Ids ids)
    {
        db.Tenants.AddRange(
            new TenantEntity { Id = ids.TenantId, Name = "Story 32.2 tenant", Status = TenantStatus.Active, CreatedAt = DateTimeOffset.UtcNow },
            new TenantEntity { Id = ids.OtherTenantId, Name = "Other tenant", Status = TenantStatus.Active, CreatedAt = DateTimeOffset.UtcNow });
        db.Contracts.AddRange(
            Contract(ids.ContractId, ids.TenantId, "FA-32-2"),
            Contract(ids.OtherContractId, ids.TenantId, "FA-32-2-OTHER"));
        db.Employees.AddRange(
            new EmployeeEntity { Id = ids.EmployeeId, TenantId = ids.TenantId, EmployeeNumber = "E-100", Name = "Taylor Employee", Email = "taylor@example.test", Status = EmploymentStatus.Active, CreatedAt = DateTimeOffset.UtcNow },
            new EmployeeEntity { Id = ids.SecondEmployeeId, TenantId = ids.TenantId, EmployeeNumber = "E-200", Name = "Morgan Employee", Email = "morgan@example.test", Status = EmploymentStatus.Active, CreatedAt = DateTimeOffset.UtcNow });
        db.SaveChanges();
    }

    private static LaborCategoryRequest Category(Ids ids, string title) => new(ids.ContractId, title,
        title, 34.12m, 4.98m, "Health and welfare fringe", new(2026, 1, 1), new(2026, 12, 31), "WD-2015-4341 Rev 24");

    private static LaborEmployeeAssignmentRequest Assignment(Ids ids, Guid categoryId) => new(ids.EmployeeId,
        ids.ContractId, categoryId, "Norfolk, VA",
        new(2026, 1, 1), new(2026, 6, 30), "HR classification review 2026-01");

    private static async Task<LaborCategoryDto> CreateCategoryAsync(HttpClient client, Ids ids, string title)
    {
        using var response = await client.SendAsync(Request(HttpMethod.Post, $"/api/contracts/{ids.ContractId}/labor-categories",
            Category(ids, title), ids, Permission.ManageContracts));
        Assert.True(response.StatusCode == HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<LaborCategoryDto>(JsonOptions))!;
    }

    private static async Task<LaborEmployeeAssignmentViewDto> CreateAssignmentAsync(HttpClient client, Ids ids, Guid categoryId)
    {
        using var response = await client.SendAsync(Request(HttpMethod.Post, $"/api/contracts/{ids.ContractId}/labor-assignments",
            Assignment(ids, categoryId), ids, Permission.ManageContracts));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<LaborEmployeeAssignmentViewDto>(JsonOptions))!;
    }

    private static ContractEntity Contract(Guid id, Guid tenantId, string number) => new()
    {
        Id = id, TenantId = tenantId, ContractNumber = number, Title = "Labor contract",
        AgencyOrPrimeName = "Synthetic agency", Relationship = ContractorRelationship.Prime, Kind = ContractKind.FixedPrice,
        Status = ContractStatus.Active, PeriodOfPerformanceStart = new(2026, 1, 1), PeriodOfPerformanceEnd = new(2026, 12, 31),
        PlaceOfPerformance = "Norfolk, VA", Description = "Synthetic No-CUI fixture.", CreatedAt = DateTimeOffset.UtcNow
    };

    private static HttpRequestMessage Request(HttpMethod method, string uri, object? body, Ids ids, params Permission[] permissions)
    {
        var request = new HttpRequestMessage(method, uri);
        if (body is not null) request.Content = JsonContent.Create(body, options: JsonOptions);
        request.Headers.Add("X-Gccs-Dev-Auth", "true");
        request.Headers.Add("X-Gccs-Dev-Tenant", ids.TenantId.ToString());
        request.Headers.Add("X-Gccs-Dev-User", ids.UserId.ToString());
        request.Headers.Add("X-Gccs-Dev-Permissions", string.Join(',', permissions));
        return ClassifiedWorkflowTestData.Confirm(request);
    }

    private sealed record Ids(Guid TenantId, Guid OtherTenantId, Guid ContractId, Guid OtherContractId, Guid EmployeeId, Guid SecondEmployeeId, Guid UserId)
    {
        public static Ids Create() => new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
    }
}
