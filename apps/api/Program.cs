using System.Security.Claims;
using System.Text.Json.Serialization;
using Gccs.Api.Security;
using Gccs.Api.LocalDevelopment;
using Gccs.Application.Audit;
using Gccs.Application.Calendar;
using Gccs.Application.Common;
using Gccs.Application.Companies;
using Gccs.Application.Cmmc;
using Gccs.Application.Compliance;
using Gccs.Application.Contracts;
using Gccs.Application.Demo;
using Gccs.Application.Evidence;
using Gccs.Application.Identity;
using Gccs.Application.NoCui;
using Gccs.Application.Notifications;
using Gccs.Application.Repositories;
using Gccs.Application.Reports;
using Gccs.Application.Subcontractors;
using Gccs.Application.Tasks;
using Gccs.Application.Tenancy;
using Gccs.Domain.Cmmc;
using Gccs.Domain.Compliance;
using Gccs.Domain.Identity;
using Gccs.Domain.Tenancy;
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
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = ApiProblemDetails.Customize;
});
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddHttpClient();
builder.Services.Configure<LocalDependencyOptions>(builder.Configuration.GetSection(LocalDependencyOptions.SectionName));
builder.Services.AddScoped<LocalDependencyHealthService>();
builder.Services.AddGccsApiSecurity(builder.Configuration, builder.Environment);
builder.Services.AddGccsInfrastructure(builder.Configuration);
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddHostedService<DevelopmentTenantBootstrapper>();
}

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
app.UseGccsTenantMembershipAuthorization(builder.Configuration, app.Environment);
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
    .RequireRateLimiting("api")
    .RequireRouteTenantScope();

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

api.MapGet("/notification-preferences", async (
    NotificationPreferenceService service,
    ITenantContext tenantContext,
    ClaimsPrincipal user,
    CancellationToken cancellationToken) =>
{
    var roleName = user.FindFirstValue(ApiSecurityExtensions.RoleNameClaimType) ?? RoleCatalog.Contributor;
    return Results.Ok(await service.GetOrCreateAsync(
        tenantContext.TenantId,
        tenantContext.UserId,
        roleName,
        cancellationToken));
})
.WithName("GetNotificationPreferences");

api.MapPut("/notification-preferences", async (
    NotificationPreferenceUpdateRequest request,
    NotificationPreferenceService service,
    ITenantContext tenantContext,
    ClaimsPrincipal user,
    CancellationToken cancellationToken) =>
{
    var roleName = user.FindFirstValue(ApiSecurityExtensions.RoleNameClaimType) ?? RoleCatalog.Contributor;
    return Results.Ok(await service.UpdateAsync(
        tenantContext.TenantId,
        tenantContext.UserId,
        roleName,
        request,
        cancellationToken));
})
.WithName("UpdateNotificationPreferences");

api.MapPost("/notifications/due-date-reminders", async (
    RunDueDateReminderRequest request,
    DueDateReminderService service,
    ITenantContext tenantContext,
    CancellationToken cancellationToken) =>
    Results.Ok(await service.RunAsync(tenantContext.TenantId, tenantContext.UserId, request, cancellationToken)))
.RequirePermission(Permission.ManageTasks)
.WithName("RunDueDateReminders");

api.MapGet("/notifications", async (
    [FromServices] AssignmentNotificationService service,
    ITenantContext tenantContext,
    CancellationToken cancellationToken) =>
    Results.Ok(await service.ListCurrentUserAsync(tenantContext.TenantId, tenantContext.UserId, cancellationToken)))
.WithName("ListNotifications");

api.MapPost("/notifications/{notificationId:guid}/read", async (
    Guid notificationId,
    [FromServices] AssignmentNotificationService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var item = await service.MarkReadAsync(tenantContext.TenantId, tenantContext.UserId, notificationId, cancellationToken);
    return item is null
        ? ApiProblemDetails.Create(
            httpContext,
            "Resource not found",
            $"Notification '{notificationId}' was not found.",
            StatusCodes.Status404NotFound,
            "resource_not_found")
        : Results.Ok(item);
})
.WithName("MarkNotificationRead");

api.MapGet("/compliance/overview", async (
    ComplianceOverviewService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    var correlationId = ApiCorrelation.Get(httpContext);
    try
    {
        var overview = await service.GetOverviewAsync(cancellationToken);
        logger.LogInformation(
            "Compliance overview returned for tenant {TenantId}. TraceId: {TraceId}. CorrelationId: {CorrelationId}.",
            tenantContext.TenantId,
            httpContext.TraceIdentifier,
            correlationId);
        return Results.Ok(overview);
    }
    catch (Exception exception) when (exception is not OperationCanceledException)
    {
        logger.LogError(
            exception,
            "Compliance overview failed for tenant {TenantId}. TraceId: {TraceId}. CorrelationId: {CorrelationId}.",
            tenantContext.TenantId,
            httpContext.TraceIdentifier,
            correlationId);
        return ApiProblemDetails.Create(
            httpContext,
            "Compliance overview unavailable",
            "The compliance overview could not be loaded. Try again later or contact support with the correlation id.",
            StatusCodes.Status500InternalServerError,
            "compliance_overview_unavailable");
    }
})
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

api.MapPost("/company-profile/sam-lookup/search", async (
    CompanyEntityLookupRequest request,
    CompanyEntityLookupService service,
    CancellationToken cancellationToken) =>
{
    try
    {
        return Results.Ok(await service.SearchAsync(request, cancellationToken));
    }
    catch (CompanyEntityLookupValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["query"] = [exception.Message]
        });
    }
    catch (CompanyEntityLookupUnavailableException exception)
    {
        return Results.Problem(
            title: "SAM.gov lookup unavailable",
            detail: exception.Message,
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
})
.RequirePermission(Permission.ViewCompanyProfile)
.WithName("SearchCompanyProfileSamLookup");

api.MapPost("/company-profile/sam-lookup/apply", async (
    ApplyCompanyEntityLookupRequest request,
    CompanyEntityLookupService service,
    HttpContext httpContext,
    ITenantContext tenantContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        return Results.Ok(await service.ApplyAsync(request, tenantContext.UserId, cancellationToken));
    }
    catch (CompanyEntityLookupConflictException exception)
    {
        return ApiProblemDetails.Create(
            httpContext,
            "SAM.gov data conflict",
            "SAM.gov data conflicts with existing profile values.",
            StatusCodes.Status409Conflict,
            "company_profile_sam_lookup_conflict",
            new Dictionary<string, object?>
            {
                ["conflicts"] = exception.Conflicts
            });
    }
})
.RequirePermission(Permission.ManageCompanyProfile)
.WithName("ApplyCompanyProfileSamLookup");

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

api.MapGet("/contracts/{contractId:guid}/size-checks", async (
    Guid contractId,
    ContractSizeCheckService service,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var checks = await service.ListCurrentTenantAsync(contractId, cancellationToken);
    return checks is null
        ? ApiProblemDetails.Create(
            httpContext,
            "Resource not found",
            $"Contract '{contractId}' was not found.",
            StatusCodes.Status404NotFound,
            "resource_not_found")
        : Results.Ok(checks);
})
.RequirePermission(Permission.ViewContracts)
.WithName("ListContractSizeChecks");

api.MapPost("/contracts/{contractId:guid}/size-checks", async (
    Guid contractId,
    ContractSizeCheckRequest request,
    ContractSizeCheckService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var check = await service.RunCurrentTenantAsync(contractId, request, tenantContext.UserId, cancellationToken);
    return check is null
        ? ApiProblemDetails.Create(
            httpContext,
            "Resource not found",
            $"Contract '{contractId}' was not found.",
            StatusCodes.Status404NotFound,
            "resource_not_found")
        : Results.Ok(check);
})
.RequirePermission(Permission.ManageContracts)
.WithName("RunContractSizeCheck");

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

api.MapPost("/contracts/{contractId:guid}/documents/{documentId:guid}/extraction-jobs", async (
    Guid contractId,
    Guid documentId,
    ContractService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var job = await service.StartExtractionJobAsync(contractId, documentId, tenantContext.UserId, cancellationToken);
    return job is null
        ? ApiProblemDetails.Create(
            httpContext,
            "Resource not found",
            $"Contract document '{documentId}' was not found.",
            StatusCodes.Status404NotFound,
            "resource_not_found")
        : Results.Created($"/api/contracts/{contractId}/documents/{documentId}/extraction-jobs/{job.Id}", job);
})
.RequirePermission(Permission.ManageContracts)
.WithName("StartContractDocumentExtraction");

api.MapPost("/extraction-jobs/{extractionJobId:guid}/process", async (
    Guid extractionJobId,
    ContractService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var result = await service.ProcessExtractionJobAsync(extractionJobId, tenantContext.UserId, cancellationToken);
    return result is null
        ? ApiProblemDetails.Create(
            httpContext,
            "Resource not found",
            $"Extraction job '{extractionJobId}' was not found.",
            StatusCodes.Status404NotFound,
            "resource_not_found")
        : Results.Ok(result);
})
.RequirePermission(Permission.ManageContracts)
.WithName("ProcessExtractionJob");

api.MapGet("/contracts/{contractId:guid}/documents/{documentId:guid}/extraction-results", async (
    Guid contractId,
    Guid documentId,
    string? reviewStatus,
    ContractService service,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var results = await service.ListExtractionResultsAsync(contractId, documentId, reviewStatus, cancellationToken);
    return results is null
        ? ApiProblemDetails.Create(
            httpContext,
            "Resource not found",
            $"Contract document '{documentId}' was not found.",
            StatusCodes.Status404NotFound,
            "resource_not_found")
        : Results.Ok(results);
})
.RequirePermission(Permission.ViewContracts)
.WithName("ListContractDocumentExtractionResults");

api.MapPatch("/contracts/{contractId:guid}/documents/{documentId:guid}/clause-candidates/{candidateId:guid}", async (
    Guid contractId,
    Guid documentId,
    Guid candidateId,
    ClauseCandidateEditRequest request,
    ContractService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var candidate = await service.EditClauseCandidateAsync(contractId, documentId, candidateId, request, tenantContext.UserId, cancellationToken);
        return candidate is null
            ? ApiProblemDetails.Create(
                httpContext,
                "Resource not found",
                $"Clause candidate '{candidateId}' was not found.",
                StatusCodes.Status404NotFound,
                "resource_not_found")
            : Results.Ok(candidate);
    }
    catch (ContractValidationException exception)
    {
        return Results.ValidationProblem(
            exception.Errors.ToDictionary(error => error.Key, error => error.Value),
            title: "Clause candidate invalid",
            detail: exception.Message,
            statusCode: StatusCodes.Status400BadRequest);
    }
})
.RequirePermission(Permission.ReviewClauses)
.WithName("EditClauseCandidate");

api.MapPost("/contracts/{contractId:guid}/documents/{documentId:guid}/clause-candidates/{candidateId:guid}/accept", async (
    Guid contractId,
    Guid documentId,
    Guid candidateId,
    ClauseCandidateReviewRequest request,
    ContractService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var candidate = await service.AcceptClauseCandidateAsync(contractId, documentId, candidateId, request, tenantContext.UserId, cancellationToken);
        return candidate is null
            ? ApiProblemDetails.Create(
                httpContext,
                "Resource not found",
                $"Clause candidate '{candidateId}' was not found.",
                StatusCodes.Status404NotFound,
                "resource_not_found")
            : Results.Ok(candidate);
    }
    catch (ContractValidationException exception)
    {
        return Results.ValidationProblem(
            exception.Errors.ToDictionary(error => error.Key, error => error.Value),
            title: "Clause candidate review invalid",
            detail: exception.Message,
            statusCode: StatusCodes.Status400BadRequest);
    }
})
.RequirePermission(Permission.ReviewClauses)
.WithName("AcceptClauseCandidate");

api.MapPost("/contracts/{contractId:guid}/documents/{documentId:guid}/clause-candidates/{candidateId:guid}/reject", async (
    Guid contractId,
    Guid documentId,
    Guid candidateId,
    ClauseCandidateReviewRequest request,
    ContractService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var candidate = await service.RejectClauseCandidateAsync(contractId, documentId, candidateId, request, tenantContext.UserId, cancellationToken);
        return candidate is null
            ? ApiProblemDetails.Create(
                httpContext,
                "Resource not found",
                $"Clause candidate '{candidateId}' was not found.",
                StatusCodes.Status404NotFound,
                "resource_not_found")
            : Results.Ok(candidate);
    }
    catch (ContractValidationException exception)
    {
        return Results.ValidationProblem(
            exception.Errors.ToDictionary(error => error.Key, error => error.Value),
            title: "Clause candidate review invalid",
            detail: exception.Message,
            statusCode: StatusCodes.Status400BadRequest);
    }
})
.RequirePermission(Permission.ReviewClauses)
.WithName("RejectClauseCandidate");

api.MapPost("/contracts/{contractId:guid}/documents/{documentId:guid}/clause-candidates/{candidateId:guid}/needs-clarification", async (
    Guid contractId,
    Guid documentId,
    Guid candidateId,
    ClauseCandidateStateChangeRequest request,
    ContractService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var candidate = await service.MarkClauseCandidateNeedsClarificationAsync(contractId, documentId, candidateId, request, tenantContext.UserId, cancellationToken);
        return candidate is null
            ? ApiProblemDetails.Create(
                httpContext,
                "Resource not found",
                $"Clause candidate '{candidateId}' was not found.",
                StatusCodes.Status404NotFound,
                "resource_not_found")
            : Results.Ok(candidate);
    }
    catch (ContractValidationException exception)
    {
        return Results.ValidationProblem(
            exception.Errors.ToDictionary(error => error.Key, error => error.Value),
            title: "Clause candidate review invalid",
            detail: exception.Message,
            statusCode: StatusCodes.Status400BadRequest);
    }
})
.RequirePermission(Permission.ReviewClauses)
.WithName("MarkClauseCandidateNeedsClarification");

api.MapPost("/contracts/{contractId:guid}/documents/{documentId:guid}/clause-candidates/{candidateId:guid}/supersede", async (
    Guid contractId,
    Guid documentId,
    Guid candidateId,
    ClauseCandidateStateChangeRequest request,
    ContractService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var candidate = await service.SupersedeClauseCandidateAsync(contractId, documentId, candidateId, request, tenantContext.UserId, cancellationToken);
        return candidate is null
            ? ApiProblemDetails.Create(
                httpContext,
                "Resource not found",
                $"Clause candidate '{candidateId}' was not found.",
                StatusCodes.Status404NotFound,
                "resource_not_found")
            : Results.Ok(candidate);
    }
    catch (ContractValidationException exception)
    {
        return Results.ValidationProblem(
            exception.Errors.ToDictionary(error => error.Key, error => error.Value),
            title: "Clause candidate review invalid",
            detail: exception.Message,
            statusCode: StatusCodes.Status400BadRequest);
    }
})
.RequirePermission(Permission.ReviewClauses)
.WithName("SupersedeClauseCandidate");

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
                $"Contract '{contractId}' or published clause library ID '{request.ClauseLibraryId}' was not found. Use a clause library ID such as 'far-52-204-21', not a contract number.",
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

api.MapGet("/contracts/{contractId:guid}/obligations", async (
    Guid contractId,
    IContractObligationMatrixRepository repository,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var rows = await repository.ListCurrentTenantAsync(contractId, cancellationToken);
    return rows is null
        ? ApiProblemDetails.Create(
            httpContext,
            "Resource not found",
            $"Contract '{contractId}' was not found.",
            StatusCodes.Status404NotFound,
            "resource_not_found")
        : Results.Ok(rows);
})
.RequirePermission(Permission.ViewObligations)
.WithName("ListContractObligationMatrix");

api.MapGet("/contracts/{contractId:guid}/obligations/export", async (
    Guid contractId,
    IContractObligationMatrixRepository repository,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var export = await repository.ExportCurrentTenantAsync(contractId, cancellationToken);
    return export is null
        ? ApiProblemDetails.Create(
            httpContext,
            "Resource not found",
            $"Contract '{contractId}' was not found.",
            StatusCodes.Status404NotFound,
            "resource_not_found")
        : Results.Ok(export);
})
.RequirePermission(Permission.ViewObligations)
.WithName("ExportContractObligationMatrix");

