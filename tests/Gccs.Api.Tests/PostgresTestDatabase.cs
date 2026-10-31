using Gccs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Api.Tests;

internal static class PostgresTestDatabase
{
    internal static void Migrate(GccsDbContext db) => MigrateAsync(db).GetAwaiter().GetResult();

    internal static async Task MigrateAsync(GccsDbContext db)
    {
        // All test hosts/processes sharing this database serialize bootstrap before EF reads history.
        // This is a session lock, not an outer transaction around transactional EF migrations.
        await db.Database.OpenConnectionAsync().ConfigureAwait(false);
        try
        {
            await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_lock(719122020260907)").ConfigureAwait(false);
            try { await db.Database.MigrateAsync().ConfigureAwait(false); }
            finally { await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_unlock(719122020260907)").ConfigureAwait(false); }
        }
        finally { await db.Database.CloseConnectionAsync().ConfigureAwait(false); }
    }
}
