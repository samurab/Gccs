using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Routing;
using OpenTelemetry.Instrumentation.AspNetCore;

namespace Gccs.Api.Security;

public static class ApiTelemetryPrivacy
{
    public static IServiceCollection AddGccsTelemetryPrivacy(
        this IServiceCollection services, IConfiguration configuration)
    {
        // Application Insights otherwise defaults these opt-outs to true.
        configuration["ASPNETCORE_DISABLE_URL_QUERY_REDACTION"] = bool.FalseString;
        configuration["HTTPCLIENT_DISABLE_URL_QUERY_REDACTION"] = bool.FalseString;

        services.Configure<AspNetCoreTraceInstrumentationOptions>(options =>
        {
            var previousEnrichment = options.EnrichWithHttpResponse;
            options.EnrichWithHttpResponse = (activity, response) =>
            {
                previousEnrichment?.Invoke(activity, response);
                var exceptionEndpoint = response.HttpContext.Features.Get<IExceptionHandlerFeature>()?.Endpoint;
                var route = ((exceptionEndpoint ?? response.HttpContext.GetEndpoint()) as RouteEndpoint)?.RoutePattern.RawText;
                activity.SetTag("url.path", route ?? "unmatched");
                activity.SetTag("url.query", null);
                activity.SetTag("user_agent.original", null);
            };
        });

        return services;
    }
}
