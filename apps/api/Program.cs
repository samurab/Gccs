using System.Security.Claims;
using System.Text.Json.Serialization;
using Gccs.Api.Security;
using Gccs.Api.LocalDevelopment;
using Gccs.Application.Audit;
using Gccs.Application.Calendar;
using Gccs.Application.Companies;
using Gccs.Application.Cmmc;
using Gccs.Application.Compliance;
using Gccs.Application.Contracts;
using Gccs.Application.Evidence;
using Gccs.Application.Identity;
using Gccs.Application.NoCui;
using Gccs.Application.Repositories;
using Gccs.Application.Reports;
using Gccs.Application.Subcontractors;
using Gccs.Application.Tasks;
using Gccs.Application.Tenancy;
using Gccs.Domain.Identity;
using Gccs.Infrastructure;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

LocalDependencyOptions.ValidateRequiredConfiguration(builder.Configuration);

if (!builder.Environment.IsDevelopment())
{
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
    if (allowedOrigins is null || allowedOrigins.Length == 0 || allowedOrigins.Any(origin => origin.Contains("localhost", StringComparison.OrdinalIgnoreCase)))
    {
        throw new InvalidOperationException("Production CORS origins must be explicitly configured and must not use localhost.");
    }

    var allowedHosts = builder.Configuration["AllowedHosts"];
    if (string.IsNullOrWhiteSpace(allowedHosts) || allowedHosts == "*")
    {
        throw new InvalidOperationException("Production AllowedHosts must be explicitly configured.");
    }
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("web", policy =>
    {
        policy
            .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:3000"])
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddHttpClient();
builder.Services.Configure<LocalDependencyOptions>(builder.Configuration.GetSection(LocalDependencyOptions.SectionName));
builder.Services.AddScoped<LocalDependencyHealthService>();
builder.Services.AddGccsApiSecurity(builder.Configuration, builder.Environment);
builder.Services.AddGccsInfrastructure(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseGccsCorrelationIds();
app.UseGccsApiFailureLogging();
app.UseGccsApiProblemDetails();
app.UseGccsSecurityHeaders();
app.UseCors("web");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", async (LocalDependencyHealthService healthService, CancellationToken cancellationToken) =>
{
    var localDependencies = await healthService.CheckAsync(cancellationToken);
    var response = new
    {
        status = localDependencies.IsHealthy ? "ok" : "degraded",
        service = "gccs-api",
        dataPosture = "No-CUI / compliance management only",
        checkedAt = DateTimeOffset.UtcNow,
        dependencies = localDependencies.Dependencies
    };

    return localDependencies.IsHealthy
        ? Results.Ok(response)
        : Results.Json(response, statusCode: StatusCodes.Status503ServiceUnavailable);
})
.AllowAnonymous()
.WithName("Health");

var api = app.MapGroup("/api")
    .RequireAuthorization()
    .RequireRateLimiting("api");

api.MapGet("/me/access", (ClaimsPrincipal user, ITenantContext tenantContext) =>
{
    var roles = user
        .FindAll(ApiSecurityExtensions.RoleNameClaimType)
        .Concat(user.FindAll(ClaimTypes.Role))
        .Select(claim => claim.Value)
        .Where(roleName => !string.IsNullOrWhiteSpace(roleName))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();
    var permissions = user
        .FindAll(ApiSecurityExtensions.PermissionClaimType)
        .Select(claim => claim.Value)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Order()
        .ToArray();

    return Results.Ok(new
    {
        tenantId = tenantContext.TenantId,
        userId = tenantContext.UserId,
        userEmail = tenantContext.UserEmail,
        roles,
        permissions,
        rolePermissionMatrix = RoleCatalog.PermissionsByRole.ToDictionary(
            role => role.Key,
            role => role.Value.Select(permission => permission.ToString()).Order().ToArray())
    });
})
.WithName("GetCurrentUserAccess");

api.MapGet("/compliance/overview", async (ComplianceOverviewService service, CancellationToken cancellationToken) =>
    Results.Ok(await service.GetOverviewAsync(cancellationToken)))
.RequirePermission(Permission.ViewObligations)
.WithName("GetComplianceOverview");

api.MapGet("/company-profile", async (
    CompanyProfileService service,
    CancellationToken cancellationToken) =>
{
    var profile = await service.GetCurrentTenantProfileAsync(cancellationToken);
    return profile is null ? Results.NoContent() : Results.Ok(profile);
})
.RequirePermission(Permission.ViewCompanyProfile)
.WithName("GetCompanyProfile");

api.MapPut("/company-profile", async (
    UpsertCompanyProfileRequest request,
    CompanyProfileService service,
    ITenantContext tenantContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var profile = await service.SaveCurrentTenantProfileAsync(request, tenantContext.UserId, cancellationToken);
        return Results.Ok(profile);
    }
    catch (CompanyProfileValidationException exception)
    {
        return Results.ValidationProblem(
            exception.Errors.ToDictionary(error => error.Key, error => error.Value),
            title: "Company profile incomplete",
            detail: exception.Message,
            statusCode: StatusCodes.Status400BadRequest);
    }
})
.RequirePermission(Permission.ManageCompanyProfile)
.WithName("SaveCompanyProfile");

api.MapGet("/contracts", async (
    ContractService service,
    CancellationToken cancellationToken) =>
    Results.Ok(await service.ListCurrentTenantAsync(cancellationToken)))
.RequirePermission(Permission.ViewContracts)
.WithName("ListContracts");

api.MapGet("/contracts/{contractId:guid}", async (
    Guid contractId,
    ContractService service,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var contract = await service.FindCurrentTenantAsync(contractId, cancellationToken);
    return contract is null
        ? ApiProblemDetails.Create(
            httpContext,
            "Resource not found",
            $"Contract '{contractId}' was not found.",
            StatusCodes.Status404NotFound,
            "resource_not_found")
        : Results.Ok(contract);
})
.RequirePermission(Permission.ViewContracts)
.WithName("GetContractById");

api.MapPost("/contracts", async (
    UpsertContractRequest request,
    ContractService service,
    ITenantContext tenantContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var contract = await service.CreateCurrentTenantAsync(request, tenantContext.UserId, cancellationToken);
        return Results.Created($"/api/contracts/{contract.Id}", contract);
    }
    catch (ContractValidationException exception)
    {
        return Results.ValidationProblem(
            exception.Errors.ToDictionary(error => error.Key, error => error.Value),
            title: "Contract record invalid",
            detail: exception.Message,
            statusCode: StatusCodes.Status400BadRequest);
    }
})
.RequirePermission(Permission.ManageContracts)
.WithName("CreateContract");

api.MapPut("/contracts/{contractId:guid}", async (
    Guid contractId,
    UpsertContractRequest request,
    ContractService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var contract = await service.UpdateCurrentTenantAsync(contractId, request, tenantContext.UserId, cancellationToken);
        return contract is null
            ? ApiProblemDetails.Create(
                httpContext,
                "Resource not found",
                $"Contract '{contractId}' was not found.",
                StatusCodes.Status404NotFound,
                "resource_not_found")
            : Results.Ok(contract);
    }
    catch (ContractValidationException exception)
    {
        return Results.ValidationProblem(
            exception.Errors.ToDictionary(error => error.Key, error => error.Value),
            title: "Contract record invalid",
            detail: exception.Message,
            statusCode: StatusCodes.Status400BadRequest);
    }
})
.RequirePermission(Permission.ManageContracts)
.WithName("UpdateContract");

