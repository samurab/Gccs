namespace Gccs.Application.Tenancy;

public sealed class SharedResponsibilityMatrixService(ISharedResponsibilityMatrixRepository repository)
{
    private static readonly string[] RequiredCategories =
    [
        "tenant-administration",
        "user-access",
        "mfa",
        "upload-classification",
        "evidence-storage",
        "encryption",
        "malware-scanning",
        "retention",
        "backup",
        "export",
        "deletion",
        "incident-reporting",
        "support",
        "customer-content-decisions"
    ];

    private static readonly HashSet<string> AllowedResponsibilities = new(StringComparer.Ordinal)
    {
        "GCCS",
        "Customer",
        "Shared",
        "ThirdPartyProvider",
        "NotApplicable"
    };

    public async Task<SharedResponsibilityMatrixDto> GetPublishedAsync(string packageRoot, CancellationToken cancellationToken = default)
    {
        var matrix = await repository.LoadAsync(packageRoot, cancellationToken);
        ValidateForPublish(matrix);

        if (!string.Equals(matrix.State, "Published", StringComparison.Ordinal))
        {
            throw new SharedResponsibilityMatrixValidationException("Shared responsibility matrix must be published before it is customer-visible.");
        }

        return matrix;
    }

    public static void ValidateForPublish(SharedResponsibilityMatrixDto matrix)
    {
        var errors = Validate(matrix);
        if (errors.Count > 0)
        {
            throw new SharedResponsibilityMatrixValidationException("Shared responsibility matrix is not publishable.", errors);
        }
    }

    public static IReadOnlyList<string> RequiredCategoryKeys => RequiredCategories;

    private static IReadOnlyList<string> Validate(SharedResponsibilityMatrixDto matrix)
    {
        var errors = new List<string>();
        Require(matrix.MatrixId, "matrixId", errors);
        Require(matrix.Version, "version", errors);
        Require(matrix.Title, "title", errors);
        Require(matrix.State, "state", errors);
        Require(matrix.ReviewOwner, "reviewOwner", errors);
        Require(matrix.SourceReference, "sourceReference", errors);

        if (matrix.EffectiveAt == default)
        {
            errors.Add("effectiveAt is required.");
        }

        if (matrix.ReviewedAt == default)
        {
            errors.Add("reviewedAt is required.");
        }

        var categories = matrix.Rows.Select(row => row.Category).ToHashSet(StringComparer.Ordinal);
        foreach (var category in RequiredCategories)
        {
            if (!categories.Contains(category))
            {
                errors.Add($"Required category '{category}' is missing.");
            }
        }

        foreach (var row in matrix.Rows)
        {
            Require(row.Category, "row.category", errors);
            Require(row.Responsibility, $"row[{row.Category}].responsibility", errors);
            Require(row.Notes, $"row[{row.Category}].notes", errors);
            Require(row.SourceReference, $"row[{row.Category}].sourceReference", errors);
            Require(row.ReviewOwner, $"row[{row.Category}].reviewOwner", errors);
            Require(row.Version, $"row[{row.Category}].version", errors);

            if (!string.IsNullOrWhiteSpace(row.Responsibility) && !AllowedResponsibilities.Contains(row.Responsibility))
            {
                errors.Add($"row[{row.Category}].responsibility is not an allowed responsibility value.");
            }

            if (row.EffectiveAt == default)
            {
                errors.Add($"row[{row.Category}].effectiveAt is required.");
            }
        }

        return errors;
    }

    private static void Require(string? value, string field, ICollection<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"{field} is required.");
        }
    }
}

public interface ISharedResponsibilityMatrixRepository
{
    Task<SharedResponsibilityMatrixDto> LoadAsync(string packageRoot, CancellationToken cancellationToken = default);
}

public sealed class SharedResponsibilityMatrixValidationException(
    string message,
    IReadOnlyList<string>? errors = null) : InvalidOperationException(message)
{
    public IReadOnlyList<string> Errors { get; } = errors ?? [];
}

public sealed record SharedResponsibilityMatrixDto(
    string MatrixId,
    string Version,
    string Title,
    string State,
    DateOnly EffectiveAt,
    string ReviewOwner,
    DateOnly ReviewedAt,
    string SourceReference,
    IReadOnlyList<SharedResponsibilityMatrixRowDto> Rows);

public sealed record SharedResponsibilityMatrixRowDto(
    string Category,
    string Responsibility,
    string Notes,
    string SourceReference,
    DateOnly EffectiveAt,
    string ReviewOwner,
    string Version);
