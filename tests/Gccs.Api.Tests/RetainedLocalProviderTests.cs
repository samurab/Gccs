using System.Text;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Gccs.Application.Security;
using Gccs.Application.Storage;
using Gccs.Infrastructure.NoCui;
using Gccs.Infrastructure.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Xunit;
using Xunit.Abstractions;

namespace Gccs.Api.Tests;

public sealed class RetainedLocalProviderTests(ITestOutputHelper output)
{
    [RetainedProviderFact]
    [Trait("Category", "RetainedLocalProvider")]
    public async Task Real_scanner_and_object_store_detect_malware_preserve_clean_bytes_and_isolate_tenant_names()
    {
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__AzureStorage")!;
        var containerName = Environment.GetEnvironmentVariable("GCCS_RETAINED_PROVIDER_CONTAINER")!;
        var host = Environment.GetEnvironmentVariable("MalwareScanning__Host")!;
        Assert.Equal("127.0.0.1", host);
        Assert.StartsWith("s397-retained-", containerName);
        var blobs = new BlobServiceClient(connection);
        Assert.Equal("127.0.0.1", blobs.Uri.Host);
        var container = blobs.GetBlobContainerClient(containerName);
        Assert.False(await container.ExistsAsync());

        var scanner = new ClamAvMalwareScanner(Options.Create(new MalwareScanningOptions
        {
            Enabled = true, Host = host, Port = int.Parse(Environment.GetEnvironmentVariable("MalwareScanning__Port")!), TimeoutSeconds = 10
        }));
        const string clean = "Synthetic public No-CUI scanner and storage verification.";
        await using var cleanStream = new MemoryStream(Encoding.UTF8.GetBytes(clean));
        Assert.Equal(MalwareScanVerdict.Clean, (await scanner.ScanAsync(new(cleanStream, "clean.txt", "text/plain", cleanStream.Length))).Verdict);
        // EICAR is generated in memory and never uploaded or written to disk.
        var eicar = "X5O!P%@AP[4\\PZX54(P^)7CC)7}$" + "EICAR-STANDARD-ANTIVIRUS-TEST-FILE!$H+H*";
        await using var malicious = new MemoryStream(Encoding.ASCII.GetBytes(eicar));
        Assert.Equal(MalwareScanVerdict.Malicious, (await scanner.ScanAsync(new(malicious, "eicar.txt", "text/plain", malicious.Length))).Verdict);

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:AzureStorage"] = connection
        }).Build();
        var storage = new AzureBlobObjectStorageService(Options.Create(new AzureBlobStorageOptions
        {
            UseManagedIdentity = false, Containers = new AzureBlobContainerOptions { Evidence = containerName }
        }), configuration);
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        cleanStream.Position = 0;
        var written = await storage.UploadAsync(new(tenantA, ObjectStorageContainer.Evidence, "clean.txt", cleanStream, "text/plain"));
        Assert.Equal(ObjectStorageNames.BuildTenantBlobName(tenantA, "clean.txt"), written.BlobName);
        await using var read = await storage.OpenReadAsync(new(tenantA, ObjectStorageContainer.Evidence, "clean.txt"));
        Assert.NotNull(read);
        using var reader = new StreamReader(read.Content);
        Assert.Equal(clean, await reader.ReadToEndAsync());
        Assert.Null(await storage.OpenReadAsync(new(tenantB, ObjectStorageContainer.Evidence, "clean.txt")));
        Assert.False(await storage.ExistsAsync(new(tenantB, ObjectStorageContainer.Evidence, "clean.txt")));
        Assert.Equal(PublicAccessType.None, (await container.GetAccessPolicyAsync()).Value.BlobPublicAccess);
        var properties = await container.GetBlobClient(written.BlobName).GetPropertiesAsync();
        Assert.Equal(tenantA.ToString("D"), properties.Value.Metadata["tenantId"]);
        Assert.Equal("text/plain", properties.Value.ContentType);
        output.WriteLine(System.Text.Json.JsonSerializer.Serialize(new
        {
            kind = "retained-real-provider", container = containerName, blob = written.BlobName,
            clean = "passed", eicar = "detected", crossTenant = "not-found", retention = "no deletion; synthetic clean blob retained"
        }));
    }
}

internal sealed class RetainedProviderFactAttribute : FactAttribute
{
    public RetainedProviderFactAttribute()
    {
        var names = new[] { "ConnectionStrings__AzureStorage", "GCCS_RETAINED_PROVIDER_CONTAINER", "MalwareScanning__Host", "MalwareScanning__Port" };
        if (names.Any(n => string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(n))))
            Skip = "Requires explicit loopback scanner/storage configuration and a fresh retained synthetic container.";
    }
}