api.MapGet("/contracts/{contractId:guid}/documents", async (
    Guid contractId,
    ContractService service,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var documents = await service.ListDocumentsAsync(contractId, cancellationToken);
    return documents is null
        ? ApiProblemDetails.Create(
            httpContext,
            "Resource not found",
            $"Contract '{contractId}' was not found.",
            StatusCodes.Status404NotFound,
            "resource_not_found")
        : Results.Ok(documents);
})
.RequirePermission(Permission.ViewContracts)
.WithName("ListContractDocuments");

api.MapPost("/contracts/{contractId:guid}/documents", async (
    Guid contractId,
    ContractDocumentUploadRequest request,
    ContractService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var document = await service.CreateDocumentMetadataAsync(contractId, request, tenantContext.UserId, cancellationToken);
        return document is null
            ? ApiProblemDetails.Create(
                httpContext,
                "Resource not found",
                $"Contract '{contractId}' was not found.",
                StatusCodes.Status404NotFound,
                "resource_not_found")
            : Results.Created($"/api/contracts/{contractId}/documents/{document.Id}", document);
    }
    catch (NoCuiAcknowledgementRequiredException exception)
    {
        return ApiProblemDetails.Create(
            httpContext,
            "No-CUI acknowledgement required",
            exception.Message,
            StatusCodes.Status428PreconditionRequired,
            "no_cui_acknowledgement_required");
    }
    catch (UploadGuardrailValidationException exception)
    {
        return Results.ValidationProblem(
            exception.Errors.ToDictionary(error => error.Key, error => error.Value),
            title: "Contract document upload rejected",
            detail: exception.Message,
            statusCode: StatusCodes.Status400BadRequest);
    }
})
.RequirePermission(Permission.ManageContracts)
.WithName("CreateContractDocumentMetadata");

api.MapDelete("/contracts/{contractId:guid}/documents/{documentId:guid}", async (
    Guid contractId,
    Guid documentId,
    ContractService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var deleted = await service.DeleteDocumentAsync(contractId, documentId, tenantContext.UserId, cancellationToken);
    return deleted
        ? Results.NoContent()
        : ApiProblemDetails.Create(
            httpContext,
            "Resource not found",
            $"Contract document '{documentId}' was not found.",
            StatusCodes.Status404NotFound,
            "resource_not_found");
})
.RequirePermission(Permission.ManageContracts)
.WithName("DeleteContractDocument");

api.MapGet("/contracts/{contractId:guid}/deliverables", async (
    Guid contractId,
    ContractService service,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var deliverables = await service.ListDeliverablesAsync(contractId, cancellationToken);
    return deliverables is null
        ? ApiProblemDetails.Create(
            httpContext,
            "Resource not found",
            $"Contract '{contractId}' was not found.",
            StatusCodes.Status404NotFound,
            "resource_not_found")
        : Results.Ok(deliverables);
})
.RequirePermission(Permission.ViewContracts)
.WithName("ListContractDeliverables");

api.MapPost("/contracts/{contractId:guid}/deliverables", async (
    Guid contractId,
    UpsertContractDeliverableRequest request,
    ContractService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var deliverable = await service.CreateDeliverableAsync(contractId, request, tenantContext.UserId, cancellationToken);
        return deliverable is null
            ? ApiProblemDetails.Create(
                httpContext,
                "Resource not found",
                $"Contract '{contractId}' was not found.",
                StatusCodes.Status404NotFound,
                "resource_not_found")
            : Results.Created($"/api/contracts/{contractId}/deliverables/{deliverable.Id}", deliverable);
    }
    catch (ContractValidationException exception)
    {
        return Results.ValidationProblem(
            exception.Errors.ToDictionary(error => error.Key, error => error.Value),
            title: "Contract deliverable invalid",
            detail: exception.Message,
            statusCode: StatusCodes.Status400BadRequest);
    }
})
.RequirePermission(Permission.ManageContracts)
.WithName("CreateContractDeliverable");

api.MapPut("/contracts/{contractId:guid}/deliverables/{deliverableId:guid}", async (
    Guid contractId,
    Guid deliverableId,
    UpsertContractDeliverableRequest request,
    ContractService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var deliverable = await service.UpdateDeliverableAsync(contractId, deliverableId, request, tenantContext.UserId, cancellationToken);
        return deliverable is null
            ? ApiProblemDetails.Create(
                httpContext,
                "Resource not found",
                $"Contract deliverable '{deliverableId}' was not found.",
                StatusCodes.Status404NotFound,
                "resource_not_found")
            : Results.Ok(deliverable);
    }
    catch (ContractValidationException exception)
    {
        return Results.ValidationProblem(
            exception.Errors.ToDictionary(error => error.Key, error => error.Value),
            title: "Contract deliverable invalid",
            detail: exception.Message,
            statusCode: StatusCodes.Status400BadRequest);
    }
})
.RequirePermission(Permission.ManageContracts)
.WithName("UpdateContractDeliverable");

