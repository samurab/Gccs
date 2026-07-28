using System.Net;
using Gccs.Application.Audit;
using Gccs.Domain.Audit;
using Gccs.Domain.Identity;
using Gccs.Domain.Reports;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class ReportPostgresTransactionTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ReportPostgresTransactionTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    [Trait("Category", "PostgresIntegration")]
    public async Task Audit_failure_rolls_back_report_and_related_rows()
    {
        var connectionString = Environment.GetEnvironmentVariable("GCCS_TEST_POSTGRES_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        var tenantId = Guid.NewGuid();
        await using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<GccsDbContext>();
                services.RemoveAll<DbContextOptions<GccsDbContext>>();
                services.RemoveAll<IAuditEventWriter>();
                services.AddDbContext<GccsDbContext>(options => options.UseNpgsql(connectionString));
                services.AddScoped<IAuditEventWriter, FailingAuditEventWriter>();

                using var provider = services.BuildServiceProvider();
                using var scope = provider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
                dbContext.Database.Migrate();
                dbContext.Tenants.Add(new TenantEntity
                {
                    Id = tenantId,
                    Name = "Atomic report tenant",
                    Status = TenantStatus.Active,
                    DataPosture = TenantDataPosture.NoCui,
                    CreatedAt = DateTimeOffset.UtcNow
                });
                dbContext.SaveChanges();
            });
        });

        try
        {
            using var client = factory.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/reports/compliance-status");
            request.Headers.Add("X-Gccs-Dev-Auth", "true");
            request.Headers.Add("X-Gccs-Dev-Tenant", tenantId.ToString());
            request.Headers.Add("X-Gccs-Dev-User", Guid.NewGuid().ToString());
            request.Headers.Add("X-Gccs-Dev-Permissions", Permission.ManageReports.ToString());

            using var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            Assert.Contains("audit_write_failed", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
            using var verificationScope = factory.Services.CreateScope();
            var verificationDbContext = verificationScope.ServiceProvider.GetRequiredService<GccsDbContext>();
            Assert.False(await verificationDbContext.Reports.AnyAsync(report =>
                report.TenantId == tenantId &&
                report.Type == ReportType.ComplianceStatus));
            Assert.False(await verificationDbContext.AuditLogEntries.AnyAsync(audit => audit.TenantId == tenantId));
        }
        finally
        {
            using var cleanupScope = factory.Services.CreateScope();
            var cleanupDbContext = cleanupScope.ServiceProvider.GetRequiredService<GccsDbContext>();
            var tenant = await cleanupDbContext.Tenants.SingleOrDefaultAsync(candidate => candidate.Id == tenantId);
            if (tenant is not null)
            {
                cleanupDbContext.Tenants.Remove(tenant);
                await cleanupDbContext.SaveChangesAsync();
            }
        }
    }

    private sealed class FailingAuditEventWriter : IAuditEventWriter
    {
        public Task WriteAsync(
            Guid tenantId,
            Guid actorUserId,
            AuditAction action,
            string entityType,
            string entityId,
            string summary,
            IReadOnlyDictionary<string, string>? metadata = null,
            CancellationToken cancellationToken = default) =>
            throw new AuditWriteException("Synthetic audit persistence failure.");
    }
}