api.MapGet("/clauses", async (
    string? query,
    string? category,
    string? sourceFamily,
    string? obligationArea,
    bool? requiresFlowDown,
    bool? includeDrafts,
    ClauseLibraryService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var canReviewContent = httpContext.User.HasClaim(ApiSecurityExtensions.PermissionClaimType, Permission.ManageObligations.ToString()) ||
            httpContext.User.HasClaim(ApiSecurityExtensions.PermissionClaimType, Permission.ReviewClauses.ToString());
        return Results.Ok(await service.SearchAsync(
            new ClauseLibrarySearchRequest(
                query,
                category,
                tenantContext.TenantId,
                sourceFamily,
                obligationArea,
                requiresFlowDown,
                includeDrafts == true && canReviewContent),
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

api.MapGet("/clauses/{clauseId}", async (
    string clauseId,
    ClauseLibraryService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var detail = await service.FindDetailAsync(clauseId, tenantContext.TenantId, cancellationToken);
    return detail is null
        ? ApiProblemDetails.Create(
            httpContext,
            "Resource not found",
            $"Clause '{clauseId}' was not found.",
            StatusCodes.Status404NotFound,
            "resource_not_found")
        : Results.Ok(detail);
})
.RequirePermission(Permission.ViewContracts)
.WithName("GetClauseLibraryDetail");

api.MapPatch("/clauses/{clauseId}/review-state", async (
    string clauseId,
    ChangeComplianceContentReviewStateRequest request,
    ComplianceContentReviewService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var updated = await service.ChangeClauseStateAsync(clauseId, request, tenantContext.TenantId, tenantContext.UserId, cancellationToken);
        return updated is null
            ? ApiProblemDetails.Create(
                httpContext,
                "Resource not found",
                $"Clause '{clauseId}' was not found.",
                StatusCodes.Status404NotFound,
                "resource_not_found")
            : Results.Ok(updated);
    }
    catch (ComplianceContentReviewException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["reviewState"] = [exception.Message]
        },
        title: "Clause review state invalid",
        detail: exception.Message,
        statusCode: StatusCodes.Status400BadRequest);
    }
})
.RequirePermission(Permission.ManageObligations)
.WithName("ChangeClauseReviewState");

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

api.MapGet("/applicability-facts", async (
    Guid? contractId,
    Guid? clauseId,
    Guid? subcontractorId,
    ApplicabilityFactService service,
    ITenantContext tenantContext,
    CancellationToken cancellationToken) =>
    Results.Ok(await service.ListAsync(
        new ApplicabilityFactQuery(tenantContext.TenantId, contractId, clauseId, subcontractorId),
        cancellationToken)))
.RequirePermission(Permission.ViewObligations)
.WithName("ListApplicabilityFacts");

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

api.MapGet("/suggested-obligations", async (
    string? reviewStatus,
    SuggestedObligationService service,
    CancellationToken cancellationToken) =>
    Results.Ok(await service.ListAsync(reviewStatus, cancellationToken)))
.RequirePermission(Permission.ViewObligations)
.WithName("ListSuggestedObligations");

api.MapGet("/suggested-obligations/{suggestionId:guid}", async (
    Guid suggestionId,
    SuggestedObligationService service,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var suggestion = await service.FindAsync(suggestionId, cancellationToken);
    return suggestion is null
        ? ApiProblemDetails.Create(
            httpContext,
            "Resource not found",
            $"Suggested obligation '{suggestionId}' was not found.",
            StatusCodes.Status404NotFound,
            "resource_not_found")
        : Results.Ok(suggestion);
})
.RequirePermission(Permission.ViewObligations)
.WithName("GetSuggestedObligationById");

api.MapPost("/suggested-obligations", async (
    CreateSuggestedObligationRequest request,
    SuggestedObligationService service,
    ITenantContext tenantContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var suggestion = await service.CreateAsync(request, tenantContext.TenantId, tenantContext.UserId, cancellationToken);
        return Results.Created($"/api/suggested-obligations/{suggestion.Id}", suggestion);
    }
    catch (SuggestedObligationValidationException exception)
    {
        return Results.ValidationProblem(
            exception.Errors.ToDictionary(error => error.Key, error => error.Value),
            title: "Suggested obligation invalid",
            detail: exception.Message,
            statusCode: StatusCodes.Status400BadRequest);
    }
})
.RequirePermission(Permission.ManageObligations)
.WithName("CreateSuggestedObligation");

api.MapPost("/suggested-obligations/{suggestionId:guid}/approve", async (
    Guid suggestionId,
    SuggestedObligationReviewRequest request,
    SuggestedObligationService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
    await ReviewSuggestedObligationAsync(
        suggestionId,
        request,
        service.ApproveAsync,
        tenantContext,
        httpContext,
        cancellationToken))
.RequirePermission(Permission.ManageObligations)
.WithName("ApproveSuggestedObligation");

api.MapPut("/suggested-obligations/{suggestionId:guid}", async (
    Guid suggestionId,
    ReviseSuggestedObligationRequest request,
    SuggestedObligationService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var suggestion = await service.ReviseAsync(suggestionId, request, tenantContext.UserId, cancellationToken);
        return suggestion is null
            ? ApiProblemDetails.Create(
                httpContext,
                "Resource not found",
                $"Suggested obligation '{suggestionId}' was not found.",
                StatusCodes.Status404NotFound,
                "resource_not_found")
            : Results.Ok(suggestion);
    }
    catch (SuggestedObligationValidationException exception)
    {
        return Results.ValidationProblem(
            exception.Errors.ToDictionary(error => error.Key, error => error.Value),
            title: "Suggested obligation invalid",
            detail: exception.Message,
            statusCode: StatusCodes.Status400BadRequest);
    }
})
.RequirePermission(Permission.ManageObligations)
.WithName("ReviseSuggestedObligation");

api.MapPost("/suggested-obligations/{suggestionId:guid}/reject", async (
    Guid suggestionId,
    SuggestedObligationReviewRequest request,
    SuggestedObligationService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
    await ReviewSuggestedObligationAsync(
        suggestionId,
        request,
        service.RejectAsync,
        tenantContext,
        httpContext,
        cancellationToken))
.RequirePermission(Permission.ManageObligations)
.WithName("RejectSuggestedObligation");

api.MapPost("/suggested-obligations/{suggestionId:guid}/escalate", async (
    Guid suggestionId,
    SuggestedObligationReviewRequest request,
    SuggestedObligationService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
    await ReviewSuggestedObligationAsync(
        suggestionId,
        request,
        service.EscalateAsync,
        tenantContext,
        httpContext,
        cancellationToken))
.RequirePermission(Permission.ManageObligations)
.WithName("EscalateSuggestedObligation");

api.MapGet("/expert-review-items", async (
    string? status,
    string? sourceType,
    Guid? assignedExpertUserId,
    string? priority,
    ExpertReviewQueueService service,
    CancellationToken cancellationToken) =>
    Results.Ok(await service.ListAsync(new ExpertReviewQueueQuery(status, sourceType, assignedExpertUserId, priority), cancellationToken)))
.RequirePermission(Permission.ViewObligations)
.WithName("ListExpertReviewItems");

api.MapPost("/expert-review-items", async (
    EscalateExpertReviewRequest request,
    ExpertReviewQueueService service,
    ITenantContext tenantContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var item = await service.EscalateAsync(request, tenantContext.TenantId, tenantContext.UserId, cancellationToken);
        return Results.Created($"/api/expert-review-items/{item.Id}", item);
    }
    catch (ExpertReviewValidationException exception)
    {
        return Results.ValidationProblem(
            exception.Errors.ToDictionary(error => error.Key, error => error.Value),
            title: "Expert review escalation invalid",
            detail: exception.Message,
            statusCode: StatusCodes.Status400BadRequest);
    }
})
.RequirePermission(Permission.ManageObligations)
.WithName("EscalateExpertReviewItem");

api.MapPost("/expert-review-items/{itemId:guid}/resolve", async (
    Guid itemId,
    ResolveExpertReviewRequest request,
    ExpertReviewQueueService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var item = await service.ResolveAsync(itemId, request, tenantContext.UserId, cancellationToken);
        return item is null
            ? ApiProblemDetails.Create(
                httpContext,
                "Resource not found",
                $"Expert review item '{itemId}' was not found.",
                StatusCodes.Status404NotFound,
                "resource_not_found")
            : Results.Ok(item);
    }
    catch (ExpertReviewValidationException exception)
    {
        return Results.ValidationProblem(
            exception.Errors.ToDictionary(error => error.Key, error => error.Value),
            title: "Expert review resolution invalid",
            detail: exception.Message,
            statusCode: StatusCodes.Status400BadRequest);
    }
})
.RequirePermission(Permission.ManageObligations)
.WithName("ResolveExpertReviewItem");

api.MapGet("/reports/approved-evidence-packages", async (
    IReportRepository repository,
    CancellationToken cancellationToken) =>
    Results.Ok(await repository.ListApprovedEvidencePackagesAsync(cancellationToken)))
.RequirePermission(Permission.ViewReports)
.WithName("ListApprovedEvidencePackages");

api.MapPost("/reports/evidence-packages", async (
    EvidencePackageGenerateRequest request,
    EvidencePackageReportService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    if ((request.ObligationIds.Count + request.ContractIds.Count + request.ControlIds.Count + request.SubcontractorIds.Count) == 0)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["scope"] = ["Evidence package scope must include at least one obligation, contract, CMMC control, or subcontractor."]
        });
    }

    if (request.IncludeDraftOrRejectedEvidence &&
        !httpContext.User.HasClaim(ApiSecurityExtensions.PermissionClaimType, Permission.ApproveEvidence.ToString()))
    {
        return ApiProblemDetails.Create(
            httpContext,
            "Forbidden",
            "Including draft or rejected evidence requires evidence approval permission.",
            StatusCodes.Status403Forbidden,
            "forbidden");
    }

    var report = await service.GenerateAsync(
        request,
        tenantContext.UserId,
        request.IncludeDraftOrRejectedEvidence,
        cancellationToken);
    return Results.Created($"/api/reports/evidence-packages/{report.Id}", report);
})
.RequirePermission(Permission.ManageReports)
.WithName("GenerateEvidencePackage");

api.MapGet("/reports/evidence-packages/{reportId:guid}", async (
    Guid reportId,
    EvidencePackageReportService service,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var report = await service.GetAsync(reportId, cancellationToken);
    return report is null
        ? ApiProblemDetails.Create(
            httpContext,
            "Resource not found",
            $"Evidence package '{reportId}' was not found.",
            StatusCodes.Status404NotFound,
            "resource_not_found")
        : Results.Ok(report);
})
.RequirePermission(Permission.ViewReports)
.WithName("GetEvidencePackage");

api.MapPost("/reports/compliance-status", async (
    ComplianceStatusReportService service,
    ITenantContext tenantContext,
    CancellationToken cancellationToken) =>
{
    var report = await service.GenerateAsync(tenantContext.UserId, cancellationToken);
    return Results.Created($"/api/reports/{report.Id}", report);
})
.RequirePermission(Permission.ViewReports)
.WithName("GenerateComplianceStatusReport");

api.MapPost("/reports/cmmc-readiness", async (
    Guid assessmentId,
    CmmcReadinessReportService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var includeEvidenceLinks = httpContext.User.HasClaim(ApiSecurityExtensions.PermissionClaimType, Permission.ViewEvidence.ToString());
    var report = await service.GenerateAsync(assessmentId, tenantContext.UserId, includeEvidenceLinks, cancellationToken);
    return report is null
        ? ApiProblemDetails.Create(
            httpContext,
            "Resource not found",
            $"CMMC assessment '{assessmentId}' was not found.",
            StatusCodes.Status404NotFound,
            "resource_not_found")
        : Results.Created($"/api/reports/{report.Id}", report);
})
.RequirePermission(Permission.ViewReports)
.WithName("GenerateCmmcReadinessReport");

api.MapPost("/reports/subcontractor-compliance", async (
    Guid? contractId,
    SubcontractorComplianceReportService service,
    ITenantContext tenantContext,
    CancellationToken cancellationToken) =>
{
    var report = await service.GenerateAsync(contractId, tenantContext.UserId, cancellationToken);
    return Results.Created($"/api/reports/{report.Id}", report);
})
.RequirePermission(Permission.ViewReports)
.WithName("GenerateSubcontractorComplianceReport");

api.MapGet("/subcontractors", async (
    string? status,
    bool? expiringInsuranceOnly,
    string? owner,
    SubcontractorService service,
    CancellationToken cancellationToken) =>
    Results.Ok(await service.ListCurrentTenantAsync(
        new SubcontractorListQuery(status, expiringInsuranceOnly ?? false, owner),
        cancellationToken)))
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

api.MapPost("/subcontractors/{subcontractorId:guid}/sam-lookup/search", async (
    Guid subcontractorId,
    SubcontractorEntityLookupRequest request,
    SubcontractorService subcontractorService,
    SubcontractorEntityLookupService lookupService,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var subcontractor = await subcontractorService.FindCurrentTenantAsync(subcontractorId, cancellationToken);
    if (subcontractor is null)
    {
        return ApiProblemDetails.Create(
            httpContext,
            "Resource not found",
            $"Subcontractor '{subcontractorId}' was not found.",
            StatusCodes.Status404NotFound,
            "resource_not_found");
    }

    try
    {
        return Results.Ok(await lookupService.SearchAsync(request, cancellationToken));
    }
    catch (SubcontractorEntityLookupValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["query"] = [exception.Message]
        });
    }
    catch (SubcontractorEntityLookupUnavailableException exception)
    {
        return Results.Problem(
            title: "SAM.gov lookup unavailable",
            detail: exception.Message,
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
})
.RequirePermission(Permission.ViewSubcontractors)
.WithName("SearchSubcontractorSamLookup");

api.MapPost("/subcontractors/{subcontractorId:guid}/sam-lookup/apply", async (
    Guid subcontractorId,
    ApplySubcontractorEntityLookupRequest request,
    SubcontractorEntityLookupService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var updated = await service.ApplyAsync(subcontractorId, request, tenantContext.UserId, cancellationToken);
    return updated is null
        ? ApiProblemDetails.Create(
            httpContext,
            "Resource not found",
            $"Subcontractor '{subcontractorId}' was not found.",
            StatusCodes.Status404NotFound,
            "resource_not_found")
        : Results.Ok(updated);
})
.RequirePermission(Permission.ManageSubcontractors)
.WithName("ApplySubcontractorSamLookup");

api.MapGet("/subcontractors/{subcontractorId:guid}/flow-downs", async (
    Guid subcontractorId,
    Guid? contractId,
    SubcontractorService service,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var flowDowns = await service.ListFlowDownsAsync(subcontractorId, contractId, cancellationToken);
    return flowDowns is null
        ? ApiProblemDetails.Create(
            httpContext,
            "Resource not found",
            $"Subcontractor '{subcontractorId}' was not found.",
            StatusCodes.Status404NotFound,
            "resource_not_found")
        : Results.Ok(flowDowns);
})
.RequirePermission(Permission.ViewSubcontractors)
.WithName("ListSubcontractorFlowDowns");

api.MapPost("/subcontractors/{subcontractorId:guid}/flow-downs", async (
    Guid subcontractorId,
    UpsertSubcontractorFlowDownRequest request,
    SubcontractorService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var created = await service.CreateFlowDownAsync(subcontractorId, request, tenantContext.UserId, cancellationToken);
        return created is null
            ? ApiProblemDetails.Create(
                httpContext,
                "Resource not found",
                $"Subcontractor '{subcontractorId}' was not found.",
                StatusCodes.Status404NotFound,
                "resource_not_found")
            : Results.Created($"/api/subcontractors/{subcontractorId}/flow-downs/{created.Id}", created);
    }
    catch (SubcontractorValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["flowDown"] = [exception.Message]
        },
        title: "Flow-down invalid",
        detail: exception.Message,
        statusCode: StatusCodes.Status400BadRequest);
    }
})
.RequirePermission(Permission.ManageSubcontractors)
.WithName("CreateSubcontractorFlowDown");

