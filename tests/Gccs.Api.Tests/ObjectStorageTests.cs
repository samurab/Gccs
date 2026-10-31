using Gccs.Application.Storage;
using Gccs.Infrastructure.Storage;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class ObjectStorageTests
{
    [Fact]
    public void Tenant_blob_names_are_scoped_under_the_tenant_id()
    {
        var tenantId = Guid.Parse("12212212-2122-1221-2212-2122122122a1");

        var blobName = ObjectStorageNames.BuildTenantBlobName(tenantId, " evidence/policy.pdf ");

        Assert.Equal("tenants/12212212-2122-1221-2212-2122122122a1/evidence/policy.pdf", blobName);
    }

    [Theory]
    [InlineData("../policy.pdf")]
    [InlineData("evidence/../../policy.pdf")]
    [InlineData("./policy.pdf")]
    [InlineData("")]
    [InlineData("   ")]
    public void Tenant_blob_names_reject_unsafe_object_names(string objectName)
    {
        var tenantId = Guid.Parse("12212212-2122-1221-2212-2122122122a2");

        Assert.Throws<ArgumentException>(() => ObjectStorageNames.BuildTenantBlobName(tenantId, objectName));
    }

    [Fact]
    public void Tenant_blob_names_normalize_windows_separators()
    {
        var tenantId = Guid.Parse("12212212-2122-1221-2212-2122122122a3");

        var blobName = ObjectStorageNames.BuildTenantBlobName(tenantId, @"reports\readiness.csv");

        Assert.Equal("tenants/12212212-2122-1221-2212-2122122122a3/reports/readiness.csv", blobName);
    }

    [Fact]
    public void Azure_blob_adapter_maps_logical_containers_to_configured_names()
    {
        var options = new AzureBlobStorageOptions
        {
            Containers = new AzureBlobContainerOptions
            {
                Evidence = "staging-evidence",
                Exports = "staging-exports",
                Reports = "staging-reports"
            }
        };

        Assert.Equal("staging-evidence", AzureBlobObjectStorageService.GetContainerName(options, ObjectStorageContainer.Evidence));
        Assert.Equal("staging-exports", AzureBlobObjectStorageService.GetContainerName(options, ObjectStorageContainer.Exports));
        Assert.Equal("staging-reports", AzureBlobObjectStorageService.GetContainerName(options, ObjectStorageContainer.Reports));
    }
}
