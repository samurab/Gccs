using System.Security.Claims;
using System.Text.Json.Serialization;
using Gccs.Api.Security;
using Gccs.Api.LocalDevelopment;
using Gccs.Application.Compliance;
using Gccs.Application.Identity;
using Gccs.Application.Repositories;
using Gccs.Application.Reports;
using Gccs.Application.Tenancy;
using Gccs.Domain.Identity;
using Gccs.Infrastructure;

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

api.MapGet("/me/access", (ClaimsPrincipal user) =>
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

api.MapGet("/obligations", async (IObligationRepository repository, CancellationToken cancellationToken) =>
    Results.Ok(await repository.ListAsync(cancellationToken)))
.RequirePermission(Permission.ViewObligations)
.WithName("ListObligations");

api.MapGet("/obligations/{id}", async (string id, IObligationRepository repository, CancellationToken cancellationToken) =>
{
    var obligation = await repository.FindByIdAsync(id, cancellationToken);
    return obligation is null
        ? Results.NotFound(new { message = $"Obligation '{id}' was not found." })
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
    CancellationToken cancellationToken) =>
{
    try
    {
        var member = await service.AssignAsync(request, tenantContext.UserId, cancellationToken);
        return Results.Created($"/api/tenant-members/{member.MembershipId}", member);
    }
    catch (DuplicateMembershipException exception)
    {
        return Results.Conflict(new { message = exception.Message });
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
    CancellationToken cancellationToken) =>
{
    var member = await service.UpdateStatusAsync(membershipId, request, tenantContext.UserId, cancellationToken);
    return member is null
        ? Results.NotFound(new { message = "Tenant membership was not found in the current tenant scope." })
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
    CancellationToken cancellationToken) =>
{
    try
    {
        var invitation = await service.CreateAsync(request, tenantContext.UserId, cancellationToken);
        return Results.Created($"/api/tenant-invitations/{invitation.InvitationId}", invitation);
    }
    catch (DuplicateInvitationException exception)
    {
        return Results.Conflict(new { message = exception.Message });
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
            ? Results.NotFound(new { message = "Invitation token was not found." })
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
    CancellationToken cancellationToken) =>
{
    var invitation = await service.ExpireAsync(invitationId, tenantContext.UserId, cancellationToken);
    return invitation is null
        ? Results.NotFound(new { message = "Invitation was not found in the current tenant scope." })
        : Results.Ok(invitation);
})
.RequirePermission(Permission.ManageUsers)
.WithName("ExpireTenantInvitation");

api.MapPost("/tenant-invitations/{invitationId:guid}/revoke", async (
    Guid invitationId,
    TenantInvitationService service,
    ITenantContext tenantContext,
    CancellationToken cancellationToken) =>
{
    var invitation = await service.RevokeAsync(invitationId, tenantContext.UserId, cancellationToken);
    return invitation is null
        ? Results.NotFound(new { message = "Invitation was not found in the current tenant scope." })
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
    CancellationToken cancellationToken) =>
{
    var tenant = await service.FindInCurrentTenantScopeAsync(tenantId, cancellationToken);
    return tenant is null
        ? Results.NotFound(new { message = "Tenant was not found in the current tenant scope." })
        : Results.Ok(tenant);
})
.RequirePermission(Permission.ManageTenant)
.WithName("GetTenant");

api.MapPatch("/tenants/{tenantId:guid}/status", async (
    Guid tenantId,
    UpdateTenantStatusRequest request,
    TenantService service,
    ITenantContext tenantContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var tenant = await service.UpdateStatusAsync(tenantId, request, tenantContext.UserId, cancellationToken);
        return tenant is null
            ? Results.NotFound(new { message = "Tenant was not found in the current tenant scope." })
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