api.MapPut("/subcontractors/{subcontractorId:guid}/flow-downs/{flowDownId:guid}", async (
    Guid subcontractorId,
    Guid flowDownId,
    UpsertSubcontractorFlowDownRequest request,
    SubcontractorService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var updated = await service.UpdateFlowDownAsync(subcontractorId, flowDownId, request, tenantContext.UserId, cancellationToken);
        return updated is null
            ? ApiProblemDetails.Create(
                httpContext,
                "Resource not found",
                $"Flow-down '{flowDownId}' was not found.",
                StatusCodes.Status404NotFound,
                "resource_not_found")
            : Results.Ok(updated);
    }
    catch (SubcontractorValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["flowDown"] = [exception.Message]
        },
        title: "Flow-down invalid",
        detail: exception.Message,
        statusCode: StatusCodes.Status400BadRequest);
    }
})
.RequirePermission(Permission.ManageSubcontractors)
.WithName("UpdateSubcontractorFlowDown");

api.MapGet("/subcontractors/{subcontractorId:guid}/supplier-obligations", async (
    Guid subcontractorId,
    Guid? contractId,
    SubcontractorService service,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var obligations = await service.ListSupplierObligationsAsync(subcontractorId, contractId, cancellationToken);
    return obligations is null
        ? ApiProblemDetails.Create(
            httpContext,
            "Resource not found",
            $"Subcontractor '{subcontractorId}' was not found.",
            StatusCodes.Status404NotFound,
            "resource_not_found")
        : Results.Ok(obligations);
})
.RequirePermission(Permission.ViewSubcontractors)
.WithName("ListSubcontractorSupplierObligations");

api.MapGet("/contracts/{contractId:guid}/supplier-obligations", async (
    Guid contractId,
    SubcontractorService service,
    CancellationToken cancellationToken) =>
    Results.Ok(await service.ListSupplierObligationsAsync(null, contractId, cancellationToken) ?? []))
.RequirePermission(Permission.ViewSubcontractors)
.WithName("ListContractSupplierObligations");

api.MapPost("/subcontractors/{subcontractorId:guid}/supplier-obligations", async (
    Guid subcontractorId,
    UpsertSupplierObligationRequest request,
    SubcontractorService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var created = await service.CreateSupplierObligationAsync(subcontractorId, request, tenantContext.UserId, cancellationToken);
        return created is null
            ? ApiProblemDetails.Create(
                httpContext,
                "Resource not found",
                $"Subcontractor '{subcontractorId}' was not found.",
                StatusCodes.Status404NotFound,
                "resource_not_found")
            : Results.Created($"/api/subcontractors/{subcontractorId}/supplier-obligations/{created.Id}", created);
    }
    catch (SubcontractorValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["supplierObligation"] = [exception.Message]
        },
        title: "Supplier obligation invalid",
        detail: exception.Message,
        statusCode: StatusCodes.Status400BadRequest);
    }
})
.RequirePermission(Permission.ManageSubcontractors)
.WithName("CreateSupplierObligation");

api.MapPost("/subcontractors/{subcontractorId:guid}/supplier-obligations/bulk-from-flow-downs", async (
    Guid subcontractorId,
    BulkCreateSupplierObligationsRequest request,
    SubcontractorService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var created = await service.BulkCreateSupplierObligationsAsync(subcontractorId, request, tenantContext.UserId, cancellationToken);
        return created is null
            ? ApiProblemDetails.Create(
                httpContext,
                "Resource not found",
                $"Subcontractor '{subcontractorId}' was not found.",
                StatusCodes.Status404NotFound,
                "resource_not_found")
            : Results.Ok(created);
    }
    catch (SubcontractorValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["supplierObligation"] = [exception.Message]
        },
        title: "Supplier obligation invalid",
        detail: exception.Message,
        statusCode: StatusCodes.Status400BadRequest);
    }
})
.RequirePermission(Permission.ManageSubcontractors)
.WithName("BulkCreateSupplierObligationsFromFlowDowns");

api.MapPut("/subcontractors/{subcontractorId:guid}/supplier-obligations/{supplierObligationId:guid}", async (
    Guid subcontractorId,
    Guid supplierObligationId,
    UpsertSupplierObligationRequest request,
    SubcontractorService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var updated = await service.UpdateSupplierObligationAsync(subcontractorId, supplierObligationId, request, tenantContext.UserId, cancellationToken);
        return updated is null
            ? ApiProblemDetails.Create(
                httpContext,
                "Resource not found",
                $"Supplier obligation '{supplierObligationId}' was not found.",
                StatusCodes.Status404NotFound,
                "resource_not_found")
            : Results.Ok(updated);
    }
    catch (SubcontractorValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["supplierObligation"] = [exception.Message]
        },
        title: "Supplier obligation invalid",
        detail: exception.Message,
        statusCode: StatusCodes.Status400BadRequest);
    }
})
.RequirePermission(Permission.ManageSubcontractors)
.WithName("UpdateSupplierObligation");

api.MapGet("/subcontractors/{subcontractorId:guid}/evidence-requests", async (
    Guid subcontractorId,
    SubcontractorService service,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var requests = await service.ListEvidenceRequestsAsync(subcontractorId, cancellationToken);
    return requests is null
        ? ApiProblemDetails.Create(
            httpContext,
            "Resource not found",
            $"Subcontractor '{subcontractorId}' was not found.",
            StatusCodes.Status404NotFound,
            "resource_not_found")
        : Results.Ok(requests);
})
.RequirePermission(Permission.ViewSubcontractors)
.WithName("ListSubcontractorEvidenceRequests");

api.MapPost("/subcontractors/{subcontractorId:guid}/evidence-requests", async (
    Guid subcontractorId,
    UpsertSubcontractorEvidenceRequestRequest request,
    SubcontractorService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var created = await service.CreateEvidenceRequestAsync(subcontractorId, request, tenantContext.UserId, cancellationToken);
        return created is null
            ? ApiProblemDetails.Create(
                httpContext,
                "Resource not found",
                $"Subcontractor '{subcontractorId}' was not found.",
                StatusCodes.Status404NotFound,
                "resource_not_found")
            : Results.Created($"/api/subcontractors/{subcontractorId}/evidence-requests/{created.Id}", created);
    }
    catch (SubcontractorValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["evidenceRequest"] = [exception.Message]
        },
        title: "Evidence request invalid",
        detail: exception.Message,
        statusCode: StatusCodes.Status400BadRequest);
    }
})
.RequirePermission(Permission.ManageSubcontractors)
.WithName("CreateSubcontractorEvidenceRequest");

api.MapPut("/subcontractors/{subcontractorId:guid}/evidence-requests/{evidenceRequestId:guid}", async (
    Guid subcontractorId,
    Guid evidenceRequestId,
    UpsertSubcontractorEvidenceRequestRequest request,
    SubcontractorService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var updated = await service.UpdateEvidenceRequestAsync(subcontractorId, evidenceRequestId, request, tenantContext.UserId, cancellationToken);
        return updated is null
            ? ApiProblemDetails.Create(
                httpContext,
                "Resource not found",
                $"Evidence request '{evidenceRequestId}' was not found.",
                StatusCodes.Status404NotFound,
                "resource_not_found")
            : Results.Ok(updated);
    }
    catch (SubcontractorValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["evidenceRequest"] = [exception.Message]
        },
        title: "Evidence request invalid",
        detail: exception.Message,
        statusCode: StatusCodes.Status400BadRequest);
    }
})
.RequirePermission(Permission.ManageSubcontractors)
.WithName("UpdateSubcontractorEvidenceRequest");

api.MapGet("/policy-templates", async (
    bool? includeReviewStates,
    PolicyTemplateService service,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var canReviewTemplates = httpContext.User.HasClaim(ApiSecurityExtensions.PermissionClaimType, Permission.ManageObligations.ToString());
    return Results.Ok(await service.ListAsync(includeReviewStates == true && canReviewTemplates, cancellationToken));
})
.RequirePermission(Permission.ViewObligations)
.WithName("ListPolicyTemplates");

api.MapGet("/policy-templates/{templateId:guid}/versions", async (
    Guid templateId,
    PolicyTemplateService service,
    CancellationToken cancellationToken) =>
    Results.Ok(await service.ListVersionsAsync(templateId, cancellationToken)))
.RequirePermission(Permission.ManageObligations)
.WithName("ListPolicyTemplateVersions");

api.MapPost("/policy-templates", async (
    UpsertPolicyTemplateRequest request,
    PolicyTemplateService service,
    ITenantContext tenantContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var created = await service.CreateAsync(request, tenantContext.UserId, cancellationToken);
        return Results.Created($"/api/policy-templates/{created.Id}", created);
    }
    catch (PolicyTemplateValidationException exception)
    {
        return Results.ValidationProblem(
            exception.Errors,
            title: "Policy template invalid",
            detail: exception.Message,
            statusCode: StatusCodes.Status400BadRequest);
    }
})
.RequirePermission(Permission.ManageObligations)
.WithName("CreatePolicyTemplate");

api.MapPut("/policy-templates/{templateId:guid}/lifecycle", async (
    Guid templateId,
    ChangePolicyTemplateLifecycleRequest request,
    PolicyTemplateService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var updated = await service.ChangeLifecycleAsync(templateId, request, tenantContext.UserId, cancellationToken);
        return updated is null
            ? ApiProblemDetails.Create(
                httpContext,
                "Resource not found",
                $"Policy template '{templateId}' was not found.",
                StatusCodes.Status404NotFound,
                "resource_not_found")
            : Results.Ok(updated);
    }
    catch (PolicyTemplateValidationException exception)
    {
        return Results.ValidationProblem(
            exception.Errors,
            title: "Policy template invalid",
            detail: exception.Message,
            statusCode: StatusCodes.Status400BadRequest);
    }
})
.RequirePermission(Permission.ManageObligations)
.WithName("ChangePolicyTemplateLifecycle");

api.MapPost("/policy-templates/{templateId:guid}/generate", async (
    Guid templateId,
    PolicyTemplateService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var generated = await service.GenerateDraftPolicyAsync(templateId, tenantContext.UserId, cancellationToken);
    return generated is null
        ? ApiProblemDetails.Create(
            httpContext,
            "Resource not found",
            $"Approved policy template '{templateId}' was not found.",
            StatusCodes.Status404NotFound,
            "resource_not_found")
        : Results.Created($"/api/generated-policies/{generated.Id}", generated);
})
.RequirePermission(Permission.ManageEvidence)
.WithName("GenerateDraftPolicyFromTemplate");

api.MapGet("/generated-policies/{policyId:guid}", async (
    Guid policyId,
    PolicyTemplateService service,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var policy = await service.FindGeneratedPolicyAsync(policyId, cancellationToken);
    return policy is null
        ? ApiProblemDetails.Create(
            httpContext,
            "Resource not found",
            $"Generated policy '{policyId}' was not found.",
            StatusCodes.Status404NotFound,
            "resource_not_found")
        : Results.Ok(policy);
})
.RequirePermission(Permission.ViewEvidence)
.WithName("GetGeneratedPolicy");

api.MapPut("/generated-policies/{policyId:guid}", async (
    Guid policyId,
    UpdateGeneratedPolicyRequest request,
    PolicyTemplateService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var updated = await service.UpdateGeneratedPolicyAsync(policyId, request, tenantContext.UserId, cancellationToken);
    return updated is null
        ? ApiProblemDetails.Create(
            httpContext,
            "Resource not found",
            $"Generated policy '{policyId}' was not found.",
            StatusCodes.Status404NotFound,
            "resource_not_found")
        : Results.Ok(updated);
})
.RequirePermission(Permission.ManageEvidence)
.WithName("UpdateGeneratedPolicy");

api.MapPut("/generated-policies/{policyId:guid}/review", async (
    Guid policyId,
    PolicyApprovalRequest request,
    PolicyTemplateService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var updated = await service.ReviewGeneratedPolicyAsync(policyId, request, tenantContext.UserId, cancellationToken);
    return updated is null
        ? ApiProblemDetails.Create(
            httpContext,
            "Resource not found",
            $"Generated policy '{policyId}' was not found.",
            StatusCodes.Status404NotFound,
            "resource_not_found")
        : Results.Ok(updated);
})
.RequirePermission(Permission.ApproveEvidence)
.WithName("ReviewGeneratedPolicy");

api.MapGet("/generated-policies/{policyId:guid}/revisions", async (
    Guid policyId,
    PolicyTemplateService service,
    CancellationToken cancellationToken) =>
    Results.Ok(await service.ListPolicyRevisionsAsync(policyId, cancellationToken)))
.RequirePermission(Permission.ViewEvidence)
.WithName("ListGeneratedPolicyRevisions");

api.MapGet("/tasks", async (
    ComplianceTaskService service,
    CancellationToken cancellationToken) =>
    Results.Ok(await service.ListCurrentTenantAsync(cancellationToken)))
.RequirePermission(Permission.ViewTasks)
.WithName("ListComplianceTasks");

api.MapGet("/tasks/{taskId:guid}", async (
    Guid taskId,
    ComplianceTaskService service,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var task = (await service.ListCurrentTenantAsync(cancellationToken)).FirstOrDefault(candidate => candidate.Id == taskId);
    return task is null
        ? ApiProblemDetails.Create(
            httpContext,
            "Resource not found",
            $"Task '{taskId}' was not found.",
            StatusCodes.Status404NotFound,
            "resource_not_found")
        : Results.Ok(task);
})
.RequirePermission(Permission.ViewTasks)
.WithName("GetComplianceTask");

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

api.MapPost("/audit-logs/cui-export", async (
    CuiAuditExportRequest request,
    CuiAuditExportService service,
    ITenantContext tenantContext,
    CancellationToken cancellationToken) =>
    Results.Ok(await service.ExportAsync(tenantContext.TenantId, tenantContext.UserId, request, cancellationToken)))
.RequirePermission(Permission.ViewAuditLog)
.WithName("ExportCuiAuditLogs");

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

api.MapGet("/content-classification-review-items", async (
    ContentClassificationReviewService service,
    CancellationToken cancellationToken) =>
    Results.Ok(await service.ListAsync(cancellationToken)))
.RequirePermission(Permission.ViewEvidence)
.WithName("ListContentClassificationReviewItems");

api.MapPatch("/evidence-items/{evidenceItemId:guid}/classification", async (
    Guid evidenceItemId,
    ReclassifyContentRequest request,
    ContentClassificationReviewService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var evidence = await service.ReclassifyEvidenceAsync(evidenceItemId, request, tenantContext.UserId, cancellationToken);
    return evidence is null
        ? ApiProblemDetails.Create(
            httpContext,
            "Resource not found",
            $"Evidence item '{evidenceItemId}' was not found.",
            StatusCodes.Status404NotFound,
            "resource_not_found")
        : Results.Ok(evidence);
})
.RequirePermission(Permission.ApproveEvidence)
.WithName("ReclassifyEvidenceItem");

api.MapGet("/demo/synthetic-dataset", async (
    SyntheticDemoDatasetService service,
    IWebHostEnvironment environment,
    CancellationToken cancellationToken) =>
{
    var packageRoot = DemoContentPackageLocator.FindPackageRoot(environment.ContentRootPath);
    return Results.Ok(await service.GetAsync(packageRoot, cancellationToken));
})
.RequirePermission(Permission.ViewEvidence)
.WithName("GetSyntheticDemoDataset");

api.MapPost("/demo/synthetic-dataset/precheck", async (
    SyntheticDemoDatasetService service,
    IWebHostEnvironment environment,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var packageRoot = DemoContentPackageLocator.FindPackageRoot(environment.ContentRootPath);
    var result = await service.PrecheckAsync(packageRoot, cancellationToken);
    return result.Allowed
        ? Results.Ok(result)
        : ApiProblemDetails.Create(
            httpContext,
            "Synthetic demo dataset precheck failed",
            string.Join(" ", result.Errors),
            StatusCodes.Status400BadRequest,
            "synthetic_demo_dataset_precheck_failed");
})
.RequirePermission(Permission.ManageObligations)
.WithName("PrecheckSyntheticDemoDataset");

api.MapPost("/demo/seed", async (
    SyntheticDemoDatasetService datasetService,
    DemoTenantSeedService seedService,
    IWebHostEnvironment environment,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var packageRoot = DemoContentPackageLocator.FindPackageRoot(environment.ContentRootPath);
        var dataset = await datasetService.GetAsync(packageRoot, cancellationToken);
        var result = await seedService.SeedAsync(dataset, tenantContext.TenantId, tenantContext.UserId, cancellationToken);
        return Results.Ok(result);
    }
    catch (DemoTenantSeedValidationException exception)
    {
        return ApiProblemDetails.Create(
            httpContext,
            "Synthetic demo seed rejected",
            exception.Message,
            StatusCodes.Status400BadRequest,
            "synthetic_demo_seed_rejected");
    }
})
.RequirePermission(Permission.ManageObligations)
.WithName("SeedDemoTenant");

