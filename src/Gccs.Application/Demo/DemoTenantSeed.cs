using Gccs.Application.Audit;
using Gccs.Domain.Audit;
using Gccs.Domain.Tenancy;

namespace Gccs.Application.Demo;

public sealed class DemoTenantSeedService(
    IDemoTenantSeedRepository repository,
    IAuditEventWriter auditEventWriter)
{
    public async Task<DemoTenantSeedResult> SeedAsync(
        SyntheticDemoDatasetDefinition dataset,
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var precheck = SyntheticDemoDatasetPrecheck.Validate(dataset);
        if (!precheck.Allowed)
        {
            throw new DemoTenantSeedValidationException("Synthetic demo dataset is not approved for import.");
        }

        var mode = await repository.GetTenantModeAsync(tenantId, cancellationToken);
        EnsureDemoSandbox(mode);

        var result = await repository.SeedAsync(dataset, tenantId, actorUserId, cancellationToken);
        await WriteAuditAsync(dataset, tenantId, actorUserId, AuditAction.Created, "seed", result, cancellationToken);
        return result;
    }

    public async Task<DemoTenantSeedResult> ResetAsync(
        SyntheticDemoDatasetDefinition dataset,
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var mode = await repository.GetTenantModeAsync(tenantId, cancellationToken);
        EnsureDemoSandbox(mode);

        var result = await repository.ResetAsync(dataset, tenantId, actorUserId, cancellationToken);
        await WriteAuditAsync(dataset, tenantId, actorUserId, AuditAction.Deleted, "reset", result, cancellationToken);
        return result;
    }

    private static void EnsureDemoSandbox(TenantDataPosture? mode)
    {
        if (mode is null)
        {
            throw new DemoTenantSeedValidationException("Tenant was not found.");
        }

        if (mode is not TenantDataPosture.DemoSandbox)
        {
            throw new DemoTenantSeedValidationException("Synthetic demo seeding is allowed only for DemoSandbox tenants.");
        }
    }

    private Task WriteAuditAsync(
        SyntheticDemoDatasetDefinition dataset,
        Guid tenantId,
        Guid actorUserId,
        AuditAction action,
        string seedAction,
        DemoTenantSeedResult result,
        CancellationToken cancellationToken) =>
        auditEventWriter.WriteAsync(
            tenantId,
            actorUserId,
            action,
            "SyntheticDemoSeed",
            dataset.Metadata.DatasetId,
            $"Synthetic demo dataset {seedAction} completed.",
            new Dictionary<string, string>
            {
                ["tenantId"] = tenantId.ToString(),
                ["actorUserId"] = actorUserId.ToString(),
                ["datasetVersion"] = dataset.Metadata.Version,
                ["seedAction"] = seedAction,
                ["timestamp"] = DateTimeOffset.UtcNow.ToString("O"),
                ["result"] = result.Succeeded ? "succeeded" : "failed",
                ["createdCount"] = result.CreatedCount.ToString(),
                ["deletedCount"] = result.DeletedCount.ToString()
            },
            cancellationToken);
}

public interface IDemoTenantSeedRepository
{
    Task<TenantDataPosture?> GetTenantModeAsync(Guid tenantId, CancellationToken cancellationToken = default);

    Task<DemoTenantSeedResult> SeedAsync(
        SyntheticDemoDatasetDefinition dataset,
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<DemoTenantSeedResult> ResetAsync(
        SyntheticDemoDatasetDefinition dataset,
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken = default);
}

public sealed class DemoTenantSeedValidationException(string message) : InvalidOperationException(message);

public sealed record DemoTenantSeedResult(
    bool Succeeded,
    string Action,
    string DatasetId,
    string DatasetVersion,
    int CreatedCount,
    int DeletedCount,
    IReadOnlyList<string> RecordTypes,
    IReadOnlyDictionary<string, string> RecordIds);