api.MapGet("/contracts/{contractId:guid}/clauses", async (
    Guid contractId,
    ContractService service,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var clauses = await service.ListClausesAsync(contractId, cancellationToken);
    return clauses is null
        ? ApiProblemDetails.Create(
            httpContext,
            "Resource not found",
            $"Contract '{contractId}' was not found.",
            StatusCodes.Status404NotFound,
            "resource_not_found")
        : Results.Ok(clauses);
})
.RequirePermission(Permission.ViewContracts)
.WithName("ListContractClauses");

api.MapPost("/contracts/{contractId:guid}/clauses", async (
    Guid contractId,
    AttachContractClauseRequest request,
    ContractService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var clause = await service.AttachClauseAsync(contractId, request, tenantContext.UserId, cancellationToken);
        return clause is null
            ? ApiProblemDetails.Create(
                httpContext,
                "Resource not found",
                $"Contract '{contractId}' or clause '{request.ClauseLibraryId}' was not found.",
                StatusCodes.Status404NotFound,
                "resource_not_found")
            : Results.Created($"/api/contracts/{contractId}/clauses/{clause.Id}", clause);
    }
    catch (ContractValidationException exception)
    {
        return Results.ValidationProblem(
            exception.Errors.ToDictionary(error => error.Key, error => error.Value),
            title: "Contract clause attachment invalid",
            detail: exception.Message,
            statusCode: StatusCodes.Status400BadRequest);
    }
})
.RequirePermission(Permission.ManageContracts)
.WithName("AttachContractClause");

api.MapDelete("/contracts/{contractId:guid}/clauses/{contractClauseId:guid}", async (
    Guid contractId,
    Guid contractClauseId,
    [FromBody] RemoveContractClauseRequest request,
    ContractService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var clause = await service.RemoveClauseAsync(contractId, contractClauseId, request, tenantContext.UserId, cancellationToken);
        return clause is null
            ? ApiProblemDetails.Create(
                httpContext,
                "Resource not found",
                $"Contract clause '{contractClauseId}' was not found.",
                StatusCodes.Status404NotFound,
                "resource_not_found")
            : Results.Ok(clause);
    }
    catch (ContractValidationException exception)
    {
        return Results.ValidationProblem(
            exception.Errors.ToDictionary(error => error.Key, error => error.Value),
            title: "Contract clause removal invalid",
            detail: exception.Message,
            statusCode: StatusCodes.Status400BadRequest);
    }
})
.RequirePermission(Permission.ManageContracts)
.WithName("RemoveContractClause");

api.MapPost("/contracts/{contractId:guid}/clauses/{contractClauseId:guid}/obligations/generate", async (
    Guid contractId,
    Guid contractClauseId,
    ContractService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var generated = await service.GenerateObligationsForClauseAsync(contractId, contractClauseId, tenantContext.UserId, cancellationToken);
    return generated is null
        ? ApiProblemDetails.Create(
            httpContext,
            "Resource not found",
            $"Contract clause '{contractClauseId}' was not found.",
            StatusCodes.Status404NotFound,
            "resource_not_found")
        : Results.Ok(generated);
})
.RequirePermission(Permission.ManageContracts)
.WithName("GenerateContractClauseObligations");

api.MapGet("/clauses", async (
    string? query,
    string? category,
    ClauseLibraryService service,
    ITenantContext tenantContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        return Results.Ok(await service.SearchAsync(
            new ClauseLibrarySearchRequest(query, category, tenantContext.TenantId),
            cancellationToken));
    }
    catch (ClauseLibrarySearchValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["category"] = [exception.Message]
        },
        title: "Clause search invalid",
        detail: exception.Message,
        statusCode: StatusCodes.Status400BadRequest);
    }
})
.RequirePermission(Permission.ViewContracts)
.WithName("SearchClauseLibrary");

api.MapGet("/contract-obligations", async (
    Guid? contractId,
    Gccs.Domain.Compliance.RiskLevel? riskLevel,
    string? owner,
    Gccs.Domain.Compliance.ComplianceTaskStatus? status,
    string? module,
    string? dueDate,
    string? source,
    IObligationDashboardRepository repository,
    CancellationToken cancellationToken) =>
    Results.Ok(await repository.ListCurrentTenantAsync(
        new ObligationDashboardQuery(contractId, riskLevel, owner, status, module, dueDate, source),
        cancellationToken)))
.RequirePermission(Permission.ViewObligations)
.WithName("ListContractObligationDashboard");

api.MapGet("/contract-obligations/{contractClauseId:guid}/{obligationId}", async (
    Guid contractClauseId,
    string obligationId,
    ObligationDetailService service,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var detail = await service.FindCurrentTenantAsync(contractClauseId, obligationId, cancellationToken);
    return detail is null
        ? ApiProblemDetails.Create(
            httpContext,
            "Resource not found",
            $"Contract obligation '{obligationId}' was not found.",
            StatusCodes.Status404NotFound,
            "resource_not_found")
        : Results.Ok(detail);
})
.RequirePermission(Permission.ViewObligations)
.WithName("GetContractObligationDetail");

api.MapPatch("/contract-obligations/{contractClauseId:guid}/{obligationId}/status", async (
    Guid contractClauseId,
    string obligationId,
    UpdateContractObligationStatusRequest request,
    ObligationDetailService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var detail = await service.UpdateStatusAsync(contractClauseId, obligationId, request.Status, tenantContext.UserId, cancellationToken);
    return detail is null
        ? ApiProblemDetails.Create(
            httpContext,
            "Resource not found",
            $"Contract obligation '{obligationId}' was not found.",
            StatusCodes.Status404NotFound,
            "resource_not_found")
        : Results.Ok(detail);
})
.RequirePermission(Permission.ManageObligations)
.WithName("UpdateContractObligationStatus");

