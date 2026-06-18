using System.Text.Json;
using Gccs.Application.Tenancy;
using Gccs.Domain.Tenancy;

namespace Gccs.Infrastructure.Tenancy;

public sealed class FileDataHandlingNoticeRepository : IDataHandlingNoticeRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter<TenantDataPosture>() }
    };

    public async Task<DataHandlingNoticeCatalogDto> LoadAsync(string packageRoot, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(packageRoot, "data-handling-notices", "notices.json");
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<DataHandlingNoticeCatalogDto>(stream, JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException($"Data handling notice catalog at '{path}' could not be read.");
    }
}