api.MapDelete("/demo/seed", async (
    SyntheticDemoDatasetService datasetService,
    DemoTenantSeedService seedService,
    IWebHostEnvironment environment,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var packageRoot = DemoContentPackageLocator.FindPackageRoot(environment.ContentRootPath);
        var dataset = await datasetService.GetAsync(packageRoot, cancellationToken);
        var result = await seedService.ResetAsync(dataset, tenantContext.TenantId, tenantContext.UserId, cancellationToken);
        return Results.Ok(result);
    }
    catch (DemoTenantSeedValidationException exception)
    {
        return ApiProblemDetails.Create(
            httpContext,
            "Synthetic demo reset rejected",
            exception.Message,
            StatusCodes.Status400BadRequest,
            "synthetic_demo_reset_rejected");
    }
})
.RequirePermission(Permission.ManageObligations)
.WithName("ResetDemoTenantSeed");

api.MapPost("/evidence-requests", async (
    CreateEvidenceRequestRequest request,
    EvidenceRequestService service,
    ITenantContext tenantContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var created = await service.CreateAsync(request, tenantContext.UserId, cancellationToken);
        return Results.Created($"/api/evidence-requests/{created.Id}", created);
    }
    catch (EvidenceRequestValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["evidenceRequest"] = [exception.Message]
        },
        title: "Evidence request invalid",
        detail: exception.Message,
        statusCode: StatusCodes.Status400BadRequest);
    }
})
.RequirePermission(Permission.ManageEvidence)
.WithName("CreateEvidenceRequest");

api.MapGet("/evidence-requests", async (
    EvidenceRequestStatus? status,
    DateOnly? dueFrom,
    DateOnly? dueTo,
    Guid? assigneeUserId,
    EvidenceRequestRelatedRecordType? relatedRecordType,
    EvidenceRequestPriority? priority,
    EvidenceRequestService service,
    CancellationToken cancellationToken) =>
    Results.Ok(await service.ListAsync(
        new EvidenceRequestDashboardQuery(status, dueFrom, dueTo, assigneeUserId, relatedRecordType, priority),
        cancellationToken)))
.RequirePermission(Permission.ViewEvidence)
.WithName("ListEvidenceRequests");

api.MapPost("/evidence-requests/reminders", async (
    EvidenceRequestReminderRequest request,
    EvidenceRequestService service,
    ITenantContext tenantContext,
    CancellationToken cancellationToken) =>
    Results.Ok(new { count = await service.SendBulkRemindersAsync(request, tenantContext.UserId, cancellationToken) }))
.RequirePermission(Permission.ManageEvidence)
.WithName("SendEvidenceRequestReminders");

api.MapPut("/evidence-requests/{requestId:guid}/submit", async (
    Guid requestId,
    SubmitEvidenceRequestRequest request,
    EvidenceRequestService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var updated = await service.SubmitAsync(requestId, request, tenantContext.UserId, cancellationToken);
        return updated is null
            ? ApiProblemDetails.Create(
                httpContext,
                "Resource not found",
                $"Evidence request '{requestId}' was not found.",
                StatusCodes.Status404NotFound,
                "resource_not_found")
            : Results.Ok(updated);
    }
    catch (EvidenceRequestValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["evidenceRequest"] = [exception.Message]
        },
        title: "Evidence request invalid",
        detail: exception.Message,
        statusCode: StatusCodes.Status400BadRequest);
    }
})
.RequirePermission(Permission.ManageEvidence)
.WithName("SubmitEvidenceRequest");

api.MapPut("/evidence-requests/{requestId:guid}/review", async (
    Guid requestId,
    ReviewEvidenceRequestRequest request,
    EvidenceRequestService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var updated = await service.ReviewAsync(requestId, request, tenantContext.UserId, cancellationToken);
        return updated is null
            ? ApiProblemDetails.Create(
                httpContext,
                "Resource not found",
                $"Evidence request '{requestId}' was not found.",
                StatusCodes.Status404NotFound,
                "resource_not_found")
            : Results.Ok(updated);
    }
    catch (EvidenceRequestValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["evidenceRequest"] = [exception.Message]
        },
        title: "Evidence request invalid",
        detail: exception.Message,
        statusCode: StatusCodes.Status400BadRequest);
    }
})
.RequirePermission(Permission.ApproveEvidence)
.WithName("ReviewEvidenceRequest");

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

api.MapGet("/cmmc/controls", async (
    CmmcAssessmentService service,
    CancellationToken cancellationToken) =>
    Results.Ok(await service.ListControlLibraryAsync(cancellationToken)))
.RequirePermission(Permission.ViewCmmc)
.WithName("ListCmmcControlLibrary");

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

api.MapGet("/cmmc/assessments/{assessmentId:guid}/responsibility-matrix", async (
    Guid assessmentId,
    CmmcAssessmentService service,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var matrix = await service.GetResponsibilityMatrixAsync(assessmentId, cancellationToken);
    return matrix is null
        ? ApiProblemDetails.Create(
            httpContext,
            "Resource not found",
            $"CMMC assessment '{assessmentId}' was not found.",
            StatusCodes.Status404NotFound,
            "resource_not_found")
        : Results.Ok(matrix);
})
.RequirePermission(Permission.ViewCmmc)
.WithName("GetCmmcResponsibilityMatrix");

api.MapGet("/cmmc/assessments/{assessmentId:guid}/responsibility-matrix/export", async (
    Guid assessmentId,
    CmmcAssessmentService service,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var csv = await service.ExportResponsibilityMatrixCsvAsync(assessmentId, cancellationToken);
    return csv is null
        ? ApiProblemDetails.Create(
            httpContext,
            "Resource not found",
            $"CMMC assessment '{assessmentId}' was not found.",
            StatusCodes.Status404NotFound,
            "resource_not_found")
        : Results.Text(csv, "text/csv");
})
.RequirePermission(Permission.ViewCmmc)
.WithName("ExportCmmcResponsibilityMatrix");

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

api.MapGet("/cmmc/assessments/{assessmentId:guid}/gaps", async (
    Guid assessmentId,
    CmmcAssessmentService service,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var gaps = await service.GetReadinessGapsAsync(assessmentId, cancellationToken);
    return gaps is null
        ? ApiProblemDetails.Create(
            httpContext,
            "Resource not found",
            $"CMMC assessment '{assessmentId}' was not found.",
            StatusCodes.Status404NotFound,
            "resource_not_found")
        : Results.Ok(gaps);
})
.RequirePermission(Permission.ViewCmmc)
.WithName("GetCmmcReadinessGaps");

api.MapPost("/cmmc/assessments/{assessmentId:guid}/gaps/{controlId}/poam-item", async (
    Guid assessmentId,
    string controlId,
    CreatePoamFromGapRequest request,
    CmmcPoamService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var created = await service.CreateAsync(
            assessmentId,
            new UpsertCmmcPoamItemRequest(
                controlId.Trim(),
                $"Readiness gap for {controlId.Trim()}",
                "Remediate the prioritized CMMC readiness gap and attach supporting evidence.",
                RiskLevel.High,
                PoamStatus.Open,
                request.OwnerUserId,
                request.OwnerFunction,
                request.TargetCompletionAt,
                null,
                null,
                []),
            tenantContext.UserId,
            cancellationToken);
        return created is null
            ? ApiProblemDetails.Create(
                httpContext,
                "Resource not found",
                $"CMMC assessment '{assessmentId}' or control '{controlId}' was not found.",
                StatusCodes.Status404NotFound,
                "resource_not_found")
            : Results.Created($"/api/cmmc/assessments/{assessmentId}/poam-items/{created.Id}", created);
    }
    catch (CmmcPoamValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["cmmcGapPoamItem"] = [exception.Message]
        },
        title: "CMMC gap POA&M item invalid",
        detail: exception.Message,
        statusCode: StatusCodes.Status400BadRequest);
    }
})
.RequirePermission(Permission.ManageCmmc)
.WithName("CreateCmmcPoamItemFromGap");

api.MapGet("/cmmc/poam-items", async (
    CmmcPoamService service,
    CancellationToken cancellationToken) =>
    Results.Ok(await service.ListCurrentTenantAsync(cancellationToken)))
.RequirePermission(Permission.ViewCmmc)
.WithName("ListCurrentTenantCmmcPoamItems");

api.MapGet("/cmmc/poam-items/{poamItemId:guid}", async (
    Guid poamItemId,
    CmmcPoamService service,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var item = await service.FindCurrentTenantAsync(poamItemId, cancellationToken);
    return item is null
        ? ApiProblemDetails.Create(
            httpContext,
            "Resource not found",
            $"CMMC POA&M item '{poamItemId}' was not found.",
            StatusCodes.Status404NotFound,
            "resource_not_found")
        : Results.Ok(item);
})
.RequirePermission(Permission.ViewCmmc)
.WithName("GetCurrentTenantCmmcPoamItem");

api.MapPatch("/cmmc/poam-items/{poamItemId:guid}", async (
    Guid poamItemId,
    UpsertCmmcPoamItemRequest request,
    CmmcPoamService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var updated = await service.UpdateCurrentTenantAsync(poamItemId, request, tenantContext.UserId, cancellationToken);
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
.WithName("UpdateCurrentTenantCmmcPoamItem");

api.MapPost("/cmmc/poam-items/{poamItemId:guid}/close", async (
    Guid poamItemId,
    CmmcPoamService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var closed = await service.CloseCurrentTenantAsync(
        poamItemId,
        tenantContext.UserId,
        DateOnly.FromDateTime(DateTime.UtcNow),
        cancellationToken);
    return closed is null
        ? ApiProblemDetails.Create(
            httpContext,
            "Resource not found",
            $"CMMC POA&M item '{poamItemId}' was not found.",
            StatusCodes.Status404NotFound,
            "resource_not_found")
        : Results.Ok(closed);
})
.RequirePermission(Permission.ManageCmmc)
.WithName("CloseCurrentTenantCmmcPoamItem");

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

api.MapGet("/enterprise/saml-configurations", async (
    SamlIdentityProviderConfigurationService service,
    CancellationToken cancellationToken) =>
    Results.Ok(await service.ListCurrentTenantAsync(cancellationToken)))
.RequirePermission(Permission.ManageUsers)
.WithName("ListSamlIdentityProviderConfigurations");

api.MapPost("/enterprise/saml-configurations", async (
    UpsertSamlIdentityProviderConfigurationRequest request,
    SamlIdentityProviderConfigurationService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var configuration = await service.CreateAsync(request, tenantContext.UserId, cancellationToken);
        return Results.Created($"/api/enterprise/saml-configurations/{configuration.Id}", configuration);
    }
    catch (SamlConfigurationValidationException exception) when (
        exception.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
    {
        return ApiProblemDetails.Create(
            httpContext,
            "Duplicate SAML configuration",
            exception.Message,
            StatusCodes.Status409Conflict,
            "duplicate_saml_configuration");
    }
    catch (SamlConfigurationValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["samlConfiguration"] = [exception.Message]
        });
    }
})
.RequirePermission(Permission.ManageUsers)
.WithName("CreateSamlIdentityProviderConfiguration");

api.MapPost("/enterprise/saml-configurations/{configurationId:guid}/test", async (
    Guid configurationId,
    SamlIdentityProviderConfigurationService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var configuration = await service.TestAsync(configurationId, tenantContext.UserId, cancellationToken);
    return configuration is null
        ? ApiProblemDetails.Create(
            httpContext,
            "Resource not found",
            "SAML configuration was not found in the current tenant scope.",
            StatusCodes.Status404NotFound,
            "resource_not_found")
        : Results.Ok(configuration);
})
.RequirePermission(Permission.ManageUsers)
.WithName("TestSamlIdentityProviderConfiguration");

api.MapPost("/enterprise/saml-configurations/{configurationId:guid}/enable", async (
    Guid configurationId,
    SamlIdentityProviderConfigurationService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var configuration = await service.EnableAsync(configurationId, tenantContext.UserId, cancellationToken);
        return configuration is null
            ? ApiProblemDetails.Create(
                httpContext,
                "Resource not found",
                "SAML configuration was not found in the current tenant scope.",
                StatusCodes.Status404NotFound,
                "resource_not_found")
            : Results.Ok(configuration);
    }
    catch (SamlConfigurationValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["samlConfiguration"] = [exception.Message]
        });
    }
})
.RequirePermission(Permission.ManageUsers)
.WithName("EnableSamlIdentityProviderConfiguration");

api.MapPost("/enterprise/saml-configurations/{configurationId:guid}/disable", async (
    Guid configurationId,
    SamlIdentityProviderConfigurationService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var configuration = await service.DisableAsync(configurationId, tenantContext.UserId, cancellationToken);
    return configuration is null
        ? ApiProblemDetails.Create(
            httpContext,
            "Resource not found",
            "SAML configuration was not found in the current tenant scope.",
            StatusCodes.Status404NotFound,
            "resource_not_found")
        : Results.Ok(configuration);
})
.RequirePermission(Permission.ManageUsers)
.WithName("DisableSamlIdentityProviderConfiguration");

api.MapPost("/enterprise/saml-configurations/{configurationId:guid}/archive", async (
    Guid configurationId,
    SamlIdentityProviderConfigurationService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var configuration = await service.ArchiveAsync(configurationId, tenantContext.UserId, cancellationToken);
    return configuration is null
        ? ApiProblemDetails.Create(
            httpContext,
            "Resource not found",
            "SAML configuration was not found in the current tenant scope.",
            StatusCodes.Status404NotFound,
            "resource_not_found")
        : Results.Ok(configuration);
})
.RequirePermission(Permission.ManageUsers)
.WithName("ArchiveSamlIdentityProviderConfiguration");

api.MapPost("/enterprise/saml-configurations/{configurationId:guid}/rotate-certificate", async (
    Guid configurationId,
    RotateSamlCertificateRequest request,
    SamlIdentityProviderConfigurationService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var configuration = await service.RotateCertificateAsync(configurationId, request, tenantContext.UserId, cancellationToken);
        return configuration is null
            ? ApiProblemDetails.Create(
                httpContext,
                "Resource not found",
                "SAML configuration was not found in the current tenant scope.",
                StatusCodes.Status404NotFound,
                "resource_not_found")
            : Results.Ok(configuration);
    }
    catch (SamlConfigurationValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["samlConfiguration"] = [exception.Message]
        });
    }
})
.RequirePermission(Permission.ManageUsers)
.WithName("RotateSamlIdentityProviderCertificate");

api.MapGet("/enterprise/sso-policy", async (
    SsoSignInEnforcementService service,
    CancellationToken cancellationToken) =>
    Results.Ok(await service.GetPolicyAsync(cancellationToken)))
.RequirePermission(Permission.ManageUsers)
.WithName("GetTenantSsoPolicy");

api.MapPut("/enterprise/sso-policy", async (
    UpdateTenantSsoPolicyRequest request,
    SsoSignInEnforcementService service,
    ITenantContext tenantContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        return Results.Ok(await service.UpdatePolicyAsync(request, tenantContext.UserId, cancellationToken));
    }
    catch (SsoSignInValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["ssoPolicy"] = [exception.Message]
        });
    }
})
.RequirePermission(Permission.ManageUsers)
.WithName("UpdateTenantSsoPolicy");

api.MapPost("/enterprise/sso/account-links", async (
    CreateSamlAccountLinkRequest request,
    SsoSignInEnforcementService service,
    ITenantContext tenantContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var link = await service.LinkAccountAsync(request, tenantContext.UserId, cancellationToken);
        return Results.Created($"/api/enterprise/sso/account-links/{link.Id}", link);
    }
    catch (SsoSignInValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["ssoAccountLink"] = [exception.Message]
        });
    }
})
.RequirePermission(Permission.ManageUsers)
.WithName("CreateSamlAccountLink");

api.MapPost("/enterprise/sso/sign-in-evaluations", async (
    SsoSignInEvaluationRequest request,
    SsoSignInEnforcementService service,
    ITenantContext tenantContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        return Results.Ok(await service.EvaluateSamlSignInAsync(request, tenantContext.UserId, cancellationToken));
    }
    catch (SsoSignInValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["ssoSignIn"] = [exception.Message]
        });
    }
})
.RequirePermission(Permission.ManageUsers)
.WithName("EvaluateSsoSignIn");

