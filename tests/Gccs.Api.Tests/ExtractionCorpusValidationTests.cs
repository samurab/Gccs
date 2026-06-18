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

    [Fact]
    public void TC_28_1_1_TC_28_1_3_and_TC_28_1_5_Corpus_metadata_enforces_allowed_non_cui_data_handling()
    {
        var root = FindCorpusRoot();
        using var corpus = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "corpus.json")));
        var rules = corpus.RootElement.GetProperty("dataHandlingRules");
        var allowed = rules.GetProperty("allowedDataClasses").EnumerateArray().Select(item => item.GetString()).ToArray();

        Assert.True(File.Exists(Path.Combine(root, "README.md")));
        Assert.Contains("synthetic", allowed);
        Assert.True(rules.GetProperty("requiresNonCuiConfirmation").GetBoolean());

        foreach (var document in corpus.RootElement.GetProperty("documents").EnumerateArray())
        {
            var dataClass = document.GetProperty("dataClass").GetString() ?? string.Empty;
            Assert.Contains(dataClass, AllowedDataClasses);
            Assert.False(document.GetProperty("containsCui").GetBoolean());
            Assert.False(string.IsNullOrWhiteSpace(document.GetProperty("documentType").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(document.GetProperty("sourceFamily").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(document.GetProperty("contractType").GetString()));
            Assert.NotEmpty(document.GetProperty("limitations").EnumerateArray());
            Assert.True(File.Exists(Path.Combine(root, document.GetProperty("file").GetString()!)));
            Assert.True(File.Exists(Path.Combine(root, document.GetProperty("labelFile").GetString()!)));
        }
    }

    [Fact]
    public void TC_28_1_2_Each_label_file_includes_expected_clause_citations_and_locations()
    {
        var root = FindCorpusRoot();
        using var corpus = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "corpus.json")));

        foreach (var document in corpus.RootElement.GetProperty("documents").EnumerateArray())
        {
            var labelPath = Path.Combine(root, document.GetProperty("labelFile").GetString()!);
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
                Assert.True(location.GetProperty("lineStart").GetInt32() > 0);
                Assert.True(location.GetProperty("lineEnd").GetInt32() >= location.GetProperty("lineStart").GetInt32());
                Assert.False(string.IsNullOrWhiteSpace(location.GetProperty("textAnchor").GetString()));
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
}
