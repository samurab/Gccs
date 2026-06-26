using System.Security.Claims;
using System.Text.Json;
using System.Threading.RateLimiting;
using Gccs.Application.Audit;
using Gccs.Application.Common;
using Gccs.Application.Identity;
using Gccs.Application.Security;
using Gccs.Application.Tenancy;
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
    private const string MembershipAuthorizationEnforcedKey = "Security:MembershipAuthorization:Enforce";

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
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    title = "Too many API requests",
                    detail = "The API rate limit was reached. Wait briefly and try again.",
                    status = StatusCodes.Status429TooManyRequests,
                    code = "rate_limit_exceeded"
                }, cancellationToken);
            };
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

    public static IApplicationBuilder UseGccsTenantMembershipAuthorization(
        this IApplicationBuilder app,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        var enforceMembership = configuration.GetValue(MembershipAuthorizationEnforcedKey, !environment.IsDevelopment());
        if (!enforceMembership)
        {
            return app;
        }

        return app.Use(async (context, next) =>
        {
            if (!context.Request.Path.StartsWithSegments("/api") ||
                context.User.Identity?.IsAuthenticated is not true)
            {
                await next();
                return;
            }

            var endpoint = context.GetEndpoint();
            if (endpoint?.Metadata.GetMetadata<IAllowAnonymous>() is not null)
            {
                await next();
                return;
            }

            if (!TryReadGuid(context.User, TenantIdClaimType, out _) ||
                !TryReadGuid(context.User, ClaimTypes.NameIdentifier, out _))
            {
                await next();
                return;
            }

            var repository = context.RequestServices.GetService<ITenantMembershipRepository>();
            if (repository is null)
            {
                await ApiProblemDetails
                    .Create(
                        context,
                        "Tenant membership authorization unavailable",
                        "Tenant membership authorization is required but the membership repository is not configured.",
                        StatusCodes.Status500InternalServerError,
                        "tenant_membership_authorization_unavailable")
                    .ExecuteAsync(context);
                return;
            }

            var membership = await repository.FindActiveCurrentUserMembershipAsync(context.RequestAborted);
            if (membership is null)
            {
                await ApiProblemDetails
                    .Create(
                        context,
                        "Tenant membership required",
                        "The authenticated user is not an active member of the requested tenant.",
                        StatusCodes.Status403Forbidden,
                        "tenant_membership_required")
                    .ExecuteAsync(context);
                return;
            }

            ReplaceRoleAndPermissionClaims(context.User, membership.RoleName);
            await next();
        });
    }

    public static RouteGroupBuilder RequireRouteTenantScope(this RouteGroupBuilder group)
    {
        group.AddEndpointFilter(async (context, next) =>
        {
            if (context.HttpContext.Request.RouteValues.TryGetValue("tenantId", out var routeValue) &&
                Guid.TryParse(routeValue?.ToString(), out var routeTenantId))
            {
                var tenantContext = context.HttpContext.RequestServices.GetRequiredService<ITenantContext>();
                if (routeTenantId != tenantContext.TenantId)
                {
                    return ApiProblemDetails.Create(
                        context.HttpContext,
                        "Tenant scope mismatch",
                        "The route tenant does not match the authenticated tenant context.",
                        StatusCodes.Status403Forbidden,
                        "tenant_scope_mismatch");
                }
            }

            return await next(context);
        });

        return group;
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

    private static bool TryReadGuid(ClaimsPrincipal principal, string claimType, out Guid value) =>
        Guid.TryParse(principal.FindFirstValue(claimType), out value);

    private static void ReplaceRoleAndPermissionClaims(ClaimsPrincipal principal, string roleName)
    {
        foreach (var identity in principal.Identities.Where(identity => identity.IsAuthenticated))
        {
            foreach (var claim in identity.Claims
                         .Where(claim =>
                             claim.Type == PermissionClaimType ||
                             claim.Type == RoleNameClaimType ||
                             claim.Type == ClaimTypes.Role)
                         .ToArray())
            {
                identity.RemoveClaim(claim);
            }

            identity.AddClaim(new Claim(RoleNameClaimType, roleName));
            foreach (var permission in RoleCatalog.GetPermissions(roleName))
            {
                identity.AddClaim(new Claim(PermissionClaimType, permission.ToString()));
            }
        }
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
                    TenantDataHandlingModeRestrictedException modeRestriction => (
                        StatusCodes.Status403Forbidden,
                        "Tenant data handling mode restricted",
                        modeRestriction.Message,
                        "tenant_data_handling_mode_restricted"),
                    ContentClassificationValidationException classification => (
                        StatusCodes.Status400BadRequest,
                        "Content classification invalid",
                        classification.Message,
                        "content_classification_invalid"),
                    BadHttpRequestException when exception.InnerException is JsonException => (
                        StatusCodes.Status400BadRequest,
                        "Invalid request body",
                        "The request body could not be parsed or contains an unsupported value.",
                        "invalid_request_body"),
                    JsonException => (
                        StatusCodes.Status400BadRequest,
                        "Invalid request body",
                        "The request body could not be parsed or contains an unsupported value.",
                        "invalid_request_body"),
                    _ => (
                        StatusCodes.Status500InternalServerError,
                        "API request failed",
                        "The API request could not be completed.",
                        "api_request_failed")
                };

                await ApiProblemDetails
                    .Create(
                        context,
                        title,
                        detail,
                        statusCode,
                        errorCode,
                        exception is TenantDataHandlingModeRestrictedException restriction
                            ? new Dictionary<string, object?>
                            {
                                ["mode"] = restriction.Mode.ToString(),
                                ["workflow"] = restriction.Workflow.ToString(),
                                ["entityType"] = restriction.EntityType,
                                ["entityId"] = restriction.EntityId
                            }
                            : null)
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