api.MapPatch("/contract-obligations/{contractClauseId:guid}/{obligationId}/owner", async (
    Guid contractClauseId,
    string obligationId,
    AssignContractObligationOwnerRequest request,
    ObligationDetailService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var detail = await service.AssignOwnerAsync(contractClauseId, obligationId, request, tenantContext.UserId, cancellationToken);
        return detail is null
            ? ApiProblemDetails.Create(
                httpContext,
                "Resource not found",
                $"Contract obligation '{obligationId}' was not found.",
                StatusCodes.Status404NotFound,
                "resource_not_found")
            : Results.Ok(detail);
    }
    catch (ObligationAssignmentValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["owner"] = [exception.Message]
        },
        title: "Obligation assignment invalid",
        detail: exception.Message,
        statusCode: StatusCodes.Status400BadRequest);
    }
})
.RequirePermission(Permission.ManageObligations)
.WithName("AssignContractObligationOwner");

api.MapGet("/obligations", async (IObligationRepository repository, CancellationToken cancellationToken) =>
    Results.Ok(await repository.ListAsync(cancellationToken)))
.RequirePermission(Permission.ViewObligations)
.WithName("ListObligations");

api.MapGet("/obligations/{id}", async (string id, IObligationRepository repository, HttpContext httpContext, CancellationToken cancellationToken) =>
{
    var obligation = await repository.FindByIdAsync(id, cancellationToken);
    return obligation is null
        ? ApiProblemDetails.Create(
            httpContext,
            "Resource not found",
            $"Obligation '{id}' was not found.",
            StatusCodes.Status404NotFound,
            "resource_not_found")
        : Results.Ok(obligation);
})
.RequirePermission(Permission.ViewObligations)
.WithName("GetObligationById");

api.MapGet("/reports/approved-evidence-packages", async (
    IReportRepository repository,
    CancellationToken cancellationToken) =>
    Results.Ok(await repository.ListApprovedEvidencePackagesAsync(cancellationToken)))
.RequirePermission(Permission.ViewReports)
.WithName("ListApprovedEvidencePackages");

api.MapGet("/subcontractors", async (
    SubcontractorService service,
    CancellationToken cancellationToken) =>
    Results.Ok(await service.ListCurrentTenantAsync(cancellationToken)))
.RequirePermission(Permission.ViewSubcontractors)
.WithName("ListSubcontractors");

api.MapGet("/subcontractors/{subcontractorId:guid}", async (
    Guid subcontractorId,
    SubcontractorService service,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var subcontractor = await service.FindCurrentTenantAsync(subcontractorId, cancellationToken);
    return subcontractor is null
        ? ApiProblemDetails.Create(
            httpContext,
            "Resource not found",
            $"Subcontractor '{subcontractorId}' was not found.",
            StatusCodes.Status404NotFound,
            "resource_not_found")
        : Results.Ok(subcontractor);
})
.RequirePermission(Permission.ViewSubcontractors)
.WithName("GetSubcontractor");

api.MapPost("/subcontractors", async (
    UpsertSubcontractorRequest request,
    SubcontractorService service,
    ITenantContext tenantContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var created = await service.CreateAsync(request, tenantContext.UserId, cancellationToken);
        return Results.Created($"/api/subcontractors/{created.Id}", created);
    }
    catch (SubcontractorValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["subcontractor"] = [exception.Message]
        },
        title: "Subcontractor invalid",
        detail: exception.Message,
        statusCode: StatusCodes.Status400BadRequest);
    }
})
.RequirePermission(Permission.ManageSubcontractors)
.WithName("CreateSubcontractor");

api.MapPut("/subcontractors/{subcontractorId:guid}", async (
    Guid subcontractorId,
    UpsertSubcontractorRequest request,
    SubcontractorService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var updated = await service.UpdateAsync(subcontractorId, request, tenantContext.UserId, cancellationToken);
        return updated is null
            ? ApiProblemDetails.Create(
                httpContext,
                "Resource not found",
                $"Subcontractor '{subcontractorId}' was not found.",
                StatusCodes.Status404NotFound,
                "resource_not_found")
            : Results.Ok(updated);
    }
    catch (SubcontractorValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["subcontractor"] = [exception.Message]
        },
        title: "Subcontractor invalid",
        detail: exception.Message,
        statusCode: StatusCodes.Status400BadRequest);
    }
})
.RequirePermission(Permission.ManageSubcontractors)
.WithName("UpdateSubcontractor");

api.MapGet("/tasks", async (
    ComplianceTaskService service,
    CancellationToken cancellationToken) =>
    Results.Ok(await service.ListCurrentTenantAsync(cancellationToken)))
.RequirePermission(Permission.ViewTasks)
.WithName("ListComplianceTasks");

api.MapPost("/tasks", async (
    CreateComplianceTaskRequest request,
    ComplianceTaskService service,
    ITenantContext tenantContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var task = await service.CreateAsync(request, tenantContext.UserId, cancellationToken);
        return Results.Created($"/api/tasks/{task.Id}", task);
    }
    catch (ComplianceTaskValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["task"] = [exception.Message]
        },
        title: "Task invalid",
        detail: exception.Message,
        statusCode: StatusCodes.Status400BadRequest);
    }
})
.RequirePermission(Permission.ManageTasks)
.WithName("CreateComplianceTask");

api.MapPatch("/tasks/{taskId:guid}", async (
    Guid taskId,
    UpdateComplianceTaskRequest request,
    ComplianceTaskService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var task = await service.UpdateAsync(taskId, request, tenantContext.UserId, cancellationToken);
        return task is null
            ? ApiProblemDetails.Create(
                httpContext,
                "Resource not found",
                $"Task '{taskId}' was not found.",
                StatusCodes.Status404NotFound,
                "resource_not_found")
            : Results.Ok(task);
    }
    catch (ComplianceTaskValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["task"] = [exception.Message]
        },
        title: "Task invalid",
        detail: exception.Message,
        statusCode: StatusCodes.Status400BadRequest);
    }
})
.RequirePermission(Permission.ManageTasks)
.WithName("UpdateComplianceTask");

