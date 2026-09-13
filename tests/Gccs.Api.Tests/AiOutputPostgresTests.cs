using System.Net;
using System.Net.Http.Json;
using Gccs.Application.Ai;
using Gccs.Application.Common;
using Gccs.Application.Security;
using Gccs.Domain.Common;
using Gccs.Domain.Identity;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Ai;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class AiOutputPostgresTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AiOutputPostgresTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [PostgresFact]
    [Trait("Category", "PostgresIntegration")]
    public async Task Migration_persists_tenant_isolated_logs_and_rejects_stale_review_decisions()
    {
        var connection = Environment.GetEnvironmentVariable("GCCS_TEST_POSTGRES_CONNECTION")!;
        var options = new DbContextOptionsBuilder<GccsDbContext>().UseGccsPostgres(connection).Options;
        var tenant = Guid.NewGuid(); var otherTenant = Guid.NewGuid(); var actor = Guid.NewGuid();
        var answerId = Guid.NewGuid(); var otherAnswerId = Guid.NewGuid();
        await using (var seed = new GccsDbContext(options))
        {
            await PostgresTestDatabase.MigrateAsync(seed);
            seed.Tenants.AddRange(Tenant(tenant), Tenant(otherTenant));
            seed.AssistantAnswers.AddRange(Answer(answerId, tenant, actor), Answer(otherAnswerId, otherTenant, actor));
            await seed.SaveChangesAsync();
        }
        try
        {
            await using var db1 = new GccsDbContext(options);
            await using var db2 = new GccsDbContext(options);
            var first = new EfGuardedAssistantRepository(db1, new FixedTenantContext(tenant, actor));
            var stale = new EfGuardedAssistantRepository(db2, new FixedTenantContext(tenant, actor));
            Assert.Null(await first.FindAnswerAsync(otherAnswerId, tenant));
            var decision = new AiOutputReviewDecisionRequest(AiOutputReviewState.Approved, "Verified.", null, 0);
            Assert.NotNull(await first.ReviewAnswerAsync(answerId, tenant, decision, actor));
            await Assert.ThrowsAsync<AiOutputReviewConflictException>(() => stale.ReviewAnswerAsync(answerId, tenant, decision, actor));
            await using var verify = new GccsDbContext(options);
            Assert.Single(await verify.AssistantOutputReviews.Where(x => x.TenantId == tenant && x.AnswerId == answerId).ToArrayAsync());
        }
        finally { await CleanupAsync(options, tenant, otherTenant); }
    }

    [PostgresFact]
    [Trait("Category", "PostgresIntegration")]
    public async Task Retention_archives_only_expired_outputs_and_appends_history()
    {
        var connection = Environment.GetEnvironmentVariable("GCCS_TEST_POSTGRES_CONNECTION")!;
        var options = new DbContextOptionsBuilder<GccsDbContext>().UseGccsPostgres(connection).Options;
        var tenant = Guid.NewGuid(); var actor = Guid.NewGuid(); var expired = Guid.NewGuid(); var current = Guid.NewGuid();
        await using var db = new GccsDbContext(options);
        await PostgresTestDatabase.MigrateAsync(db);
        db.Tenants.Add(Tenant(tenant));
        db.AssistantAnswers.AddRange(
            Answer(expired, tenant, actor, DateTimeOffset.UtcNow.AddMinutes(-1)),
            Answer(current, tenant, actor, DateTimeOffset.UtcNow.AddDays(30)));
        await db.SaveChangesAsync();
        try
        {
            var archived = await new EfAiOutputRetentionRepository(db).ArchiveExpiredAsync(DateTimeOffset.UtcNow, 100);
            Assert.Equal(expired, Assert.Single(archived).AnswerId);
            Assert.Equal(AiOutputReviewState.Archived, (await db.AssistantAnswers.SingleAsync(x => x.Id == expired)).ReviewState);
            Assert.Equal(AiOutputReviewState.Draft, (await db.AssistantAnswers.SingleAsync(x => x.Id == current)).ReviewState);
            Assert.Single(await db.AssistantOutputReviews.Where(x => x.AnswerId == expired).ToArrayAsync());
        }
        finally { await CleanupAsync(options, tenant); }
    }

    [PostgresFact]
    [Trait("Category", "PostgresIntegration")]
    public async Task Concurrent_retention_workers_skip_an_already_claimed_output()
    {
        var connection = Environment.GetEnvironmentVariable("GCCS_TEST_POSTGRES_CONNECTION")!;
        var options = new DbContextOptionsBuilder<GccsDbContext>().UseGccsPostgres(connection).Options;
        var tenant = Guid.NewGuid(); var actor = Guid.NewGuid(); var expired = Guid.NewGuid();
        await using (var seed = new GccsDbContext(options))
        {
            await PostgresTestDatabase.MigrateAsync(seed);
            seed.Tenants.Add(Tenant(tenant));
            seed.AssistantAnswers.Add(Answer(expired, tenant, actor, DateTimeOffset.UtcNow.AddMinutes(-1)));
            await seed.SaveChangesAsync();
        }
        try
        {
            await using var firstDb = new GccsDbContext(options);
            await using var secondDb = new GccsDbContext(options);
            await using var firstTransaction = await firstDb.Database.BeginTransactionAsync();
            var first = await new EfAiOutputRetentionRepository(firstDb)
                .ArchiveExpiredAsync(DateTimeOffset.UtcNow, 100);
            Assert.Equal(expired, Assert.Single(first).AnswerId);

            await using var secondTransaction = await secondDb.Database.BeginTransactionAsync();
            var second = await new EfAiOutputRetentionRepository(secondDb)
                .ArchiveExpiredAsync(DateTimeOffset.UtcNow, 100);
            Assert.Empty(second);
            await secondTransaction.RollbackAsync();
            await firstTransaction.CommitAsync();

            await using var verify = new GccsDbContext(options);
            Assert.Single(await verify.AssistantOutputReviews.Where(x => x.AnswerId == expired).ToArrayAsync());
        }
        finally { await CleanupAsync(options, tenant); }
    }

    [PostgresFact]
    [Trait("Category", "PostgresIntegration")]
    public async Task Draft_ai_reference_rolls_back_report_generation_and_provenance_link()
    {
        var connection = Environment.GetEnvironmentVariable("GCCS_TEST_POSTGRES_CONNECTION")!;
        var tenant = Guid.NewGuid(); var actor = Guid.NewGuid(); var answerId = Guid.NewGuid();
        await using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<GccsDbContext>();
                services.RemoveAll<DbContextOptions<GccsDbContext>>();
                services.AddDbContext<GccsDbContext>(options => options.UseGccsPostgres(connection));
                using var provider = services.BuildServiceProvider();
                using var scope = provider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
                PostgresTestDatabase.Migrate(db);
                db.Tenants.Add(Tenant(tenant));
                db.AssistantAnswers.Add(Answer(answerId, tenant, actor));
                NoticeTestData.Seed(db, actor);
                db.SaveChanges();
            });
        });
        try
        {
            using var client = factory.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/reports/compliance-status")
            {
                Content = JsonContent.Create(new ClassifiedWorkflowRequest(
                    new ContentClassificationRequest(ContentClassification.Unclassified), answerId))
            };
            request.Headers.Add("X-Gccs-Dev-Auth", "true");
            request.Headers.Add("X-Gccs-Dev-Tenant", tenant.ToString());
            request.Headers.Add("X-Gccs-Dev-User", actor.ToString());
            request.Headers.Add("X-Gccs-Dev-Permissions", Permission.ManageReports.ToString());
            using var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains("ai_output_provenance_invalid", await response.Content.ReadAsStringAsync());

            await using var scope = factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
            Assert.False(await db.Reports.AnyAsync(x => x.TenantId == tenant));
            Assert.False(await db.AssistantOutputUsages.AnyAsync(x => x.TenantId == tenant));
        }
        finally { await CleanupAsync(new DbContextOptionsBuilder<GccsDbContext>().UseGccsPostgres(connection).Options, tenant); }
    }

    private static TenantEntity Tenant(Guid id) => new() { Id = id, Name = $"AI output {id:N}", Status = TenantStatus.Active,
        DataPosture = TenantDataPosture.NoCui, CreatedAt = DateTimeOffset.UtcNow };

    private static AssistantAnswerEntity Answer(Guid id, Guid tenant, Guid actor, DateTimeOffset? retainUntil = null) => new()
    {
        Id = id, TenantId = tenant, ActorUserId = actor, Prompt = "Allowed prompt", PromptMetadataJson = "{}",
        ModelConfigurationJson = "{}", RetrievalPolicyJson = "[]", WorkflowContext = "report", Status = "Draft",
        Answer = "Draft output", CitationsJson = "[]", SupportStatus = "SourceSupported", DraftLabel = "Draft",
        RequiresReview = true, HumanReviewStatus = "pending", Classification = ContentClassification.Unclassified,
        Result = "Draft", ReviewState = AiOutputReviewState.Draft, RetainUntil = retainUntil ?? DateTimeOffset.UtcNow.AddDays(365),
        CreatedAt = DateTimeOffset.UtcNow
    };

    private static async Task CleanupAsync(DbContextOptions<GccsDbContext> options, params Guid[] tenants)
    {
        await using var db = new GccsDbContext(options);
        await db.AssistantOutputUsages.Where(x => tenants.Contains(x.TenantId)).ExecuteDeleteAsync();
        await db.AssistantOutputReviews.Where(x => tenants.Contains(x.TenantId)).ExecuteDeleteAsync();
        await db.AssistantAnswers.Where(x => tenants.Contains(x.TenantId)).ExecuteDeleteAsync();
        await db.Reports.Where(x => tenants.Contains(x.TenantId)).ExecuteDeleteAsync();
        await db.DataHandlingNoticeAcknowledgements.Where(x => tenants.Contains(x.TenantId)).ExecuteDeleteAsync();
        await db.AuditLogEntries.Where(x => tenants.Contains(x.TenantId)).ExecuteDeleteAsync();
        await db.Tenants.Where(x => tenants.Contains(x.Id)).ExecuteDeleteAsync();
    }

    private sealed class FixedTenantContext(Guid tenantId, Guid userId) : ICurrentTenantContext
    {
        public Guid TenantId => tenantId;
        public Guid UserId => userId;
        public string UserEmail => "ai-reviewer@example.test";
    }
}
