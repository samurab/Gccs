using System.Data.Common;
using System.Text.Json;
using Gccs.Application.Marketing;
using Gccs.Infrastructure.Marketing;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using Xunit;

namespace Gccs.Api.Tests;

[CollectionDefinition("Demo request retention", DisableParallelization = true)]
public sealed class DemoRequestRetentionCollection { }

[Collection("Demo request retention")]
public sealed class DemoRequestRetentionPostgresTests
{
    // Retained fixtures stay newer than legacy tests' 365-day purge boundary.
    private static readonly DateTimeOffset Cutoff = new(DateTimeOffset.UtcNow.Date.AddDays(-2), TimeSpan.Zero);

    [PostgresFact]
    [Trait("Category", "PostgresIntegration")]
    public async Task Retention_preserves_immutable_history_and_only_purges_expired_terminal_requests()
    {
        var options = Options();
        await using var db = new GccsDbContext(options);
        await PostgresTestDatabase.MigrateAsync(db);
        var appointmentOnly = await SeedAsync(db, appointmentHistory: true);
        var responseOnly = await SeedAsync(db, responseHistory: true);
        var both = await SeedAsync(db, appointmentHistory: true, responseHistory: true);
        var sent = await SeedAsync(db);
        var captured = await SeedAsync(db, status: "Captured");
        var failed = await SeedAsync(db, status: "Failed");
        var recent = await SeedAsync(db, receivedAt: Cutoff.AddDays(1));
        var atCutoff = await SeedAsync(db, receivedAt: Cutoff);
        var withoutDelivery = await SeedAsync(db, status: null);
        var nonterminal = new List<Scenario>();
        foreach (var status in new[] { "Queued", "RetryScheduled", "Processing", "Unknown" })
            nonterminal.Add(await SeedAsync(db, status: status));
        var mixed = await SeedAsync(db);
        db.DemoRequestDeliveries.Add(new DemoRequestDeliveryEntity
        {
            Id = Guid.NewGuid(), DemoRequestId = mixed.RequestId,
            DeliveryKind = "RequesterAcknowledgement", Status = "Queued",
            CreatedAt = Cutoff, UpdatedAt = Cutoff
        });
        await db.SaveChangesAsync();
        var preserved = new[] { appointmentOnly, responseOnly, both, recent, atCutoff, withoutDelivery, mixed }
            .Concat(nonterminal).ToArray();
        var before = await SnapshotAsync(db, preserved);
        var requestCount = await db.DemoRequests.CountAsync();

        var deleted = await new EfDemoRequestRepository(db).DeleteExpiredAsync(Cutoff);

        Assert.Equal(requestCount - await db.DemoRequests.CountAsync(), deleted);
        Assert.True(deleted >= 3);
        Assert.Equal(before, await SnapshotAsync(db, preserved));
        foreach (var scenario in new[] { sent, captured, failed })
        {
            Assert.False(await db.DemoRequests.AnyAsync(item => item.Id == scenario.RequestId));
            Assert.False(await db.DemoRequestDeliveries.AnyAsync(item => item.DemoRequestId == scenario.RequestId));
            Assert.False(await db.DemoAppointments.AnyAsync(item => item.DemoRequestId == scenario.RequestId));
            Assert.False(await db.DemoFollowUpRequests.AnyAsync(item => item.DemoRequestId == scenario.RequestId));
        }
        Assert.Equal(0, await new EfDemoRequestRepository(db).DeleteExpiredAsync(Cutoff));
        Assert.Equal(before, await SnapshotAsync(db, preserved));
    }

