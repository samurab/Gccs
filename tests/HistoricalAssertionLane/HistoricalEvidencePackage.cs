using System.Collections.Frozen;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Win32.SafeHandles;

namespace Gccs.Api.Tests;

// The package and approved manifest digest must be supplied by a separately authorized trusted runner.
internal static class HistoricalEvidencePackage
{
    internal const string SourceCommitSha = "e9978a59caac28c6009188edb3ac8d242a70e82a";
    internal const string RootVariable = "FEDRIL_HISTORICAL_EVIDENCE_ROOT";
    internal const string ManifestHashVariable = "FEDRIL_HISTORICAL_EVIDENCE_MANIFEST_SHA256";
    private const int MaximumFileBytes = 1024 * 1024;

    internal sealed record ExpectedInput(int SizeBytes, string GitBlobOid);
    internal sealed record InputBinding(int SizeBytes, string GitBlobOid, string Sha256);

    internal static readonly IReadOnlyDictionary<string, ExpectedInput> DeclaredInputs =
        new Dictionary<string, ExpectedInput>(StringComparer.Ordinal)
    {
        ["output/playwright/production-readiness/pr-3.3/admin-cycle-and-cleanup.json"] = new(2823, "dfa7c9b7142e40d3ed09a99ca207bc90ed177b66"),
        ["output/playwright/production-readiness/pr-3.3/orphan-tenant-cleanup.json"] = new(1370, "d99b1685031b03b4a75bddc0296c2a08906d0841"),
        ["output/playwright/production-readiness/pr-3.3/orphan-tenant-firewall-cleanup.json"] = new(3, "fe51488c7066f6687ef680d6bfaa4f7768ef205c"),
        ["output/playwright/production-readiness/pr-3.3/owner-session-probes.json"] = new(3450, "74c1a546f3e895930d5d9187c26efd0e038515d6"),
        ["output/playwright/production-readiness/pr-3.3/role-matrix-admin.json"] = new(9556, "caabda97b6618dc5f0366eded6b6496a6aba3ef0"),
        ["output/playwright/production-readiness/pr-3.3/role-matrix-advisor.json"] = new(9560, "1752cf8b4fcb5f7e5ca825bdf0ed0ff2b3ac81c9"),
        ["output/playwright/production-readiness/pr-3.3/role-matrix-auditor.json"] = new(9665, "8a743a3359577f86fc6ad0344618a5f1e99d92c2"),
        ["output/playwright/production-readiness/pr-3.3/role-matrix-compliance-manager.json"] = new(9710, "6abb4b5dc2733213e892af8d07cc8f9a8024c922"),
        ["output/playwright/production-readiness/pr-3.3/role-matrix-contributor.json"] = new(9687, "2ce51d11eb6fa62a297149590c019694f180c526"),
        ["output/playwright/production-readiness/pr-3.3/role-matrix-firewall-cleanup.json"] = new(3, "fe51488c7066f6687ef680d6bfaa4f7768ef205c"),
        ["output/playwright/production-readiness/pr-3.3/role-matrix-fixture-cleanup.json"] = new(475, "345dd6f02f6da26ebc860ce4a138743558406691"),
        ["output/playwright/production-readiness/pr-3.3/role-matrix-local-secret-cleanup.json"] = new(58, "b0e1d82dbb60d070006eacb2ab54a6b29f7d100f"),
        ["output/playwright/production-readiness/pr-3.3/role-matrix-no-mutation-summary.json"] = new(272, "c8d56fcbb1a5d1137d103bde15888ce280f7182e"),
        ["output/playwright/production-readiness/pr-3.3/role-matrix-owner.json"] = new(9008, "6c4eee785fbbe969812b04ee083d3de2d1fd24e7"),
        ["output/playwright/production-readiness/pr-3.4/authenticated-upload-report-smoke.json"] = new(8871, "86664bafb7f3e5f42688dba50bbf1c24122d7c2d"),
        ["output/playwright/production-readiness/pr-3.4/authentication-blocker.json"] = new(1103, "168353109f061cbdefe8f8096d3c8a3e9360a93f"),
        ["output/playwright/production-readiness/pr-3.4/staging-health.json"] = new(513, "a527e612298ef1bba923428d86e26ed5ef0cab94"),
        ["output/playwright/production-readiness/pr-7.2/authenticated-production-smoke.json"] = new(8746, "0ab9feeaa7e403b42c326fcd6a570e3512da6c3a"),
        ["output/playwright/production-readiness/pr-7.3/pilot-onboarding-evidence.json"] = new(2892, "66d3d968712e7436245093ded58e1b5e71073440"),
        ["output/playwright/production-readiness/pr-8.1/daily-monitoring-2026-07-22.json"] = new(9838, "1337089a8563bd3b87f37f8b537a7a3acbdc633c"),
        ["output/playwright/production-readiness/pr-8.1/pilot-monitoring-evidence.json"] = new(1964, "d763d3e041e4d0a5f1bf6e8eeb4e3ffbac1ef53f"),
        ["output/playwright/production-readiness/pr-8.2/post-launch-readiness-review.json"] = new(3000, "db5c95e00f3dd9e87de37cb3d6237d0f487e0f76"),
        ["output/playwright/production-readiness/pr-8.3/phase-2-gate.json"] = new(5191, "8ba3f0b33dd516eaafeeeb5349d3b9c19184cc04"),
        ["output/production-readiness/backup-restore/restore-rehearsal-summary.json"] = new(953, "89d605852bd29cd7e88707a5c392f4ef6dcebe04"),
        ["output/production-readiness/backup-restore/staging-postgres-backup-config.json"] = new(626, "47b4e9b7c62b4ede39f2bc31b64607e226d3c6a0"),
        ["output/production-readiness/customer-claims-review.json"] = new(5964, "36cbee6b3414a65b4502348959b2a229a0362df1"),
        ["output/production-readiness/deployment-migration-rollback/gccs-staging-migrations.sql"] = new(221175, "65ad8819fc6578bb5b73ff27cf5d40efbe2f42cb"),
        ["output/production-readiness/expert-content/high-risk-obligation-review.json"] = new(5640, "1d8cab08a80955906c7ba67937dd6c4f25e99f68"),
        ["output/production-readiness/expert-content/staging-content-review-summary.json"] = new(2314, "51d25c0916ce9cfb1548da7f9d64ce0bca7a2452"),
    }.ToFrozenDictionary(StringComparer.Ordinal);

