using System.Text;
using Gccs.Application.Contracts;

namespace Gccs.Infrastructure.Contracts;

public sealed class DefaultContractDocumentTextExtractor : IContractDocumentTextExtractor
{
    public Task<DocumentTextExtractionResult> ExtractTextAsync(
        ContractDocumentDto document,
        CancellationToken cancellationToken = default)
    {
        if (!string.Equals(document.ContentType, "text/plain", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(DocumentTextExtractionResult.Failure(
                $"Document '{document.FileName}' has unsupported content type '{document.ContentType}' for MVP text extraction."));
        }

        if (string.IsNullOrWhiteSpace(document.StorageUri) ||
            document.StorageUri.StartsWith("pending://", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(DocumentTextExtractionResult.Failure(
                $"Document '{document.FileName}' is not readable because file storage is still pending."));
        }

        if (document.StorageUri.StartsWith("data:text/plain;base64,", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var payload = document.StorageUri["data:text/plain;base64,".Length..];
                var text = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
                return Task.FromResult(string.IsNullOrWhiteSpace(text)
                    ? DocumentTextExtractionResult.Failure($"Document '{document.FileName}' did not contain readable text.")
                    : DocumentTextExtractionResult.Success(text));
            }
            catch (FormatException)
            {
                return Task.FromResult(DocumentTextExtractionResult.Failure(
                    $"Document '{document.FileName}' contained unreadable text payload."));
            }
        }

        return Task.FromResult(DocumentTextExtractionResult.Failure(
            $"Document '{document.FileName}' storage location is not supported by the MVP extractor."));
    }
}
