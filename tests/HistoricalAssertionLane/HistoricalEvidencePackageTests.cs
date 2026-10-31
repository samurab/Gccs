using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace Gccs.Api.Tests;

// Every test here uses in-memory synthetic bytes only; no historical package is opened.
public sealed class HistoricalEvidencePackageTests
{
    [Fact]
    public void Declared_population_is_exact_and_never_routes_current_source()
    {
        Assert.Equal(29, HistoricalEvidencePackage.DeclaredInputs.Count);
        Assert.All(HistoricalEvidencePackage.DeclaredInputs.Keys, path =>
        {
            Assert.StartsWith("output/", path);
            Assert.True(HistoricalEvidencePackage.IsDeclaredInput(path));
            Assert.DoesNotContain("..", path);
        });
        foreach (var path in new[] { "docs/api/openapi.yaml", "docs/release/approved-release.json",
            "packages/compliance-content/obligations/mvp.json", ".github/workflows/production.yml",
            "output/unlisted.json", "../output/example.json", "/output/example.json" })
            Assert.False(HistoricalEvidencePackage.IsDeclaredInput(path));
    }

    [Fact]
    public void Unlisted_read_fails_before_accessing_any_package_or_repository()
    {
        var failure = Assert.Throws<InvalidOperationException>(() => HistoricalEvidencePackage.ReadText("output/unlisted.json"));
        Assert.Equal("Historical evidence contract failed: unlisted-input.", failure.Message);
        Assert.Null(failure.InnerException);
    }

    [Fact]
    public void Exact_29_entry_synthetic_manifest_is_accepted()
    {
        var fixture = CreateManifest();
        var bytes = Encoding.UTF8.GetBytes(fixture.Root.ToJsonString());
        var result = HistoricalEvidencePackage.ValidateManifest(bytes, HistoricalEvidencePackage.Digest(bytes),
            fixture.Expected, HistoricalEvidencePackage.SourceCommitSha);
        Assert.Equal(29, result.Count);
        foreach (var path in HistoricalEvidencePackage.DeclaredInputs.Keys)
            Assert.Equal(fixture.Expected[path].GitBlobOid, result[path].GitBlobOid);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("extra")]
    [InlineData("duplicate")]
    [InlineData("wrong-source")]
    [InlineData("wrong-size")]
    [InlineData("wrong-oid")]
    [InlineData("unknown-field")]
    [InlineData("missing-digest")]
    [InlineData("uppercase-digest")]
    [InlineData("wrong-schema")]
    [InlineData("unlisted-traversal")]
    public void Invalid_manifest_never_changes_the_trusted_population(string mutation)
    {
        var fixture = CreateManifest();
        var entries = fixture.Root["entries"]!.AsArray();
        switch (mutation)
        {
            case "missing": entries.RemoveAt(0); break;
            case "extra": entries.Add(entries[0]!.DeepClone()); break;
            case "duplicate": entries[1] = entries[0]!.DeepClone(); break;
            case "wrong-source": fixture.Root["sourceCommitSha"] = new string('0', 40); break;
            case "wrong-size": entries[0]!["sizeBytes"] = 999; break;
            case "wrong-oid": entries[0]!["gitBlobOid"] = new string('0', 40); break;
            case "unknown-field": fixture.Root["synthetic-untrusted-field"] = true; break;
            case "missing-digest": entries[0]!["sha256"] = null; break;
            case "uppercase-digest": entries[0]!["sha256"] = new string('A', 64); break;
            case "wrong-schema": fixture.Root["schemaVersion"] = 2; break;
            case "unlisted-traversal": entries[0]!["path"] = "../synthetic-canary.json"; break;
        }
        var bytes = Encoding.UTF8.GetBytes(fixture.Root.ToJsonString());
        var failure = Assert.Throws<InvalidOperationException>(() => HistoricalEvidencePackage.ValidateManifest(bytes,
            HistoricalEvidencePackage.Digest(bytes), fixture.Expected, HistoricalEvidencePackage.SourceCommitSha));
        Assert.StartsWith("Historical evidence contract failed:", failure.Message);
        Assert.DoesNotContain("synthetic", failure.Message);
        Assert.Null(failure.InnerException);
    }

    [Theory]
    [InlineData("")]
    [InlineData("unbound")]
    [InlineData("0000000000000000000000000000000000000000000000000000000000000000")]
    public void Manifest_requires_an_external_exact_digest_binding(string digest)
    {
        var fixture = CreateManifest();
        var bytes = Encoding.UTF8.GetBytes(fixture.Root.ToJsonString());
        Assert.Throws<InvalidOperationException>(() => HistoricalEvidencePackage.ValidateManifest(bytes, digest,
            fixture.Expected, HistoricalEvidencePackage.SourceCommitSha));
    }