api.MapPost("/enterprise/sso/break-glass", async (
    CreateBreakGlassAccessRequest request,
    SsoSignInEnforcementService service,
    ITenantContext tenantContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var grant = await service.CreateBreakGlassGrantAsync(request, tenantContext.UserId, cancellationToken);
        return Results.Created($"/api/enterprise/sso/break-glass/{grant.Id}", grant);
    }
    catch (SsoSignInValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["breakGlass"] = [exception.Message]
        });
    }
})
.RequirePermission(Permission.ManageUsers)
.WithName("CreateBreakGlassAccessGrant");

api.MapPost("/enterprise/sso/break-glass/{grantId:guid}/use", async (
    Guid grantId,
    SsoSignInEnforcementService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var result = await service.UseBreakGlassGrantAsync(grantId, tenantContext.UserId, cancellationToken);
    return result is null
        ? ApiProblemDetails.Create(
            httpContext,
            "Resource not found",
            "Break-glass access grant was not found in the current tenant scope.",
            StatusCodes.Status404NotFound,
            "resource_not_found")
        : Results.Ok(result);
})
.RequirePermission(Permission.ManageUsers)
.WithName("UseBreakGlassAccessGrant");

api.MapGet("/enterprise/scim/configuration", async (
    ScimProvisioningService service,
    CancellationToken cancellationToken) =>
    Results.Ok(await service.GetConfigurationAsync(cancellationToken)))
.RequirePermission(Permission.ManageUsers)
.WithName("GetScimProvisioningConfiguration");

api.MapPost("/enterprise/scim/enable", async (
    EnableScimProvisioningRequest request,
    ScimProvisioningService service,
    ITenantContext tenantContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        return Results.Ok(await service.EnableAsync(request, tenantContext.UserId, cancellationToken));
    }
    catch (ScimProvisioningValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["scim"] = [exception.Message] });
    }
})
.RequirePermission(Permission.ManageUsers)
.WithName("EnableScimProvisioning");

api.MapPost("/enterprise/scim/token/rotate", async (
    ScimProvisioningService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var result = await service.RotateTokenAsync(tenantContext.UserId, cancellationToken);
    return result is null
        ? ApiProblemDetails.Create(httpContext, "Resource not found", "SCIM provisioning is not enabled for the current tenant.", StatusCodes.Status404NotFound, "resource_not_found")
        : Results.Ok(result);
})
.RequirePermission(Permission.ManageUsers)
.WithName("RotateScimToken");

api.MapPost("/enterprise/scim/token/revoke", async (
    ScimProvisioningService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var configuration = await service.RevokeTokenAsync(tenantContext.UserId, cancellationToken);
    return configuration is null
        ? ApiProblemDetails.Create(httpContext, "Resource not found", "SCIM provisioning is not enabled for the current tenant.", StatusCodes.Status404NotFound, "resource_not_found")
        : Results.Ok(configuration);
})
.RequirePermission(Permission.ManageUsers)
.WithName("RevokeScimToken");

api.MapPut("/enterprise/scim/group-mappings", async (
    UpsertScimGroupMappingRequest request,
    ScimProvisioningService service,
    ITenantContext tenantContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        return Results.Ok(await service.UpsertGroupMappingAsync(request, tenantContext.UserId, cancellationToken));
    }
    catch (ScimProvisioningValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["scimGroupMapping"] = [exception.Message] });
    }
})
.RequirePermission(Permission.ManageUsers)
.WithName("UpsertScimGroupMapping");

api.MapPut("/enterprise/scim/users", async (
    ScimProvisionUserRequest request,
    ScimProvisioningService service,
    ITenantContext tenantContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        return Results.Ok(await service.ProvisionUserAsync(request, tenantContext.UserId, cancellationToken));
    }
    catch (ScimProvisioningValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["scimUser"] = [exception.Message] });
    }
})
.RequirePermission(Permission.ManageUsers)
.WithName("ProvisionScimUser");

api.MapPost("/enterprise/scim/users/{externalId}/reactivate", async (
    string externalId,
    ScimTokenRequest request,
    ScimProvisioningService service,
    ITenantContext tenantContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        return Results.Ok(await service.ReactivateUserAsync(request, externalId, tenantContext.UserId, cancellationToken));
    }
    catch (ScimProvisioningValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["scimUser"] = [exception.Message] });
    }
})
.RequirePermission(Permission.ManageUsers)
.WithName("ReactivateScimUser");

api.MapPost("/enterprise/scim/users/{externalId}/groups", async (
    string externalId,
    ScimGroupAssignmentRequest request,
    ScimProvisioningService service,
    ITenantContext tenantContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        return Results.Ok(await service.AssignGroupAsync(request, externalId, tenantContext.UserId, cancellationToken));
    }
    catch (ScimProvisioningValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["scimGroup"] = [exception.Message] });
    }
})
.RequirePermission(Permission.ManageUsers)
.WithName("AssignScimUserGroup");

api.MapDelete("/enterprise/scim/users/{externalId}/groups", async (
    string externalId,
    [FromBody] ScimGroupAssignmentRequest request,
    ScimProvisioningService service,
    ITenantContext tenantContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        return Results.Ok(await service.RemoveGroupAsync(request, externalId, tenantContext.UserId, cancellationToken));
    }
    catch (ScimProvisioningValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["scimGroup"] = [exception.Message] });
    }
})
.RequirePermission(Permission.ManageUsers)
.WithName("RemoveScimUserGroup");

api.MapGet("/enterprise/government-cloud-environments", async (
    GovernmentCloudEnvironmentService service,
    CancellationToken cancellationToken) =>
    Results.Ok(await service.ListAsync(cancellationToken)))
.RequirePermission(Permission.ManageTenant)
.WithName("ListGovernmentCloudEnvironments");

api.MapPost("/enterprise/government-cloud-environments", async (
    UpsertGovernmentCloudEnvironmentRequest request,
    GovernmentCloudEnvironmentService service,
    ITenantContext tenantContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var environment = await service.CreateAsync(request, tenantContext.UserId, cancellationToken);
        return Results.Created($"/api/enterprise/government-cloud-environments/{environment.Id}", environment);
    }
    catch (GovernmentCloudEnvironmentValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["environment"] = [exception.Message] });
    }
})
.RequirePermission(Permission.ManageTenant)
.WithName("CreateGovernmentCloudEnvironment");

api.MapPut("/enterprise/government-cloud-environments/{environmentId:guid}", async (
    Guid environmentId,
    UpsertGovernmentCloudEnvironmentRequest request,
    GovernmentCloudEnvironmentService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var environment = await service.UpdateAsync(environmentId, request, tenantContext.UserId, cancellationToken);
        return environment is null
            ? ApiProblemDetails.Create(httpContext, "Resource not found", "Government cloud environment was not found in the current tenant scope.", StatusCodes.Status404NotFound, "resource_not_found")
            : Results.Ok(environment);
    }
    catch (GovernmentCloudEnvironmentValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["environment"] = [exception.Message] });
    }
})
.RequirePermission(Permission.ManageTenant)
.WithName("UpdateGovernmentCloudEnvironment");

api.MapPost("/enterprise/government-cloud-environments/{environmentId:guid}/submit-review", async (
    Guid environmentId,
    ReviewGovernmentCloudEnvironmentRequest request,
    GovernmentCloudEnvironmentService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var environment = await service.SubmitForReviewAsync(environmentId, request, tenantContext.UserId, cancellationToken);
        return environment is null
            ? ApiProblemDetails.Create(httpContext, "Resource not found", "Government cloud environment was not found in the current tenant scope.", StatusCodes.Status404NotFound, "resource_not_found")
            : Results.Ok(environment);
    }
    catch (GovernmentCloudEnvironmentValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["environmentReview"] = [exception.Message] });
    }
})
.RequirePermission(Permission.ManageTenant)
.WithName("SubmitGovernmentCloudEnvironmentReview");

api.MapPost("/enterprise/government-cloud-environments/{environmentId:guid}/approve", async (
    Guid environmentId,
    ReviewGovernmentCloudEnvironmentRequest request,
    GovernmentCloudEnvironmentService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var environment = await service.ApproveAsync(environmentId, request, tenantContext.UserId, cancellationToken);
        return environment is null
            ? ApiProblemDetails.Create(httpContext, "Resource not found", "Government cloud environment was not found in the current tenant scope.", StatusCodes.Status404NotFound, "resource_not_found")
            : Results.Ok(environment);
    }
    catch (GovernmentCloudEnvironmentValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["environmentApproval"] = [exception.Message] });
    }
})
.RequirePermission(Permission.ManageTenant)
.WithName("ApproveGovernmentCloudEnvironment");

api.MapPost("/enterprise/government-cloud-environments/{environmentId:guid}/block", async (
    Guid environmentId,
    ReviewGovernmentCloudEnvironmentRequest request,
    GovernmentCloudEnvironmentService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var environment = await service.BlockAsync(environmentId, request, tenantContext.UserId, cancellationToken);
    return environment is null
        ? ApiProblemDetails.Create(httpContext, "Resource not found", "Government cloud environment was not found in the current tenant scope.", StatusCodes.Status404NotFound, "resource_not_found")
        : Results.Ok(environment);
})
.RequirePermission(Permission.ManageTenant)
.WithName("BlockGovernmentCloudEnvironment");

api.MapPost("/enterprise/government-cloud-environments/{environmentId:guid}/deploy", async (
    Guid environmentId,
    ReviewGovernmentCloudEnvironmentRequest request,
    GovernmentCloudEnvironmentService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var environment = await service.MarkDeployedAsync(environmentId, request, tenantContext.UserId, cancellationToken);
    return environment is null
        ? ApiProblemDetails.Create(httpContext, "Resource not found", "Government cloud environment was not found in the current tenant scope.", StatusCodes.Status404NotFound, "resource_not_found")
        : Results.Ok(environment);
})
.RequirePermission(Permission.ManageTenant)
.WithName("DeployGovernmentCloudEnvironment");

api.MapPost("/enterprise/government-cloud-environments/{environmentId:guid}/retire", async (
    Guid environmentId,
    ReviewGovernmentCloudEnvironmentRequest request,
    GovernmentCloudEnvironmentService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var environment = await service.RetireAsync(environmentId, request, tenantContext.UserId, cancellationToken);
    return environment is null
        ? ApiProblemDetails.Create(httpContext, "Resource not found", "Government cloud environment was not found in the current tenant scope.", StatusCodes.Status404NotFound, "resource_not_found")
        : Results.Ok(environment);
})
.RequirePermission(Permission.ManageTenant)
.WithName("RetireGovernmentCloudEnvironment");

api.MapPost("/enterprise/government-cloud-environments/{environmentId:guid}/select-regulated-deployment", async (
    Guid environmentId,
    GovernmentCloudEnvironmentService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var result = await service.SelectForRegulatedTenantDeploymentAsync(environmentId, tenantContext.UserId, cancellationToken);
    return result is null
        ? ApiProblemDetails.Create(httpContext, "Resource not found", "Government cloud environment was not found in the current tenant scope.", StatusCodes.Status404NotFound, "resource_not_found")
        : Results.Ok(result);
})
.RequirePermission(Permission.ManageTenant)
.WithName("SelectGovernmentCloudEnvironmentForRegulatedDeployment");

api.MapGet("/enterprise/regulated-tenant-provisioning", async (
    RegulatedTenantProvisioningService service,
    CancellationToken cancellationToken) =>
    Results.Ok(await service.ListAsync(cancellationToken)))
.RequirePermission(Permission.ManageTenant)
.WithName("ListRegulatedTenantProvisioningRequests");

api.MapPost("/enterprise/regulated-tenant-provisioning", async (
    CreateRegulatedTenantProvisioningRequest request,
    RegulatedTenantProvisioningService service,
    ITenantContext tenantContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var created = await service.CreateAsync(request, tenantContext.UserId, cancellationToken);
        return Results.Created($"/api/enterprise/regulated-tenant-provisioning/{created.Id}", created);
    }
    catch (RegulatedTenantProvisioningValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["regulatedTenantProvisioning"] = [exception.Message] });
    }
})
.RequirePermission(Permission.ManageTenant)
.WithName("CreateRegulatedTenantProvisioningRequest");

api.MapPost("/enterprise/regulated-tenant-provisioning/{requestId:guid}/approvals", async (
    Guid requestId,
    RegulatedProvisioningApprovalRequest request,
    RegulatedTenantProvisioningService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var updated = await service.ApproveAsync(requestId, request, tenantContext.UserId, cancellationToken);
        return updated is null
            ? ApiProblemDetails.Create(httpContext, "Resource not found", "Regulated tenant provisioning request was not found in the current tenant scope.", StatusCodes.Status404NotFound, "resource_not_found")
            : Results.Ok(updated);
    }
    catch (RegulatedTenantProvisioningValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["regulatedTenantProvisioningApproval"] = [exception.Message] });
    }
})
.RequirePermission(Permission.ManageTenant)
.WithName("ApproveRegulatedTenantProvisioningRequest");

api.MapPost("/enterprise/regulated-tenant-provisioning/{requestId:guid}/checklist", async (
    Guid requestId,
    RegulatedProvisioningChecklistRequest request,
    RegulatedTenantProvisioningService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var updated = await service.CompleteChecklistItemAsync(requestId, request, tenantContext.UserId, cancellationToken);
    return updated is null
        ? ApiProblemDetails.Create(httpContext, "Resource not found", "Regulated tenant provisioning request was not found in the current tenant scope.", StatusCodes.Status404NotFound, "resource_not_found")
        : Results.Ok(updated);
})
.RequirePermission(Permission.ManageTenant)
.WithName("CompleteRegulatedTenantProvisioningChecklistItem");

api.MapPost("/enterprise/regulated-tenant-provisioning/{requestId:guid}/start", async (
    Guid requestId,
    RegulatedTenantProvisioningService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var updated = await service.StartProvisioningAsync(requestId, tenantContext.UserId, cancellationToken);
        return updated is null
            ? ApiProblemDetails.Create(httpContext, "Resource not found", "Regulated tenant provisioning request was not found in the current tenant scope.", StatusCodes.Status404NotFound, "resource_not_found")
            : Results.Ok(updated);
    }
    catch (RegulatedTenantProvisioningValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["regulatedTenantProvisioningStart"] = [exception.Message] });
    }
})
.RequirePermission(Permission.ManageTenant)
.WithName("StartRegulatedTenantProvisioning");

api.MapPost("/enterprise/regulated-tenant-provisioning/{requestId:guid}/validation", async (
    Guid requestId,
    RegulatedTenantProvisioningService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var updated = await service.MarkValidationAsync(requestId, tenantContext.UserId, cancellationToken);
    return updated is null
        ? ApiProblemDetails.Create(httpContext, "Resource not found", "Regulated tenant provisioning request was not found in the current tenant scope.", StatusCodes.Status404NotFound, "resource_not_found")
        : Results.Ok(updated);
})
.RequirePermission(Permission.ManageTenant)
.WithName("ValidateRegulatedTenantProvisioning");

api.MapPost("/enterprise/regulated-tenant-provisioning/{requestId:guid}/complete", async (
    Guid requestId,
    RegulatedTenantProvisioningService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var updated = await service.CompleteProvisioningAsync(requestId, tenantContext.UserId, cancellationToken);
        return updated is null
            ? ApiProblemDetails.Create(httpContext, "Resource not found", "Regulated tenant provisioning request was not found in the current tenant scope.", StatusCodes.Status404NotFound, "resource_not_found")
            : Results.Ok(updated);
    }
    catch (RegulatedTenantProvisioningValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["regulatedTenantProvisioningComplete"] = [exception.Message] });
    }
})
.RequirePermission(Permission.ManageTenant)
.WithName("CompleteRegulatedTenantProvisioning");