api.MapPost("/tasks/renewals/generate", async (
    GenerateRenewalTasksRequest request,
    RenewalGenerationService service,
    ITenantContext tenantContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        return Results.Ok(await service.GenerateAsync(request, tenantContext.UserId, tenantContext.TenantId, cancellationToken));
    }
    catch (ComplianceTaskValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["leadTimeDays"] = [exception.Message]
        },
        title: "Renewal generation invalid",
        detail: exception.Message,
        statusCode: StatusCodes.Status400BadRequest);
    }
})
.RequirePermission(Permission.ManageTasks)
.WithName("GenerateRenewalTasks");

api.MapGet("/calendar/events", async (
    DateOnly from,
    DateOnly? to,
    string? owner,
    string? status,
    Gccs.Domain.Compliance.RiskLevel? risk,
    Guid? contractId,
    string? module,
    ICalendarRepository repository,
    CancellationToken cancellationToken) =>
    Results.Ok(await repository.ListCurrentTenantAsync(
        new CalendarEventQuery(from, to, owner, status, risk, contractId, module),
        cancellationToken)))
.RequirePermission(Permission.ViewTasks)
.WithName("ListCalendarEvents");

api.MapGet("/audit-logs", async (
    AuditLogService service,
    int? page,
    int? pageSize,
    Guid? actorUserId,
    string? action,
    string? entityType,
    DateTimeOffset? from,
    DateTimeOffset? to,
    CancellationToken cancellationToken) =>
{
    try
    {
        var request = new AuditLogQueryRequest(
            page ?? 1,
            pageSize ?? 25,
            actorUserId,
            action,
            entityType,
            from,
            to);

        return Results.Ok(await service.ListCurrentTenantAsync(request, cancellationToken));
    }
    catch (ArgumentException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["auditLogQuery"] = [exception.Message]
        });
    }
})
.RequirePermission(Permission.ViewAuditLog)
.WithName("ListAuditLogs");

api.MapMethods("/audit-logs/{auditLogEntryId:guid}", [HttpMethods.Put, HttpMethods.Patch, HttpMethods.Delete], (
    Guid auditLogEntryId,
    HttpContext httpContext) =>
    ApiProblemDetails.Create(
        httpContext,
        "Audit events are append-only",
        $"Audit log entry '{auditLogEntryId}' cannot be updated or deleted through application APIs.",
        StatusCodes.Status405MethodNotAllowed,
        "audit_log_append_only"))
.RequirePermission(Permission.ViewAuditLog)
.WithName("RejectAuditLogMutation");

api.MapGet("/no-cui-acknowledgement", async (
    NoCuiAcknowledgementService service,
    CancellationToken cancellationToken) =>
    Results.Ok(await service.GetCurrentStatusAsync(cancellationToken)))
.RequirePermission(Permission.ViewEvidence)
.WithName("GetNoCuiAcknowledgementStatus");

api.MapPost("/no-cui-acknowledgement", async (
    AcknowledgeNoCuiRequest request,
    NoCuiAcknowledgementService service,
    ITenantContext tenantContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        return Results.Ok(await service.AcknowledgeAsync(request, tenantContext.UserId, cancellationToken));
    }
    catch (ArgumentException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["noCuiAcknowledgement"] = [exception.Message]
        });
    }
})
.RequirePermission(Permission.ManageEvidence)
.WithName("AcknowledgeNoCuiNotice");

api.MapGet("/evidence-items", async (
    string? tag,
    EvidenceMetadataService service,
    CancellationToken cancellationToken) =>
    Results.Ok(await service.ListCurrentTenantAsync(new EvidenceMetadataQuery(tag), cancellationToken)))
.RequirePermission(Permission.ViewEvidence)
.WithName("ListEvidenceItems");

api.MapPost("/evidence-items", async (
    UpsertEvidenceMetadataRequest request,
    EvidenceMetadataService service,
    ITenantContext tenantContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var created = await service.CreateAsync(request, tenantContext.UserId, cancellationToken);
        return Results.Created($"/api/evidence-items/{created.Id}", created);
    }
    catch (EvidenceMetadataValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["evidenceMetadata"] = [exception.Message]
        },
        title: "Evidence metadata invalid",
        detail: exception.Message,
        statusCode: StatusCodes.Status400BadRequest);
    }
})
.RequirePermission(Permission.ManageEvidence)
.WithName("CreateEvidenceItem");

api.MapGet("/evidence-items/{evidenceItemId:guid}", async (
    Guid evidenceItemId,
    EvidenceMetadataService service,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var evidence = await service.FindCurrentTenantAsync(evidenceItemId, cancellationToken);
    return evidence is null
        ? ApiProblemDetails.Create(
            httpContext,
            "Resource not found",
            $"Evidence item '{evidenceItemId}' was not found.",
            StatusCodes.Status404NotFound,
            "resource_not_found")
        : Results.Ok(evidence);
})
.RequirePermission(Permission.ViewEvidence)
.WithName("GetEvidenceItem");

api.MapPut("/evidence-items/{evidenceItemId:guid}", async (
    Guid evidenceItemId,
    UpsertEvidenceMetadataRequest request,
    EvidenceMetadataService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var updated = await service.UpdateAsync(evidenceItemId, request, tenantContext.UserId, cancellationToken);
        return updated is null
            ? ApiProblemDetails.Create(
                httpContext,
                "Resource not found",
                $"Evidence item '{evidenceItemId}' was not found.",
                StatusCodes.Status404NotFound,
                "resource_not_found")
            : Results.Ok(updated);
    }
    catch (EvidenceMetadataValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["evidenceMetadata"] = [exception.Message]
        },
        title: "Evidence metadata invalid",
        detail: exception.Message,
        statusCode: StatusCodes.Status400BadRequest);
    }
})
.RequirePermission(Permission.ManageEvidence)
.WithName("UpdateEvidenceItem");

