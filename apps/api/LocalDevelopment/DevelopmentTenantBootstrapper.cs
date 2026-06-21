using Gccs.Domain.Common;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Api.LocalDevelopment;

public sealed class DevelopmentTenantBootstrapper(
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    ILogger<DevelopmentTenantBootstrapper> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var developmentAuthEnabled = configuration.GetValue("Security:DevelopmentAuth:Enabled", true);
        if (!developmentAuthEnabled)
        {
            return;
        }

        var tenantIdValue = configuration.GetValue(
            "Security:DevelopmentAuth:DefaultTenantId",
            "11111111-1111-1111-1111-111111111111");
        var userIdValue = configuration.GetValue(
            "Security:DevelopmentAuth:DefaultUserId",
            "22222222-2222-2222-2222-222222222222");

        if (!Guid.TryParse(tenantIdValue, out var tenantId) || !Guid.TryParse(userIdValue, out var userId))
        {
            logger.LogWarning("Development tenant bootstrap skipped because development auth IDs are not valid GUIDs.");
            return;
        }

        try
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();

            if (!await dbContext.Database.CanConnectAsync(cancellationToken))
            {
                logger.LogWarning("Development tenant bootstrap skipped because the database is not reachable.");
                return;
            }

            var now = DateTimeOffset.UtcNow;
            var tenantExists = await dbContext.Tenants.AnyAsync(tenant => tenant.Id == tenantId, cancellationToken);
            if (!tenantExists)
            {
                dbContext.Tenants.Add(new TenantEntity
                {
                    Id = tenantId,
                    Name = "GCCS Development Tenant",
                    Status = TenantStatus.Active,
                    DataPosture = TenantDataPosture.NoCui,
                    TrialEndsAt = null,
                    CreatedAt = now,
                    CreatedByUserId = userId
                });

                await dbContext.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Development tenant bootstrap created default tenant {TenantId}.", tenantId);
            }

            try
            {
                var historyExists = await dbContext.TenantDataHandlingModeHistory
                    .AnyAsync(history => history.TenantId == tenantId, cancellationToken);
                if (historyExists)
                {
                    return;
                }

                dbContext.TenantDataHandlingModeHistory.Add(new TenantDataHandlingModeHistoryEntity
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    PreviousMode = null,
                    NewMode = TenantDataPosture.NoCui,
                    ActorUserId = userId,
                    ChangedAt = now,
                    Reason = "Development tenant bootstrap created the default NoCui tenant.",
                    ApprovalRecordReference = null
                });

                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogWarning(
                    exception,
                    "Development tenant bootstrap created tenant {TenantId}, but skipped mode history seeding.",
                    tenantId);
            }
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Development tenant bootstrap skipped because tenant creation failed.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
