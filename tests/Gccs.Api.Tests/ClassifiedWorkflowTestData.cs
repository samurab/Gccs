using System.Net.Http.Json;
using System.Text.Json.Nodes;
namespace Gccs.Api.Tests;

internal static class ClassifiedWorkflowTestData
{
    // Explicit consent for pre-existing feature scenarios, not used by classification-boundary tests.
    public static HttpRequestMessage Confirm(HttpRequestMessage request)
    {
        var path = request.RequestUri?.OriginalString.Split('?')[0] ?? "";
        if (request.Method != HttpMethod.Post || !(path.EndsWith("/extraction-jobs") ||
            path is "/api/reports/compliance-status" or "/api/reports/cmmc-readiness" or "/api/reports/subcontractor-compliance" or "/api/reports/evidence-packages"))
            return request;
        var body = request.Content is null ? new JsonObject() : JsonNode.Parse(request.Content.ReadAsStringAsync().GetAwaiter().GetResult()) as JsonObject ?? new JsonObject();
        if (body["classification"] is null) body["classification"] = new JsonObject { ["classification"] = "Unclassified" };
        request.Content?.Dispose(); request.Content = JsonContent.Create(body);
        return request;
    }
}