api.MapPost("/evidence-items/{evidenceItemId:guid}/reviews", async (
    Guid evidenceItemId,
    EvidenceReviewRequest request,
    EvidenceApprovalService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var review = await service.ReviewAsync(evidenceItemId, request, tenantContext.UserId, cancellationToken);
        return review is null
            ? ApiProblemDetails.Create(
                httpContext,
                "Resource not found",
                $"Evidence item '{evidenceItemId}' was not found.",
                StatusCodes.Status404NotFound,
                "resource_not_found")
            : Results.Created($"/api/evidence-items/{evidenceItemId}/reviews/{review.Id}", review);
    }
    catch (EvidenceReviewValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["evidenceReview"] = [exception.Message]
        },
        title: "Evidence review invalid",
        detail: exception.Message,
        statusCode: StatusCodes.Status400BadRequest);
    }
})
.RequirePermission(Permission.ApproveEvidence)
.WithName("CreateEvidenceReview");

api.MapPost("/evidence-items/{evidenceItemId:guid}/upload-intents", async (
    Guid evidenceItemId,
    EvidenceUploadIntentRequest request,
    NoCuiAcknowledgementService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var uploadIntent = await service.CreateEvidenceUploadIntentAsync(
            evidenceItemId,
            request,
            tenantContext.UserId,
            cancellationToken);

        return Results.Created($"/api/evidence-items/{evidenceItemId}/upload-intents/{uploadIntent.Id}", uploadIntent);
    }
    catch (NoCuiAcknowledgementRequiredException exception)
    {
        return ApiProblemDetails.Create(
            httpContext,
            "No-CUI acknowledgement required",
            exception.Message,
            StatusCodes.Status428PreconditionRequired,
            "no_cui_acknowledgement_required");
    }
    catch (UploadGuardrailValidationException exception)
    {
        return Results.ValidationProblem(
            exception.Errors.ToDictionary(error => error.Key, error => error.Value),
            title: "Evidence upload rejected",
            detail: exception.Message,
            statusCode: StatusCodes.Status400BadRequest);
    }
    catch (ArgumentException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["uploadIntent"] = [exception.Message]
        });
    }
})
.RequirePermission(Permission.ManageEvidence)
.WithName("CreateEvidenceUploadIntent");

api.MapGet("/cmmc/assessments", async (
    CmmcAssessmentService service,
    CancellationToken cancellationToken) =>
    Results.Ok(await service.ListCurrentTenantAsync(cancellationToken)))
.RequirePermission(Permission.ViewCmmc)
.WithName("ListCmmcAssessments");

api.MapGet("/cmmc/assessments/{assessmentId:guid}", async (
    Guid assessmentId,
    CmmcAssessmentService service,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var assessment = await service.FindCurrentTenantAsync(assessmentId, cancellationToken);
    return assessment is null
        ? ApiProblemDetails.Create(
            httpContext,
            "Resource not found",
            $"CMMC assessment '{assessmentId}' was not found.",
            StatusCodes.Status404NotFound,
            "resource_not_found")
        : Results.Ok(assessment);
})
.RequirePermission(Permission.ViewCmmc)
.WithName("GetCmmcAssessment");

api.MapPost("/cmmc/assessments", async (
    UpsertCmmcAssessmentRequest request,
    CmmcAssessmentService service,
    ITenantContext tenantContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var created = await service.CreateAsync(request, tenantContext.UserId, cancellationToken);
        return Results.Created($"/api/cmmc/assessments/{created.Id}", created);
    }
    catch (CmmcAssessmentValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["cmmcAssessment"] = [exception.Message]
        },
        title: "CMMC assessment invalid",
        detail: exception.Message,
        statusCode: StatusCodes.Status400BadRequest);
    }
})
.RequirePermission(Permission.ManageCmmc)
.WithName("CreateCmmcAssessment");

api.MapPut("/cmmc/assessments/{assessmentId:guid}", async (
    Guid assessmentId,
    UpsertCmmcAssessmentRequest request,
    CmmcAssessmentService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var updated = await service.UpdateAsync(assessmentId, request, tenantContext.UserId, cancellationToken);
        return updated is null
            ? ApiProblemDetails.Create(
                httpContext,
                "Resource not found",
                $"CMMC assessment '{assessmentId}' was not found.",
                StatusCodes.Status404NotFound,
                "resource_not_found")
            : Results.Ok(updated);
    }
    catch (CmmcAssessmentValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["cmmcAssessment"] = [exception.Message]
        },
        title: "CMMC assessment invalid",
        detail: exception.Message,
        statusCode: StatusCodes.Status400BadRequest);
    }
})
.RequirePermission(Permission.ManageCmmc)
.WithName("UpdateCmmcAssessment");

api.MapGet("/cmmc/assessments/{assessmentId:guid}/controls", async (
    Guid assessmentId,
    CmmcAssessmentService service,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var controls = await service.ListControlStatusesAsync(assessmentId, cancellationToken);
    return controls is null
        ? ApiProblemDetails.Create(
            httpContext,
            "Resource not found",
            $"CMMC assessment '{assessmentId}' was not found.",
            StatusCodes.Status404NotFound,
            "resource_not_found")
        : Results.Ok(controls);
})
.RequirePermission(Permission.ViewCmmc)
.WithName("ListCmmcControlStatuses");

api.MapPatch("/cmmc/assessments/{assessmentId:guid}/controls/{controlId}", async (
    Guid assessmentId,
    string controlId,
    UpsertCmmcControlStatusRequest request,
    CmmcAssessmentService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var updated = await service.UpsertControlStatusAsync(
            assessmentId,
            controlId,
            request,
            tenantContext.UserId,
            cancellationToken);
        return updated is null
            ? ApiProblemDetails.Create(
                httpContext,
                "Resource not found",
                $"CMMC assessment '{assessmentId}' was not found.",
                StatusCodes.Status404NotFound,
                "resource_not_found")
            : Results.Ok(updated);
    }
    catch (CmmcAssessmentValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["cmmcControlStatus"] = [exception.Message]
        },
        title: "CMMC control status invalid",
        detail: exception.Message,
        statusCode: StatusCodes.Status400BadRequest);
    }
})
.RequirePermission(Permission.ManageCmmc)
.WithName("UpdateCmmcControlStatus");

