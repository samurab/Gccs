using System.Text.Json;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class ExtractionCorpusValidationTests
{
    private static readonly HashSet<string> AllowedDataClasses = new(StringComparer.OrdinalIgnoreCase)
    {
        "public",
        "synthetic",
        "approved_non_cui"
    };

    private static readonly HashSet<string> ExpectedProhibitedContent = new(StringComparer.OrdinalIgnoreCase)
    {
        "cui",
        "classified",
        "export_controlled",
        "payroll",
        "personal_data",
        "secrets"
    };

    [Fact]
    public void TC_28_1_1_TC_28_1_3_and_TC_28_1_5_Corpus_metadata_enforces_allowed_non_cui_data_handling()
    {
        var root = FindCorpusRoot();
        using var corpus = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "corpus.json")));
        var rules = corpus.RootElement.GetProperty("dataHandlingRules");
        var allowed = rules.GetProperty("allowedDataClasses").EnumerateArray().Select(item => item.GetString() ?? string.Empty).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var prohibited = rules.GetProperty("prohibitedContent").EnumerateArray().Select(item => item.GetString() ?? string.Empty).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seenFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seenLabelFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seenDataClasses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        Assert.True(File.Exists(Path.Combine(root, "README.md")));
        Assert.Equal("gccs-extraction-content-test-set", corpus.RootElement.GetProperty("corpusId").GetString());
        Assert.False(string.IsNullOrWhiteSpace(corpus.RootElement.GetProperty("corpusOwner").GetString()));
        Assert.Equal(AllowedDataClasses, allowed);
        Assert.True(ExpectedProhibitedContent.IsSubsetOf(prohibited), "Corpus data handling rules must prohibit CUI and sensitive customer/security data.");
        Assert.True(rules.GetProperty("requiresNonCuiConfirmation").GetBoolean());
        Assert.False(rules.GetProperty("customerDataAllowed").GetBoolean());
        Assert.True(rules.GetProperty("approvedNonCuiRequiresWrittenApproval").GetBoolean());

        foreach (var document in corpus.RootElement.GetProperty("documents").EnumerateArray())
        {
            var id = document.GetProperty("id").GetString() ?? string.Empty;
            var file = document.GetProperty("file").GetString() ?? string.Empty;
            var labelFile = document.GetProperty("labelFile").GetString() ?? string.Empty;
            var dataClass = document.GetProperty("dataClass").GetString() ?? string.Empty;
            Assert.True(seenIds.Add(id), $"Duplicate document id: {id}");
            Assert.True(seenFiles.Add(file), $"Duplicate document file: {file}");
            Assert.True(seenLabelFiles.Add(labelFile), $"Duplicate label file: {labelFile}");
            seenDataClasses.Add(dataClass);
            Assert.Contains(dataClass, AllowedDataClasses);
            Assert.False(document.GetProperty("containsCui").GetBoolean());
            Assert.False(string.IsNullOrWhiteSpace(document.GetProperty("documentType").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(document.GetProperty("sourceFamily").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(document.GetProperty("sourceReference").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(document.GetProperty("contractType").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(document.GetProperty("approvedNonCuiBasis").GetString()));
            Assert.NotEmpty(document.GetProperty("limitations").EnumerateArray());
            Assert.True(File.Exists(Path.Combine(root, file)));
            Assert.True(File.Exists(Path.Combine(root, labelFile)));
        }

        Assert.True(AllowedDataClasses.IsSubsetOf(seenDataClasses), "Corpus should include public, synthetic, and approved non-CUI coverage.");
        AssertDirectoryMatchesCorpus(Path.Combine(root, "documents"), "*.txt", seenFiles.Select(Path.GetFileName));
        AssertDirectoryMatchesCorpus(Path.Combine(root, "labels"), "*.json", seenLabelFiles.Select(Path.GetFileName));
    }

    [Fact]
    public void TC_28_1_2_Each_label_file_includes_expected_clause_citations_and_locations()
    {
        var root = FindCorpusRoot();
        using var corpus = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "corpus.json")));

        foreach (var document in corpus.RootElement.GetProperty("documents").EnumerateArray())
        {
            var labelPath = Path.Combine(root, document.GetProperty("labelFile").GetString()!);
            var documentPath = Path.Combine(root, document.GetProperty("file").GetString()!);
            var sourceLines = File.ReadAllLines(documentPath);
            using var labels = JsonDocument.Parse(File.ReadAllText(labelPath));
            Assert.Equal(document.GetProperty("id").GetString(), labels.RootElement.GetProperty("documentId").GetString());
            var clauses = labels.RootElement.GetProperty("expectedClauses").EnumerateArray().ToArray();
            Assert.NotEmpty(clauses);

            foreach (var clause in clauses)
            {
                Assert.False(string.IsNullOrWhiteSpace(clause.GetProperty("citation").GetString()));
                Assert.False(string.IsNullOrWhiteSpace(clause.GetProperty("title").GetString()));
                Assert.True(clause.TryGetProperty("flowDownRequired", out var flowDown) && flowDown.ValueKind is JsonValueKind.True or JsonValueKind.False);
                var location = clause.GetProperty("sourceLocation");
                var lineStart = location.GetProperty("lineStart").GetInt32();
                var lineEnd = location.GetProperty("lineEnd").GetInt32();
                var textAnchor = location.GetProperty("textAnchor").GetString() ?? string.Empty;
                Assert.True(lineStart > 0);
                Assert.True(lineEnd >= lineStart);
                Assert.True(lineEnd <= sourceLines.Length);
                Assert.False(string.IsNullOrWhiteSpace(textAnchor));
                var referencedText = string.Join("\n", sourceLines.Skip(lineStart - 1).Take(lineEnd - lineStart + 1));
                Assert.Contains(textAnchor, referencedText, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void TC_28_1_4_Label_sets_are_reviewed_before_benchmark_use()
    {
        var root = FindCorpusRoot();
        using var corpus = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "corpus.json")));

        foreach (var document in corpus.RootElement.GetProperty("documents").EnumerateArray())
        {
            Assert.True(document.GetProperty("approvedForBenchmark").GetBoolean());
            var review = document.GetProperty("labelReview");
            Assert.Equal("approved", review.GetProperty("status").GetString());
            Assert.False(string.IsNullOrWhiteSpace(review.GetProperty("reviewedBy").GetString()));
            Assert.True(DateOnly.TryParse(review.GetProperty("reviewedAt").GetString(), out _));
            Assert.False(string.IsNullOrWhiteSpace(review.GetProperty("notes").GetString()));
        }
    }

    [Fact]
    public void TC_28_1_5_Readme_documents_data_handling_and_review_requirements()
    {
        var readme = File.ReadAllText(Path.Combine(FindCorpusRoot(), "README.md"));

        Assert.Contains("Allowed data classes", readme, StringComparison.Ordinal);
        Assert.Contains("Customer data is not allowed", readme, StringComparison.Ordinal);
        Assert.Contains("approved_non_cui", readme, StringComparison.Ordinal);
        Assert.Contains("actual file line numbers", readme, StringComparison.Ordinal);
        Assert.Contains("Review Workflow", readme, StringComparison.Ordinal);
    }

    private static string FindCorpusRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "tests", "fixtures", "extraction-corpus");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate tests/fixtures/extraction-corpus.");
    }

    private static void AssertDirectoryMatchesCorpus(string directory, string searchPattern, IEnumerable<string?> expectedFiles)
    {
        var expected = expectedFiles.Where(file => !string.IsNullOrWhiteSpace(file)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var actual = Directory.GetFiles(directory, searchPattern).Select(Path.GetFileName).ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Equal(expected, actual);
    }
}
