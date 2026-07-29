using System.Net;
using System.Text;
using Gccs.Application.Audit;
using Gccs.Application.Common;
using Gccs.Application.NoCui;
using Gccs.Domain.Audit;
using Gccs.Domain.Companies;
using Gccs.Domain.Common;
using Gccs.Domain.Contracts;
using Gccs.Domain.Identity;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class ContractExtractionPostgresTransactionTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ContractExtractionPostgresTransactionTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    [Trait("Category", "PostgresIntegration")]
    public async Task Audit_failure_rolls_back_extraction_job_creation()
    {
        var connectionString = Environment.GetEnvironmentVariable("GCCS_TEST_POSTGRES_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var contractId = Guid.NewGuid();
        var documentId = Guid.NewGuid();
        var text = "FAR 52.204-21 - Basic Safeguarding.";
        await using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.UseSetting("ExtractionProcessing:Enabled", "false");
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
                    Name = "Atomic extraction tenant",
                    Status = TenantStatus.Active,
                    DataPosture = TenantDataPosture.NoCui,
                    CreatedAt = DateTimeOffset.UtcNow
                });
                dbContext.Contracts.Add(new ContractEntity
                {
                    Id = contractId,
                    TenantId = tenantId,
                    ContractNumber = $"ATOMIC-{contractId:N}",
                    Title = "Atomic extraction contract",
                    AgencyOrPrimeName = "Synthetic Prime",
                    Relationship = ContractorRelationship.Prime,
                    Kind = ContractKind.PurchaseOrder,
                    Status = ContractStatus.Active,
                    PeriodOfPerformanceStart = new DateOnly(2026, 7, 1),
                    PeriodOfPerformanceEnd = new DateOnly(2027, 6, 30),
                    PlaceOfPerformance = "Remote",
                    Description = "Synthetic No-CUI transaction verification.",
                    DataHandlingPosture = DataHandlingPosture.NoFciOrCui,
                    CreatedAt = DateTimeOffset.UtcNow
                });
                dbContext.Set<ContractDocumentEntity>().Add(new ContractDocumentEntity
                {
                    Id = documentId,
                    ContractId = contractId,
                    Type = ContractDocumentType.Contract,
                    FileName = "atomic.txt",
                    ContentType = "text/plain",
                    SizeBytes = Encoding.UTF8.GetByteCount(text),
                    StorageUri = $"data:text/plain;base64,{Convert.ToBase64String(Encoding.UTF8.GetBytes(text))}",
                    ValidationStatus = "accepted",
                    MalwareScanStatus = EvidenceUploadGuardrails.CleanMalwareScanStatus,
                    NoticeVersion = NoCuiNotice.CurrentVersion,
                    UploadedAt = DateTimeOffset.UtcNow,
                    UploadedByUserId = actorUserId,
                    ContainsPotentialCui = false,
                    Classification = ContentClassification.Unclassified,
                    ClassificationSource = ContentClassificationSource.UserSelected,
                    ClassificationReason = "Synthetic No-CUI transaction verification."
                });
                dbContext.SaveChanges();
            });
        });

        try
        {
            using var client = factory.CreateClient();
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/contracts/{contractId}/documents/{documentId}/extraction-jobs");
            request.Headers.Add("X-Gccs-Dev-Auth", "true");
            request.Headers.Add("X-Gccs-Dev-Tenant", tenantId.ToString());
            request.Headers.Add("X-Gccs-Dev-User", actorUserId.ToString());
            request.Headers.Add("X-Gccs-Dev-Permissions", Permission.ManageContracts.ToString());

            using var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            Assert.Contains("audit_write_failed", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
            using var verificationScope = factory.Services.CreateScope();
            var verificationDbContext = verificationScope.ServiceProvider.GetRequiredService<GccsDbContext>();
            Assert.False(await verificationDbContext.Set<ExtractionJobEntity>()
                .AnyAsync(job => job.TenantId == tenantId));
            Assert.False(await verificationDbContext.AuditLogEntries
                .AnyAsync(audit => audit.TenantId == tenantId));
        }
        finally
        {
            using var cleanupScope = factory.Services.CreateScope();
            var cleanupDbContext = cleanupScope.ServiceProvider.GetRequiredService<GccsDbContext>();
            var documents = await cleanupDbContext.Set<ContractDocumentEntity>()
                .Where(document => document.ContractId == contractId)
                .ToArrayAsync();
            cleanupDbContext.RemoveRange(documents);
            var contract = await cleanupDbContext.Contracts
                .SingleOrDefaultAsync(candidate => candidate.Id == contractId);
            if (contract is not null)
            {
                cleanupDbContext.Contracts.Remove(contract);
            }

            var tenant = await cleanupDbContext.Tenants
                .SingleOrDefaultAsync(candidate => candidate.Id == tenantId);
            if (tenant is not null)
            {
                cleanupDbContext.Tenants.Remove(tenant);
            }

            await cleanupDbContext.SaveChangesAsync();
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
