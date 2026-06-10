using Gccs.Api.Security;
using Gccs.Application.Compliance;
using Gccs.Application.Repositories;
using Gccs.Domain.Identity;
using Gccs.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

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

app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    service = "gccs-api",
    dataPosture = "No-CUI / compliance management only",
    checkedAt = DateTimeOffset.UtcNow
}))
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

app.Run();

public partial class Program;