api.MapPost("/enterprise/regulated-tenant-provisioning/{requestId:guid}/fail", async (
    Guid requestId,
    RegulatedProvisioningFailureRequest request,
    RegulatedTenantProvisioningService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var updated = await service.RecordFailureAsync(requestId, request, tenantContext.UserId, cancellationToken);
    return updated is null
        ? ApiProblemDetails.Create(httpContext, "Resource not found", "Regulated tenant provisioning request was not found in the current tenant scope.", StatusCodes.Status404NotFound, "resource_not_found")
        : Results.Ok(updated);
})
.RequirePermission(Permission.ManageTenant)
.WithName("FailRegulatedTenantProvisioning");

api.MapPost("/enterprise/regulated-tenant-provisioning/{requestId:guid}/suspend", async (
    Guid requestId,
    RegulatedTenantProvisioningService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var updated = await service.SuspendAsync(requestId, tenantContext.UserId, cancellationToken);
    return updated is null
        ? ApiProblemDetails.Create(httpContext, "Resource not found", "Regulated tenant provisioning request was not found in the current tenant scope.", StatusCodes.Status404NotFound, "resource_not_found")
        : Results.Ok(updated);
})
.RequirePermission(Permission.ManageTenant)
.WithName("SuspendRegulatedTenantProvisioning");

api.MapPost("/enterprise/regulated-tenant-provisioning/{requestId:guid}/retire", async (
    Guid requestId,
    RegulatedTenantProvisioningService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var updated = await service.RetireAsync(requestId, tenantContext.UserId, cancellationToken);
    return updated is null
        ? ApiProblemDetails.Create(httpContext, "Resource not found", "Regulated tenant provisioning request was not found in the current tenant scope.", StatusCodes.Status404NotFound, "resource_not_found")
        : Results.Ok(updated);
})
.RequirePermission(Permission.ManageTenant)
.WithName("RetireRegulatedTenantProvisioning");

api.MapPost("/enterprise/government-cloud-release-readiness", async (
    CreateGovernmentCloudReleaseReadinessRequest request,
    GovernmentCloudReleaseReadinessService service,
    ITenantContext tenantContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var created = await service.CreateAsync(request, tenantContext.UserId, cancellationToken);
        return Results.Created($"/api/enterprise/government-cloud-release-readiness/{created.Id}", created);
    }
    catch (GovernmentCloudReleaseReadinessValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["releaseReadiness"] = [exception.Message] });
    }
})
.RequirePermission(Permission.ManageTenant)
.WithName("CreateGovernmentCloudReleaseReadiness");

api.MapPost("/enterprise/government-cloud-release-readiness/{readinessId:guid}/checklist", async (
    Guid readinessId,
    CompleteGovernmentCloudReleaseChecklistRequest request,
    GovernmentCloudReleaseReadinessService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var updated = await service.CompleteChecklistAsync(readinessId, request, tenantContext.UserId, cancellationToken);
    return updated is null
        ? ApiProblemDetails.Create(httpContext, "Resource not found", "Government cloud release readiness record was not found in the current tenant scope.", StatusCodes.Status404NotFound, "resource_not_found")
        : Results.Ok(updated);
})
.RequirePermission(Permission.ManageTenant)
.WithName("CompleteGovernmentCloudReleaseChecklist");

api.MapPost("/enterprise/government-cloud-release-readiness/{readinessId:guid}/evidence", async (
    Guid readinessId,
    GovernmentCloudReleaseEvidenceRequest request,
    GovernmentCloudReleaseReadinessService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var updated = await service.LinkEvidenceAsync(readinessId, request, tenantContext.UserId, cancellationToken);
    return updated is null
        ? ApiProblemDetails.Create(httpContext, "Resource not found", "Government cloud release readiness record was not found in the current tenant scope.", StatusCodes.Status404NotFound, "resource_not_found")
        : Results.Ok(updated);
})
.RequirePermission(Permission.ManageTenant)
.WithName("LinkGovernmentCloudReleaseEvidence");

api.MapPost("/enterprise/government-cloud-release-readiness/{readinessId:guid}/gaps", async (
    Guid readinessId,
    GovernmentCloudReleaseGapRequest request,
    GovernmentCloudReleaseReadinessService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var updated = await service.AddGapAsync(readinessId, request, tenantContext.UserId, cancellationToken);
    return updated is null
        ? ApiProblemDetails.Create(httpContext, "Resource not found", "Government cloud release readiness record was not found in the current tenant scope.", StatusCodes.Status404NotFound, "resource_not_found")
        : Results.Ok(updated);
})
.RequirePermission(Permission.ManageTenant)
.WithName("AddGovernmentCloudReleaseGap");

api.MapPost("/enterprise/government-cloud-release-readiness/{readinessId:guid}/approve", async (
    Guid readinessId,
    GovernmentCloudReleaseApprovalRequest request,
    GovernmentCloudReleaseReadinessService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var updated = await service.ApproveAsync(readinessId, request, tenantContext.UserId, cancellationToken);
        return updated is null
            ? ApiProblemDetails.Create(httpContext, "Resource not found", "Government cloud release readiness record was not found in the current tenant scope.", StatusCodes.Status404NotFound, "resource_not_found")
            : Results.Ok(updated);
    }
    catch (GovernmentCloudReleaseReadinessValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["releaseApproval"] = [exception.Message] });
    }
})
.RequirePermission(Permission.ManageTenant)
.WithName("ApproveGovernmentCloudRelease");

api.MapPost("/enterprise/government-cloud-release-readiness/{readinessId:guid}/deploy", async (
    Guid readinessId,
    GovernmentCloudReleaseDeploymentRequest request,
    GovernmentCloudReleaseReadinessService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var updated = await service.DeployAsync(readinessId, request, tenantContext.UserId, cancellationToken);
        return updated is null
            ? ApiProblemDetails.Create(httpContext, "Resource not found", "Government cloud release readiness record was not found in the current tenant scope.", StatusCodes.Status404NotFound, "resource_not_found")
            : Results.Ok(updated);
    }
    catch (GovernmentCloudReleaseReadinessValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["releaseDeployment"] = [exception.Message] });
    }
})
.RequirePermission(Permission.ManageTenant)
.WithName("DeployGovernmentCloudRelease");

api.MapGet("/enterprise/fedramp/control-mappings", async (
    string? family,
    FedRampGapSeverity? severity,
    string? owner,
    DateOnly? targetDate,
    FedRampControlMappingService service,
    CancellationToken cancellationToken) =>
    Results.Ok(await service.ListAsync(new FedRampGapReportFilter(family, severity, owner, targetDate), cancellationToken)))
.RequirePermission(Permission.ManageTenant)
.WithName("ListFedRampControlMappings");

api.MapPost("/enterprise/fedramp/control-mappings", async (
    CreateFedRampControlMappingRequest request,
    FedRampControlMappingService service,
    ITenantContext tenantContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var created = await service.CreateAsync(request, tenantContext.UserId, cancellationToken);
        return Results.Created($"/api/enterprise/fedramp/control-mappings/{created.Id}", created);
    }
    catch (FedRampControlMappingValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["fedRampControlMapping"] = [exception.Message] });
    }
})
.RequirePermission(Permission.ManageTenant)
.WithName("CreateFedRampControlMapping");

api.MapPost("/enterprise/fedramp/control-mappings/{mappingId:guid}/evidence", async (
    Guid mappingId,
    FedRampEvidenceLinkRequest request,
    FedRampControlMappingService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var updated = await service.LinkEvidenceAsync(mappingId, request, tenantContext.UserId, cancellationToken);
    return updated is null
        ? ApiProblemDetails.Create(httpContext, "Resource not found", "FedRAMP control mapping was not found in the current tenant scope.", StatusCodes.Status404NotFound, "resource_not_found")
        : Results.Ok(updated);
})
.RequirePermission(Permission.ManageTenant)
.WithName("LinkFedRampControlEvidence");

api.MapPost("/enterprise/fedramp/control-mappings/{mappingId:guid}/gaps", async (
    Guid mappingId,
    FedRampGapRequest request,
    FedRampControlMappingService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var updated = await service.AddGapAsync(mappingId, request, tenantContext.UserId, cancellationToken);
    return updated is null
        ? ApiProblemDetails.Create(httpContext, "Resource not found", "FedRAMP control mapping was not found in the current tenant scope.", StatusCodes.Status404NotFound, "resource_not_found")
        : Results.Ok(updated);
})
.RequirePermission(Permission.ManageTenant)
.WithName("AddFedRampControlGap");

api.MapPost("/enterprise/fedramp/control-mappings/{mappingId:guid}/state", async (
    Guid mappingId,
    FedRampControlReviewRequest request,
    FedRampControlMappingService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var updated = await service.ChangeStateAsync(mappingId, request, tenantContext.UserId, cancellationToken);
        return updated is null
            ? ApiProblemDetails.Create(httpContext, "Resource not found", "FedRAMP control mapping was not found in the current tenant scope.", StatusCodes.Status404NotFound, "resource_not_found")
            : Results.Ok(updated);
    }
    catch (FedRampControlMappingValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["fedRampControlReview"] = [exception.Message] });
    }
})
.RequirePermission(Permission.ManageTenant)
.WithName("ChangeFedRampControlMappingState");

api.MapPost("/enterprise/trust-artifacts", async (
    CreateTrustArtifactRequest request,
    TrustArtifactLibraryService service,
    ITenantContext tenantContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var created = await service.CreateAsync(request, tenantContext.UserId, cancellationToken);
        return Results.Created($"/api/enterprise/trust-artifacts/{created.Id}", created);
    }
    catch (TrustArtifactValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["trustArtifact"] = [exception.Message] });
    }
})
.RequirePermission(Permission.ManageTenant)
.WithName("CreateTrustArtifact");

api.MapPost("/enterprise/trust-artifacts/{artifactId:guid}/status", async (
    Guid artifactId,
    TrustArtifactStatusRequest request,
    TrustArtifactLibraryService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var updated = await service.ChangeStatusAsync(artifactId, request, tenantContext.UserId, cancellationToken);
        return updated is null
            ? ApiProblemDetails.Create(httpContext, "Resource not found", "Trust artifact was not found in the current tenant scope.", StatusCodes.Status404NotFound, "resource_not_found")
            : Results.Ok(updated);
    }
    catch (TrustArtifactValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["trustArtifactStatus"] = [exception.Message] });
    }
})
.RequirePermission(Permission.ManageTenant)
.WithName("ChangeTrustArtifactStatus");

api.MapPost("/enterprise/trust-artifacts/{artifactId:guid}/share", async (
    Guid artifactId,
    TrustArtifactShareRequest request,
    TrustArtifactLibraryService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var result = await service.ShareAsync(artifactId, request, tenantContext.UserId, cancellationToken);
    return result is null
        ? ApiProblemDetails.Create(httpContext, "Resource not found", "Trust artifact was not found in the current tenant scope.", StatusCodes.Status404NotFound, "resource_not_found")
        : Results.Ok(result);
})
.RequirePermission(Permission.ManageTenant)
.WithName("ShareTrustArtifact");

api.MapPost("/enterprise/fedramp/readiness-packages", async (
    CreateFedRampReadinessPackageRequest request,
    FedRampReadinessExportPackageService service,
    ITenantContext tenantContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var package = await service.GenerateAsync(request, tenantContext.UserId, cancellationToken);
        return Results.Created($"/api/enterprise/fedramp/readiness-packages/{package.Id}", package);
    }
    catch (FedRampReadinessPackageValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["fedRampReadinessPackage"] = [exception.Message] });
    }
})
.RequirePermission(Permission.ManageTenant)
.WithName("GenerateFedRampReadinessPackage");

api.MapPost("/enterprise/fedramp/readiness-packages/{packageId:guid}/status", async (
    Guid packageId,
    FedRampReadinessPackageStatusRequest request,
    FedRampReadinessExportPackageService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var package = await service.ChangeStatusAsync(packageId, request, tenantContext.UserId, cancellationToken);
    return package is null
        ? ApiProblemDetails.Create(httpContext, "Resource not found", "FedRAMP readiness package was not found in the current tenant scope.", StatusCodes.Status404NotFound, "resource_not_found")
        : Results.Ok(package);
})
.RequirePermission(Permission.ManageTenant)
.WithName("ChangeFedRampReadinessPackageStatus");

api.MapPost("/enterprise/fedramp/readiness-packages/{packageId:guid}/share", async (
    Guid packageId,
    FedRampReadinessPackageShareRequest request,
    FedRampReadinessExportPackageService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var package = await service.ShareAsync(packageId, request, tenantContext.UserId, cancellationToken);
        return package is null
            ? ApiProblemDetails.Create(httpContext, "Resource not found", "FedRAMP readiness package was not found in the current tenant scope.", StatusCodes.Status404NotFound, "resource_not_found")
            : Results.Ok(package);
    }
    catch (FedRampReadinessPackageValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["fedRampReadinessPackageShare"] = [exception.Message] });
    }
})
.RequirePermission(Permission.ManageTenant)
.WithName("ShareFedRampReadinessPackage");

api.MapGet("/compliance/ssp/sections", async (
    SspSectionService service,
    CancellationToken cancellationToken) =>
{
    var sections = await service.ListAsync(cancellationToken);
    return Results.Ok(sections);
})
.RequirePermission(Permission.ManageTenant)
.WithName("ListSspSections");

api.MapGet("/compliance/ssp/sections/{sectionId:guid}", async (
    Guid sectionId,
    SspSectionService service,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var section = await service.GetAsync(sectionId, cancellationToken);
    return section is null
        ? ApiProblemDetails.Create(httpContext, "Resource not found", "SSP section was not found in the current tenant scope.", StatusCodes.Status404NotFound, "resource_not_found")
        : Results.Ok(section);
})
.RequirePermission(Permission.ManageTenant)
.WithName("GetSspSection");

api.MapPost("/compliance/ssp/sections", async (
    CreateSspSectionRequest request,
    SspSectionService service,
    ITenantContext tenantContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var section = await service.CreateAsync(request, tenantContext.UserId, cancellationToken);
        return Results.Created($"/api/compliance/ssp/sections/{section.Id}", section);
    }
    catch (SspSectionValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["sspSection"] = [exception.Message] });
    }
})
.RequirePermission(Permission.ManageTenant)
.WithName("CreateSspSection");

api.MapPut("/compliance/ssp/sections/{sectionId:guid}", async (
    Guid sectionId,
    UpdateSspSectionRequest request,
    SspSectionService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var section = await service.UpdateAsync(sectionId, request, tenantContext.UserId, cancellationToken);
        return section is null
            ? ApiProblemDetails.Create(httpContext, "Resource not found", "SSP section was not found in the current tenant scope.", StatusCodes.Status404NotFound, "resource_not_found")
            : Results.Ok(section);
    }
    catch (SspSectionValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["sspSection"] = [exception.Message] });
    }
})
.RequirePermission(Permission.ManageTenant)
.WithName("UpdateSspSection");

api.MapPost("/compliance/ssp/sections/{sectionId:guid}/status", async (
    Guid sectionId,
    SspSectionStatusRequest request,
    SspSectionService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var section = await service.ChangeStatusAsync(sectionId, request, tenantContext.UserId, cancellationToken);
        return section is null
            ? ApiProblemDetails.Create(httpContext, "Resource not found", "SSP section was not found in the current tenant scope.", StatusCodes.Status404NotFound, "resource_not_found")
            : Results.Ok(section);
    }
    catch (SspSectionValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["sspSectionStatus"] = [exception.Message] });
    }
})
.RequirePermission(Permission.ManageTenant)
.WithName("ChangeSspSectionStatus");

api.MapPost("/compliance/ssp/sections/{sectionId:guid}/narratives", async (
    Guid sectionId,
    GenerateSspNarrativeDraftRequest request,
    SspSectionService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var narrative = await service.GenerateNarrativeDraftAsync(sectionId, request, tenantContext.UserId, cancellationToken);
        return narrative is null
            ? ApiProblemDetails.Create(httpContext, "Resource not found", "SSP section was not found in the current tenant scope.", StatusCodes.Status404NotFound, "resource_not_found")
            : Results.Created($"/api/compliance/ssp/sections/{sectionId}/narratives/{narrative.Id}", narrative);
    }
    catch (SspNarrativeValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["sspNarrative"] = [exception.Message] });
    }
})
.RequirePermission(Permission.ManageTenant)
.WithName("GenerateSspNarrativeDraft");

