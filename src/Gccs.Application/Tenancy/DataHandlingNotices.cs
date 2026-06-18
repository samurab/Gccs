using Gccs.Domain.Tenancy;

namespace Gccs.Application.Tenancy;

public sealed class DataHandlingNoticeService(IDataHandlingNoticeRepository repository)
{
    private static readonly TenantDataPosture[] RequiredModes =
    [
        TenantDataPosture.DemoSandbox,
        TenantDataPosture.NoCui,
        TenantDataPosture.CuiReady
    ];

    public async Task<IReadOnlyList<DataHandlingNoticeDto>> ListPublishedAsync(
        string packageRoot,
        CancellationToken cancellationToken = default)
    {
        var catalog = await repository.LoadAsync(packageRoot, cancellationToken);
        ValidateCatalogForPublish(catalog);
        return catalog.Notices
            .Where(notice => notice.State == "Published")
            .OrderBy(notice => notice.Mode)
            .ThenBy(notice => notice.NoticeId)
            .ToArray();
    }

    public async Task<DataHandlingNoticeDto?> GetPublishedAsync(
        string packageRoot,
        TenantDataPosture mode,
        string workflowContext,
        CancellationToken cancellationToken = default)
    {
        var notices = await ListPublishedAsync(packageRoot, cancellationToken);
        var context = NormalizeContext(workflowContext);
        return notices
            .Where(notice => notice.Mode == mode)
            .Where(notice => notice.WorkflowContexts.Contains(context, StringComparer.OrdinalIgnoreCase))
            .OrderByDescending(notice => notice.EffectiveAt)
            .FirstOrDefault();
    }

    public static void ValidateCatalogForPublish(DataHandlingNoticeCatalogDto catalog)
    {
        var errors = Validate(catalog);
        if (errors.Count > 0)
        {
            throw new DataHandlingNoticeValidationException("Data handling notice catalog is not publishable.", errors);
        }
    }

    private static IReadOnlyList<string> Validate(DataHandlingNoticeCatalogDto catalog)
    {
        var errors = new List<string>();
        foreach (var mode in RequiredModes)
        {
            if (!catalog.Notices.Any(notice => notice.Mode == mode && notice.State == "Published"))
            {
                errors.Add($"Published notice for {mode} is required.");
            }
        }

        foreach (var notice in catalog.Notices.Where(notice => notice.State == "Published"))
        {
            Require(notice.NoticeId, "noticeId", errors);
            Require(notice.Version, $"notice[{notice.NoticeId}].version", errors);
            Require(notice.Title, $"notice[{notice.NoticeId}].title", errors);
            Require(notice.Body, $"notice[{notice.NoticeId}].body", errors);
            Require(notice.Owner, $"notice[{notice.NoticeId}].owner", errors);
            Require(notice.Reviewer, $"notice[{notice.NoticeId}].reviewer", errors);
            Require(notice.SourceReference, $"notice[{notice.NoticeId}].sourceReference", errors);

            if (notice.WorkflowContexts.Count == 0)
            {
                errors.Add($"notice[{notice.NoticeId}].workflowContexts is required.");
            }

            if (notice.ReviewedAt == default)
            {
                errors.Add($"notice[{notice.NoticeId}].reviewedAt is required.");
            }

            if (notice.EffectiveAt == default)
            {
                errors.Add($"notice[{notice.NoticeId}].effectiveAt is required.");
            }
        }

        return errors;
    }

    private static string NormalizeContext(string workflowContext) =>
        string.IsNullOrWhiteSpace(workflowContext) ? "Onboarding" : workflowContext.Trim();

    private static void Require(string? value, string field, ICollection<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"{field} is required.");
        }
    }
}

public interface IDataHandlingNoticeRepository
{
    Task<DataHandlingNoticeCatalogDto> LoadAsync(string packageRoot, CancellationToken cancellationToken = default);
}

public sealed class DataHandlingNoticeValidationException(
    string message,
    IReadOnlyList<string>? errors = null) : InvalidOperationException(message)
{
    public IReadOnlyList<string> Errors { get; } = errors ?? [];
}

public sealed record DataHandlingNoticeCatalogDto(IReadOnlyList<DataHandlingNoticeDto> Notices);

public sealed record DataHandlingNoticeDto(
    string NoticeId,
    string Version,
    TenantDataPosture Mode,
    IReadOnlyList<string> WorkflowContexts,
    string Title,
    string Body,
    string State,
    string Owner,
    string Reviewer,
    DateOnly ReviewedAt,
    DateOnly EffectiveAt,
    string SourceReference);
