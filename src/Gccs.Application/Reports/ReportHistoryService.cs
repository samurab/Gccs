namespace Gccs.Application.Reports;

public sealed class ReportHistoryService(IReportRepository repository)
{
    public const int DefaultLimit = 25;
    public const int MaximumLimit = 50;

    public Task<IReadOnlyList<ReportHistoryItemDto>> ListAsync(
        int? limit,
        CancellationToken cancellationToken = default)
    {
        var requestedLimit = limit ?? DefaultLimit;
        if (requestedLimit is < 1 or > MaximumLimit)
        {
            throw new ArgumentOutOfRangeException(
                nameof(limit),
                $"Report history limit must be between 1 and {MaximumLimit}.");
        }

        return repository.ListRecentReportsAsync(requestedLimit, cancellationToken);
    }

    public Task<ReportArtifactDetailDto?> GetAsync(
        Guid reportId,
        CancellationToken cancellationToken = default) =>
        repository.GetReportArtifactAsync(reportId, cancellationToken);
}
