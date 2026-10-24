using System.Net;
using System.Net.Sockets;
using System.Text;
using Azure;
using Azure.Storage.Blobs;
using Gccs.Application.Storage;
using Gccs.Infrastructure.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class DurableObjectWriteIntentTests
{
    [RetainedIntentProviderFact]
    public async Task Create_only_conflict_preserves_original_bytes_and_etag()
    {
        var storage = CreateRetainedStorage();
        var tenant = Guid.NewGuid();
        var name = $"intent/{Guid.NewGuid():N}.txt";
        var original = await UploadAsync(storage, tenant, name, "Synthetic original No-CUI bytes.");

        var failure = await Assert.ThrowsAsync<RequestFailedException>(() =>
            UploadAsync(storage, tenant, name, "Synthetic conflicting bytes."));
        Assert.Contains(failure.Status, new[] { 409, 412 });
        Assert.False(string.IsNullOrWhiteSpace(original.ETag));
        await using var read = await storage.OpenReadAsync(new(tenant, ObjectStorageContainer.Evidence, name));
        Assert.NotNull(read);
        Assert.Equal(original.ETag, read.ETag);
        using var reader = new StreamReader(read.Content);
        Assert.Equal("Synthetic original No-CUI bytes.", await reader.ReadToEndAsync());
    }

    [RetainedIntentProviderFact]
    public async Task Concurrent_create_only_writers_have_one_winner_and_retain_winner_content()
    {
        var storage = CreateRetainedStorage();
        var tenant = Guid.NewGuid();
        var name = $"intent/{Guid.NewGuid():N}.txt";
        async Task<(string Content, ObjectStorageWriteResult? Result, int? Status)> Attempt(string content)
        {
            try { return (content, await UploadAsync(storage, tenant, name, content), null); }
            catch (RequestFailedException exception) { return (content, null, exception.Status); }
        }

        var attempts = await Task.WhenAll(Attempt("Synthetic writer A."), Attempt("Synthetic writer B."));
        var winner = Assert.Single(attempts, attempt => attempt.Result is not null);
        Assert.Contains(Assert.Single(attempts, attempt => attempt.Result is null).Status, new int?[] { 409, 412 });
        await using var read = await storage.OpenReadAsync(new(tenant, ObjectStorageContainer.Evidence, name));
        Assert.NotNull(read);
        Assert.Equal(winner.Result!.ETag, read.ETag);
        using var reader = new StreamReader(read.Content);
        Assert.Equal(winner.Content, await reader.ReadToEndAsync());
    }

    [RetainedIntentProviderFact]
    public async Task Same_attempt_name_in_another_tenant_cannot_read_or_conflict_with_owner()
    {
        var storage = CreateRetainedStorage();
        var owner = Guid.NewGuid();
        var other = Guid.NewGuid();
        var name = $"intent/{Guid.NewGuid():N}.txt";
        var first = await UploadAsync(storage, owner, name, "Synthetic owner bytes.");
        Assert.Null(await storage.OpenReadAsync(new(other, ObjectStorageContainer.Evidence, name)));
        var second = await UploadAsync(storage, other, name, "Synthetic other tenant bytes.");
        Assert.NotEqual(first.BlobName, second.BlobName);
        await using var read = await storage.OpenReadAsync(new(owner, ObjectStorageContainer.Evidence, name));
        Assert.NotNull(read);
        Assert.Equal(first.ETag, read.ETag);
        using var reader = new StreamReader(read.Content);
        Assert.Equal("Synthetic owner bytes.", await reader.ReadToEndAsync());
    }

    // This responder records SDK requests; it has no backing objects and cannot delete stored data.
    [Theory]
    [InlineData(202)]
    [InlineData(404)]
    [InlineData(412)]
    public async Task Conditional_delete_sends_expected_version_and_preserves_provider_outcome(int status)
    {
        using var reservation = new TcpListener(IPAddress.Loopback, 0);
        reservation.Start();
        var port = ((IPEndPoint)reservation.LocalEndpoint).Port;
        reservation.Stop();
        using var listener = new HttpListener();
        listener.Prefixes.Add($"http://127.0.0.1:{port}/");
        listener.Start();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var captured = RespondAsync();
        var connection = $"DefaultEndpointsProtocol=http;AccountName=synthetic;AccountKey={Convert.ToBase64String(new byte[32])};BlobEndpoint=http://127.0.0.1:{port}/synthetic;";
        var storage = CreateStorage(connection, "synthetic-intent-tests");
        var request = new ObjectStorageReadRequest(Guid.NewGuid(), ObjectStorageContainer.Evidence,
            "intent/synthetic.txt", ExpectedETag: "\"synthetic-original-version\"");
        if (status == 412)
        {
            var failure = await Assert.ThrowsAsync<RequestFailedException>(() => storage.DeleteAsync(request, timeout.Token));
            Assert.Equal(412, failure.Status);
        }
        else
        {
            Assert.Equal(status == 202, await storage.DeleteAsync(request, timeout.Token));
        }
        var recorded = await captured;
        Assert.Equal("DELETE", recorded.Method);
        Assert.Equal(request.ExpectedETag, recorded.IfMatch);
        Assert.Equal("include", recorded.Snapshots);
        Assert.Contains(ObjectStorageNames.BuildTenantBlobName(request.TenantId, request.ObjectName), recorded.Path);

        async Task<(string Method, string? IfMatch, string? Snapshots, string Path)> RespondAsync()
        {
            var context = await listener.GetContextAsync().WaitAsync(timeout.Token);
            var record = (context.Request.HttpMethod, context.Request.Headers["If-Match"],
                context.Request.Headers["x-ms-delete-snapshots"], context.Request.Url!.AbsolutePath);
            context.Response.StatusCode = status;
            context.Response.Headers["x-ms-request-id"] = "synthetic-recording-request";
            if (status != 202)
            {
                var code = status == 404 ? "BlobNotFound" : "ConditionNotMet";
                context.Response.Headers["x-ms-error-code"] = code;
                context.Response.ContentType = "application/xml";
                var body = Encoding.UTF8.GetBytes($"<Error><Code>{code}</Code><Message>Synthetic provider response.</Message></Error>");
                context.Response.ContentLength64 = body.Length;
                await context.Response.OutputStream.WriteAsync(body, timeout.Token);
            }
            else context.Response.ContentLength64 = 0;
            context.Response.Close();
            if (status == 412)
            {
                var head = await listener.GetContextAsync().WaitAsync(timeout.Token);
                Assert.Equal("HEAD", head.Request.HttpMethod);
                Assert.Equal(record.Item4, head.Request.Url!.AbsolutePath);
                head.Response.StatusCode = 200;
                head.Response.Headers["ETag"] = "\"synthetic-present-version\"";
                head.Response.Headers["Last-Modified"] = DateTimeOffset.UtcNow.ToString("R");
                head.Response.ContentLength64 = 0;
                head.Response.Close();
            }
            return record;
        }
    }

    [Theory]
    [InlineData(404)]
    [InlineData(403)]
    [InlineData(-1)]
    public async Task Conditional_412_reconciles_only_confirmed_absence_and_preserves_read_failure_or_cancellation(int headStatus)
    {
        using var reservation = new TcpListener(IPAddress.Loopback, 0);
        reservation.Start(); var port = ((IPEndPoint)reservation.LocalEndpoint).Port; reservation.Stop();
        using var listener = new HttpListener(); listener.Prefixes.Add($"http://127.0.0.1:{port}/"); listener.Start();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var connection = $"DefaultEndpointsProtocol=http;AccountName=synthetic;AccountKey={Convert.ToBase64String(new byte[32])};BlobEndpoint=http://127.0.0.1:{port}/synthetic;";
        var storage = CreateStorage(connection, "synthetic-intent-tests");
        var request = new ObjectStorageReadRequest(Guid.NewGuid(), ObjectStorageContainer.Evidence, "intent/synthetic.txt", "\"expected-version\"");
        var responder = RespondAsync();
        if (headStatus == 404) Assert.False(await storage.DeleteAsync(request, timeout.Token));
        else if (headStatus == -1) await Assert.ThrowsAnyAsync<OperationCanceledException>(() => storage.DeleteAsync(request, timeout.Token));
        else Assert.Equal(403, (await Assert.ThrowsAsync<RequestFailedException>(() => storage.DeleteAsync(request, timeout.Token))).Status);
        await responder;

        async Task RespondAsync()
        {
            var delete = await listener.GetContextAsync().WaitAsync(timeout.Token);
            Assert.Equal("DELETE", delete.Request.HttpMethod);
            Assert.Equal(request.ExpectedETag, delete.Request.Headers["If-Match"]);
            var path = delete.Request.Url!.AbsolutePath;
            delete.Response.StatusCode = 412; delete.Response.Headers["x-ms-error-code"] = "ConditionNotMet";
            delete.Response.ContentLength64 = 0; delete.Response.Close();
            var head = await listener.GetContextAsync().WaitAsync(timeout.Token);
            Assert.Equal("HEAD", head.Request.HttpMethod); Assert.Equal(path, head.Request.Url!.AbsolutePath);
            if (headStatus == -1)
            {
                timeout.Cancel(); head.Response.Abort(); return;
            }
            head.Response.StatusCode = headStatus;
            head.Response.Headers["x-ms-error-code"] = headStatus == 404 ? "BlobNotFound" : "AuthorizationFailure";
            head.Response.ContentLength64 = 0; head.Response.Close();
        }
    }

    [Fact]
    public async Task Conditional_412_without_expected_version_is_rethrown_without_followup_read()
    {
        using var reservation = new TcpListener(IPAddress.Loopback, 0);
        reservation.Start(); var port = ((IPEndPoint)reservation.LocalEndpoint).Port; reservation.Stop();
        using var listener = new HttpListener(); listener.Prefixes.Add($"http://127.0.0.1:{port}/"); listener.Start();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var connection = $"DefaultEndpointsProtocol=http;AccountName=synthetic;AccountKey={Convert.ToBase64String(new byte[32])};BlobEndpoint=http://127.0.0.1:{port}/synthetic;";
        var storage = CreateStorage(connection, "synthetic-intent-tests");
        var response = RespondAsync();
        var failure = await Assert.ThrowsAsync<RequestFailedException>(() => storage.DeleteAsync(
            new(Guid.NewGuid(), ObjectStorageContainer.Evidence, "intent/synthetic.txt"), timeout.Token));
        Assert.Equal(412, failure.Status); await response;
        async Task RespondAsync()
        {
            var context = await listener.GetContextAsync().WaitAsync(timeout.Token);
            Assert.Equal("DELETE", context.Request.HttpMethod); Assert.Null(context.Request.Headers["If-Match"]);
            context.Response.StatusCode = 412; context.Response.Headers["x-ms-error-code"] = "ConditionNotMet";
            context.Response.ContentLength64 = 0; context.Response.Close();
        }
    }

    private static async Task<ObjectStorageWriteResult> UploadAsync(IObjectStorageService storage,
        Guid tenant, string name, string content)
    {
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        return await storage.UploadAsync(new(tenant, ObjectStorageContainer.Evidence, name, stream,
            "text/plain", CreateOnly: true));
    }

    private static AzureBlobObjectStorageService CreateRetainedStorage()
    {
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__AzureStorage")!;
        var container = Environment.GetEnvironmentVariable("GCCS_INTENT_PROVIDER_CONTAINER")!;
        Assert.Equal("127.0.0.1", new BlobServiceClient(connection).Uri.Host);
        Assert.StartsWith("s397-intent-", container);
        return CreateStorage(connection, container);
    }

    private static AzureBlobObjectStorageService CreateStorage(string connection, string container) => new(
        Options.Create(new AzureBlobStorageOptions
        {
            UseManagedIdentity = false,
            Containers = new AzureBlobContainerOptions { Evidence = container }
        }), new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:AzureStorage"] = connection
        }).Build());
}

internal sealed class RetainedIntentProviderFactAttribute : FactAttribute
{
    public RetainedIntentProviderFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ConnectionStrings__AzureStorage")) ||
            string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GCCS_INTENT_PROVIDER_CONTAINER")))
            Skip = "Requires explicit loopback object storage and a retained synthetic intent container.";
    }
}