    [Theory]
    [InlineData("size")]
    [InlineData("sha256")]
    [InlineData("git-oid")]
    public void Payload_requires_all_three_independent_bindings(string mutation)
    {
        var bytes = Encoding.UTF8.GetBytes("{\"synthetic\":true}");
        var binding = Binding(bytes);
        binding = mutation switch
        {
            "size" => binding with { SizeBytes = bytes.Length + 1 },
            "sha256" => binding with { Sha256 = new string('0', 64) },
            _ => binding with { GitBlobOid = new string('0', 40) }
        };
        Assert.Throws<InvalidOperationException>(() => HistoricalEvidencePackage.VerifyPayload("output/example.json", bytes, binding));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Verified_utf8_json_preserves_text_and_historical_BOM_read_semantics(bool bom)
    {
        const string text = "{\"synthetic\":true}";
        var bytes = Encoding.UTF8.GetBytes(text);
        if (bom) bytes = new byte[] { 0xef, 0xbb, 0xbf }.Concat(bytes).ToArray();
        Assert.Equal(text, HistoricalEvidencePackage.VerifyPayload("output/example.json", bytes, Binding(bytes)));
    }

    [Fact]
    public void Malformed_json_or_encoding_has_no_payload_or_parser_exception_in_error()
    {
        foreach (var bytes in new[] { Encoding.UTF8.GetBytes("{\"synthetic-private-canary\": ???}"), new byte[] { 0xff, 0xfe, 0x01 } })
        {
            var failure = Assert.Throws<InvalidOperationException>(() => HistoricalEvidencePackage.VerifyPayload("output/example.json", bytes, Binding(bytes)));
            Assert.Equal("Historical evidence contract failed: payload-format.", failure.Message);
            Assert.Null(failure.InnerException);
        }
    }

    [Fact]
    public void Malformed_manifest_has_no_payload_or_parser_exception_in_error()
    {
        var bytes = Encoding.UTF8.GetBytes("{\"synthetic-private-canary\": ???}");
        var failure = Assert.Throws<InvalidOperationException>(() => HistoricalEvidencePackage.ValidateManifest(bytes,
            HistoricalEvidencePackage.Digest(bytes), HistoricalEvidencePackage.DeclaredInputs, HistoricalEvidencePackage.SourceCommitSha));
        Assert.Equal("Historical evidence contract failed: manifest-invalid.", failure.Message);
        Assert.Null(failure.InnerException);
    }

    [Theory]
    [InlineData("/synthetic/package", "manifest.json", true)]
    [InlineData("/synthetic/package", "output/declared.json", true)]
    [InlineData("relative/package", "manifest.json", false)]
    [InlineData("/synthetic/../package", "manifest.json", false)]
    [InlineData("/synthetic/./package", "manifest.json", false)]
    [InlineData("/", "manifest.json", false)]
    [InlineData("/synthetic/package", "../manifest.json", false)]
    [InlineData("/synthetic/package", "/manifest.json", false)]
    [InlineData("/synthetic/package", "output//manifest.json", false)]
    [InlineData("/synthetic/package", "output\\manifest.json", false)]
    public void Package_components_reject_traversal_and_ambiguous_separators(string root, string path, bool expected)
        => Assert.Equal(expected, HistoricalEvidencePackage.IsSafePackagePath(root, path));

    [Theory]
    [InlineData(0x205, 1, 0x8000, true)]
    [InlineData(0x205, 2, 0x8000, false)]
    [InlineData(0x205, 0, 0x8000, false)]
    [InlineData(0x200, 1, 0x8000, false)]
    [InlineData(0x205, 1, 0xa000, false)]
    [InlineData(0x205, 1, 0x4000, false)]
    [InlineData(0x205, 1, 0x1000, false)]
    public void Open_descriptor_metadata_requires_regular_single_link(uint mask, uint links, ushort mode, bool expected)
        => Assert.Equal(expected, HistoricalEvidencePackage.IsRegularSingleLink(mask, links, mode));

    private static HistoricalEvidencePackage.InputBinding Binding(byte[] bytes) =>
        new(bytes.Length, HistoricalEvidencePackage.GitBlobOid(bytes), HistoricalEvidencePackage.Digest(bytes));

    private static (JsonObject Root, IReadOnlyDictionary<string, HistoricalEvidencePackage.ExpectedInput> Expected) CreateManifest()
    {
        var bytes = Encoding.UTF8.GetBytes("{\"synthetic\":true}");
        var expected = HistoricalEvidencePackage.DeclaredInputs.Keys.ToDictionary(path => path,
            _ => new HistoricalEvidencePackage.ExpectedInput(bytes.Length, HistoricalEvidencePackage.GitBlobOid(bytes)), StringComparer.Ordinal);
        var root = new JsonObject
        {
            ["schemaVersion"] = 1,
            ["sourceCommitSha"] = HistoricalEvidencePackage.SourceCommitSha,
            ["entries"] = new JsonArray(expected.Select(pair => (JsonNode)new JsonObject
            {
                ["path"] = pair.Key,
                ["sizeBytes"] = pair.Value.SizeBytes,
                ["gitBlobOid"] = pair.Value.GitBlobOid,
                ["sha256"] = HistoricalEvidencePackage.Digest(bytes)
            }).ToArray())
        };
        return (root, expected);
    }
}