    [PostgresFact]
    [Trait("Category", "PostgresIntegration")]
    public async Task Retention_preserves_valid_pending_follow_ups_until_the_expiry_boundary()
    {
        var now = Cutoff.AddDays(2);
        var clock = new FixedTimeProvider(now);
        await using var db = new GccsDbContext(Options());
        await PostgresTestDatabase.MigrateAsync(db);
        var active = new List<Scenario>();
        foreach (var status in new[] { "Sent", "Captured", "Failed" })
            active.Add(await SeedAsync(db, status: status,
                followUpStatus: DemoFollowUpCatalog.Pending, followUpExpiresAt: now.AddMilliseconds(1)));
        var atExpiry = await SeedAsync(db,
            followUpStatus: DemoFollowUpCatalog.Pending, followUpExpiresAt: now);
        var pastExpiry = await SeedAsync(db,
            followUpStatus: DemoFollowUpCatalog.Pending, followUpExpiresAt: now.AddMilliseconds(-1));
        var alreadyExpired = await SeedAsync(db,
            followUpStatus: DemoFollowUpCatalog.Expired, followUpExpiresAt: now.AddDays(1));
        var before = await SnapshotAsync(db, active.ToArray());
        var repository = new EfDemoRequestRepository(db, timeProvider: clock);

        await repository.DeleteExpiredAsync(Cutoff);

        Assert.Equal(before, await SnapshotAsync(db, active.ToArray()));
        foreach (var expired in new[] { atExpiry, pastExpiry, alreadyExpired })
            Assert.False(await db.DemoRequests.AnyAsync(item => item.Id == expired.RequestId));
        Assert.Equal(0, await repository.DeleteExpiredAsync(Cutoff));
        Assert.Equal(before, await SnapshotAsync(db, active.ToArray()));

        clock.UtcNow = now.AddMilliseconds(1);
        await repository.DeleteExpiredAsync(Cutoff);

        foreach (var expired in active)
            Assert.False(await db.DemoRequests.AnyAsync(item => item.Id == expired.RequestId));
    }

    [PostgresFact]
    [Trait("Category", "PostgresIntegration")]
    public async Task Retention_racing_pending_follow_up_creation_or_renewal_rolls_back_child_deletes()
    {
        var now = Cutoff.AddDays(2);
        var options = Options();
        await using var db = new GccsDbContext(options);
        await PostgresTestDatabase.MigrateAsync(db);
        foreach (var create in new[] { true, false })
        {
            var scenario = await SeedAsync(db);
            string? committed = null;
            var interceptor = new RetentionDeleteInterceptor(async () =>
            {
                await using var writer = new GccsDbContext(options);
                if (create)
                    writer.DemoFollowUpRequests.Add(new DemoFollowUpRequestEntity
                    {
                        Id = Guid.NewGuid(), DemoRequestId = scenario.RequestId,
                        TokenHash = Convert.ToHexString(Guid.NewGuid().ToByteArray()).PadRight(64, '0'),
                        Status = DemoFollowUpCatalog.Pending,
                        TemplateVersion = DemoFollowUpCatalog.TemplateVersion,
                        NoCuiNoticeVersion = DemoFollowUpCatalog.NoCuiNoticeVersion,
                        ExpiresAt = now.AddDays(1), RequestedAt = now, UpdatedAt = now
                    });
                else
                {
                    var followUp = await writer.DemoFollowUpRequests.SingleAsync(item => item.Id == scenario.FollowUpId);
                    followUp.Status = DemoFollowUpCatalog.Pending;
                    followUp.ExpiresAt = now.AddDays(1);
                    followUp.UpdatedAt = now;
                }
                await writer.SaveChangesAsync();
                committed = await SnapshotAsync(writer, [scenario]);
            });
            await using (var purge = new GccsDbContext(Options(interceptor)))
            {
                var exception = await Assert.ThrowsAnyAsync<Exception>(() =>
                    new EfDemoRequestRepository(purge, timeProvider: new FixedTimeProvider(now))
                        .DeleteExpiredAsync(Cutoff));
                var postgresException = exception is PostgresException postgres
                    ? postgres
                    : Assert.IsType<PostgresException>(Assert.IsType<InvalidOperationException>(exception).InnerException);
                Assert.Contains(postgresException.SqlState, new[]
                {
                    PostgresErrorCodes.SerializationFailure, PostgresErrorCodes.ForeignKeyViolation
                });
            }
            Assert.True(interceptor.Invoked);
            Assert.NotNull(committed);
            Assert.Equal(committed, await SnapshotAsync(db, [scenario]));

            await new EfDemoRequestRepository(db, timeProvider: new FixedTimeProvider(now)).DeleteExpiredAsync(Cutoff);

            Assert.Equal(committed, await SnapshotAsync(db, [scenario]));
        }
    }