api.MapPut("/compliance/ssp/sections/{sectionId:guid}/narratives/{narrativeId:guid}", async (
    Guid sectionId,
    Guid narrativeId,
    EditSspNarrativeDraftRequest request,
    SspSectionService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var narrative = await service.EditNarrativeDraftAsync(sectionId, narrativeId, request, tenantContext.UserId, cancellationToken);
        return narrative is null
            ? ApiProblemDetails.Create(httpContext, "Resource not found", "SSP narrative was not found in the current tenant scope.", StatusCodes.Status404NotFound, "resource_not_found")
            : Results.Ok(narrative);
    }
    catch (SspNarrativeValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["sspNarrative"] = [exception.Message] });
    }
})
.RequirePermission(Permission.ManageTenant)
.WithName("EditSspNarrativeDraft");

api.MapPost("/compliance/ssp/sections/{sectionId:guid}/narratives/{narrativeId:guid}/approve", async (
    Guid sectionId,
    Guid narrativeId,
    ApproveSspNarrativeRequest request,
    SspSectionService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var narrative = await service.ApproveNarrativeAsync(sectionId, narrativeId, request, tenantContext.UserId, cancellationToken);
        return narrative is null
            ? ApiProblemDetails.Create(httpContext, "Resource not found", "SSP narrative was not found in the current tenant scope.", StatusCodes.Status404NotFound, "resource_not_found")
            : Results.Ok(narrative);
    }
    catch (SspNarrativeValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["sspNarrativeApproval"] = [exception.Message] });
    }
})
.RequirePermission(Permission.ManageTenant)
.WithName("ApproveSspNarrative");

api.MapGet("/compliance/ssp/sections/{sectionId:guid}/narratives/{narrativeId:guid}/comparison", async (
    Guid sectionId,
    Guid narrativeId,
    SspSectionService service,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var comparison = await service.CompareNarrativeAsync(sectionId, narrativeId, cancellationToken);
    return comparison is null
        ? ApiProblemDetails.Create(httpContext, "Resource not found", "SSP narrative was not found in the current tenant scope.", StatusCodes.Status404NotFound, "resource_not_found")
        : Results.Ok(comparison);
})
.RequirePermission(Permission.ManageTenant)
.WithName("CompareSspNarrative");

api.MapGet("/compliance/ssp/export-packages", async (
    SspSectionService service,
    CancellationToken cancellationToken) =>
{
    var packages = await service.ListExportPackagesAsync(cancellationToken);
    return Results.Ok(packages);
})
.RequirePermission(Permission.ManageTenant)
.WithName("ListSspExportPackages");

api.MapPost("/compliance/ssp/export-packages", async (
    CreateSspExportPackageRequest request,
    SspSectionService service,
    ITenantContext tenantContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var package = await service.GenerateExportPackageAsync(request, tenantContext.UserId, cancellationToken);
        return Results.Created($"/api/compliance/ssp/export-packages/{package.Id}", package);
    }
    catch (SspExportPackageValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["sspExportPackage"] = [exception.Message] });
    }
})
.RequirePermission(Permission.ManageTenant)
.WithName("GenerateSspExportPackage");

api.MapPost("/enterprise/cui/enclaves", async (
    CreateCuiEnclaveBoundaryRequest request,
    CuiEnclaveBoundaryService service,
    ITenantContext tenantContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var enclave = await service.CreateAsync(request, tenantContext.UserId, cancellationToken);
        return Results.Created($"/api/enterprise/cui/enclaves/{enclave.Id}", enclave);
    }
    catch (CuiEnclaveBoundaryValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["cuiEnclave"] = [exception.Message] });
    }
})
.RequirePermission(Permission.ManageTenant)
.WithName("CreateCuiEnclaveBoundary");

api.MapPost("/enterprise/cui/enclaves/{enclaveId:guid}/status", async (
    Guid enclaveId,
    CuiEnclaveStatusRequest request,
    CuiEnclaveBoundaryService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var enclave = await service.ChangeStatusAsync(enclaveId, request, tenantContext.UserId, cancellationToken);
        return enclave is null
            ? ApiProblemDetails.Create(httpContext, "Resource not found", "CUI enclave boundary was not found in the current tenant scope.", StatusCodes.Status404NotFound, "resource_not_found")
            : Results.Ok(enclave);
    }
    catch (CuiEnclaveBoundaryValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["cuiEnclaveStatus"] = [exception.Message] });
    }
})
.RequirePermission(Permission.ManageTenant)
.WithName("ChangeCuiEnclaveBoundaryStatus");

api.MapPost("/enterprise/cui/enclaves/{enclaveId:guid}/processing-check", async (
    Guid enclaveId,
    CuiProcessingRequest request,
    CuiEnclaveBoundaryService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var decision = await service.EvaluateProcessingAsync(enclaveId, request, tenantContext.UserId, cancellationToken);
    return decision is null
        ? ApiProblemDetails.Create(httpContext, "Resource not found", "CUI enclave boundary was not found in the current tenant scope.", StatusCodes.Status404NotFound, "resource_not_found")
        : Results.Ok(decision);
})
.RequirePermission(Permission.ManageTenant)
.WithName("EvaluateCuiEnclaveProcessing");

api.MapPost("/enterprise/cui/customer-managed-key-policies", async (
    RegisterCustomerManagedKeyPolicyRequest request,
    CustomerManagedKeyPolicyService service,
    ITenantContext tenantContext,
    CancellationToken cancellationToken) =>
{
    var policy = await service.RegisterAsync(request, tenantContext.UserId, cancellationToken);
    return Results.Created($"/api/enterprise/cui/customer-managed-key-policies/{policy.Id}", policy);
})
.RequirePermission(Permission.ManageTenant)
.WithName("RegisterCustomerManagedKeyPolicy");

api.MapPost("/enterprise/cui/customer-managed-key-policies/{policyId:guid}/validate", async (
    Guid policyId,
    CustomerManagedKeyValidationRequest request,
    CustomerManagedKeyPolicyService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var policy = await service.ValidateAsync(policyId, request, tenantContext.UserId, cancellationToken);
    return policy is null
        ? ApiProblemDetails.Create(httpContext, "Resource not found", "Customer-managed key policy was not found in the current tenant scope.", StatusCodes.Status404NotFound, "resource_not_found")
        : Results.Ok(policy);
})
.RequirePermission(Permission.ManageTenant)
.WithName("ValidateCustomerManagedKeyPolicy");

api.MapPost("/enterprise/cui/customer-managed-key-policies/{policyId:guid}/activate", async (
    Guid policyId,
    CustomerManagedKeyValidationRequest request,
    CustomerManagedKeyPolicyService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var policy = await service.ActivateAsync(policyId, request, tenantContext.UserId, cancellationToken);
        return policy is null
            ? ApiProblemDetails.Create(httpContext, "Resource not found", "Customer-managed key policy was not found in the current tenant scope.", StatusCodes.Status404NotFound, "resource_not_found")
            : Results.Ok(policy);
    }
    catch (CustomerManagedKeyPolicyValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["customerManagedKeyPolicy"] = [exception.Message] });
    }
})
.RequirePermission(Permission.ManageTenant)
.WithName("ActivateCustomerManagedKeyPolicy");

api.MapPost("/enterprise/cui/customer-managed-key-policies/{policyId:guid}/status", async (
    Guid policyId,
    CustomerManagedKeyStatusRequest request,
    CustomerManagedKeyPolicyService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var policy = await service.ChangeStatusAsync(policyId, request, tenantContext.UserId, cancellationToken);
    return policy is null
        ? ApiProblemDetails.Create(httpContext, "Resource not found", "Customer-managed key policy was not found in the current tenant scope.", StatusCodes.Status404NotFound, "resource_not_found")
        : Results.Ok(policy);
})
.RequirePermission(Permission.ManageTenant)
.WithName("ChangeCustomerManagedKeyPolicyStatus");

api.MapPost("/enterprise/cui/customer-managed-key-policies/{policyId:guid}/workflow-check", async (
    Guid policyId,
    CustomerManagedKeyWorkflowRequest request,
    CustomerManagedKeyPolicyService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var decision = await service.EvaluateWorkflowAsync(policyId, request, tenantContext.UserId, cancellationToken);
    return decision is null
        ? ApiProblemDetails.Create(httpContext, "Resource not found", "Customer-managed key policy was not found in the current tenant scope.", StatusCodes.Status404NotFound, "resource_not_found")
        : Results.Ok(decision);
})
.RequirePermission(Permission.ManageTenant)
.WithName("EvaluateCustomerManagedKeyWorkflow");

api.MapPost("/enterprise/cui/access/view", async (
    CuiEnclaveOperationRequest request,
    CuiEnclaveAccessControlService service,
    ITenantContext tenantContext,
    CancellationToken cancellationToken) =>
    Results.Ok(await service.RecordOperationAsync(request with { Operation = CuiEnclaveOperation.View }, tenantContext.UserId, cancellationToken)))
.RequirePermission(Permission.ViewEnclave)
.WithName("ViewCuiEnclaveAccess");

api.MapPost("/enterprise/cui/access/upload", async (
    CuiEnclaveOperationRequest request,
    CuiEnclaveAccessControlService service,
    ITenantContext tenantContext,
    CancellationToken cancellationToken) =>
    Results.Ok(await service.RecordOperationAsync(request with { Operation = CuiEnclaveOperation.Upload }, tenantContext.UserId, cancellationToken)))
.RequirePermission(Permission.UploadEnclave)
.WithName("UploadCuiEnclaveAccess");

api.MapPost("/enterprise/cui/access/download", async (
    CuiEnclaveOperationRequest request,
    CuiEnclaveAccessControlService service,
    ITenantContext tenantContext,
    CancellationToken cancellationToken) =>
    Results.Ok(await service.RecordOperationAsync(request with { Operation = CuiEnclaveOperation.Download }, tenantContext.UserId, cancellationToken)))
.RequirePermission(Permission.DownloadEnclave)
.WithName("DownloadCuiEnclaveAccess");

api.MapPost("/enterprise/cui/access/approve", async (
    CuiEnclaveOperationRequest request,
    CuiEnclaveAccessControlService service,
    ITenantContext tenantContext,
    CancellationToken cancellationToken) =>
    Results.Ok(await service.RecordOperationAsync(request with { Operation = CuiEnclaveOperation.Approve }, tenantContext.UserId, cancellationToken)))
.RequirePermission(Permission.ApproveEnclave)
.WithName("ApproveCuiEnclaveAccess");

api.MapPost("/enterprise/cui/support-access", async (
    CuiEnclaveSupportAccessRequest request,
    CuiEnclaveAccessControlService service,
    ITenantContext tenantContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        return Results.Ok(await service.RequestSupportAccessAsync(request, tenantContext.UserId, cancellationToken));
    }
    catch (CuiEnclaveAccessValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["cuiEnclaveSupportAccess"] = [exception.Message] });
    }
})
.RequirePermission(Permission.SupportEnclave)
.WithName("RequestCuiEnclaveSupportAccess");

api.MapPost("/enterprise/cui/support-access/{accessId:guid}/expire", async (
    Guid accessId,
    CuiEnclaveAccessControlService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var expired = await service.ExpireSupportAccessAsync(accessId, tenantContext.UserId, cancellationToken);
    return expired is null
        ? ApiProblemDetails.Create(httpContext, "Resource not found", "CUI enclave support access was not found in the current tenant scope.", StatusCodes.Status404NotFound, "resource_not_found")
        : Results.Ok(expired);
})
.RequirePermission(Permission.SupportEnclave)
.WithName("ExpireCuiEnclaveSupportAccess");

api.MapPost("/enterprise/cui/exports", async (
    CuiEnclaveExportRequest request,
    CuiEnclaveAccessControlService service,
    ITenantContext tenantContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        return Results.Ok(await service.CreateExportAsync(request, tenantContext.UserId, cancellationToken));
    }
    catch (CuiEnclaveAccessValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["cuiEnclaveExport"] = [exception.Message] });
    }
})
.RequirePermission(Permission.ExportEnclave)
.WithName("CreateCuiEnclaveExport");

api.MapPost("/enterprise/cui/emergency-access", async (
    CuiEnclaveEmergencyAccessRequest request,
    CuiEnclaveAccessControlService service,
    ITenantContext tenantContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        return Results.Ok(await service.RequestEmergencyAccessAsync(request, tenantContext.UserId, cancellationToken));
    }
    catch (CuiEnclaveAccessValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["cuiEnclaveEmergencyAccess"] = [exception.Message] });
    }
})
.RequirePermission(Permission.EmergencyEnclave)
.WithName("RequestCuiEnclaveEmergencyAccess");

api.MapPost("/enterprise/cui/emergency-access/{accessId:guid}/post-access-review", async (
    Guid accessId,
    CuiEnclavePostAccessReviewRequest request,
    CuiEnclaveAccessControlService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var reviewed = await service.CompletePostAccessReviewAsync(accessId, request.Reviewer, tenantContext.UserId, cancellationToken);
    return reviewed is null
        ? ApiProblemDetails.Create(httpContext, "Resource not found", "CUI enclave emergency access was not found in the current tenant scope.", StatusCodes.Status404NotFound, "resource_not_found")
        : Results.Ok(reviewed);
})
.RequirePermission(Permission.EmergencyEnclave)
.WithName("CompleteCuiEnclaveEmergencyPostAccessReview");

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

api.MapPatch("/tenants/{tenantId:guid}/data-handling-mode", async (
    Guid tenantId,
    UpdateTenantDataHandlingModeRequest request,
    TenantService service,
    IServiceProvider serviceProvider,
    IWebHostEnvironment environment,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        if (request.DataHandlingMode is TenantDataPosture.CuiReady && !string.IsNullOrWhiteSpace(request.ApprovalRecordReference))
        {
            var matrixService = serviceProvider.GetRequiredService<SharedResponsibilityMatrixService>();
            var matrixAcknowledgementService = serviceProvider.GetRequiredService<SharedResponsibilityMatrixAcknowledgementService>();
            var packageRoot = ComplianceContentPackageLocator.FindPackageRoot(environment.ContentRootPath);
            var matrix = await matrixService.GetPublishedAsync(packageRoot, cancellationToken);
            await matrixAcknowledgementService.EnsureCurrentAcknowledgedAsync(tenantId, matrix, tenantContext.UserId, cancellationToken);
        }

        var tenant = await service.UpdateDataHandlingModeAsync(tenantId, request, tenantContext.UserId, cancellationToken);
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
            ["tenantDataHandlingMode"] = [exception.Message]
        });
    }
    catch (CuiReadyApprovalChecklistValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["approvalRecordReference"] = [exception.Message]
        });
    }
    catch (SharedResponsibilityMatrixAcknowledgementException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["sharedResponsibilityMatrix"] = [exception.Message]
        });
    }
})
.RequirePermission(Permission.ManageTenant)
.WithName("UpdateTenantDataHandlingMode");

api.MapGet("/tenants/{tenantId:guid}/cui-ready-checklists", async (
    Guid tenantId,
    CuiReadyApprovalChecklistService service,
    CancellationToken cancellationToken) =>
    Results.Ok(await service.ListAsync(tenantId, cancellationToken)))
.RequirePermission(Permission.ManageTenant)
.WithName("ListCuiReadyApprovalChecklists");

api.MapPost("/tenants/{tenantId:guid}/cui-ready-checklists", async (
    Guid tenantId,
    CuiReadyApprovalChecklistService service,
    ITenantContext tenantContext,
    CancellationToken cancellationToken) =>
    Results.Created(
        $"/api/tenants/{tenantId}/cui-ready-checklists",
        await service.CreateAsync(tenantId, tenantContext.UserId, cancellationToken)))
.RequirePermission(Permission.ManageTenant)
.WithName("CreateCuiReadyApprovalChecklist");

api.MapPut("/tenants/{tenantId:guid}/cui-ready-checklists/{checklistId:guid}/items/{itemKey}", async (
    Guid tenantId,
    Guid checklistId,
    string itemKey,
    UpdateCuiReadyChecklistItemRequest request,
    CuiReadyApprovalChecklistService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var checklist = await service.UpdateItemAsync(tenantId, checklistId, itemKey, request, tenantContext.UserId, cancellationToken);
        return checklist is null
            ? ApiProblemDetails.Create(httpContext, "Resource not found", "Checklist item was not found.", StatusCodes.Status404NotFound, "resource_not_found")
            : Results.Ok(checklist);
    }
    catch (CuiReadyApprovalChecklistValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["checklistItem"] = [exception.Message]
        });
    }
})
.RequirePermission(Permission.ManageTenant)
.WithName("UpdateCuiReadyApprovalChecklistItem");

