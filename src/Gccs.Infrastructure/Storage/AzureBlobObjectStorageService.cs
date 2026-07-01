using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Gccs.Application.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Gccs.Infrastructure.Storage;

public sealed class AzureBlobObjectStorageService : IObjectStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly AzureBlobStorageOptions _options;

    public AzureBlobObjectStorageService(
        IOptions<AzureBlobStorageOptions> options,
        IConfiguration configuration)
    {
        _options = options.Value;
        _blobServiceClient = CreateClient(_options, configuration);
    }

    public async Task<ObjectStorageWriteResult> UploadAsync(
        ObjectStorageWriteRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request.Content);

        var blobName = ObjectStorageNames.BuildTenantBlobName(request.TenantId, request.ObjectName);
        var blob = GetContainerClient(request.Container).GetBlobClient(blobName);
        var metadata = NormalizeMetadata(request.Metadata);
        metadata["tenantId"] = request.TenantId.ToString("D");

        var options = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders
            {
                ContentType = string.IsNullOrWhiteSpace(request.ContentType)
                    ? "application/octet-stream"
                    : request.ContentType.Trim()
            },
            Metadata = metadata
        };

        var response = await blob.UploadAsync(request.Content, options, cancellationToken);
        return new ObjectStorageWriteResult(
            request.Container,
            blobName,
            blob.Uri,
            response.Value.ETag.ToString(),
            response.Value.LastModified);
    }

    public async Task<ObjectStorageReadResult?> OpenReadAsync(
        ObjectStorageReadRequest request,
        CancellationToken cancellationToken = default)
    {
        var blobName = ObjectStorageNames.BuildTenantBlobName(request.TenantId, request.ObjectName);
        var blob = GetContainerClient(request.Container).GetBlobClient(blobName);

        if (!await blob.ExistsAsync(cancellationToken))
        {
            return null;
        }

        var download = await blob.DownloadStreamingAsync(cancellationToken: cancellationToken);
        var details = download.Value.Details;
        return new ObjectStorageReadResult(
            request.Container,
            blobName,
            download.Value.Content,
            details.ContentType ?? "application/octet-stream",
            details.ContentLength,
            details.ETag.ToString(),
            details.LastModified);
    }

    public async Task<bool> ExistsAsync(
        ObjectStorageReadRequest request,
        CancellationToken cancellationToken = default)
    {
        var blobName = ObjectStorageNames.BuildTenantBlobName(request.TenantId, request.ObjectName);
        return await GetContainerClient(request.Container)
            .GetBlobClient(blobName)
            .ExistsAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(
        ObjectStorageReadRequest request,
        CancellationToken cancellationToken = default)
    {
        var blobName = ObjectStorageNames.BuildTenantBlobName(request.TenantId, request.ObjectName);
        var response = await GetContainerClient(request.Container)
            .GetBlobClient(blobName)
            .DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: cancellationToken);
        return response.Value;
    }

    private BlobContainerClient GetContainerClient(ObjectStorageContainer container) =>
        _blobServiceClient.GetBlobContainerClient(GetContainerName(_options, container));

    public static string GetContainerName(AzureBlobStorageOptions options, ObjectStorageContainer container)
    {
        var name = container switch
        {
            ObjectStorageContainer.Evidence => options.Containers.Evidence,
            ObjectStorageContainer.Exports => options.Containers.Exports,
            ObjectStorageContainer.Reports => options.Containers.Reports,
            _ => throw new ArgumentOutOfRangeException(nameof(container), container, "Unknown storage container.")
        };

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException($"Storage container '{container}' is not configured.");
        }

        return name.Trim();
    }

    private static BlobServiceClient CreateClient(AzureBlobStorageOptions options, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("AzureStorage");
        if (!options.UseManagedIdentity && !string.IsNullOrWhiteSpace(connectionString))
        {
            return new BlobServiceClient(connectionString);
        }

        var serviceUri = BuildServiceUri(options);
        return new BlobServiceClient(serviceUri, new DefaultAzureCredential());
    }

    private static Uri BuildServiceUri(AzureBlobStorageOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.BlobServiceUri))
        {
            return new Uri(options.BlobServiceUri.Trim(), UriKind.Absolute);
        }

        if (string.IsNullOrWhiteSpace(options.AccountName))
        {
            throw new InvalidOperationException("Storage:BlobServiceUri or Storage:AccountName must be configured.");
        }

        return new Uri($"https://{options.AccountName.Trim()}.blob.core.windows.net", UriKind.Absolute);
    }

    private static Dictionary<string, string> NormalizeMetadata(IReadOnlyDictionary<string, string>? metadata)
    {
        var normalized = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (metadata is null)
        {
            return normalized;
        }

        foreach (var item in metadata)
        {
            if (string.IsNullOrWhiteSpace(item.Key) || string.IsNullOrWhiteSpace(item.Value))
            {
                continue;
            }

            normalized[item.Key.Trim()] = item.Value.Trim();
        }

        return normalized;
    }
}
