using System.Text;
using Gccs.Application.Audit;
using Gccs.Application.Common;
using Gccs.Application.NoCui;
using Gccs.Application.Contracts;
using Gccs.Application.Reports;
using Gccs.Application.Tenancy;
using Gccs.Domain.Common;
using Gccs.Infrastructure;
using Gccs.Infrastructure.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Gccs.Application.Security;
using Gccs.Application.Storage;
using Gccs.Domain.Audit;
using Gccs.Domain.Contracts;
using Gccs.Domain.Companies;
using Gccs.Domain.Evidence;
using Gccs.Domain.Reports;
using Gccs.Domain.Storage;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Audit;
using Gccs.Infrastructure.Common;
using Gccs.Infrastructure.NoCui;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Gccs.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace Gccs.Api.Tests;

// Every relational fixture creates a fresh retained synthetic database. No teardown deletes data.
public sealed class DurableObjectWriteIntentPostgresTests
{
    [PostgresTheory]
    [InlineData("EvidenceItem")]
    [InlineData("Contract")]
    [InlineData("ReportExport")]
    public async Task Publication_and_success_audit_and_tenant_link_commit_atomically(string kind)
    {
        await using var f = await Fixture.CreateAsync(kind);
        var published = await f.ExecuteAsync();
        Assert.NotNull(published);
        var intent = await f.IntentAsync();
        Assert.Equal(ObjectWriteIntentState.Committed, intent.State);
        Assert.Equal(1, f.Storage.Writes); Assert.Equal(0, f.Storage.Deletes);
        Assert.Equal(1, await f.Db.AuditLogEntries.CountAsync(x => x.TenantId == f.Tenant && x.Action == f.SuccessAction));
        Assert.Empty(await f.Db.Set<ObjectCleanupEntity>().ToArrayAsync());
        Assert.True(await f.LinkedAsync(intent.Id));
        Assert.All(await f.Db.AuditLogEntries.ToArrayAsync(), x => Assert.DoesNotContain("synthetic-private-bytes", x.MetadataJson));
    }

    [PostgresTheory]
    [InlineData("EvidenceItem")]
    [InlineData("Contract")]
    [InlineData("ReportExport")]
    public async Task Publication_audit_rollback_retains_terminal_obligation_without_inline_deletion(string kind)
    {
        await using var f = await Fixture.CreateAsync(kind);
        f.Failure.EntityType = f.SuccessEntityType;
        await Assert.ThrowsAsync<AuditWriteException>(() => f.ExecuteAsync());
        Assert.Equal(1, f.Storage.Writes); Assert.Equal(0, f.Storage.Deletes);
        Assert.Equal(ObjectWriteIntentState.CleanupPending, (await f.IntentAsync()).State);
        var queue = await f.Db.Set<ObjectCleanupEntity>().SingleAsync();
        Assert.Equal("\"fixture-version\"", queue.ExpectedETag);
        Assert.NotNull(queue.ObjectWriteIntentId);
        Assert.False(await f.Db.AuditLogEntries.AnyAsync(x => x.Action == f.SuccessAction));
        Assert.False(await f.HasPublicationAsync());
    }

    [PostgresFact]
    public async Task Failed_preparation_audit_commits_neither_intent_nor_audit_and_issues_zero_writes()
    {
        await using var f = await Fixture.CreateAsync();
        f.Failure.EntityType = "ObjectWriteIntent";
        await Assert.ThrowsAsync<AuditWriteException>(() => f.ExecuteAsync());
        Assert.Equal(0, f.Storage.Writes);
        Assert.Empty(await f.Db.Set<ObjectWriteIntentEntity>().ToArrayAsync());
        Assert.Empty(await f.Db.AuditLogEntries.ToArrayAsync());
    }

    [Fact]
    public async Task Nonrelational_production_capability_fails_closed_before_storage()
    {
        await using var db = new GccsDbContext(new DbContextOptionsBuilder<GccsDbContext>()
            .UseInMemoryDatabase("retained-intent-negative-" + Guid.NewGuid().ToString("N")).Options);
        var storage = new RetainedStorage();
        var audit = new EfAuditEventWriter(db, new RequestMetadata());
        var provider = new ServiceCollection().AddSingleton(db).BuildServiceProvider();
        var transaction = new EfApplicationTransaction(provider);
        var queue = new EfObjectCleanupQueue(db, storage, audit);
        var coordinator = new DurableObjectWriteCoordinator(new EfObjectWriteIntentRepository(db, transaction, audit, queue), storage);
        await Assert.ThrowsAsync<InvalidOperationException>(() => coordinator.ExecuteAsync<Published>(
            new(Guid.NewGuid(), ObjectStorageContainer.Evidence, "synthetic/file.txt", new MemoryStream([1]), "text/plain"),
            Guid.NewGuid(), "EvidenceItem", Guid.NewGuid(), (_, _) => Task.FromResult<Published?>(new(Guid.NewGuid()))));
        Assert.Equal(0, storage.Writes);
    }

