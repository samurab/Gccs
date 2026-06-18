using System.Text.Json;
using Gccs.Application.Tenancy;

namespace Gccs.Infrastructure.Tenancy;

public sealed class FileSharedResponsibilityMatrixRepository : ISharedResponsibilityMatrixRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<SharedResponsibilityMatrixDto> LoadAsync(string packageRoot, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(packageRoot, "shared-responsibility-matrix", "baseline.json");
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<SharedResponsibilityMatrixDto>(stream, JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException($"Shared responsibility matrix content at '{path}' could not be read.");
    }
}
