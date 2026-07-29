using System.Text;
using Gccs.Application.Contracts;
using Gccs.Application.Security;
using Gccs.Application.Storage;

namespace Gccs.Infrastructure.Contracts;

public sealed class DefaultContractDocumentTextExtractor(
    IObjectStorageService objectStorageService,
    ICurrentTenantContext tenantContext) : IContractDocumentTextExtractor
{
    public async Task<DocumentTextExtractionResult> ExtractTextAsync(
        ContractDocumentDto document,
        CancellationToken cancellationToken = default)
    {
        if (!string.Equals(document.ContentType, "text/plain", StringComparison.OrdinalIgnoreCase))
        {
            return DocumentTextExtractionResult.Failure(
                $"Document '{document.FileName}' has unsupported content type '{document.ContentType}' for MVP text extraction.");
        }

        if (string.IsNullOrWhiteSpace(document.StorageUri) ||
            document.StorageUri.StartsWith("pending://", StringComparison.OrdinalIgnoreCase))
        {
            return DocumentTextExtractionResult.Failure(
                $"Document '{document.FileName}' is not readable because file storage is still pending.");
        }

        if (document.StorageUri.StartsWith("data:text/plain;base64,", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var payload = document.StorageUri["data:text/plain;base64,".Length..];
                var text = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
                return string.IsNullOrWhiteSpace(text)
                    ? DocumentTextExtractionResult.Failure($"Document '{document.FileName}' did not contain readable text.")
                    : DocumentTextExtractionResult.Success(text);
            }
            catch (FormatException)
            {
                return DocumentTextExtractionResult.Failure(
                    $"Document '{document.FileName}' contained unreadable text payload.");
            }
        }

        if (!document.StorageUri.StartsWith("contracts/", StringComparison.Ordinal))
        {
            return DocumentTextExtractionResult.Failure(
                $"Document '{document.FileName}' storage location is not supported by the MVP extractor.");
        }

        await using var stored = await objectStorageService.OpenReadAsync(
            new ObjectStorageReadRequest(
                tenantContext.TenantId,
                ObjectStorageContainer.ContractDocuments,
                document.StorageUri),
            cancellationToken);
        if (stored is null)
        {
            return DocumentTextExtractionResult.Failure(
                $"Document '{document.FileName}' could not be found in tenant-scoped object storage.");
        }

        using var reader = new StreamReader(
            stored.Content,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true,
            leaveOpen: true);
        var storedText = await reader.ReadToEndAsync(cancellationToken);
        return string.IsNullOrWhiteSpace(storedText)
            ? DocumentTextExtractionResult.Failure($"Document '{document.FileName}' did not contain readable text.")
            : DocumentTextExtractionResult.Success(storedText);
    }
}