    internal static bool IsDeclaredInput(string logicalPath) => DeclaredInputs.ContainsKey(logicalPath);

    internal static string ReadText(string logicalPath)
    {
        if (!IsDeclaredInput(logicalPath)) throw Failure("unlisted-input");
        var root = Environment.GetEnvironmentVariable(RootVariable);
        var approvedDigest = Environment.GetEnvironmentVariable(ManifestHashVariable);
        if (string.IsNullOrWhiteSpace(root) || !IsHex(approvedDigest, 64)) throw Failure("package-not-configured");

        var manifestBytes = ReadRegularFile(root, "manifest.json", null, 128 * 1024);
        var manifest = ValidateManifest(manifestBytes, approvedDigest!, DeclaredInputs, SourceCommitSha);
        var binding = manifest[logicalPath];
        var bytes = ReadRegularFile(root, logicalPath, binding.SizeBytes, MaximumFileBytes);
        return VerifyPayload(logicalPath, bytes, binding);
    }

    internal static IReadOnlyDictionary<string, InputBinding> ValidateManifest(
        byte[] bytes, string approvedDigest, IReadOnlyDictionary<string, ExpectedInput> expected, string sourceCommit)
    {
        if (!IsHex(approvedDigest, 64) || Digest(bytes) != approvedDigest) throw Failure("manifest-digest");
        try
        {
            using var document = JsonDocument.Parse(bytes);
            var root = document.RootElement;
            RequireProperties(root, "schemaVersion", "sourceCommitSha", "entries");
            if (root.GetProperty("schemaVersion").GetInt32() != 1 ||
                root.GetProperty("sourceCommitSha").GetString() != sourceCommit) throw Failure("manifest-source");
            var entries = root.GetProperty("entries");
            if (entries.ValueKind != JsonValueKind.Array || entries.GetArrayLength() != expected.Count)
                throw Failure("manifest-population");
            var result = new Dictionary<string, InputBinding>(StringComparer.Ordinal);
            foreach (var entry in entries.EnumerateArray())
            {
                RequireProperties(entry, "path", "sizeBytes", "gitBlobOid", "sha256");
                var path = entry.GetProperty("path").GetString();
                var oid = entry.GetProperty("gitBlobOid").GetString();
                var digest = entry.GetProperty("sha256").GetString();
                var size = entry.GetProperty("sizeBytes").GetInt32();
                if (path is null || !expected.TryGetValue(path, out var source) || size != source.SizeBytes ||
                    size < 0 || size > MaximumFileBytes || oid != source.GitBlobOid || !IsHex(oid, 40) ||
                    !IsHex(digest, 64) || !result.TryAdd(path, new(size, oid, digest!)))
                    throw Failure("manifest-binding");
            }
            return result;
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException or FormatException or OverflowException)
        {
            // Neither parser paths/characters nor nested exceptions may enter test logs.
            throw Failure("manifest-invalid");
        }
    }

