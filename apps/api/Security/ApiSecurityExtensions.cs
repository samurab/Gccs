using System.Security.Claims;
using System.Threading.RateLimiting;
using Gccs.Application.Audit;
using Gccs.Application.Security;
using Gccs.Domain.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;

namespace Gccs.Api.Security;

public static class ApiSecurityExtensions
{
    public const string DevelopmentAuthenticationScheme = "Development";
    public const string JwtAuthenticationScheme = JwtBearerDefaults.AuthenticationScheme;
    public const string PermissionClaimType = "permission";
    public const string RoleNameClaimType = "gccs_role";
    public const string TenantIdClaimType = "tenant_id";

    public static IServiceCollection AddGccsApiSecurity(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        var developmentAuthEnabled = environment.IsDevelopment() &&
            configuration.GetValue("Security:DevelopmentAuth:Enabled", true);

        if (developmentAuthEnabled)
        {
            services.AddAuthentication(DevelopmentAuthenticationScheme)
                .AddScheme<DevelopmentAuthenticationOptions, DevelopmentAuthenticationHandler>(
                    DevelopmentAuthenticationScheme,
                    options =>
                    {
                        options.DefaultTenantId = configuration.GetValue(
                            "Security:DevelopmentAuth:DefaultTenantId",
                            DevelopmentAuthenticationOptions.FallbackTenantId);
                        options.DefaultUserId = configuration.GetValue(
                            "Security:DevelopmentAuth:DefaultUserId",
                            DevelopmentAuthenticationOptions.FallbackUserId);
                        options.DefaultEmail = configuration.GetValue(
                            "Security:DevelopmentAuth:DefaultEmail",
                            "developer@gccs.local");
                    });
        }
        else
        {
            var authority = configuration["Authentication:Authority"];
            var audience = configuration["Authentication:Audience"];

            if (string.IsNullOrWhiteSpace(authority) || string.IsNullOrWhiteSpace(audience))
            {
                throw new InvalidOperationException(
                    "Authentication:Authority and Authentication:Audience must be configured outside local development auth.");
            }

            services.AddAuthentication(JwtAuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Authority = authority;
                    options.Audience = audience;
                    options.RequireHttpsMetadata = !environment.IsDevelopment();
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        NameClaimType = ClaimTypes.Email,
                        RoleClaimType = PermissionClaimType
                    };
                });
        }

        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();

            foreach (var permission in Enum.GetValues<Permission>())
            {
                options.AddPolicy(permission.ToString(), policy =>
                    policy.RequireAuthenticatedUser()
                        .RequireClaim(PermissionClaimType, permission.ToString()));
            }
        });
        services.AddTransient<IClaimsTransformation, RolePermissionClaimsTransformation>();
        services.AddSingleton<IAuthorizationMiddlewareResultHandler, ProblemDetailsAuthorizationResultHandler>();

        services.AddHttpContextAccessor();
        services.AddScoped<ITenantContext, HttpTenantContext>();
        services.AddScoped<ICurrentTenantContext>(serviceProvider => serviceProvider.GetRequiredService<ITenantContext>());
        services.AddScoped<IAuditRequestMetadata, HttpAuditRequestMetadata>();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy("api", httpContext =>
            {
                var partitionKey =
                    httpContext.User.FindFirstValue(TenantIdClaimType) ??
                    httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                    httpContext.Connection.RemoteIpAddress?.ToString() ??
                    "anonymous";

                return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
                {
                    AutoReplenishment = true,
                    PermitLimit = configuration.GetValue("Security:RateLimiting:PermitLimit", 120),
                    QueueLimit = 0,
                    Window = TimeSpan.FromMinutes(configuration.GetValue("Security:RateLimiting:WindowMinutes", 1))
                });
            });
        });

        return services;
    }

    public static IApplicationBuilder UseGccsSecurityHeaders(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            var headers = context.Response.Headers;
            headers.TryAdd("X-Content-Type-Options", "nosniff");
            headers.TryAdd("X-Frame-Options", "DENY");
            headers.TryAdd("Referrer-Policy", "no-referrer");
            headers.TryAdd("Permissions-Policy", "camera=(), microphone=(), geolocation=()");

            if (context.Request.Path.StartsWithSegments("/api"))
            {
                headers.TryAdd("Cache-Control", "no-store");
            }

            await next();
        });
    }

    public static IApplicationBuilder UseGccsCorrelationIds(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            var correlationId = ApiCorrelation.Ensure(context);
            context.Response.OnStarting(() =>
            {
                context.Response.Headers[ApiCorrelation.HeaderName] = correlationId;
                return Task.CompletedTask;
            });

            await next();
        });
    }

    public static IApplicationBuilder UseGccsApiFailureLogging(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            await next();

            if (!context.Request.Path.StartsWithSegments("/api") ||
                context.Response.StatusCode < StatusCodes.Status400BadRequest)
            {
                return;
            }

            var correlationId = ApiCorrelation.Get(context);
            var loggerFactory = context.RequestServices.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("Gccs.Api.Security.ApiFailureLogging");
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
            var tenantId = context.User.FindFirstValue(TenantIdClaimType) ?? "none";

            if (context.Response.StatusCode >= StatusCodes.Status500InternalServerError)
            {
                logger.LogError(
                    "API request failed. StatusCode={StatusCode} Method={Method} Path={Path} CorrelationId={CorrelationId} TraceId={TraceId} UserId={UserId} TenantId={TenantId}",
                    context.Response.StatusCode,
                    context.Request.Method,
                    context.Request.Path.Value,
                    correlationId,
                    context.TraceIdentifier,
                    userId,
                    tenantId);
                return;
            }

            logger.LogWarning(
                "API request failed. StatusCode={StatusCode} Method={Method} Path={Path} CorrelationId={CorrelationId} TraceId={TraceId} UserId={UserId} TenantId={TenantId}",
                context.Response.StatusCode,
                context.Request.Method,
                context.Request.Path.Value,
                correlationId,
                context.TraceIdentifier,
                userId,
                tenantId);
        });
    }

    public static IApplicationBuilder UseGccsApiProblemDetails(this IApplicationBuilder app)
    {
        return app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
                if (exception is null)
                {
                    return;
                }

                if (!context.Request.Path.StartsWithSegments("/api"))
                {
                    throw exception;
                }

                var (statusCode, title, detail, errorCode) = exception switch
                {
                    AuditWriteException => (
                        StatusCodes.Status500InternalServerError,
                        "Critical audit failure",
                        "A required audit event could not be written for this compliance-relevant action.",
                        "audit_write_failed"),
                    MissingTenantContextException => (
                        StatusCodes.Status400BadRequest,
                        "Tenant context required",
                        "An active tenant context is required for this tenant-scoped API request.",
                        "missing_tenant_context"),
                    InvalidUserContextException => (
                        StatusCodes.Status400BadRequest,
                        "User context required",
                        "A valid authenticated user context is required for this API request.",
                        "invalid_user_context"),
                    _ => (
                        StatusCodes.Status500InternalServerError,
                        "API request failed",
                        "The API request could not be completed.",
                        "api_request_failed")
                };

                await ApiProblemDetails
                    .Create(context, title, detail, statusCode, errorCode)
                    .ExecuteAsync(context);
            });
        });
    }

    public static IEndpointConventionBuilder RequirePermission(
        this IEndpointConventionBuilder builder,
        Permission permission)
    {
        return builder.RequireAuthorization(permission.ToString());
    }
}
