using System.Diagnostics;
using System.Text.Json;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class ExtractionEvaluationRunnerTests
{
    [Fact]
    public async Task TC_28_2_1_TC_28_2_4_and_TC_28_2_5_Runner_outputs_metrics_for_allowed_corpus_without_customer_data()
    {
        var repoRoot = FindRepoRoot();
        var output = Path.Combine(Path.GetTempPath(), $"gccs-extraction-eval-{Guid.NewGuid():N}");

        var result = await RunEvaluationAsync(repoRoot, $"--corpus tests/fixtures/extraction-corpus --output-dir \"{output}\" --min-precision 1 --min-recall 1");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("precision=1", result.StdOut);
        var jsonPath = Path.Combine(output, "latest.json");
        var markdownPath = Path.Combine(output, "latest.md");
        Assert.True(File.Exists(jsonPath));
        Assert.True(File.Exists(markdownPath));
        using var report = JsonDocument.Parse(await File.ReadAllTextAsync(jsonPath));
        Assert.False(report.RootElement.GetProperty("customerDataUsed").GetBoolean());
        Assert.Equal(1m, report.RootElement.GetProperty("metrics").GetProperty("precision").GetDecimal());
        Assert.Equal(1m, report.RootElement.GetProperty("metrics").GetProperty("recall").GetDecimal());
        Assert.Equal("passed", report.RootElement.GetProperty("thresholdStatus").GetString());
        Assert.Contains("Extraction Evaluation", await File.ReadAllTextAsync(markdownPath));
    }

    [Fact]
    public async Task TC_28_2_2_and_TC_28_2_3_Runner_identifies_missed_extra_clauses_and_fails_thresholds()
    {
        var repoRoot = FindRepoRoot();
        var corpusRoot = CreateImperfectCorpus();
        var output = Path.Combine(Path.GetTempPath(), $"gccs-extraction-eval-{Guid.NewGuid():N}");

        var result = await RunEvaluationAsync(repoRoot, $"--corpus \"{corpusRoot}\" --output-dir \"{output}\" --min-precision 0.9 --min-recall 0.9");

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("thresholds failed", result.StdErr, StringComparison.OrdinalIgnoreCase);
        using var report = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(output, "latest.json")));
        var metrics = report.RootElement.GetProperty("metrics");
        Assert.Equal(1, metrics.GetProperty("falsePositiveCount").GetInt32());
        Assert.Equal(1, metrics.GetProperty("falseNegativeCount").GetInt32());
        var document = report.RootElement.GetProperty("documents").EnumerateArray().Single();
        Assert.Contains("FAR 52.999-1", document.GetProperty("falsePositives").EnumerateArray().Select(item => item.GetString()));
        Assert.Contains("FAR 52.222-41", document.GetProperty("falseNegatives").EnumerateArray().Select(item => item.GetString()));
        Assert.Equal("failed", report.RootElement.GetProperty("thresholdStatus").GetString());
    }

    private static async Task<(int ExitCode, string StdOut, string StdErr)> RunEvaluationAsync(string repoRoot, string arguments)
    {
        var start = new ProcessStartInfo
        {
            FileName = "python3",
            Arguments = $"tools/extraction-evaluation/evaluate_corpus.py {arguments}",
            WorkingDirectory = repoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Could not start extraction evaluation.");
        var stdout = await process.StandardOutput.ReadToEndAsync();
        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        return (process.ExitCode, stdout, stderr);
    }

    private static string CreateImperfectCorpus()
    {
        var root = Path.Combine(Path.GetTempPath(), $"gccs-corpus-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(root, "documents"));
        Directory.CreateDirectory(Path.Combine(root, "labels"));
        File.WriteAllText(Path.Combine(root, "corpus.json"), """
            {
              "schemaVersion": "1.0",
              "dataHandlingRules": {
                "allowedDataClasses": ["synthetic"],
                "prohibitedContent": ["cui"],
                "requiresNonCuiConfirmation": true
              },
              "documents": [
                {
                  "id": "imperfect",
                  "title": "Imperfect Synthetic",
                  "file": "documents/imperfect.txt",
                  "labelFile": "labels/imperfect.labels.json",
                  "documentType": "test",
                  "sourceFamily": "synthetic",
                  "contractType": "test",
                  "dataClass": "synthetic",
                  "containsCui": false,
                  "limitations": ["Synthetic threshold fixture."],
                  "approvedForBenchmark": true,
                  "labelReview": {
                    "status": "approved",
                    "reviewedBy": "QA",
                    "reviewedAt": "2026-06-18",
                    "notes": "Synthetic test."
                  }
                }
              ]
            }
            """);
        File.WriteAllText(Path.Combine(root, "documents", "imperfect.txt"), """
            Line 1: FAR 52.204-21 is present.
            Line 2: FAR 52.999-1 is an extra synthetic detection.
            """);
        File.WriteAllText(Path.Combine(root, "labels", "imperfect.labels.json"), """
            {
              "documentId": "imperfect",
              "expectedClauses": [
                {
                  "citation": "FAR 52.204-21",
                  "title": "Basic Safeguarding",
                  "flowDownRequired": true,
                  "sourceLocation": { "lineStart": 1, "lineEnd": 1, "textAnchor": "FAR 52.204-21" }
                },
                {
                  "citation": "FAR 52.222-41",
                  "title": "Service Contract Labor Standards",
                  "flowDownRequired": false,
                  "sourceLocation": { "lineStart": 2, "lineEnd": 2, "textAnchor": "FAR 52.222-41" }
                }
              ]
            }
            """);
        return root;
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Gccs.slnx")) &&
                File.Exists(Path.Combine(directory.FullName, "tools", "extraction-evaluation", "evaluate_corpus.py")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