api.MapPost("/tenants/{tenantId:guid}/cui-ready-checklists/{checklistId:guid}/submit", async (
    Guid tenantId,
    Guid checklistId,
    CuiReadyApprovalChecklistService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var checklist = await service.SubmitForReviewAsync(tenantId, checklistId, tenantContext.UserId, cancellationToken);
    return checklist is null
        ? ApiProblemDetails.Create(httpContext, "Resource not found", "Checklist was not found.", StatusCodes.Status404NotFound, "resource_not_found")
        : Results.Ok(checklist);
})
.RequirePermission(Permission.ManageTenant)
.WithName("SubmitCuiReadyApprovalChecklist");

api.MapPost("/tenants/{tenantId:guid}/cui-ready-checklists/{checklistId:guid}/approve", async (
    Guid tenantId,
    Guid checklistId,
    ReviewCuiReadyChecklistRequest request,
    CuiReadyApprovalChecklistService service,
    SharedResponsibilityMatrixService matrixService,
    SharedResponsibilityMatrixAcknowledgementService matrixAcknowledgementService,
    IWebHostEnvironment environment,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var packageRoot = ComplianceContentPackageLocator.FindPackageRoot(environment.ContentRootPath);
        var matrix = await matrixService.GetPublishedAsync(packageRoot, cancellationToken);
        await matrixAcknowledgementService.EnsureCurrentAcknowledgedAsync(tenantId, matrix, tenantContext.UserId, cancellationToken);
    }
    catch (SharedResponsibilityMatrixAcknowledgementException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["sharedResponsibilityMatrix"] = [exception.Message]
        });
    }

    return await ReviewCuiReadyChecklistAsync(tenantId, checklistId, request, service.ApproveAsync, tenantContext, httpContext, cancellationToken);
})
.RequirePermission(Permission.ManageTenant)
.WithName("ApproveCuiReadyApprovalChecklist");

api.MapPost("/tenants/{tenantId:guid}/cui-ready-checklists/{checklistId:guid}/reject", async (
    Guid tenantId,
    Guid checklistId,
    ReviewCuiReadyChecklistRequest request,
    CuiReadyApprovalChecklistService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
    await ReviewCuiReadyChecklistAsync(tenantId, checklistId, request, service.RejectAsync, tenantContext, httpContext, cancellationToken))
.RequirePermission(Permission.ManageTenant)
.WithName("RejectCuiReadyApprovalChecklist");

api.MapPost("/tenants/{tenantId:guid}/cui-ready-checklists/{checklistId:guid}/supersede", async (
    Guid tenantId,
    Guid checklistId,
    ReviewCuiReadyChecklistRequest request,
    CuiReadyApprovalChecklistService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
    await ReviewCuiReadyChecklistAsync(tenantId, checklistId, request, service.SupersedeAsync, tenantContext, httpContext, cancellationToken))
.RequirePermission(Permission.ManageTenant)
.WithName("SupersedeCuiReadyApprovalChecklist");

api.MapGet("/shared-responsibility-matrix/published", async (
    SharedResponsibilityMatrixService service,
    IWebHostEnvironment environment,
    CancellationToken cancellationToken) =>
{
    try
    {
        var packageRoot = ComplianceContentPackageLocator.FindPackageRoot(environment.ContentRootPath);
        return Results.Ok(await service.GetPublishedAsync(packageRoot, cancellationToken));
    }
    catch (SharedResponsibilityMatrixValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["sharedResponsibilityMatrix"] = exception.Errors.Count > 0 ? exception.Errors.ToArray() : [exception.Message]
        });
    }
})
.RequirePermission(Permission.ManageTenant)
.WithName("GetPublishedSharedResponsibilityMatrix");

api.MapGet("/data-handling-notices/published", async (
    [FromQuery] TenantDataPosture mode,
    [FromQuery] string workflowContext,
    DataHandlingNoticeService service,
    IWebHostEnvironment environment,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var packageRoot = ComplianceContentPackageLocator.FindPackageRoot(environment.ContentRootPath);
        var notice = await service.GetPublishedAsync(packageRoot, mode, workflowContext, cancellationToken);
        return notice is null
            ? ApiProblemDetails.Create(httpContext, "Resource not found", "No published data handling notice matched the mode and workflow context.", StatusCodes.Status404NotFound, "resource_not_found")
            : Results.Ok(notice);
    }
    catch (DataHandlingNoticeValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["dataHandlingNotice"] = exception.Errors.Count > 0 ? exception.Errors.ToArray() : [exception.Message]
        });
    }
})
.RequirePermission(Permission.ManageTenant)
.WithName("GetPublishedDataHandlingNotice");

api.MapGet("/tenants/{tenantId:guid}/data-handling-notice-acknowledgements", async (
    Guid tenantId,
    [FromQuery] TenantDataPosture mode,
    [FromQuery] string workflowContext,
    DataHandlingNoticeService noticeService,
    DataHandlingNoticeAcknowledgementService acknowledgementService,
    IWebHostEnvironment environment,
    ITenantContext tenantContext,
    CancellationToken cancellationToken) =>
{
    var packageRoot = ComplianceContentPackageLocator.FindPackageRoot(environment.ContentRootPath);
    var notice = await noticeService.GetPublishedAsync(packageRoot, mode, workflowContext, cancellationToken);
    if (notice is null)
    {
        return Results.NotFound();
    }

    return Results.Ok(await acknowledgementService.ListAsync(tenantId, tenantContext.UserId, notice, cancellationToken));
})
.RequirePermission(Permission.ManageTenant)
.WithName("ListDataHandlingNoticeAcknowledgements");

api.MapPost("/tenants/{tenantId:guid}/data-handling-notice-acknowledgements", async (
    Guid tenantId,
    AcknowledgeDataHandlingNoticeRequest request,
    DataHandlingNoticeService noticeService,
    DataHandlingNoticeAcknowledgementService acknowledgementService,
    IWebHostEnvironment environment,
    ITenantContext tenantContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var packageRoot = ComplianceContentPackageLocator.FindPackageRoot(environment.ContentRootPath);
        var notice = await noticeService.GetPublishedAsync(packageRoot, request.Mode, request.WorkflowContext, cancellationToken);
        if (notice is null)
        {
            return Results.NotFound();
        }

        var acknowledgement = await acknowledgementService.AcknowledgeAsync(
            tenantId,
            tenantContext.UserId,
            notice,
            request,
            cancellationToken);
        return Results.Created($"/api/tenants/{tenantId}/data-handling-notice-acknowledgements", acknowledgement);
    }
    catch (DataHandlingNoticeAcknowledgementRequiredException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["dataHandlingNotice"] = [exception.Message]
        });
    }
})
.RequirePermission(Permission.ManageTenant)
.WithName("AcknowledgeDataHandlingNotice");

api.MapGet("/tenants/{tenantId:guid}/cui-support-escalations", async (
    Guid tenantId,
    CuiSupportEscalationService service,
    CancellationToken cancellationToken) =>
    Results.Ok(await service.ListAsync(tenantId, cancellationToken)))
.RequirePermission(Permission.ManageTenant)
.WithName("ListCuiSupportEscalations");

api.MapPost("/tenants/{tenantId:guid}/cui-support-escalations", async (
    Guid tenantId,
    CreateCuiSupportEscalationRequest request,
    CuiSupportEscalationService service,
    ITenantContext tenantContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var escalation = await service.CreateAsync(tenantId, request, tenantContext.UserId, cancellationToken);
        return Results.Created($"/api/tenants/{tenantId}/cui-support-escalations/{escalation.Id}", escalation);
    }
    catch (CuiSupportEscalationValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["cuiSupportEscalation"] = [exception.Message]
        });
    }
})
.RequirePermission(Permission.ManageTenant)
.WithName("CreateCuiSupportEscalation");

api.MapPatch("/tenants/{tenantId:guid}/cui-support-escalations/{escalationId:guid}", async (
    Guid tenantId,
    Guid escalationId,
    UpdateCuiSupportEscalationRequest request,
    CuiSupportEscalationService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var escalation = await service.UpdateSupportFieldsAsync(tenantId, escalationId, request, tenantContext.UserId, cancellationToken);
        return escalation is null
            ? ApiProblemDetails.Create(httpContext, "Resource not found", "CUI support escalation was not found.", StatusCodes.Status404NotFound, "resource_not_found")
            : Results.Ok(escalation);
    }
    catch (CuiSupportEscalationValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["cuiSupportEscalation"] = [exception.Message]
        });
    }
})
.RequirePermission(Permission.ManageTenant)
.WithName("UpdateCuiSupportEscalation");

api.MapPost("/tenants/{tenantId:guid}/cui-support-escalations/{escalationId:guid}/status", async (
    Guid tenantId,
    Guid escalationId,
    ChangeCuiSupportEscalationStatusRequest request,
    CuiSupportEscalationService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var escalation = await service.ChangeStatusAsync(tenantId, escalationId, request, tenantContext.UserId, cancellationToken);
        return escalation is null
            ? ApiProblemDetails.Create(httpContext, "Resource not found", "CUI support escalation was not found.", StatusCodes.Status404NotFound, "resource_not_found")
            : Results.Ok(escalation);
    }
    catch (CuiSupportEscalationValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["cuiSupportEscalation"] = [exception.Message]
        });
    }
})
.RequirePermission(Permission.ManageTenant)
.WithName("ChangeCuiSupportEscalationStatus");

api.MapPost("/tenants/{tenantId:guid}/cui-support-escalations/{escalationId:guid}/resolve", async (
    Guid tenantId,
    Guid escalationId,
    ResolveCuiSupportEscalationRequest request,
    CuiSupportEscalationService service,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var escalation = await service.ResolveAsync(tenantId, escalationId, request, tenantContext.UserId, cancellationToken);
        return escalation is null
            ? ApiProblemDetails.Create(httpContext, "Resource not found", "CUI support escalation was not found.", StatusCodes.Status404NotFound, "resource_not_found")
            : Results.Ok(escalation);
    }
    catch (CuiSupportEscalationValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["cuiSupportEscalation"] = [exception.Message]
        });
    }
})
.RequirePermission(Permission.ManageTenant)
.WithName("ResolveCuiSupportEscalation");

api.MapGet("/tenants/{tenantId:guid}/shared-responsibility-matrix/acknowledgements", async (
    Guid tenantId,
    SharedResponsibilityMatrixService matrixService,
    SharedResponsibilityMatrixAcknowledgementService acknowledgementService,
    IWebHostEnvironment environment,
    CancellationToken cancellationToken) =>
{
    var packageRoot = ComplianceContentPackageLocator.FindPackageRoot(environment.ContentRootPath);
    var matrix = await matrixService.GetPublishedAsync(packageRoot, cancellationToken);
    return Results.Ok(await acknowledgementService.ListAsync(tenantId, matrix, cancellationToken));
})
.RequirePermission(Permission.ManageTenant)
.WithName("ListSharedResponsibilityMatrixAcknowledgements");

api.MapPost("/tenants/{tenantId:guid}/shared-responsibility-matrix/acknowledgements", async (
    Guid tenantId,
    AcknowledgeSharedResponsibilityMatrixRequest request,
    SharedResponsibilityMatrixService matrixService,
    SharedResponsibilityMatrixAcknowledgementService acknowledgementService,
    IWebHostEnvironment environment,
    ITenantContext tenantContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var packageRoot = ComplianceContentPackageLocator.FindPackageRoot(environment.ContentRootPath);
        var matrix = await matrixService.GetPublishedAsync(packageRoot, cancellationToken);
        var acknowledgement = await acknowledgementService.AcknowledgeAsync(tenantId, matrix, request, tenantContext.UserId, cancellationToken);
        return Results.Created($"/api/tenants/{tenantId}/shared-responsibility-matrix/acknowledgements", acknowledgement);
    }
    catch (SharedResponsibilityMatrixAcknowledgementException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["sharedResponsibilityMatrix"] = [exception.Message]
        });
    }
})
.RequirePermission(Permission.ManageTenant)
.WithName("AcknowledgeSharedResponsibilityMatrix");

api.MapGet("/tenants/{tenantId:guid}/data-handling-mode/history", async (
    Guid tenantId,
    TenantService service,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var history = await service.ListDataHandlingModeHistoryAsync(tenantId, cancellationToken);
    if (history.Count == 0)
    {
        var tenant = await service.FindInCurrentTenantScopeAsync(tenantId, cancellationToken);
        if (tenant is null)
        {
            return ApiProblemDetails.Create(
                httpContext,
                "Resource not found",
                "Tenant was not found in the current tenant scope.",
                StatusCodes.Status404NotFound,
                "resource_not_found");
        }
    }

    return Results.Ok(history);
})
.RequirePermission(Permission.ManageTenant)
.WithName("ListTenantDataHandlingModeHistory");

static async Task<IResult> ReviewCuiReadyChecklistAsync(
    Guid tenantId,
    Guid checklistId,
    ReviewCuiReadyChecklistRequest request,
    Func<Guid, Guid, ReviewCuiReadyChecklistRequest, Guid, CancellationToken, Task<CuiReadyApprovalChecklistDto?>> reviewAction,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken)
{
    try
    {
        var checklist = await reviewAction(tenantId, checklistId, request, tenantContext.UserId, cancellationToken);
        return checklist is null
            ? ApiProblemDetails.Create(httpContext, "Resource not found", "Checklist was not found.", StatusCodes.Status404NotFound, "resource_not_found")
            : Results.Ok(checklist);
    }
    catch (CuiReadyApprovalChecklistValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["checklist"] = [exception.Message]
        });
    }
}

static async Task<IResult> ReviewSuggestedObligationAsync(
    Guid suggestionId,
    SuggestedObligationReviewRequest request,
    Func<Guid, SuggestedObligationReviewRequest, Guid, CancellationToken, Task<SuggestedObligationDto?>> reviewAction,
    ITenantContext tenantContext,
    HttpContext httpContext,
    CancellationToken cancellationToken)
{
    try
    {
        var suggestion = await reviewAction(suggestionId, request, tenantContext.UserId, cancellationToken);
        return suggestion is null
            ? ApiProblemDetails.Create(
                httpContext,
                "Resource not found",
                $"Suggested obligation '{suggestionId}' was not found.",
                StatusCodes.Status404NotFound,
                "resource_not_found")
            : Results.Ok(suggestion);
    }
    catch (SuggestedObligationValidationException exception)
    {
        return Results.ValidationProblem(
            exception.Errors.ToDictionary(error => error.Key, error => error.Value),
            title: "Suggested obligation review invalid",
            detail: exception.Message,
            statusCode: StatusCodes.Status400BadRequest);
    }
}

if (app.Environment.IsDevelopment())
{
    api.MapPost("/dev/compliance-content/import", async (
        IComplianceContentImporter importer,
        IWebHostEnvironment environment,
        CancellationToken cancellationToken) =>
    {
        var packageRoot = ComplianceContentPackageLocator.FindPackageRoot(environment.ContentRootPath);
        var report = await importer.ImportDirectoryAsync(packageRoot, cancellationToken);

        return report.Succeeded
            ? Results.Ok(report)
            : Results.BadRequest(report);
    })
    .RequirePermission(Permission.ManageObligations)
    .WithName("ImportDevelopmentComplianceContent");
}

app.Run();

public partial class Program;

internal static class ComplianceContentPackageLocator
{
    public static string FindPackageRoot(string contentRootPath)
    {
        var current = new DirectoryInfo(contentRootPath);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "Gccs.slnx")))
        {
            current = current.Parent;
        }

        if (current is null)
        {
            throw new DirectoryNotFoundException(
                $"Could not locate repository root from content root '{contentRootPath}'.");
        }

        return Path.Combine(current.FullName, "packages", "compliance-content");
    }
}

internal static class DemoContentPackageLocator
{
    public static string FindPackageRoot(string contentRootPath)
    {
        var current = new DirectoryInfo(contentRootPath);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "Gccs.slnx")))
        {
            current = current.Parent;
        }

        if (current is null)
        {
            throw new DirectoryNotFoundException(
                $"Could not locate repository root from content root '{contentRootPath}'.");
        }

        return Path.Combine(current.FullName, "packages", "demo-content");
    }
}
