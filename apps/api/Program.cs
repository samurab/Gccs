using System.Text.Json.Serialization;
using Gccs.Api.Security;
using Gccs.Api.LocalDevelopment;
using Gccs.Application.Compliance;
using Gccs.Application.Repositories;
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

api.MapGet("/compliance/overview", async (ComplianceOverviewService service, ITenantContext tenantContext, CancellationToken cancellationToken) =>
    Results.Ok(await service.GetOverviewAsync(cancellationToken)))
.WithName("GetComplianceOverview");

api.MapGet("/obligations", async (IObligationRepository repository, CancellationToken cancellationToken) =>
    Results.Ok(await repository.ListAsync(cancellationToken)))
.RequirePermission(Permission.AuditorReadOnly)
.WithName("ListObligations");

api.MapGet("/obligations/{id}", async (string id, IObligationRepository repository, CancellationToken cancellationToken) =>
{
    var obligation = await repository.FindByIdAsync(id, cancellationToken);
    return obligation is null
        ? Results.NotFound(new { message = $"Obligation '{id}' was not found." })
        : Results.Ok(obligation);
})
.RequirePermission(Permission.AuditorReadOnly)
.WithName("GetObligationById");

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
