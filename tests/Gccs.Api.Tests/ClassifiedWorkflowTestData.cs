using System.Net.Http.Json;
using System.Text.Json.Nodes;
namespace Gccs.Api.Tests;

internal static class ClassifiedWorkflowTestData
{
    // Explicit consent for pre-existing feature scenarios, not used by classification-boundary tests.
    public static HttpRequestMessage Confirm(HttpRequestMessage request)
    {
        var path = request.RequestUri?.OriginalString.Split('?')[0] ?? "";
        var evidenceMetadata = path == "/api/evidence-items" ||
            (path.StartsWith("/api/evidence-items/") && Guid.TryParse(path.Split('/').Last(), out _));
        if (!(request.Method == HttpMethod.Post || request.Method == HttpMethod.Put) || !(path.EndsWith("/extraction-jobs") ||
            path.EndsWith("/upload-intents") || evidenceMetadata ||
            path is "/api/reports/compliance-status" or "/api/reports/cmmc-readiness" or "/api/reports/subcontractor-compliance" or "/api/reports/evidence-packages"))
            return request;
        var body = request.Content is null ? new JsonObject() : JsonNode.Parse(request.Content.ReadAsStringAsync().GetAwaiter().GetResult()) as JsonObject ?? new JsonObject();
        if (body["classification"] is null) body["classification"] = new JsonObject {
            ["classification"] = body["containsPotentialCui"]?.GetValue<bool>() == true ? "Cui" : "Unclassified" };
        request.Content?.Dispose(); request.Content = JsonContent.Create(body);
        return request;
    }
}
