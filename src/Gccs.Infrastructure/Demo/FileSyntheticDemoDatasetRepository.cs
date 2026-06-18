using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Demo;

namespace Gccs.Infrastructure.Demo;

public sealed class FileSyntheticDemoDatasetRepository : ISyntheticDemoDatasetRepository
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<SyntheticDemoDatasetDefinition> LoadAsync(string packageRoot, CancellationToken cancellationToken = default)
    {
        var datasetPath = Path.Combine(packageRoot, "synthetic-cui", "dataset.json");
        if (!File.Exists(datasetPath))
        {
            throw new FileNotFoundException("Synthetic demo dataset file was not found.", datasetPath);
        }

        await using var stream = File.OpenRead(datasetPath);
        var dataset = await JsonSerializer.DeserializeAsync<SyntheticDemoDatasetDefinition>(stream, SerializerOptions, cancellationToken);
        return dataset ?? throw new InvalidOperationException("Synthetic demo dataset file was empty or invalid.");
    }
}
