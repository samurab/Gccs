namespace Gccs.Infrastructure.Storage;

public sealed class AzureBlobStorageOptions
{
    public const string SectionName = "Storage";

    public string AccountName { get; set; } = string.Empty;

    public string BlobServiceUri { get; set; } = string.Empty;

    public bool UseManagedIdentity { get; set; } = true;

    public AzureBlobContainerOptions Containers { get; set; } = new();
}

public sealed class AzureBlobContainerOptions
{
    public string Evidence { get; set; } = "evidence";

    public string Exports { get; set; } = "exports";

    public string Reports { get; set; } = "reports";
}
