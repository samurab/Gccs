using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Audit;
using Gccs.Application.Contracts;
using Gccs.Application.NoCui;
using Gccs.Domain.Audit;
using Gccs.Domain.Companies;
using Gccs.Domain.Compliance;
using Gccs.Domain.Contracts;
using Gccs.Domain.Identity;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Audit;
using Gccs.Infrastructure.Contracts;
using Gccs.Infrastructure.NoCui;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class ContractRecordTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;

    public ContractRecordTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TC_8_1_1_Create_draft_and_active_contract_records()
    {
        var tenantId = Guid.Parse("81818181-8181-8181-8181-8181818181a1");
        await using var factory = CreateFactory("tc-8-1-1", dbContext => SeedTenant(dbContext, tenantId));
        using var client = factory.CreateClient();

        using var draftRequest = CreateRequest(
            HttpMethod.Post,
            "/api/contracts",
            CreateRequestBody("W15QKN-26-C-0001", ContractStatus.Draft),
            tenantId,
            Guid.NewGuid(),
            Permission.ManageContracts);
        var draftResponse = await client.SendAsync(draftRequest);
        var draft = await draftResponse.Content.ReadFromJsonAsync<ContractDto>(JsonOptions);
        using var activeRequest = CreateRequest(
            HttpMethod.Post,
            "/api/contracts",
            CreateRequestBody("W15QKN-26-C-0002", ContractStatus.Active),
            tenantId,
            Guid.NewGuid(),
            Permission.ManageContracts);
        var activeResponse = await client.SendAsync(activeRequest);
        var active = await activeResponse.Content.ReadFromJsonAsync<ContractDto>(JsonOptions);

        Assert.Equal(HttpStatusCode.Created, draftResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, activeResponse.StatusCode);
        Assert.NotNull(draft);
        Assert.NotNull(active);
        Assert.Equal(ContractStatus.Draft, draft.Status);
        Assert.Equal(ContractStatus.Active, active.Status);
        Assert.Equal(DataHandlingPosture.FciOnly, active.DataHandlingPosture);
    }

    [Fact]
    public async Task TC_8_1_2_Contract_list_is_tenant_scoped()
    {
        var tenantAId = Guid.Parse("81818181-8181-8181-8181-8181818181a2");
        var tenantBId = Guid.Parse("81818181-8181-8181-8181-8181818181b2");
        await using var factory = CreateFactory("tc-8-1-2", dbContext =>
        {
            SeedTenant(dbContext, tenantAId, "Tenant A");
            SeedTenant(dbContext, tenantBId, "Tenant B");
            dbContext.Contracts.Add(CreateContractEntity(tenantAId, "A-ONLY", "Tenant A contract"));
            dbContext.Contracts.Add(CreateContractEntity(tenantBId, "B-ONLY", "Tenant B contract"));
        });
        using var client = factory.CreateClient();

        using var tenantARequest = CreateRequest(HttpMethod.Get, "/api/contracts", tenantAId, Guid.NewGuid(), Permission.ViewContracts);
        var tenantAResponse = await client.SendAsync(tenantARequest);
        var tenantAContracts = await tenantAResponse.Content.ReadFromJsonAsync<ContractDto[]>(JsonOptions);
        using var tenantBRequest = CreateRequest(HttpMethod.Get, "/api/contracts", tenantBId, Guid.NewGuid(), Permission.ViewContracts);
        var tenantBResponse = await client.SendAsync(tenantBRequest);
        var tenantBContracts = await tenantBResponse.Content.ReadFromJsonAsync<ContractDto[]>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, tenantAResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, tenantBResponse.StatusCode);
        Assert.NotNull(tenantAContracts);
        Assert.NotNull(tenantBContracts);
        Assert.Equal(["A-ONLY"], tenantAContracts.Select(contract => contract.ContractNumber).ToArray());
        Assert.Equal(["B-ONLY"], tenantBContracts.Select(contract => contract.ContractNumber).ToArray());
    }

    [Fact]
    public async Task TC_8_1_3_Contract_detail_shows_key_fields()
    {
        var tenantId = Guid.Parse("81818181-8181-8181-8181-8181818181a3");
        await using var factory = CreateFactory("tc-8-1-3", dbContext => SeedTenant(dbContext, tenantId));
        using var client = factory.CreateClient();
        using var createRequest = CreateRequest(
            HttpMethod.Post,
            "/api/contracts",
            CreateRequestBody("FA8750-26-F-0003", ContractStatus.Active),
            tenantId,
            Guid.NewGuid(),
            Permission.ManageContracts);
        var createResponse = await client.SendAsync(createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<ContractDto>(JsonOptions);
        Assert.NotNull(created);

        using var detailRequest = CreateRequest(HttpMethod.Get, $"/api/contracts/{created.Id}", tenantId, Guid.NewGuid(), Permission.ViewContracts);
        var detailResponse = await client.SendAsync(detailRequest);
        var detail = await detailResponse.Content.ReadFromJsonAsync<ContractDto>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
        Assert.NotNull(detail);
        Assert.Equal("FA8750-26-F-0003", detail.ContractNumber);
        Assert.Equal("Department of Defense", detail.AgencyOrPrimeName);
        Assert.Equal(ContractorRelationship.Subcontractor, detail.Relationship);
        Assert.Equal(ContractKind.FixedPrice, detail.Kind);
        Assert.Equal(new DateOnly(2026, 7, 1), detail.PeriodOfPerformanceStart);
        Assert.Equal(new DateOnly(2027, 6, 30), detail.PeriodOfPerformanceEnd);
        Assert.Equal(DataHandlingPosture.FciOnly, detail.DataHandlingPosture);
    }

    [Fact]
    public async Task TC_8_1_4_Contract_create_and_update_are_audit_logged()
    {
        var tenantId = Guid.Parse("81818181-8181-8181-8181-8181818181a4");
        var actorUserId = Guid.Parse("81818181-8181-8181-8181-8181818181b4");
        await using var factory = CreateFactory("tc-8-1-4", dbContext => SeedTenant(dbContext, tenantId));
        using var client = factory.CreateClient();

        using var createRequest = CreateRequest(
            HttpMethod.Post,
            "/api/contracts",
            CreateRequestBody("N00178-26-C-0004", ContractStatus.Draft),
            tenantId,
            actorUserId,
            Permission.ManageContracts);
        var createResponse = await client.SendAsync(createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<ContractDto>(JsonOptions);
        Assert.NotNull(created);
        using var updateRequest = CreateRequest(
            HttpMethod.Put,
            $"/api/contracts/{created.Id}",
            CreateRequestBody("N00178-26-C-0004", ContractStatus.Active) with { Title = "Updated support services contract" },
            tenantId,
            actorUserId,
            Permission.ManageContracts);
        var updateResponse = await client.SendAsync(updateRequest);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var auditEvents = await dbContext.AuditLogEntries
            .Where(audit => audit.TenantId == tenantId && audit.EntityType == "Contract")
            .OrderBy(audit => audit.OccurredAt)
            .ToArrayAsync();

        Assert.Equal([AuditAction.Created, AuditAction.Updated], auditEvents.Select(audit => audit.Action).ToArray());
        Assert.All(auditEvents, audit => Assert.Equal(actorUserId, audit.ActorUserId));
    }

    [Fact]
    public async Task TC_8_2_1_Contract_document_upload_requires_no_cui_acknowledgement()
    {
        var tenantId = Guid.Parse("82828282-8282-8282-8282-8282828282a1");
        var contractId = Guid.Parse("82828282-8282-8282-8282-8282828282b1");
        await using var factory = CreateFactory("tc-8-2-1", dbContext =>
        {
            SeedTenant(dbContext, tenantId);
            dbContext.Contracts.Add(CreateContractEntity(tenantId, "DOC-ACK", "Document acknowledgement contract", contractId));
        });
        using var client = factory.CreateClient();

        using var uploadRequest = CreateRequest(
            HttpMethod.Post,
            $"/api/contracts/{contractId}/documents",
            CreateDocumentRequest("contract.pdf", "application/pdf"),
            tenantId,
            Guid.NewGuid(),
            Permission.ManageContracts);
        var uploadResponse = await client.SendAsync(uploadRequest);

        Assert.Equal((HttpStatusCode)428, uploadResponse.StatusCode);
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Empty(await dbContext.Set<ContractDocumentEntity>().Where(document => document.ContractId == contractId).ToArrayAsync());
    }

    [Fact]
    public async Task TC_8_2_2_Valid_contract_document_metadata_is_linked_to_contract()
    {
        var tenantId = Guid.Parse("82828282-8282-8282-8282-8282828282a2");
        var userId = Guid.Parse("82828282-8282-8282-8282-8282828282b2");
        var contractId = Guid.Parse("82828282-8282-8282-8282-8282828282c2");
        await using var factory = CreateFactory("tc-8-2-2", dbContext =>
        {
            SeedTenant(dbContext, tenantId);
            dbContext.NoCuiAcknowledgements.Add(CreateAcknowledgement(tenantId, userId));
            dbContext.Contracts.Add(CreateContractEntity(tenantId, "DOC-LINK", "Document link contract", contractId));
        });
        using var client = factory.CreateClient();

        using var uploadRequest = CreateRequest(
            HttpMethod.Post,
            $"/api/contracts/{contractId}/documents",
            CreateDocumentRequest("sow.pdf", "application/pdf") with { Type = ContractDocumentType.StatementOfWork },
            tenantId,
            userId,
            Permission.ManageContracts);
        var uploadResponse = await client.SendAsync(uploadRequest);
        var document = await uploadResponse.Content.ReadFromJsonAsync<ContractDocumentDto>(JsonOptions);

        Assert.Equal(HttpStatusCode.Created, uploadResponse.StatusCode);
        Assert.NotNull(document);
        Assert.Equal(contractId, document.ContractId);
        Assert.Equal(ContractDocumentType.StatementOfWork, document.Type);
        Assert.Equal("sow.pdf", document.FileName);
        Assert.StartsWith($"pending://contracts/{contractId}/documents/{document.Id}/", document.StorageUri, StringComparison.Ordinal);
        Assert.Equal("accepted", document.ValidationStatus);
        Assert.Equal("scan-pending", document.MalwareScanStatus);
    }

    [Fact]
    public async Task TC_8_2_3_Disallowed_contract_document_file_is_rejected()
    {
        var tenantId = Guid.Parse("82828282-8282-8282-8282-8282828282a3");
        var userId = Guid.Parse("82828282-8282-8282-8282-8282828282b3");
        var contractId = Guid.Parse("82828282-8282-8282-8282-8282828282c3");
        await using var factory = CreateFactory("tc-8-2-3", dbContext =>
        {
            SeedTenant(dbContext, tenantId);
            dbContext.NoCuiAcknowledgements.Add(CreateAcknowledgement(tenantId, userId));
            dbContext.Contracts.Add(CreateContractEntity(tenantId, "DOC-BLOCK", "Document block contract", contractId));
        });
        using var client = factory.CreateClient();

        using var uploadRequest = CreateRequest(
            HttpMethod.Post,
            $"/api/contracts/{contractId}/documents",
            CreateDocumentRequest("installer.exe", "application/x-msdownload"),
            tenantId,
            userId,
            Permission.ManageContracts);
        var uploadResponse = await client.SendAsync(uploadRequest);

        Assert.Equal(HttpStatusCode.BadRequest, uploadResponse.StatusCode);
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Empty(await dbContext.Set<ContractDocumentEntity>().Where(document => document.ContractId == contractId).ToArrayAsync());
        Assert.Contains(await dbContext.AuditLogEntries.ToArrayAsync(), audit => audit.EntityType == "ContractDocument" && audit.Action == AuditAction.Rejected);
    }

    [Fact]
    public async Task TC_8_2_4_Contract_document_upload_and_delete_are_audit_logged()
    {
        var tenantId = Guid.Parse("82828282-8282-8282-8282-8282828282a4");
        var userId = Guid.Parse("82828282-8282-8282-8282-8282828282b4");
        var contractId = Guid.Parse("82828282-8282-8282-8282-8282828282c4");
        await using var factory = CreateFactory("tc-8-2-4", dbContext =>
        {
            SeedTenant(dbContext, tenantId);
            dbContext.NoCuiAcknowledgements.Add(CreateAcknowledgement(tenantId, userId));
            dbContext.Contracts.Add(CreateContractEntity(tenantId, "DOC-AUDIT", "Document audit contract", contractId));
        });
        using var client = factory.CreateClient();

        using var uploadRequest = CreateRequest(
            HttpMethod.Post,
            $"/api/contracts/{contractId}/documents",
            CreateDocumentRequest("subcontract.pdf", "application/pdf") with { Type = ContractDocumentType.Subcontract },
            tenantId,
            userId,
            Permission.ManageContracts);
        var uploadResponse = await client.SendAsync(uploadRequest);
        var document = await uploadResponse.Content.ReadFromJsonAsync<ContractDocumentDto>(JsonOptions);
        Assert.NotNull(document);
        using var deleteRequest = CreateRequest(
            HttpMethod.Delete,
            $"/api/contracts/{contractId}/documents/{document.Id}",
            tenantId,
            userId,
            Permission.ManageContracts);
        var deleteResponse = await client.SendAsync(deleteRequest);

        Assert.Equal(HttpStatusCode.Created, uploadResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var auditEvents = await dbContext.AuditLogEntries
            .Where(audit => audit.TenantId == tenantId && audit.EntityType == "ContractDocument")
            .OrderBy(audit => audit.OccurredAt)
            .ToArrayAsync();

        Assert.Equal([AuditAction.Uploaded, AuditAction.Deleted], auditEvents.Select(audit => audit.Action).ToArray());
    }

    [Fact]
    public async Task TC_8_3_1_Deliverables_appear_on_contract_detail()
    {
        var tenantId = Guid.Parse("83838383-8383-8383-8383-8383838383a1");
        var contractId = Guid.Parse("83838383-8383-8383-8383-8383838383b1");
        await using var factory = CreateFactory("tc-8-3-1", dbContext =>
        {
            SeedTenant(dbContext, tenantId);
            dbContext.Contracts.Add(CreateContractEntity(tenantId, "DEL-DETAIL", "Deliverable detail contract", contractId));
        });
        using var client = factory.CreateClient();

        using var createRequest = CreateRequest(
            HttpMethod.Post,
            $"/api/contracts/{contractId}/deliverables",
            CreateDeliverableRequest("Monthly status report", new DateOnly(2026, 8, 15)),
            tenantId,
            Guid.NewGuid(),
            Permission.ManageContracts);
        var createResponse = await client.SendAsync(createRequest);
        using var listRequest = CreateRequest(HttpMethod.Get, $"/api/contracts/{contractId}/deliverables", tenantId, Guid.NewGuid(), Permission.ViewContracts);
        var listResponse = await client.SendAsync(listRequest);
        var deliverables = await listResponse.Content.ReadFromJsonAsync<ContractDeliverableDto[]>(JsonOptions);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var deliverable = Assert.Single(deliverables ?? []);
        Assert.Equal("Monthly status report", deliverable.Name);
        Assert.Equal("Contracts", deliverable.OwnerFunction);
        Assert.Equal(DeliverableStatus.NotStarted, deliverable.Status);
    }

    [Fact]
    public async Task TC_8_3_2_Deliverable_due_dates_create_calendar_tasks()
    {
        var tenantId = Guid.Parse("83838383-8383-8383-8383-8383838383a2");
        var contractId = Guid.Parse("83838383-8383-8383-8383-8383838383b2");
        var actorUserId = Guid.Parse("83838383-8383-8383-8383-8383838383c2");
        var dueAt = new DateOnly(2026, 9, 1);
        await using var factory = CreateFactory("tc-8-3-2", dbContext =>
        {
            SeedTenant(dbContext, tenantId);
            dbContext.Contracts.Add(CreateContractEntity(tenantId, "DEL-TASK", "Deliverable task contract", contractId));
        });
        using var client = factory.CreateClient();

        using var createRequest = CreateRequest(
            HttpMethod.Post,
            $"/api/contracts/{contractId}/deliverables",
            CreateDeliverableRequest("Submit security plan", dueAt),
            tenantId,
            actorUserId,
            Permission.ManageContracts);
        var createResponse = await client.SendAsync(createRequest);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var task = await dbContext.ComplianceTasks.SingleAsync(task => task.ContractId == contractId && task.Title == "Submit security plan");
        Assert.Equal(tenantId, task.TenantId);
        Assert.Equal(ComplianceTaskType.CalendarReminder, task.Type);
        Assert.Equal(dueAt, task.DueAt);
        Assert.Equal(actorUserId, task.CreatedByUserId);
    }

    [Fact]
    public async Task TC_8_3_3_Past_due_incomplete_deliverables_are_flagged()
    {
        var tenantId = Guid.Parse("83838383-8383-8383-8383-8383838383a3");
        var contractId = Guid.Parse("83838383-8383-8383-8383-8383838383b3");
        await using var factory = CreateFactory("tc-8-3-3", dbContext =>
        {
            SeedTenant(dbContext, tenantId);
            dbContext.Contracts.Add(CreateContractEntity(tenantId, "DEL-LATE", "Deliverable overdue contract", contractId));
            dbContext.Set<ContractDeliverableEntity>().Add(new ContractDeliverableEntity
            {
                Id = Guid.Parse("83838383-8383-8383-8383-8383838383c3"),
                ContractId = contractId,
                Name = "Late report",
                Description = "Past due",
                DueAt = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-2),
                OwnerFunction = "Contracts",
                Status = DeliverableStatus.InProgress
            });
        });
        using var client = factory.CreateClient();

        using var listRequest = CreateRequest(HttpMethod.Get, $"/api/contracts/{contractId}/deliverables", tenantId, Guid.NewGuid(), Permission.ViewContracts);
        var listResponse = await client.SendAsync(listRequest);
        var deliverables = await listResponse.Content.ReadFromJsonAsync<ContractDeliverableDto[]>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        Assert.True(Assert.Single(deliverables ?? []).IsOverdue);
    }

    [Fact]
    public async Task TC_8_3_4_Deliverable_status_changes_are_audit_logged()
    {
        var tenantId = Guid.Parse("83838383-8383-8383-8383-8383838383a4");
        var contractId = Guid.Parse("83838383-8383-8383-8383-8383838383b4");
        var actorUserId = Guid.Parse("83838383-8383-8383-8383-8383838383c4");
        await using var factory = CreateFactory("tc-8-3-4", dbContext =>
        {
            SeedTenant(dbContext, tenantId);
            dbContext.Contracts.Add(CreateContractEntity(tenantId, "DEL-AUDIT", "Deliverable audit contract", contractId));
        });
        using var client = factory.CreateClient();

        using var createRequest = CreateRequest(
            HttpMethod.Post,
            $"/api/contracts/{contractId}/deliverables",
            CreateDeliverableRequest("Closeout package", new DateOnly(2026, 10, 1)),
            tenantId,
            actorUserId,
            Permission.ManageContracts);
        var createResponse = await client.SendAsync(createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<ContractDeliverableDto>(JsonOptions);
        Assert.NotNull(created);
        using var updateRequest = CreateRequest(
            HttpMethod.Put,
            $"/api/contracts/{contractId}/deliverables/{created.Id}",
            CreateDeliverableRequest("Closeout package", new DateOnly(2026, 10, 1)) with { Status = DeliverableStatus.Submitted },
            tenantId,
            actorUserId,
            Permission.ManageContracts);
        var updateResponse = await client.SendAsync(updateRequest);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var auditEvents = await dbContext.AuditLogEntries
            .Where(audit => audit.TenantId == tenantId && audit.EntityType == "ContractDeliverable")
            .OrderBy(audit => audit.OccurredAt)
            .ToArrayAsync();

        Assert.Equal([AuditAction.Created, AuditAction.Updated], auditEvents.Select(audit => audit.Action).ToArray());
    }

    private WebApplicationFactory<Program> CreateFactory(string databaseName, Action<GccsDbContext>? seed = null) =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.UseSetting("ConnectionStrings:GccsDatabase", string.Empty);
            builder.ConfigureServices(services =>
            {
                services.AddDbContext<GccsDbContext>(options => options.UseInMemoryDatabase(databaseName));
                services.AddScoped<ContractService>();
                services.AddScoped<IContractRepository, EfContractRepository>();
                services.AddScoped<INoCuiAcknowledgementRepository, EfNoCuiAcknowledgementRepository>();
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

    private static UpsertContractRequest CreateRequestBody(string contractNumber, ContractStatus status) =>
        new(
            contractNumber,
            "Base operations support services",
            "Department of Defense",
            ContractorRelationship.Subcontractor,
            ContractKind.FixedPrice,
            status,
            new DateOnly(2026, 6, 15),
            new DateOnly(2026, 7, 1),
            new DateOnly(2027, 6, 30),
            "Arlington, VA",
            "No-CUI contract intake record for compliance tracking.",
            DataHandlingPosture.FciOnly);

    private static ContractEntity CreateContractEntity(Guid tenantId, string contractNumber, string title, Guid? id = null) =>
        new()
        {
            Id = id ?? Guid.NewGuid(),
            TenantId = tenantId,
            ContractNumber = contractNumber,
            Title = title,
            AgencyOrPrimeName = "Sample Prime",
            Relationship = ContractorRelationship.Prime,
            Kind = ContractKind.PurchaseOrder,
            Status = ContractStatus.Active,
            AwardedAt = new DateOnly(2026, 6, 15),
            PeriodOfPerformanceStart = new DateOnly(2026, 7, 1),
            PeriodOfPerformanceEnd = new DateOnly(2027, 6, 30),
            PlaceOfPerformance = "Remote",
            Description = "Seeded tenant-scoped contract.",
            DataHandlingPosture = DataHandlingPosture.NoFciOrCui,
            CreatedAt = DateTimeOffset.UtcNow
        };

    private static ContractDocumentUploadRequest CreateDocumentRequest(string fileName, string contentType) =>
        new(
            ContractDocumentType.Contract,
            fileName,
            contentType,
            2048,
            false);

    private static UpsertContractDeliverableRequest CreateDeliverableRequest(string name, DateOnly dueAt) =>
        new(
            name,
            "Deliverable tracked from contract intake.",
            dueAt,
            "Contracts",
            DeliverableStatus.NotStarted);

    private static NoCuiAcknowledgementEntity CreateAcknowledgement(Guid tenantId, Guid userId) =>
        new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            NoticeVersion = NoCuiNotice.CurrentVersion,
            NoticeCopy = NoCuiNotice.Copy,
            AcknowledgedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByUserId = userId
        };

    private static HttpRequestMessage CreateRequest<TContent>(
        HttpMethod method,
        string requestUri,
        TContent content,
        Guid tenantId,
        Guid userId,
        Permission permission)
    {
        var request = CreateRequest(method, requestUri, tenantId, userId, permission);
        request.Content = JsonContent.Create(content, options: JsonOptions);
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

    private static void SeedTenant(GccsDbContext dbContext, Guid tenantId, string name = "Contract Tenant")
    {
        dbContext.Tenants.Add(new TenantEntity
        {
            Id = tenantId,
            Name = name,
            Status = TenantStatus.Active,
            DataPosture = TenantDataPosture.NoCui,
            CreatedAt = DateTimeOffset.UtcNow
        });
    }
}
