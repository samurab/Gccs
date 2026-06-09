using Gccs.Application.Compliance;
using Gccs.Application.Repositories;
using Gccs.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddGccsInfrastructure();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("web");

app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    service = "gccs-api",
    dataPosture = "No-CUI / compliance management only",
    checkedAt = DateTimeOffset.UtcNow
}))
.WithName("Health");

app.MapGet("/api/compliance/overview", async (ComplianceOverviewService service, CancellationToken cancellationToken) =>
    Results.Ok(await service.GetOverviewAsync(cancellationToken)))
.WithName("GetComplianceOverview");

app.MapGet("/api/obligations", async (IObligationRepository repository, CancellationToken cancellationToken) =>
    Results.Ok(await repository.ListAsync(cancellationToken)))
.WithName("ListObligations");

app.MapGet("/api/obligations/{id}", async (string id, IObligationRepository repository, CancellationToken cancellationToken) =>
{
    var obligation = await repository.FindByIdAsync(id, cancellationToken);
    return obligation is null
        ? Results.NotFound(new { message = $"Obligation '{id}' was not found." })
        : Results.Ok(obligation);
})
.WithName("GetObligationById");

app.Run();

public partial class Program;