api.MapGet("/cmmc/assessments/{assessmentId:guid}/poam-items", async (
    Guid assessmentId,
    CmmcPoamService service,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var items = await service.ListCurrentTenantAsync(assessmentId, cancellationToken);
    return items is null
        ? ApiProblemDetails.Create(
            httpContext,
            "Resource not found",
            $"CMMC assessment '{assessmentId}' was not found.",
            StatusCodes.Status404NotFound,
            "resource_not_found")
        : Results.Ok(items);
})
.RequirePermission(Permission.ViewCmmc)
.WithName("ListCmmcPoamItems");

api.MapPost("/cmmc/assessments/{assessmentId:guid}/poam-items", async (
    Guid assessmentId,
    UpsertCmmcPoamItemRequest request,
    CmmcPoamService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var created = await service.CreateAsync(assessmentId, request, tenantContext.UserId, cancellationToken);
        return created is null
            ? ApiProblemDetails.Create(
                httpContext,
                "Resource not found",
                $"CMMC assessment '{assessmentId}' or control '{request.ControlId}' was not found.",
                StatusCodes.Status404NotFound,
                "resource_not_found")
            : Results.Created($"/api/cmmc/assessments/{assessmentId}/poam-items/{created.Id}", created);
    }
    catch (CmmcPoamValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["cmmcPoamItem"] = [exception.Message]
        },
        title: "CMMC POA&M item invalid",
        detail: exception.Message,
        statusCode: StatusCodes.Status400BadRequest);
    }
})
.RequirePermission(Permission.ManageCmmc)
.WithName("CreateCmmcPoamItem");

api.MapPatch("/cmmc/assessments/{assessmentId:guid}/poam-items/{poamItemId:guid}", async (
    Guid assessmentId,
    Guid poamItemId,
    UpsertCmmcPoamItemRequest request,
    CmmcPoamService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var updated = await service.UpdateAsync(assessmentId, poamItemId, request, tenantContext.UserId, cancellationToken);
        return updated is null
            ? ApiProblemDetails.Create(
                httpContext,
                "Resource not found",
                $"CMMC POA&M item '{poamItemId}' was not found.",
                StatusCodes.Status404NotFound,
                "resource_not_found")
            : Results.Ok(updated);
    }
    catch (CmmcPoamValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["cmmcPoamItem"] = [exception.Message]
        },
        title: "CMMC POA&M item invalid",
        detail: exception.Message,
        statusCode: StatusCodes.Status400BadRequest);
    }
})
.RequirePermission(Permission.ManageCmmc)
.WithName("UpdateCmmcPoamItem");

api.MapGet("/cmmc/affirmations", async (
    CmmcAffirmationService service,
    CancellationToken cancellationToken) =>
    Results.Ok(await service.ListCurrentTenantAsync(cancellationToken)))
.RequirePermission(Permission.ViewCmmc)
.WithName("ListCmmcAffirmations");

api.MapPost("/cmmc/affirmations", async (
    UpsertCmmcAffirmationRequest request,
    CmmcAffirmationService service,
    ITenantContext tenantContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var created = await service.CreateAsync(request, tenantContext.UserId, cancellationToken);
        return Results.Created($"/api/cmmc/affirmations/{created.Id}", created);
    }
    catch (CmmcAffirmationValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["cmmcAffirmation"] = [exception.Message]
        },
        title: "CMMC affirmation invalid",
        detail: exception.Message,
        statusCode: StatusCodes.Status400BadRequest);
    }
})
.RequirePermission(Permission.ManageCmmc)
.WithName("CreateCmmcAffirmation");

api.MapPatch("/cmmc/affirmations/{affirmationId:guid}", async (
    Guid affirmationId,
    UpsertCmmcAffirmationRequest request,
    CmmcAffirmationService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var updated = await service.UpdateAsync(affirmationId, request, tenantContext.UserId, cancellationToken);
        return updated is null
            ? ApiProblemDetails.Create(
                httpContext,
                "Resource not found",
                $"CMMC affirmation '{affirmationId}' was not found.",
                StatusCodes.Status404NotFound,
                "resource_not_found")
            : Results.Ok(updated);
    }
    catch (CmmcAffirmationValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["cmmcAffirmation"] = [exception.Message]
        },
        title: "CMMC affirmation invalid",
        detail: exception.Message,
        statusCode: StatusCodes.Status400BadRequest);
    }
})
.RequirePermission(Permission.ManageCmmc)
.WithName("UpdateCmmcAffirmation");

api.MapGet("/evidence-items/{evidenceItemId:guid}/download", async (
    Guid evidenceItemId,
    NoCuiAcknowledgementService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var file = await service.GetLatestFileForDownloadAsync(evidenceItemId, tenantContext.UserId, cancellationToken);
    return file is null
        ? ApiProblemDetails.Create(
            httpContext,
            "Resource not found",
            $"Evidence file for item '{evidenceItemId}' was not found.",
            StatusCodes.Status404NotFound,
            "resource_not_found")
        : Results.Ok(file);
})
.RequirePermission(Permission.ViewEvidence)
.WithName("DownloadEvidenceFileMetadata");

api.MapDelete("/evidence-items/{evidenceItemId:guid}/file", async (
    Guid evidenceItemId,
    NoCuiAcknowledgementService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var deleted = await service.DeleteLatestFileAsync(evidenceItemId, tenantContext.UserId, cancellationToken);
    return deleted is null
        ? ApiProblemDetails.Create(
            httpContext,
            "Resource not found",
            $"Evidence file for item '{evidenceItemId}' was not found.",
            StatusCodes.Status404NotFound,
            "resource_not_found")
        : Results.Ok(deleted);
})
.RequirePermission(Permission.ManageEvidence)
.WithName("DeleteEvidenceFileMetadata");

api.MapGet("/tenant-members", async (
    TenantMembershipService service,
    CancellationToken cancellationToken) =>
    Results.Ok(await service.ListCurrentTenantMembersAsync(cancellationToken)))
.RequirePermission(Permission.ManageUsers)
.WithName("ListCurrentTenantMembers");

