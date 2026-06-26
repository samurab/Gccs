using System.Text;
using Gccs.Application.Audit;
using Gccs.Domain.Audit;

namespace Gccs.Application.Reports;

public sealed class SimpleReportExportService(
    ISimpleReportExportRepository repository,
    IAuditEventWriter auditEventWriter)
{
    public async Task<SimpleReportExportDto> ExportAsync(
        SimpleReportExportRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(request);
        var data = await repository.GetExportDataAsync(normalized, actorUserId, cancellationToken);
        var generatedAt = DateTimeOffset.UtcNow;
        var csv = BuildCsv(data, generatedAt, actorUserId, normalized.AppliedFilters);
        var fileName = $"gccs-{normalized.ReportType.ToString().ToLowerInvariant()}-{generatedAt:yyyyMMddHHmmss}.csv";

        await auditEventWriter.WriteAsync(
            data.TenantId,
            actorUserId,
            AuditAction.Exported,
            "ReportExport",
            $"{normalized.ReportType}:{generatedAt:O}",
            "Simple report export was generated.",
            new Dictionary<string, string>
            {
                ["reportType"] = normalized.ReportType.ToString(),
                ["generatedAt"] = generatedAt.ToString("O"),
                ["fileName"] = fileName,
                ["rowCount"] = data.Rows.Count.ToString(),
                ["appliedFilters"] = normalized.AppliedFilters
            },
            cancellationToken);

        return new SimpleReportExportDto(fileName, "text/csv", Encoding.UTF8.GetBytes(csv));
    }

    private static SimpleReportExportQuery Normalize(SimpleReportExportRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ReportType))
        {
            throw new SimpleReportExportValidationException("Report type must be one of: compliance-overview, poam-list, evidence-inventory, audit-log.");
        }

        var reportType = request.ReportType.Trim().ToLowerInvariant() switch
        {
            "compliance-overview" or "complianceoverview" => SimpleReportExportType.ComplianceOverview,
            "poam-list" or "poamlist" or "poam" => SimpleReportExportType.PoamList,
            "evidence-inventory" or "evidenceinventory" or "evidence" => SimpleReportExportType.EvidenceInventory,
            "audit-log" or "auditlog" or "audit" => SimpleReportExportType.AuditLog,
            _ => throw new SimpleReportExportValidationException("Report type must be one of: compliance-overview, poam-list, evidence-inventory, audit-log.")
        };

        return new SimpleReportExportQuery(
            reportType,
            string.IsNullOrWhiteSpace(request.AppliedFilters) ? "none" : request.AppliedFilters.Trim());
    }

    private static string BuildCsv(
        SimpleReportExportData data,
        DateTimeOffset generatedAt,
        Guid actorUserId,
        string appliedFilters)
    {
        var csv = new StringBuilder();
        AppendRow(csv, "TenantId", data.TenantId.ToString());
        AppendRow(csv, "TenantName", data.TenantName);
        AppendRow(csv, "GeneratedDate", generatedAt.ToString("O"));
        AppendRow(csv, "GeneratedBy", actorUserId.ToString());
        AppendRow(csv, "ReportType", data.ReportType.ToString());
        AppendRow(csv, "AppliedFilters", appliedFilters);
        csv.AppendLine();
        AppendRow(csv, data.Headers);

        foreach (var row in data.Rows)
        {
            AppendRow(csv, row);
        }

        return csv.ToString();
    }

    private static void AppendRow(StringBuilder csv, params string?[] values) =>
        csv.AppendLine(string.Join(",", values.Select(Escape)));

    private static void AppendRow(StringBuilder csv, IReadOnlyList<string> values) =>
        csv.AppendLine(string.Join(",", values.Select(Escape)));

    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var normalized = value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        return normalized.Contains(',') || normalized.Contains('"') || normalized.Contains('\n')
            ? $"\"{normalized.Replace("\"", "\"\"", StringComparison.Ordinal)}\""
            : normalized;
    }
}

public interface ISimpleReportExportRepository
{
    Task<SimpleReportExportData> GetExportDataAsync(
        SimpleReportExportQuery query,
        Guid actorUserId,
        CancellationToken cancellationToken = default);
}

public sealed record SimpleReportExportRequest(string ReportType, string AppliedFilters);

public sealed record SimpleReportExportQuery(SimpleReportExportType ReportType, string AppliedFilters);

public sealed record SimpleReportExportData(
    Guid TenantId,
    string TenantName,
    SimpleReportExportType ReportType,
    IReadOnlyList<string> Headers,
    IReadOnlyList<IReadOnlyList<string>> Rows);

public sealed record SimpleReportExportDto(string FileName, string ContentType, byte[] Content);

public enum SimpleReportExportType
{
    ComplianceOverview,
    PoamList,
    EvidenceInventory,
    AuditLog
}

public sealed class SimpleReportExportValidationException(string message) : InvalidOperationException(message);