    internal static string VerifyPayload(string logicalPath, byte[] bytes, InputBinding binding)
    {
        if (bytes.Length != binding.SizeBytes || Digest(bytes) != binding.Sha256 || GitBlobOid(bytes) != binding.GitBlobOid)
            throw Failure("payload-binding");
        try
        {
            var offset = bytes.AsSpan().StartsWith(new byte[] { 0xef, 0xbb, 0xbf }) ? 3 : 0;
            var text = new UTF8Encoding(false, true).GetString(bytes.AsSpan(offset));
            if (logicalPath.EndsWith(".json", StringComparison.Ordinal))
            {
                using var parsed = JsonDocument.Parse(text);
            }
            return text;
        }
        catch (Exception exception) when (exception is DecoderFallbackException or JsonException)
        {
            throw Failure("payload-format");
        }
    }

    internal static string Digest(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));

    internal static string GitBlobOid(byte[] bytes)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA1);
        hash.AppendData(Encoding.ASCII.GetBytes("blob " + bytes.Length.ToString(CultureInfo.InvariantCulture) + "\0"));
        hash.AppendData(bytes);
        return Convert.ToHexStringLower(hash.GetHashAndReset());
    }

    private static void RequireProperties(JsonElement element, params string[] expected)
    {
        if (element.ValueKind != JsonValueKind.Object) throw Failure("manifest-shape");
        var names = element.EnumerateObject().Select(property => property.Name).ToArray();
        if (names.Length != expected.Length || names.Distinct(StringComparer.Ordinal).Count() != names.Length ||
            names.Any(name => !expected.Contains(name, StringComparer.Ordinal))) throw Failure("manifest-shape");
    }

    private static bool IsHex(string? value, int length) => value is not null && value.Length == length &&
        value.All(character => character is >= '0' and <= '9' or >= 'a' and <= 'f');

    private static InvalidOperationException Failure(string code) => new("Historical evidence contract failed: " + code + ".");

    private static byte[] ReadRegularFile(string root, string logicalPath, int? expectedSize, int maximum)
    {
        // Linux hosted runners are the supported package-consumption boundary. Other systems fail closed.
        if (!OperatingSystem.IsLinux()) throw Failure("unsupported-package-platform");
        var openFlags = OpenFlagsFor(RuntimeInformation.ProcessArchitecture);
        if (!IsSafePackagePath(root, logicalPath)) throw Failure("unsafe-path");
        var parts = root.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var relative = logicalPath.Split('/');
        try
        {
            var directory = OpenAt(-100, "/", openFlags.Directory);
            try
            {
                foreach (var part in parts.Concat(relative.Take(relative.Length - 1)))
                {
                    var next = OpenAt(directory.DangerousGetHandle().ToInt32(), part, openFlags.Directory);
                    directory.Dispose();
                    directory = next;
                }
                using var file = OpenAt(directory.DangerousGetHandle().ToInt32(), relative[^1], openFlags.File);
                var before = Inspect(file);
                if (before.Size < 0 || before.Size > maximum || (expectedSize.HasValue && before.Size != expectedSize.Value))
                    throw Failure("file-size");
                var size = checked((int)before.Size);
                var buffer = new byte[size + 1];
                using var stream = new FileStream(file, FileAccess.Read, 4096, false);
                var count = 0;
                while (count < buffer.Length)
                {
                    var read = stream.Read(buffer.AsSpan(count));
                    if (read == 0) break;
                    count += read;
                }
                var after = Inspect(file);
                if (count != size || after.Size != size) throw Failure("file-changed");
                return buffer.AsSpan(0, count).ToArray();
            }
            finally { directory.Dispose(); }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or DllNotFoundException or EntryPointNotFoundException)
        {
            throw Failure("package-unavailable");
        }
    }

    internal static bool IsSafePackagePath(string root, string logicalPath)
    {
        var parts = root.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var relative = logicalPath.Split('/');
        return root.StartsWith('/') && !root.Contains('\\') && parts.Length > 0 &&
            !parts.Any(part => part is "." or "..") && !logicalPath.StartsWith('/') && !logicalPath.Contains('\\') &&
            !relative.Any(part => part.Length == 0 || part is "." or "..");
    }

    internal static bool IsRegularSingleLink(uint mask, uint links, ushort mode) =>
        (mask & 0x205) == 0x205 && (mode & 0xf000) == 0x8000 && links == 1;

    // Linux arm64 overrides O_DIRECTORY and O_NOFOLLOW from the generic UAPI values.
    // Keep the supported architectures explicit; unknown native ABIs fail before openat.
    internal static (int Directory, int File) OpenFlagsFor(Architecture architecture) => architecture switch
    {
        Architecture.X64 => (0x10000 | 0x20000 | 0x80000, 0x20000 | 0x80000 | 0x800),
        Architecture.Arm64 => (0x4000 | 0x8000 | 0x80000, 0x8000 | 0x80000 | 0x800),
        _ => throw Failure("unsupported-package-architecture")
    };

    private static SafeFileHandle OpenAt(int parent, string name, int flags)
    {
        var descriptor = NativeOpenAt(parent, name, flags);
        if (descriptor < 0) throw Failure("unsafe-or-unavailable-file");
        return new SafeFileHandle((IntPtr)descriptor, ownsHandle: true);
    }

    private static FileMetadata Inspect(SafeFileHandle file)
    {
        const uint required = 0x205; // STATX_TYPE | STATX_NLINK | STATX_SIZE
        if (NativeStatx(file.DangerousGetHandle().ToInt32(), "", 0x1100, required, out var metadata) != 0 ||
            !IsRegularSingleLink(metadata.Mask, metadata.LinkCount, metadata.Mode))
            throw Failure("not-regular-single-link");
        return metadata;
    }

    [StructLayout(LayoutKind.Explicit, Size = 256)]
    private struct FileMetadata
    {
        [FieldOffset(0)] public uint Mask;
        [FieldOffset(16)] public uint LinkCount;
        [FieldOffset(28)] public ushort Mode;
        [FieldOffset(40)] public long Size;
    }

    [DllImport("libc", EntryPoint = "openat", SetLastError = true)]
    private static extern int NativeOpenAt(int parent, [MarshalAs(UnmanagedType.LPUTF8Str)] string name, int flags);

    [DllImport("libc", EntryPoint = "statx", SetLastError = true)]
    private static extern int NativeStatx(int descriptor, [MarshalAs(UnmanagedType.LPUTF8Str)] string path,
        int flags, uint mask, out FileMetadata metadata);
}
