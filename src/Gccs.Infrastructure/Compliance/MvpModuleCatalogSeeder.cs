using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Compliance;

public static class MvpModuleCatalogSeeder
{
    public static async Task SyncAsync(GccsDbContext dbContext, CancellationToken cancellationToken = default)
    {
        if (dbContext.Database.IsRelational())
        {
            var values = string.Join(
                ",\n                    ",
                MvpModuleCatalog.Definitions.Select(definition =>
                    $"('{EscapeSql(definition.Key)}', '{EscapeSql(definition.Name)}', '{EscapeSql(definition.Purpose)}', '{EscapeSql(definition.Status)}')"));

            var sql = $"""
                INSERT INTO gccs.mvp_modules ("key", name, purpose, status)
                VALUES
                    {values}
                ON CONFLICT ("key") DO UPDATE
                SET name = EXCLUDED.name,
                    purpose = EXCLUDED.purpose,
                    status = EXCLUDED.status;
                """;

            await dbContext.Database.ExecuteSqlRawAsync(sql, cancellationToken);
        }
        else
        {
            foreach (var module in MvpModuleCatalog.Definitions)
            {
                var existing = await dbContext.MvpModules.SingleOrDefaultAsync(item => item.Key == module.Key, cancellationToken);
                if (existing is null)
                {
                    dbContext.MvpModules.Add(new MvpModuleEntity
                    {
                        Key = module.Key,
                        Name = module.Name,
                        Purpose = module.Purpose,
                        Status = module.Status
                    });
                }
                else
                {
                    existing.Name = module.Name;
                    existing.Purpose = module.Purpose;
                    existing.Status = module.Status;
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        dbContext.ChangeTracker.Clear();
    }

    private static string EscapeSql(string value) =>
        value.Replace("'", "''", StringComparison.Ordinal);
}