    [PostgresFact]
    public async Task Ambient_transaction_is_rejected_before_any_provider_or_preparation_write()
    {
        await using var f = await Fixture.CreateAsync();
        await using var outer = await f.Db.Database.BeginTransactionAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.ExecuteAsync());
        Assert.Equal(0, f.Storage.Writes);
        Assert.Empty(await f.Db.Set<ObjectWriteIntentEntity>().ToArrayAsync());
    }

    [PostgresFact]
    public async Task Unrelated_dirty_business_entity_is_not_flushed_by_preparation()
    {
        await using var f = await Fixture.CreateAsync();
        (await f.Db.EvidenceItems.SingleAsync()).Name = "unsaved-business-change";
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.ExecuteAsync());
        Assert.Equal(0, f.Storage.Writes);
        await using var observer = new GccsDbContext(f.Options);
        Assert.Equal("Synthetic retained evidence", (await observer.EvidenceItems.SingleAsync()).Name);
        Assert.Empty(await observer.Set<ObjectWriteIntentEntity>().ToArrayAsync());
    }

    [PostgresTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Provider_exception_before_or_after_persistence_is_held_without_publication_or_cleanup(bool afterWrite)
    {
        await using var f = await Fixture.CreateAsync();
        f.Storage.OnUpload = (_, _) => throw new IOException("synthetic lost acknowledgement");
        f.Storage.RetainBeforeCallback = afterWrite;
        await Assert.ThrowsAsync<IOException>(() => f.ExecuteAsync());
        Assert.Equal(afterWrite, f.Storage.Retained);
        Assert.Equal(ObjectWriteIntentState.Ambiguous, (await f.IntentAsync()).State);
        Assert.False(await f.HasPublicationAsync());
        Assert.Empty(await f.Db.Set<ObjectCleanupEntity>().ToArrayAsync());
        Assert.Equal(0, f.Storage.Deletes);
    }

    [PostgresFact]
    public async Task Cancellation_after_issue_retains_ambiguous_pointer_even_if_provider_retained_bytes()
    {
        await using var f = await Fixture.CreateAsync();
        f.Storage.OnUpload = (_, _) => throw new OperationCanceledException("synthetic cancellation");
        await Assert.ThrowsAsync<OperationCanceledException>(() => f.ExecuteAsync());
        Assert.True(f.Storage.Retained);
        Assert.Equal(ObjectWriteIntentState.Ambiguous, (await f.IntentAsync()).State);
        Assert.Empty(await f.Db.Set<ObjectCleanupEntity>().ToArrayAsync());
    }

    [PostgresFact]
    public async Task Missing_provider_version_fails_closed_with_private_ambiguous_pointer()
    {
        await using var f = await Fixture.CreateAsync();
        f.Storage.ETag = null;
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.ExecuteAsync());
        Assert.Equal(ObjectWriteIntentState.Ambiguous, (await f.IntentAsync()).State);
        Assert.False(await f.HasPublicationAsync());
        Assert.Empty(await f.Db.Set<ObjectCleanupEntity>().ToArrayAsync());
    }

    [PostgresFact]
    public async Task Late_terminal_write_after_intent_lease_expiry_cannot_publish_or_be_age_cleaned()
    {
        await using var f = await Fixture.CreateAsync();
        f.Storage.OnUpload = async (_, _) =>
        {
            await using var other = new GccsDbContext(f.Options);
            var intent = await other.Set<ObjectWriteIntentEntity>().SingleAsync();
            intent.LeaseUntil = DateTimeOffset.UtcNow.AddMinutes(-1);
            await other.SaveChangesAsync();
        };
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.ExecuteAsync());
        Assert.True(f.Storage.Retained);
        Assert.Equal(ObjectWriteIntentState.Ambiguous, (await f.IntentAsync()).State);
        Assert.False(await f.HasPublicationAsync());
        Assert.Empty(await f.Db.Set<ObjectCleanupEntity>().ToArrayAsync());
    }

    [PostgresFact]
    public async Task Report_lease_lost_after_write_cannot_publish_under_stale_claim()
    {
        await using var f = await Fixture.CreateAsync("ReportExport");
        f.Storage.OnUpload = async (_, _) =>
        {
            await using var other = new GccsDbContext(f.Options);
            var export = await other.ReportExports.SingleAsync();
            export.ProcessingLeaseId = Guid.NewGuid();
            await other.SaveChangesAsync();
        };
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.ExecuteAsync());
        Assert.Equal(ObjectWriteIntentState.CleanupPending, (await f.IntentAsync()).State);
        Assert.False(await f.HasPublicationAsync());
    }

    [PostgresFact]
    public async Task Database_fault_after_provider_success_preserves_precommitted_pointer()
    {
        await using var f = await Fixture.CreateAsync();
        f.Storage.OnUpload = (_, _) => { f.Failure.FailAllSaves = true; return Task.CompletedTask; };
        await Assert.ThrowsAnyAsync<Exception>(() => f.ExecuteAsync());
        await using var observer = new GccsDbContext(f.Options);
        Assert.Equal(ObjectWriteIntentState.Writing, (await observer.Set<ObjectWriteIntentEntity>().SingleAsync()).State);
        Assert.Empty(await observer.EvidenceFileVersions.ToArrayAsync());
        Assert.Empty(await observer.Set<ObjectCleanupEntity>().ToArrayAsync());
        Assert.True(f.Storage.Retained);
    }

    [PostgresFact]
    public async Task Committed_transaction_with_lost_acknowledgement_is_reconciled_without_cleanup()
    {
        await using var f = await Fixture.CreateAsync();
        f.CommitFailure.ThrowWhenCommitted = () => f.Db.ChangeTracker.Entries<ObjectWriteIntentEntity>()
            .Any(x => x.Entity.State == ObjectWriteIntentState.Committed);
        Assert.NotNull(await f.ExecuteAsync());
        Assert.Equal(ObjectWriteIntentState.Committed, (await f.IntentAsync()).State);
        Assert.True(await f.HasPublicationAsync());
        Assert.Empty(await f.Db.Set<ObjectCleanupEntity>().ToArrayAsync());
        Assert.Equal(0, f.Storage.Deletes);
    }

    [PostgresFact]
    public async Task Null_publication_result_leaves_terminal_cleanup_obligation()
    {
        await using var f = await Fixture.CreateAsync();
        Assert.Null(await f.Coordinator.ExecuteAsync<Published>(f.Request(), f.User, f.Kind, f.Resource,
            (_, _) => Task.FromResult<Published?>(null), resourceLeaseId: f.ResourceLease));
        Assert.Equal(ObjectWriteIntentState.CleanupPending, (await f.IntentAsync()).State);
        Assert.False(await f.HasPublicationAsync());
    }

    [PostgresFact]
    public async Task Repeated_and_concurrent_claims_issue_only_one_fenced_attempt()
    {
        await using var f = await Fixture.CreateAsync();
        var handle = await f.Repository.PrepareAsync(f.Request(), f.User, f.Kind, f.Resource, null, default);
        async Task<bool> Claim()
        {
            await using var db = new GccsDbContext(f.Options);
            var repo = f.NewRepository(db);
            try { await repo.StartAsync(handle, default); return true; }
            catch (InvalidOperationException) { return false; }
        }
        var claims = await Task.WhenAll(Claim(), Claim());
        Assert.Single(claims, x => x);
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Repository.StartAsync(handle, default));
        Assert.Equal(0, f.Storage.Writes);
        Assert.Equal(ObjectWriteIntentState.Writing, (await f.IntentAsync()).State);
    }

    [PostgresTheory]
    [InlineData("tenant")]
    [InlineData("owner")]
    [InlineData("generation")]
    [InlineData("object")]
    [InlineData("container")]
    public async Task Forged_capability_cannot_transition_another_attempt(string field)
    {
        await using var f = await Fixture.CreateAsync();
        var handle = await f.Repository.PrepareAsync(f.Request(), f.User, f.Kind, f.Resource, null, default);
        var forged = field switch
        {
            "tenant" => handle with { TenantId = Guid.NewGuid() },
            "owner" => handle with { OwnerId = Guid.NewGuid() },
            "generation" => handle with { Generation = handle.Generation + 1 },
            "object" => handle with { ObjectName = "other/file.txt" },
            _ => handle with { Container = ObjectStorageContainer.Reports }
        };
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Repository.StartAsync(forged, default));
        Assert.Equal(ObjectWriteIntentState.Prepared, (await f.IntentAsync()).State);
        Assert.Equal(0, f.Storage.Writes);
    }

    [PostgresTheory]
    [InlineData(ObjectWriteIntentState.Prepared)]
    [InlineData(ObjectWriteIntentState.Writing)]
    [InlineData(ObjectWriteIntentState.Written)]
    [InlineData(ObjectWriteIntentState.Ambiguous)]
    [InlineData(ObjectWriteIntentState.ConflictHold)]
    [InlineData(ObjectWriteIntentState.Committed)]
    public async Task Cleanup_cannot_delete_active_held_or_committed_intents(ObjectWriteIntentState state)
    {
        await using var f = await Fixture.CreateAsync();
        var intent = await f.SeedIntentAndJobAsync(state);
        Assert.True(await f.Queue.ProcessNextAsync());
        Assert.Equal(0, f.Storage.Deletes);
        Assert.Equal(state, (await f.IntentAsync()).State);
        var job = await f.Db.Set<ObjectCleanupEntity>().SingleAsync();
        Assert.Null(job.CompletedAt); Assert.Equal("cleanup_safety_hold", job.LastErrorCode);
        Assert.Equal(intent.Id, job.ObjectWriteIntentId);
    }

    [PostgresTheory]
    [InlineData("etag")]
    [InlineData("object")]
    [InlineData("container")]
    public async Task Cleanup_rejects_mismatched_version_or_target(string field)
    {
        await using var f = await Fixture.CreateAsync();
        await f.SeedIntentAndJobAsync(ObjectWriteIntentState.CleanupPending);
        var job = await f.Db.Set<ObjectCleanupEntity>().SingleAsync();
        if (field == "etag") job.ExpectedETag = "\"other-version\"";
        if (field == "object") job.ObjectName = "other/file.txt";
        if (field == "container") job.Container = ObjectStorageContainer.Reports;
        await f.Db.SaveChangesAsync();
        Assert.True(await f.Queue.ProcessNextAsync());
        Assert.Equal(0, f.Storage.Deletes);
        Assert.Null((await f.Db.Set<ObjectCleanupEntity>().SingleAsync()).CompletedAt);
    }

    [PostgresFact]
    public async Task Legacy_cleanup_job_cannot_delete_a_live_reference_or_new_held_owner()
    {
        await using var f = await Fixture.CreateAsync();
        await f.CreateBusinessAsync();
        var job = f.Job(); f.Db.Set<ObjectCleanupEntity>().Add(job); await f.Db.SaveChangesAsync();
        Assert.True(await f.Queue.ProcessNextAsync());
        Assert.Equal(0, f.Storage.Deletes);
        Assert.Null((await f.Db.Set<ObjectCleanupEntity>().SingleAsync()).CompletedAt);
    }

    [PostgresFact]
    public async Task Conditional_fake_absence_completes_queue_and_intent_with_atomic_audit()
    {
        await using var f = await Fixture.CreateAsync();
        await f.SeedIntentAndJobAsync(ObjectWriteIntentState.CleanupPending);
        Assert.True(await f.Queue.ProcessNextAsync());
        Assert.Equal(1, f.Storage.Deletes);
        Assert.Equal("\"fixture-version\"", f.Storage.LastDelete!.ExpectedETag);
        Assert.Equal(f.Tenant, f.Storage.LastDelete.TenantId);
        Assert.Equal(ObjectWriteIntentState.Cleaned, (await f.IntentAsync()).State);
        Assert.NotNull((await f.Db.Set<ObjectCleanupEntity>().SingleAsync()).CompletedAt);
        Assert.Single(await f.Db.AuditLogEntries.Where(x => x.EntityType == "ObjectCleanup" && x.Action == AuditAction.Deleted).ToArrayAsync());
    }

    [PostgresFact]
    public async Task Provider_cleanup_failure_preserves_backoff_and_terminal_obligation()
    {
        await using var f = await Fixture.CreateAsync();
        await f.SeedIntentAndJobAsync(ObjectWriteIntentState.CleanupPending);
        f.Storage.FailDelete = true;
        Assert.True(await f.Queue.ProcessNextAsync());
        var job = await f.Db.Set<ObjectCleanupEntity>().SingleAsync();
        Assert.Null(job.CompletedAt); Assert.Equal(1, job.Attempts);
        Assert.Equal("cleanup_provider_failure", job.LastErrorCode);
        Assert.True(job.NextAttemptAt > DateTimeOffset.UtcNow);
        Assert.Equal(ObjectWriteIntentState.CleanupPending, (await f.IntentAsync()).State);
    }

    [PostgresFact]
    public async Task Cleanup_completion_audit_failure_rolls_back_job_and_intent_for_retry()
    {
        await using var f = await Fixture.CreateAsync();
        await f.SeedIntentAndJobAsync(ObjectWriteIntentState.CleanupPending);
        f.Failure.EntityType = "ObjectCleanup";
        await Assert.ThrowsAsync<AuditWriteException>(() => f.Queue.ProcessNextAsync());
        await using var observer = new GccsDbContext(f.Options);
        Assert.Null((await observer.Set<ObjectCleanupEntity>().SingleAsync()).CompletedAt);
        Assert.Equal(ObjectWriteIntentState.CleanupPending, (await observer.Set<ObjectWriteIntentEntity>().SingleAsync()).State);
        Assert.False(await observer.AuditLogEntries.AnyAsync(x => x.EntityType == "ObjectCleanup"));
    }

    [PostgresFact]
    public async Task Normal_evidence_tombstone_audit_and_intent_cleanup_transition_are_atomic()
    {
        await using var f = await Fixture.CreateAsync();
        await f.ExecuteAsync();
        var service = new EvidenceFileService(new EfNoCuiAcknowledgementRepository(f.Db, new TenantContext(f.Tenant, f.User)),
            new TenantContext(f.Tenant, f.User), f.Audit, null!, f.Storage, null!, f.Transaction, f.Queue, f.Coordinator);
        Assert.NotNull(await service.DeleteLatestFileAsync(f.Evidence, f.User));
        Assert.NotNull((await f.Db.EvidenceFileVersions.SingleAsync()).DeletedAt);
        Assert.Equal(ObjectWriteIntentState.CleanupPending, (await f.IntentAsync()).State);
        Assert.Single(await f.Db.Set<ObjectCleanupEntity>().ToArrayAsync());
        Assert.Equal(0, f.Storage.Deletes);
    }

    [PostgresFact]
    public async Task Tenant_composite_constraint_rejects_foreign_intent_binding()
    {
        await using var f = await Fixture.CreateAsync();
        var handle = await f.Repository.PrepareAsync(f.Request(), f.User, f.Kind, f.Resource, null, default);
        var foreignTenant = Guid.NewGuid();
        f.Db.Tenants.Add(Fixture.TenantEntity(foreignTenant)); await f.Db.SaveChangesAsync();
        f.Db.EvidenceFileVersions.Add(new EvidenceFileVersionEntity { Id = Guid.NewGuid(), EvidenceItemId = f.Evidence,
            VersionNumber = 1, FileName = "synthetic.txt", ContentType = "text/plain", ValidationStatus = "accepted", MalwareScanStatus = "clean",
            ObjectWriteIntentId = handle.Id, ObjectWriteIntentTenantId = foreignTenant, UploadedAt = DateTimeOffset.UtcNow, UploadedByUserId = f.User });
        await Assert.ThrowsAsync<DbUpdateException>(() => f.Db.SaveChangesAsync());
        await using var observer = new GccsDbContext(f.Options);
        Assert.Empty(await observer.EvidenceFileVersions.ToArrayAsync());
    }

    [PostgresFact]
    public async Task Additive_migration_up_preserves_prior_legacy_rows_and_matches_current_model()
    {
        await using var f = await Fixture.CreateAsync(migrate: true);
        Assert.False(f.Db.Database.HasPendingModelChanges());
        Assert.Contains("20261018171852_AddDurableObjectWriteIntents", await f.Db.Database.GetAppliedMigrationsAsync());
        Assert.Single(await f.Db.EvidenceFileVersions.ToArrayAsync());
        Assert.Null((await f.Db.EvidenceFileVersions.SingleAsync()).ObjectWriteIntentId);
        Assert.Null((await f.Db.Set<ContractDocumentEntity>().SingleAsync()).ObjectWriteIntentId);
        Assert.Null((await f.Db.ReportExports.SingleAsync()).ObjectWriteIntentId);
        Assert.Null((await f.Db.Set<ObjectCleanupEntity>().SingleAsync()).ObjectWriteIntentId);
        Assert.Empty(await f.Db.Set<ObjectWriteIntentEntity>().ToArrayAsync());
    }

    [PostgresProviderFact]
    public async Task Real_Azurite_lost_acknowledgement_retains_object_and_durable_ambiguous_pointer()
    {
        await using var f = await Fixture.CreateAsync();
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__AzureStorage")!;
        Assert.Equal("127.0.0.1", new Azure.Storage.Blobs.BlobServiceClient(connection).Uri.Host);
        var container = Environment.GetEnvironmentVariable("GCCS_INTENT_PROVIDER_CONTAINER")!;
        Assert.StartsWith("s397-intent-", container);
        var storage = new AzureBlobObjectStorageService(Options.Create(new AzureBlobStorageOptions
        { UseManagedIdentity = false, Containers = new AzureBlobContainerOptions { Evidence = container } }),
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:AzureStorage"] = connection }).Build());
        var coordinator = new DurableObjectWriteCoordinator(f.Repository, new LostAcknowledgementStorage(storage));
        await Assert.ThrowsAsync<IOException>(() => coordinator.ExecuteAsync<Published>(f.Request(), f.User, f.Kind, f.Resource,
            (_, _) => throw new InvalidOperationException("Unknown write cannot publish.")));
        Assert.True(await storage.ExistsAsync(new(f.Tenant, f.Container, f.ObjectName)));
        Assert.Equal(ObjectWriteIntentState.Ambiguous, (await f.IntentAsync()).State);
        Assert.Empty(await f.Db.EvidenceFileVersions.ToArrayAsync());
        Assert.Empty(await f.Db.Set<ObjectCleanupEntity>().ToArrayAsync());
        Assert.False(await f.Db.AuditLogEntries.AnyAsync(x => x.Action == AuditAction.Uploaded));
    }

    [PostgresTheory]
    [InlineData("EvidenceItem", false)] [InlineData("EvidenceItem", true)]
    [InlineData("Contract", false)] [InlineData("Contract", true)]
    [InlineData("ReportExport", false)] [InlineData("ReportExport", true)]
    public async Task Actual_writer_service_preserves_durable_publication_and_rollback(string kind, bool failSuccessAudit)
    {
        await using var f = await Fixture.CreateAsync(kind);
        await f.SeedPolicyPrerequisitesAsync();
        using var provider = f.ServiceProvider();
        f.Failure.EntityType = failSuccessAudit ? f.SuccessEntityType : null;
        async Task Write()
        {
            await using var content = new MemoryStream(Encoding.UTF8.GetBytes("Synthetic public fixture."));
            if (kind == "EvidenceItem") Assert.NotNull(await provider.GetRequiredService<EvidenceFileService>().UploadEvidenceFileAsync(
                f.Evidence, new("synthetic.txt", "text/plain", content.Length, content, true, false, new(ContentClassification.Unclassified)), f.User));
            else if (kind == "Contract") Assert.NotNull(await provider.GetRequiredService<ContractDocumentFileService>().UploadAsync(
                f.Contract, new(ContractDocumentType.Other, "synthetic.txt", "text/plain", content.Length, content, true, false, new(ContentClassification.Unclassified)), f.User));
            else Assert.NotNull(await provider.GetRequiredService<ReportExportService>().ProcessAsync(
                new(f.Export, f.Tenant, f.Report, f.User, "synthetic@example.invalid", f.Lease, 1)));
        }
        if (failSuccessAudit) await Assert.ThrowsAsync<AuditWriteException>(Write); else await Write();
        var intent = await f.IntentAsync();
        Assert.Equal(failSuccessAudit ? ObjectWriteIntentState.CleanupPending : ObjectWriteIntentState.Committed, intent.State);
        Assert.Equal(1, f.Storage.Writes); Assert.Equal(0, f.Storage.Deletes);
        Assert.Equal(!failSuccessAudit, await f.HasPublicationAsync());
        Assert.Equal(!failSuccessAudit, await f.Db.AuditLogEntries.AnyAsync(x => x.Action == f.SuccessAction));
        if (!failSuccessAudit) Assert.True(await f.LinkedAsync(intent.Id));
    }

    [PostgresFact]
    public async Task Contract_metadata_only_pending_locator_retains_legacy_null_intent_behavior()
    {
        await using var f = await Fixture.CreateAsync("Contract");
        var repository = new EfContractRepository(f.Db, new TenantContext(f.Tenant, f.User));
        Assert.NotNull(await repository.CreateDocumentMetadataAsync(f.Contract,
            new(ContractDocumentType.Other, "synthetic.txt", "text/plain", 5, false, new(ContentClassification.Unclassified)), f.User, NoCuiNotice.CurrentVersion));
        var document = await f.Db.Set<ContractDocumentEntity>().SingleAsync();
        Assert.StartsWith("pending://", document.StorageUri);
        Assert.Null(document.ObjectWriteIntentId); Assert.Null(document.ObjectWriteIntentTenantId);
        Assert.Equal(0, f.Storage.Writes); Assert.Empty(await f.Db.Set<ObjectWriteIntentEntity>().ToArrayAsync());
    }

    [PostgresFact]
    public async Task Null_after_saved_business_change_rolls_back_business_and_success_audit()
    {
        await using var f = await Fixture.CreateAsync();
        var result = await f.Coordinator.ExecuteAsync<Published>(f.Request(), f.User, f.Kind, f.Resource, async (_, token) =>
        {
            var saved = await f.CreateBusinessAsync(token);
            await f.Audit.WriteAsync(f.Tenant, f.User, AuditAction.Uploaded, "EvidenceFileVersion", saved.Id.ToString(),
                "Synthetic premature success before unavailable publication.", cancellationToken: token);
            return null;
        });
        Assert.Null(result);
        Assert.False(await f.HasPublicationAsync());
        Assert.False(await f.Db.AuditLogEntries.AnyAsync(x => x.Action == AuditAction.Uploaded));
        Assert.Equal(ObjectWriteIntentState.CleanupPending, (await f.IntentAsync()).State);
        Assert.Equal(0, f.Storage.Deletes);
    }

    private sealed class PostgresProviderFactAttribute : FactAttribute
    {
        public PostgresProviderFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GCCS_TEST_POSTGRES_CONNECTION")) ||
                string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ConnectionStrings__AzureStorage")) ||
                string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GCCS_INTENT_PROVIDER_CONTAINER")))
                Skip = "Explicit retained PostgreSQL and loopback object-storage configuration is required.";
        }
    }

    private sealed class PostgresTheoryAttribute : TheoryAttribute
    {
        public PostgresTheoryAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GCCS_TEST_POSTGRES_CONNECTION")))
                Skip = "A retained PostgreSQL test connection is required.";
        }
    }

    private sealed record Published(Guid Id);
    private sealed class Fixture : IAsyncDisposable
    {
        public required DbContextOptions<GccsDbContext> Options { get; init; }
        public required GccsDbContext Db { get; init; }
        public required string Kind { get; init; }
        public Guid Tenant { get; } = Guid.NewGuid(); public Guid User { get; } = Guid.NewGuid();
        public Guid Evidence { get; } = Guid.NewGuid(); public Guid Contract { get; } = Guid.NewGuid();
        public Guid Report { get; } = Guid.NewGuid(); public Guid Export { get; } = Guid.NewGuid();
        public Guid Lease { get; } = Guid.NewGuid();
        public string ObjectName { get; } = "synthetic/" + Guid.NewGuid().ToString("N") + "/file.txt";
        public Guid Resource => Kind == "Contract" ? Contract : Kind == "ReportExport" ? Export : Evidence;
        public Guid? ResourceLease => Kind == "ReportExport" ? Lease : null;
        public ObjectStorageContainer Container => Kind == "Contract" ? ObjectStorageContainer.ContractDocuments :
            Kind == "ReportExport" ? ObjectStorageContainer.Reports : ObjectStorageContainer.Evidence;
        public string SuccessEntityType => Kind == "Contract" ? "ContractDocument" : Kind == "ReportExport" ? "ReportExport" : "EvidenceFileVersion";
        public AuditAction SuccessAction => Kind == "ReportExport" ? AuditAction.Exported : AuditAction.Uploaded;
        public FailureInterceptor Failure { get; init; } = new();
        public CommitFailureInterceptor CommitFailure { get; init; } = new();
        public RetainedStorage Storage { get; } = new();
        public IApplicationTransaction Transaction => new EfApplicationTransaction(new ServiceCollection().AddSingleton(Db).BuildServiceProvider());
        public IAuditEventWriter Audit => new EfAuditEventWriter(Db, new RequestMetadata());
        public EfObjectCleanupQueue Queue => new(Db, Storage, Audit);
        public EfObjectWriteIntentRepository Repository => new(Db, Transaction, Audit, Queue);
        public DurableObjectWriteCoordinator Coordinator => new(Repository, Storage);
        public EfObjectWriteIntentRepository NewRepository(GccsDbContext db)
        {
            var audit = new EfAuditEventWriter(db, new RequestMetadata());
            return new(db, new EfApplicationTransaction(new ServiceCollection().AddSingleton(db).BuildServiceProvider()), audit,
                new EfObjectCleanupQueue(db, Storage, audit));
        }
        public ObjectStorageWriteRequest Request() => new(Tenant, Container, ObjectName, new MemoryStream(Encoding.UTF8.GetBytes("synthetic-private-bytes")), "text/plain");
        public Task<Published?> ExecuteAsync() => Coordinator.ExecuteAsync(Request(), User, Kind, Resource, async (_, token) =>
        {
            var result = await CreateBusinessAsync(token);
            await Audit.WriteAsync(Tenant, User, SuccessAction, SuccessEntityType, result.Id.ToString(), "Synthetic publication.", cancellationToken: token);
            return result;
        }, resourceLeaseId: ResourceLease);
        public async Task<Published> CreateBusinessAsync(CancellationToken token = default)
        {
            var id = Guid.NewGuid();
            if (Kind == "EvidenceItem") Db.EvidenceFileVersions.Add(new EvidenceFileVersionEntity { Id = id, EvidenceItemId = Evidence,
                VersionNumber = 1, FileName = "synthetic.txt", ContentType = "text/plain", SizeBytes = 23, StorageUri = ObjectName,
                ValidationStatus = "accepted", MalwareScanStatus = "clean", UploadedAt = DateTimeOffset.UtcNow, UploadedByUserId = User });
            else if (Kind == "Contract") Db.Set<ContractDocumentEntity>().Add(new ContractDocumentEntity { Id = id, ContractId = Contract,
                FileName = "synthetic.txt", ContentType = "text/plain", SizeBytes = 23, StorageUri = ObjectName,
                ValidationStatus = "accepted", MalwareScanStatus = "clean", NoticeVersion = "synthetic", UploadedAt = DateTimeOffset.UtcNow, UploadedByUserId = User });
            else
            {
                var export = await Db.ReportExports.SingleAsync(x => x.Id == Export, token);
                export.ObjectName = ObjectName; export.Status = ReportExportStatus.Ready; export.ProcessingLeaseId = null; export.ProcessingLeaseUntil = null;
                id = export.Id;
            }
            await Db.SaveChangesAsync(token); return new(id);
        }
        public Task<bool> HasPublicationAsync() => Kind == "Contract" ? Db.Set<ContractDocumentEntity>().AnyAsync() :
            Kind == "ReportExport" ? Db.ReportExports.AnyAsync(x => x.Status == ReportExportStatus.Ready) : Db.EvidenceFileVersions.AnyAsync();
        public Task<bool> LinkedAsync(Guid intent) => Kind == "Contract" ? Db.Set<ContractDocumentEntity>().AnyAsync(x => x.ObjectWriteIntentId == intent && x.ObjectWriteIntentTenantId == Tenant) :
            Kind == "ReportExport" ? Db.ReportExports.AnyAsync(x => x.ObjectWriteIntentId == intent && x.TenantId == Tenant) :
            Db.EvidenceFileVersions.AnyAsync(x => x.ObjectWriteIntentId == intent && x.ObjectWriteIntentTenantId == Tenant);
        public async Task<ObjectWriteIntentEntity> IntentAsync()
        {
            Db.ChangeTracker.Clear(); return await Db.Set<ObjectWriteIntentEntity>().SingleAsync();
        }
        public ObjectCleanupEntity Job() => new() { Id = Guid.NewGuid(), TenantId = Tenant, ActorUserId = User, Container = Container,
            ObjectName = ObjectName, CreatedAt = DateTimeOffset.UtcNow, NextAttemptAt = DateTimeOffset.UtcNow };
        public async Task<ObjectWriteIntentEntity> SeedIntentAndJobAsync(ObjectWriteIntentState state)
        {
            var handle = await Repository.PrepareAsync(Request(), User, Kind, Resource, ResourceLease, default);
            var intent = await Db.Set<ObjectWriteIntentEntity>().SingleAsync();
            intent.State = state; intent.ETag = "\"fixture-version\"";
            // Isolate assertion of cleanup atomicity from fixture-creation audit rows.
            var job = Job(); job.ObjectWriteIntentId = handle.Id; job.ExpectedETag = intent.ETag;
            Db.Set<ObjectCleanupEntity>().Add(job); await Db.SaveChangesAsync();
            return intent;
        }
        public static TenantEntity TenantEntity(Guid id) => new() { Id = id, Name = "Synthetic retained tenant", Status = TenantStatus.Active, DataPosture = TenantDataPosture.NoCui, CreatedAt = DateTimeOffset.UtcNow };
        public static async Task<Fixture> CreateAsync(string kind = "EvidenceItem", bool migrate = false)
        {
            var builder = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("GCCS_TEST_POSTGRES_CONNECTION")!);
            var name = "s397_intents_" + Guid.NewGuid().ToString("N");
            await using (var admin = new NpgsqlConnection(builder.ConnectionString))
            {
                await admin.OpenAsync(); await using var create = new NpgsqlCommand($"CREATE DATABASE \"{name}\"", admin); await create.ExecuteNonQueryAsync();
            }
            builder.Database = name;
            var failure = new FailureInterceptor(); var commitFailure = new CommitFailureInterceptor();
            var options = new DbContextOptionsBuilder<GccsDbContext>().UseGccsPostgres(builder.ConnectionString).Options;
            var db = new GccsDbContext(new DbContextOptionsBuilder<GccsDbContext>().UseGccsPostgres(builder.ConnectionString)
                .AddInterceptors(failure, commitFailure).Options);
            var fixture = new Fixture { Db = db, Options = options, Kind = kind, Failure = failure, CommitFailure = commitFailure };
            if (migrate)
            {
                var migrations = db.GetService<IMigrator>();
                var previous = db.Database.GetMigrations().Last(x => x != "20261018171852_AddDurableObjectWriteIntents");
                await migrations.MigrateAsync(previous);
                // Structural precheck: fresh prior-schema DB; no nullable link columns or intent table yet.
                await fixture.InsertLegacyAsync(TenantEntity(fixture.Tenant), "tenants");
                await fixture.InsertLegacyAsync(new EvidenceItemEntity { Id = fixture.Evidence, TenantId = fixture.Tenant,
                    Name = "Synthetic legacy", Description = "Synthetic", Type = EvidenceType.Other, OwnerFunction = "Synthetic",
                    Status = EvidenceStatus.InReview, CreatedAt = DateTimeOffset.UtcNow }, "evidence_items");
                await fixture.InsertLegacyAsync(new ContractEntity { Id = fixture.Contract, TenantId = fixture.Tenant,
                    ContractNumber = "SYNTHETIC-LEGACY", Title = "Synthetic", AgencyOrPrimeName = "Synthetic",
                    Relationship = ContractorRelationship.Prime, Kind = ContractKind.FixedPrice, Status = ContractStatus.Active,
                    PeriodOfPerformanceStart = new(2026, 1, 1), PeriodOfPerformanceEnd = new(2027, 1, 1), PlaceOfPerformance = "Synthetic",
                    Description = "Synthetic", DataHandlingPosture = DataHandlingPosture.FciOnly, CreatedAt = DateTimeOffset.UtcNow }, "contracts");
                await fixture.InsertLegacyAsync(new ReportEntity { Id = fixture.Report, TenantId = fixture.Tenant,
                    Type = ReportType.ComplianceStatus, Status = ReportStatus.Complete, Title = "Synthetic", GeneratedAt = DateTimeOffset.UtcNow,
                    GeneratedByUserId = fixture.User, CreatedAt = DateTimeOffset.UtcNow }, "reports");
                await fixture.InsertLegacyAsync(new EvidenceFileVersionEntity { Id = Guid.NewGuid(), EvidenceItemId = fixture.Evidence,
                    VersionNumber = 1, FileName = "legacy-evidence.txt", ContentType = "text/plain", StorageUri = "legacy/evidence.txt",
                    ValidationStatus = "accepted", MalwareScanStatus = "clean", UploadedAt = DateTimeOffset.UtcNow, UploadedByUserId = fixture.User }, "evidence_file_versions");
                await fixture.InsertLegacyAsync(new ContractDocumentEntity { Id = Guid.NewGuid(), ContractId = fixture.Contract,
                    FileName = "legacy-contract.txt", ContentType = "text/plain", StorageUri = "pending://legacy/synthetic",
                    UploadedAt = DateTimeOffset.UtcNow, UploadedByUserId = fixture.User }, "contract_documents");
                await fixture.InsertLegacyAsync(new ReportExportEntity { Id = fixture.Export, TenantId = fixture.Tenant,
                    ReportId = fixture.Report, Status = ReportExportStatus.Processing, RequestedAt = DateTimeOffset.UtcNow,
                    RequestedByUserId = fixture.User, ProcessingLeaseId = fixture.Lease,
                    ProcessingLeaseUntil = DateTimeOffset.UtcNow.AddMinutes(5), CreatedAt = DateTimeOffset.UtcNow }, "report_exports");
                await fixture.InsertLegacyAsync(fixture.Job(), "object_cleanup");
                await migrations.MigrateAsync();
                return fixture;
            }
            await db.Database.EnsureCreatedAsync();
            await fixture.SeedParentsAsync(); return fixture;
        }
        private async Task InsertLegacyAsync(object entity, string table)
        {
            // Use only columns actually present in the prior schema; parameterize every value.
            await Db.Database.OpenConnectionAsync();
            var present = new HashSet<string>(StringComparer.Ordinal);
            try
            {
                await using var command = Db.Database.GetDbConnection().CreateCommand();
                command.CommandText = "SELECT column_name FROM information_schema.columns WHERE table_schema = 'gccs' AND table_name = @table";
                var parameter = command.CreateParameter(); parameter.ParameterName = "table"; parameter.Value = table; command.Parameters.Add(parameter);
                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync()) present.Add(reader.GetString(0));
            }
            finally { await Db.Database.CloseConnectionAsync(); }
            Assert.DoesNotContain("object_write_intent_id", present);
            var store = Microsoft.EntityFrameworkCore.Metadata.StoreObjectIdentifier.Table(table, "gccs");
            var properties = Db.Entry(entity).Properties.Where(x => present.Contains(x.Metadata.GetColumnName(store)!) &&
                x.Metadata.GetComputedColumnSql() is null && x.Metadata.ValueGenerated != Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAddOrUpdate).ToArray();
            var values = properties.Select(x => x.Metadata.GetTypeMapping().Converter is { } converter
                ? converter.ConvertToProvider(x.CurrentValue) : x.CurrentValue).ToArray();
            var columns = string.Join(",", properties.Select(x => "\"" + x.Metadata.GetColumnName(store) + "\""));
            var parameters = string.Join(",", properties.Select((x, i) => "{" + i + "}::" + x.Metadata.GetColumnType()));
            // Identifiers and casts come only from EF metadata and the actual prior schema above.
            var insertSql = "INSERT INTO gccs.\"" + table + "\" (" + columns + ") VALUES (" + parameters + ")";
            await Db.Database.ExecuteSqlRawAsync(insertSql, values!);
        }
        private async Task SeedParentsAsync(bool skipTenantAndEvidence = false)
        {
            if (!skipTenantAndEvidence)
            {
                Db.Tenants.Add(TenantEntity(Tenant));
                Db.EvidenceItems.Add(new EvidenceItemEntity { Id = Evidence, TenantId = Tenant, Name = "Synthetic retained evidence", Description = "Synthetic", Type = EvidenceType.Other, OwnerFunction = "Synthetic", Status = EvidenceStatus.InReview, CreatedAt = DateTimeOffset.UtcNow });
            }
            Db.Contracts.Add(new ContractEntity { Id = Contract, TenantId = Tenant, ContractNumber = "SYNTHETIC-" + Contract.ToString("N"), Title = "Synthetic", AgencyOrPrimeName = "Synthetic", Relationship = ContractorRelationship.Prime, Kind = ContractKind.FixedPrice, Status = ContractStatus.Active, PeriodOfPerformanceStart = new(2026, 1, 1), PeriodOfPerformanceEnd = new(2027, 1, 1), PlaceOfPerformance = "Synthetic", Description = "Synthetic", DataHandlingPosture = DataHandlingPosture.FciOnly, CreatedAt = DateTimeOffset.UtcNow });
            Db.Reports.Add(new ReportEntity { Id = Report, TenantId = Tenant, Type = ReportType.ComplianceStatus, Status = ReportStatus.Complete, Title = "Synthetic", GeneratedAt = DateTimeOffset.UtcNow, GeneratedByUserId = User, CreatedAt = DateTimeOffset.UtcNow });
            Db.ReportExports.Add(new ReportExportEntity { Id = Export, TenantId = Tenant, ReportId = Report, Status = ReportExportStatus.Processing, RequestedAt = DateTimeOffset.UtcNow, RequestedByUserId = User, ProcessingLeaseId = Lease, ProcessingLeaseUntil = DateTimeOffset.UtcNow.AddMinutes(5), CreatedAt = DateTimeOffset.UtcNow });
            await Db.SaveChangesAsync();
        }
        public async Task SeedPolicyPrerequisitesAsync()
        {
            Db.NoCuiAcknowledgements.Add(new NoCuiAcknowledgementEntity { Id = Guid.NewGuid(), TenantId = Tenant, UserId = User,
                NoticeVersion = NoCuiNotice.CurrentVersion, NoticeCopy = NoCuiNotice.Copy, AcknowledgedAt = DateTimeOffset.UtcNow,
                CreatedAt = DateTimeOffset.UtcNow, CreatedByUserId = User });
            foreach (var workflow in new[] { "EvidenceUpload", "ContractUpload", "ReportGeneration" })
                Db.DataHandlingNoticeAcknowledgements.Add(new DataHandlingNoticeAcknowledgementEntity { Id = Guid.NewGuid(),
                    TenantId = Tenant, UserId = User, Mode = TenantDataPosture.NoCui, WorkflowContext = workflow,
                    NoticeId = "no-cui-general", NoticeVersion = "2026.06.phase1a", AcknowledgedAt = DateTimeOffset.UtcNow });
            await Db.SaveChangesAsync();
        }
        public ServiceProvider ServiceProvider()
        {
            var services = new ServiceCollection();
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
                { ["ConnectionStrings:GccsDatabase"] = Db.Database.GetConnectionString() }).Build();
            services.AddLogging(); services.AddGccsInfrastructure(config);
            var root = new DirectoryInfo(AppContext.BaseDirectory);
            while (!Directory.Exists(Path.Combine(root.FullName, "packages", "compliance-content")))
                root = root.Parent ?? throw new InvalidOperationException("The source-backed notice package is unavailable.");
            services.AddSingleton(new DataHandlingNoticePackage(Path.Combine(root.FullName, "packages", "compliance-content")));
            services.AddScoped<CurrentDataHandlingNoticeService>();
            services.AddScoped<ICurrentDataHandlingNoticeGuard>(p => p.GetRequiredService<CurrentDataHandlingNoticeService>());
            services.AddSingleton(Db); services.AddSingleton<ICurrentTenantContext>(new TenantContext(Tenant, User));
            services.AddSingleton<IAuditRequestMetadata>(new RequestMetadata());
            services.AddSingleton<IObjectStorageService>(Storage); services.AddSingleton<IMalwareScanner>(new CleanScanner());
            services.AddSingleton<IReportPdfRenderer>(new SyntheticPdfRenderer());
            return services.BuildServiceProvider();
        }
        public ValueTask DisposeAsync() => Db.DisposeAsync(); // Connection disposal retains every database and row.
    }

    private sealed class FailureInterceptor : SaveChangesInterceptor
    {
        public string? EntityType { get; set; }
        public bool FailAllSaves { get; set; }
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData data,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (FailAllSaves || (EntityType is not null && data.Context!.ChangeTracker.Entries<AuditLogEntryEntity>()
                .Any(x => x.State == EntityState.Added && x.Entity.EntityType == EntityType)))
                throw new InvalidOperationException("Synthetic persistence fault.");
            return ValueTask.FromResult(result);
        }
    }
    private sealed class CommitFailureInterceptor : DbTransactionInterceptor
    {
        public Func<bool>? ThrowWhenCommitted { get; set; }
        public override Task TransactionCommittedAsync(System.Data.Common.DbTransaction transaction, TransactionEndEventData eventData,
            CancellationToken cancellationToken = default)
        {
            if (ThrowWhenCommitted?.Invoke() == true)
            {
                ThrowWhenCommitted = null;
                throw new InvalidOperationException("Synthetic lost commit acknowledgement.");
            }
            return Task.CompletedTask;
        }
    }
    private sealed class RequestMetadata : IAuditRequestMetadata
    {
        public string IpAddress => ""; public string UserAgent => "Synthetic retained boundary tests"; public string CorrelationId => "";
    }
    private sealed class TenantContext(Guid tenant, Guid user) : ICurrentTenantContext
    {
        public Guid TenantId => tenant; public Guid UserId => user; public string UserEmail => "synthetic@example.invalid";
    }
    private sealed class CleanScanner : IMalwareScanner
    {
        public Task<MalwareScanResult> ScanAsync(MalwareScanRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(MalwareScanResult.Clean("synthetic-clean-scanner", "synthetic"));
    }
    private sealed class SyntheticPdfRenderer : IReportPdfRenderer
    {
        public RenderedReportPdf Render(ReportArtifactDetailDto report) => new(Encoding.UTF8.GetBytes("Synthetic PDF fixture"), "application/pdf");
    }
    private sealed class LostAcknowledgementStorage(IObjectStorageService inner) : IObjectStorageService
    {
        public async Task<ObjectStorageWriteResult> UploadAsync(ObjectStorageWriteRequest request, CancellationToken token = default)
        { await inner.UploadAsync(request, token); throw new IOException("Synthetic lost acknowledgement after real provider completion."); }
        public Task<ObjectStorageReadResult?> OpenReadAsync(ObjectStorageReadRequest request, CancellationToken token = default) => inner.OpenReadAsync(request, token);
        public Task<bool> ExistsAsync(ObjectStorageReadRequest request, CancellationToken token = default) => inner.ExistsAsync(request, token);
        public Task<bool> DeleteAsync(ObjectStorageReadRequest request, CancellationToken token = default) => throw new InvalidOperationException("Real deletion is not authorized by this retained test.");
    }
    private sealed class RetainedStorage : IObjectStorageService
    {
        public int Writes { get; private set; } public int Deletes { get; private set; }
        public bool Retained { get; private set; } public bool RetainBeforeCallback { get; set; } = true;
        public string? ETag { get; set; } = "\"fixture-version\""; public bool FailDelete { get; set; }
        public ObjectStorageReadRequest? LastDelete { get; private set; }
        public Func<ObjectStorageWriteRequest, CancellationToken, Task>? OnUpload { get; set; }
        public async Task<ObjectStorageWriteResult> UploadAsync(ObjectStorageWriteRequest request, CancellationToken cancellationToken = default)
        {
            Assert.True(request.CreateOnly); Writes++;
            if (RetainBeforeCallback) Retained = true;
            if (OnUpload is not null) await OnUpload(request, cancellationToken);
            Retained = true;
            return new(request.Container, ObjectStorageNames.BuildTenantBlobName(request.TenantId, request.ObjectName), new Uri("https://synthetic.invalid/retained"), ETag, DateTimeOffset.UtcNow);
        }
        public Task<ObjectStorageReadResult?> OpenReadAsync(ObjectStorageReadRequest request, CancellationToken cancellationToken = default) => Task.FromResult<ObjectStorageReadResult?>(null);
        public Task<bool> ExistsAsync(ObjectStorageReadRequest request, CancellationToken cancellationToken = default) => Task.FromResult(Retained);
        public Task<bool> DeleteAsync(ObjectStorageReadRequest request, CancellationToken cancellationToken = default)
        {
            LastDelete = request; Deletes++;
            if (FailDelete) throw new IOException("Synthetic private provider failure");
            return Task.FromResult(false); // Non-disposing fake proves queue transitions; no object is removed.
        }
    }
}
