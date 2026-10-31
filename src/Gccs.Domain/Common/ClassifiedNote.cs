namespace Gccs.Domain.Common;

public sealed record ClassifiedNote(Guid Id, Guid TenantId, string Title, string Body,
    ContentClassification Classification, long Revision, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt)
{
    public const int MaximumTitleLength = 240;
    public const int MaximumBodyLength = 20_000;

    public static bool HasValidContent(string? title, string? body) =>
        !string.IsNullOrWhiteSpace(title) && title.Length <= MaximumTitleLength &&
        !string.IsNullOrWhiteSpace(body) && body.Length <= MaximumBodyLength;
}