api.MapPost("/tenant-members", async (
    AssignTenantMemberRequest request,
    TenantMembershipService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var member = await service.AssignAsync(request, tenantContext.UserId, cancellationToken);
        return Results.Created($"/api/tenant-members/{member.MembershipId}", member);
    }
    catch (DuplicateMembershipException exception)
    {
        return ApiProblemDetails.Create(
            httpContext,
            "Duplicate tenant membership",
            exception.Message,
            StatusCodes.Status409Conflict,
            "duplicate_membership");
    }
    catch (ArgumentException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["membership"] = [exception.Message]
        });
    }
})
.RequirePermission(Permission.ManageUsers)
.WithName("AssignTenantMember");

api.MapPatch("/tenant-members/{membershipId:guid}/status", async (
    Guid membershipId,
    UpdateTenantMembershipStatusRequest request,
    TenantMembershipService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var member = await service.UpdateStatusAsync(membershipId, request, tenantContext.UserId, cancellationToken);
    return member is null
        ? ApiProblemDetails.Create(
            httpContext,
            "Resource not found",
            "Tenant membership was not found in the current tenant scope.",
            StatusCodes.Status404NotFound,
            "resource_not_found")
        : Results.Ok(member);
})
.RequirePermission(Permission.ManageUsers)
.WithName("UpdateTenantMembershipStatus");

api.MapGet("/tenant-invitations", async (
    TenantInvitationService service,
    CancellationToken cancellationToken) =>
    Results.Ok(await service.ListCurrentTenantInvitationsAsync(cancellationToken)))
.RequirePermission(Permission.ManageUsers)
.WithName("ListCurrentTenantInvitations");

api.MapPost("/tenant-invitations", async (
    CreateTenantInvitationRequest request,
    TenantInvitationService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var invitation = await service.CreateAsync(request, tenantContext.UserId, cancellationToken);
        return Results.Created($"/api/tenant-invitations/{invitation.InvitationId}", invitation);
    }
    catch (DuplicateInvitationException exception)
    {
        return ApiProblemDetails.Create(
            httpContext,
            "Duplicate tenant invitation",
            exception.Message,
            StatusCodes.Status409Conflict,
            "duplicate_invitation");
    }
    catch (ArgumentException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["invitation"] = [exception.Message]
        });
    }
})
.RequirePermission(Permission.ManageUsers)
.WithName("CreateTenantInvitation");

api.MapPost("/invitations/{token}/accept", async (
    string token,
    AcceptTenantInvitationRequest request,
    TenantInvitationService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var invitation = await service.AcceptAsync(
            token,
            request,
            tenantContext.UserId,
            tenantContext.UserEmail,
            cancellationToken);

        return invitation is null
            ? ApiProblemDetails.Create(
                httpContext,
                "Resource not found",
                "Invitation token was not found.",
                StatusCodes.Status404NotFound,
                "resource_not_found")
            : Results.Ok(invitation);
    }
    catch (InvalidInvitationStateException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["invitation"] = [exception.Message]
        });
    }
    catch (ArgumentException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["invitation"] = [exception.Message]
        });
    }
})
.WithName("AcceptTenantInvitation");

api.MapPost("/tenant-invitations/{invitationId:guid}/expire", async (
    Guid invitationId,
    TenantInvitationService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var invitation = await service.ExpireAsync(invitationId, tenantContext.UserId, cancellationToken);
    return invitation is null
        ? ApiProblemDetails.Create(
            httpContext,
            "Resource not found",
            "Invitation was not found in the current tenant scope.",
            StatusCodes.Status404NotFound,
            "resource_not_found")
        : Results.Ok(invitation);
})
.RequirePermission(Permission.ManageUsers)
.WithName("ExpireTenantInvitation");

api.MapPost("/tenant-invitations/{invitationId:guid}/revoke", async (
    Guid invitationId,
    TenantInvitationService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var invitation = await service.RevokeAsync(invitationId, tenantContext.UserId, cancellationToken);
    return invitation is null
        ? ApiProblemDetails.Create(
            httpContext,
            "Resource not found",
            "Invitation was not found in the current tenant scope.",
            StatusCodes.Status404NotFound,
            "resource_not_found")
        : Results.Ok(invitation);
})
.RequirePermission(Permission.ManageUsers)
.WithName("RevokeTenantInvitation");

api.MapPost("/tenants", async (
    CreateTenantRequest request,
    TenantService service,
    ITenantContext tenantContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var tenant = await service.CreateAsync(request, tenantContext.UserId, cancellationToken);
        return Results.Created($"/api/tenants/{tenant.Id}", tenant);
    }
    catch (ArgumentException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["tenant"] = [exception.Message]
        });
    }
})
.RequirePermission(Permission.ManageTenant)
.WithName("CreateTenant");

api.MapGet("/tenants/{tenantId:guid}", async (
    Guid tenantId,
    TenantService service,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var tenant = await service.FindInCurrentTenantScopeAsync(tenantId, cancellationToken);
    return tenant is null
        ? ApiProblemDetails.Create(
            httpContext,
            "Resource not found",
            "Tenant was not found in the current tenant scope.",
            StatusCodes.Status404NotFound,
            "resource_not_found")
        : Results.Ok(tenant);
})
.RequirePermission(Permission.ManageTenant)
.WithName("GetTenant");

api.MapPatch("/tenants/{tenantId:guid}/status", async (
    Guid tenantId,
    UpdateTenantStatusRequest request,
    TenantService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var tenant = await service.UpdateStatusAsync(tenantId, request, tenantContext.UserId, cancellationToken);
        return tenant is null
            ? ApiProblemDetails.Create(
                httpContext,
                "Resource not found",
                "Tenant was not found in the current tenant scope.",
                StatusCodes.Status404NotFound,
                "resource_not_found")
            : Results.Ok(tenant);
    }
    catch (ArgumentException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["tenant"] = [exception.Message]
        });
    }
})
.RequirePermission(Permission.ManageTenant)
.WithName("UpdateTenantStatus");

app.Run();

public partial class Program;