    [PostgresFact]
    [Trait("Category", "PostgresIntegration")]
    public async Task Retention_cancellation_after_child_delete_rolls_back_the_complete_request()
    {
        var options = Options();
        await using var db = new GccsDbContext(options);
        await PostgresTestDatabase.MigrateAsync(db);
        var scenario = await SeedAsync(db);
        var before = await SnapshotAsync(db, [scenario]);
        using var cancellation = new CancellationTokenSource();
        var interceptor = new RetentionDeleteInterceptor(() =>
        {
            cancellation.Cancel();
            return Task.CompletedTask;
        }, afterDelete: true);
        await using (var purge = new GccsDbContext(Options(interceptor)))
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                new EfDemoRequestRepository(purge).DeleteExpiredAsync(Cutoff, cancellation.Token));

        Assert.True(interceptor.Invoked);
        Assert.Equal(before, await SnapshotAsync(db, [scenario]));
    }

    [PostgresFact]
    [Trait("Category", "PostgresIntegration")]
    public async Task Retention_failure_after_child_delete_rolls_back_the_complete_request()
    {
        var options = Options();
        await using var db = new GccsDbContext(options);
        await PostgresTestDatabase.MigrateAsync(db);
        var scenario = await SeedAsync(db);
        var before = await SnapshotAsync(db, [scenario]);
        var interceptor = new RetentionDeleteInterceptor(
            () => throw new InvalidOperationException("Injected retention failure."), afterDelete: true);
        await using (var purge = new GccsDbContext(Options(interceptor)))
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                new EfDemoRequestRepository(purge).DeleteExpiredAsync(Cutoff));

        Assert.True(interceptor.Invoked);
        Assert.Equal(before, await SnapshotAsync(db, [scenario]));
    }

    [PostgresFact]
    [Trait("Category", "PostgresIntegration")]
    public async Task Retention_racing_committed_history_preserves_history_and_rolls_back_child_deletes()
    {
        var options = Options();
        await using var db = new GccsDbContext(options);
        await PostgresTestDatabase.MigrateAsync(db);
        foreach (var appointmentHistory in new[] { true, false })
        {
            var scenario = await SeedAsync(db);
            string? committed = null;
            var interceptor = new RetentionDeleteInterceptor(async () =>
            {
                await using var writer = new GccsDbContext(options);
                AddHistory(writer, scenario, appointmentHistory, !appointmentHistory);
                await writer.SaveChangesAsync();
                committed = await SnapshotAsync(writer, [scenario]);
            });
            await using (var purge = new GccsDbContext(Options(interceptor)))
            {
                var exception = await Assert.ThrowsAsync<PostgresException>(() =>
                    new EfDemoRequestRepository(purge).DeleteExpiredAsync(Cutoff));
                Assert.Contains(exception.SqlState, new[]
                {
                    PostgresErrorCodes.SerializationFailure, PostgresErrorCodes.ForeignKeyViolation
                });
            }
            Assert.True(interceptor.Invoked);
            Assert.NotNull(committed);
            Assert.Equal(committed, await SnapshotAsync(db, [scenario]));
        }
    }

    [PostgresFact]
    [Trait("Category", "PostgresIntegration")]
    public async Task Retention_racing_queued_delivery_preserves_pending_work_and_rolls_back_child_deletes()
    {
        var options = Options();
        await using var db = new GccsDbContext(options);
        await PostgresTestDatabase.MigrateAsync(db);
        var scenario = await SeedAsync(db);
        var queuedDeliveryId = Guid.NewGuid();
        string? committed = null;
        var interceptor = new RetentionDeleteInterceptor(async () =>
        {
            await using var writer = new GccsDbContext(options);
            writer.DemoRequestDeliveries.Add(new DemoRequestDeliveryEntity
            {
                Id = queuedDeliveryId, DemoRequestId = scenario.RequestId,
                DeliveryKind = "RequesterAcknowledgement", Status = "Queued",
                CreatedAt = Cutoff, UpdatedAt = Cutoff
            });
            await writer.SaveChangesAsync();
            committed = await SnapshotAsync(writer, [scenario]);
        });
        await using (var purge = new GccsDbContext(Options(interceptor)))
        {
            var exception = await Assert.ThrowsAsync<PostgresException>(() =>
                new EfDemoRequestRepository(purge).DeleteExpiredAsync(Cutoff));
            Assert.Contains(exception.SqlState, new[]
            {
                PostgresErrorCodes.SerializationFailure, PostgresErrorCodes.ForeignKeyViolation
            });
        }
        Assert.True(interceptor.Invoked);
        Assert.NotNull(committed);
        Assert.Equal(committed, await SnapshotAsync(db, [scenario]));
        Assert.Equal("Queued", await db.DemoRequestDeliveries.Where(item => item.Id == queuedDeliveryId)
            .Select(item => item.Status).SingleAsync());

        await new EfDemoRequestRepository(db).DeleteExpiredAsync(Cutoff);

        Assert.Equal(committed, await SnapshotAsync(db, [scenario]));
    }

    private static DbContextOptions<GccsDbContext> Options(IInterceptor? interceptor = null)
    {
        var builder = new DbContextOptionsBuilder<GccsDbContext>()
            .UseGccsPostgres(Environment.GetEnvironmentVariable("GCCS_TEST_POSTGRES_CONNECTION")!);
        if (interceptor is not null) builder.AddInterceptors(interceptor);
        return builder.Options;
    }

    private static async Task<Scenario> SeedAsync(
        GccsDbContext db,
        bool appointmentHistory = false,
        bool responseHistory = false,
        string? status = "Sent",
        DateTimeOffset? receivedAt = null,
        string? followUpStatus = null,
        DateTimeOffset? followUpExpiresAt = null)
    {
        var scenario = new Scenario(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        db.DemoRequests.Add(new DemoRequestEntity
        {
            Id = scenario.RequestId, FirstName = "Synthetic", LastName = "Retention",
            Email = "retention@example.invalid", Company = "Synthetic retention fixture",
            ConsentNoticeVersion = DemoRequestService.ConsentNoticeVersion,
            DeduplicationKey = Convert.ToHexString(Guid.NewGuid().ToByteArray()).PadRight(64, '0'),
            ReceivedAt = receivedAt ?? Cutoff.AddDays(-1)
        });
        db.DemoAppointments.Add(new DemoAppointmentEntity
        {
            Id = scenario.AppointmentId, DemoRequestId = scenario.RequestId,
            Status = "Confirmed", ConfirmedStartAt = Cutoff, ConfirmedEndAt = Cutoff.AddMinutes(30),
            ConfirmedTimeZone = "UTC", DurationMinutes = 30, HostUserId = Guid.NewGuid(),
            MeetingMethod = "ConnectionDetailsToFollow", ConfirmedByUserId = Guid.NewGuid(),
            ConfirmedAt = Cutoff, UpdatedAt = Cutoff
        });
        db.DemoFollowUpRequests.Add(new DemoFollowUpRequestEntity
        {
            Id = scenario.FollowUpId, DemoRequestId = scenario.RequestId,
            TokenHash = Convert.ToHexString(Guid.NewGuid().ToByteArray()).PadRight(64, '0'),
            Status = followUpStatus ?? (responseHistory ? "Responded" : "Expired"),
            TemplateVersion = DemoFollowUpCatalog.TemplateVersion,
            NoCuiNoticeVersion = DemoFollowUpCatalog.NoCuiNoticeVersion,
            ExpiresAt = followUpExpiresAt ?? Cutoff, RequestedAt = Cutoff.AddDays(-1), UpdatedAt = Cutoff
        });
        if (status is not null)
            db.DemoRequestDeliveries.Add(new DemoRequestDeliveryEntity
            {
                Id = Guid.NewGuid(), DemoRequestId = scenario.RequestId, Status = status,
                DemoFollowUpRequestId = scenario.FollowUpId, CreatedAt = Cutoff, UpdatedAt = Cutoff
            });
        AddHistory(db, scenario, appointmentHistory, responseHistory);
        await db.SaveChangesAsync();
        return scenario;
    }

    private static void AddHistory(GccsDbContext db, Scenario scenario, bool appointmentHistory, bool responseHistory)
    {
        if (appointmentHistory)
            db.DemoAppointmentEvents.Add(new DemoAppointmentEventEntity
            {
                Id = Guid.NewGuid(), DemoRequestId = scenario.RequestId, DemoAppointmentId = scenario.AppointmentId,
                EventType = "Confirmed", NewStatus = "Confirmed", ConfirmedStartAt = Cutoff,
                ConfirmedEndAt = Cutoff.AddMinutes(30), ConfirmedTimeZone = "UTC", DurationMinutes = 30,
                HostUserId = Guid.NewGuid(), ActorUserId = Guid.NewGuid(),
                MeetingMethod = "ConnectionDetailsToFollow", OccurredAt = Cutoff,
                IpAddress = "127.0.0.1", UserAgent = "Synthetic retention fixture", CorrelationId = "synthetic"
            });
        if (responseHistory)
            db.DemoFollowUpResponses.Add(new DemoFollowUpResponseEntity
            {
                Id = Guid.NewGuid(), DemoRequestId = scenario.RequestId, DemoFollowUpRequestId = scenario.FollowUpId,
                Goals = "Synthetic goal", Challenges = "Synthetic challenge", NoCuiConfirmed = true,
                NoCuiNoticeVersion = DemoFollowUpCatalog.NoCuiNoticeVersion, SubmittedAt = Cutoff,
                IpAddress = "127.0.0.1", UserAgent = "Synthetic retention fixture", CorrelationId = "synthetic"
            });
    }

    private static async Task<string> SnapshotAsync(GccsDbContext db, Scenario[] scenarios)
    {
        var ids = scenarios.Select(item => item.RequestId).ToArray();
        return JsonSerializer.Serialize(new
        {
            Requests = await db.DemoRequests.AsNoTracking().Where(item => ids.Contains(item.Id)).OrderBy(item => item.Id).ToArrayAsync(),
            Deliveries = await db.DemoRequestDeliveries.AsNoTracking().Where(item => ids.Contains(item.DemoRequestId)).OrderBy(item => item.Id).ToArrayAsync(),
            Appointments = await db.DemoAppointments.AsNoTracking().Where(item => ids.Contains(item.DemoRequestId)).OrderBy(item => item.Id).ToArrayAsync(),
            Events = await db.DemoAppointmentEvents.AsNoTracking().Where(item => ids.Contains(item.DemoRequestId)).OrderBy(item => item.Id).ToArrayAsync(),
            FollowUps = await db.DemoFollowUpRequests.AsNoTracking().Where(item => ids.Contains(item.DemoRequestId)).OrderBy(item => item.Id).ToArrayAsync(),
            Responses = await db.DemoFollowUpResponses.AsNoTracking().Where(item => ids.Contains(item.DemoRequestId)).OrderBy(item => item.Id).ToArrayAsync()
        });
    }

    private sealed record Scenario(Guid RequestId, Guid AppointmentId, Guid FollowUpId);

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset UtcNow { get; set; } = now;
        public override DateTimeOffset GetUtcNow() => UtcNow;
    }

    private sealed class RetentionDeleteInterceptor(Func<Task> action, bool afterDelete = false) : DbCommandInterceptor
    {
        public bool Invoked { get; private set; }

        public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (!afterDelete) await InvokeOnceAsync(command, eventData.Context!);
            return result;
        }

        public override async ValueTask<int> NonQueryExecutedAsync(
            DbCommand command, CommandExecutedEventData eventData, int result,
            CancellationToken cancellationToken = default)
        {
            if (afterDelete) await InvokeOnceAsync(command, eventData.Context!);
            return result;
        }

        private async Task InvokeOnceAsync(DbCommand command, DbContext db)
        {
            if (Invoked) return;
            var entityType = db.Model.FindEntityType(typeof(DemoRequestDeliveryEntity))!;
            var target = db.GetService<ISqlGenerationHelper>().DelimitIdentifier(entityType.GetTableName()!, entityType.GetSchema());
            if (!command.CommandText.StartsWith($"DELETE FROM {target} ", StringComparison.Ordinal)) return;
            Invoked = true;
            await action();
        }
    }
}
