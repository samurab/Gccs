namespace Gccs.Application.Compliance;

public interface IComplianceContentImporter
{
    Task<ComplianceContentImportReport> ImportDirectoryAsync(string packageRoot, CancellationToken cancellationToken = default);

    Task<ComplianceContentImportReport> ImportFileAsync(string filePath, CancellationToken cancellationToken = default);
}

public sealed record ComplianceContentImportReport(
    bool Succeeded,
    int FilesProcessed,
    int ClausesCreated,
    int ClausesUpdated,
    int ClauseObligationMappingsCreated,
    int ClauseObligationMappingsUpdated,
    int ObligationsCreated,
    int ObligationsUpdated,
    IReadOnlyList<ComplianceContentImportError> Errors,
    IReadOnlyList<string> Logs);

public sealed record ComplianceContentImportError(
    string File,
    string Path,
    string Field,
    string Message);

public sealed record ComplianceContentImportCounts(
    int ClausesCreated,
    int ClausesUpdated,
    int ClauseObligationMappingsCreated,
    int ClauseObligationMappingsUpdated,
    int ObligationsCreated,
    int ObligationsUpdated);
