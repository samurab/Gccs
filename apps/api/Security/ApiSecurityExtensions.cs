using System.Security.Claims;
using System.Threading.RateLimiting;
using Gccs.Domain.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

namespace Gccs.Api.Security;

public static class ApiSecurityExtensions
{
    public const string DevelopmentAuthenticationScheme = "Development";
    public const string JwtAuthenticationScheme = JwtBearerDefaults.AuthenticationScheme;
    public const string PermissionClaimType = "permission";
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

        services.AddHttpContextAccessor();
        services.AddScoped<ITenantContext, HttpTenantContext>();

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

    public static IEndpointConventionBuilder RequirePermission(
        this IEndpointConventionBuilder builder,
        Permission permission)
    {
        return builder.RequireAuthorization(permission.ToString());
    }
}
