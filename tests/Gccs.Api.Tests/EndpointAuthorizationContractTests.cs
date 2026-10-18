using Gccs.Api.Security;
using Gccs.Domain.Identity;
using Gccs.Domain.Tenancy;
using Gccs.Application.Identity;
using Gccs.Application.Audit;
using Gccs.Infrastructure.Audit;
using Gccs.Infrastructure.Identity;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text.Json;
using System.Security.Claims;
using Xunit;
using Xunit.Abstractions;

namespace Gccs.Api.Tests;

public sealed class EndpointAuthorizationContractTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly ITestOutputHelper _output;

    public EndpointAuthorizationContractTests(WebApplicationFactory<Program> factory, ITestOutputHelper output)
    {
        _output = output;
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.UseSetting("ConnectionStrings:GccsDatabase", string.Empty);
            // This inventory isolates authentication; rate-limit behavior has its own boundary test.
            builder.UseSetting("Security:RateLimiting:PermitLimit", int.MaxValue.ToString());
            builder.ConfigureServices(services => services.AddTransient<IStartupFilter, MatchedEndpointCapture>());
        });
    }

    [Fact]
    public async Task Registered_api_inventory_preserves_the_reviewed_anonymous_boundary()
    {
        using var client = _factory.CreateClient();
        var endpoints = ApiEndpoints();
        Assert.NotEmpty(endpoints);
        var anonymous = endpoints
            .Where(endpoint => endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null)
            .SelectMany(endpoint => endpoint.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods
                .Select(method => $"{method} {endpoint.RoutePattern.RawText}"))
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(new[]
        {
            "POST /api/public/demo-request-details/context",
            "POST /api/public/demo-request-details/responses",
            "POST /api/public/demo-requests"
        }, anonymous);

        var policies = _factory.Services.GetRequiredService<IAuthorizationPolicyProvider>();
        foreach (var endpoint in endpoints)
        {
            var isAnonymous = endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null;
            if (!isAnonymous)
            {
                var policy = await AuthorizationPolicy.CombineAsync(
                    policies, endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>(),
                    endpoint.Metadata.GetOrderedMetadata<AuthorizationPolicy>());
                Assert.NotNull(policy);
                using var scope = _factory.Services.CreateScope();
                var authorization = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();
                var anonymousUser = new ClaimsPrincipal(new ClaimsIdentity());
                var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider, User = anonymousUser };
                context.SetEndpoint(endpoint);
                Assert.False((await authorization.AuthorizeAsync(anonymousUser, context, policy.Requirements)).Succeeded);
            }

            _output.WriteLine(JsonSerializer.Serialize(new
            {
                route = endpoint.RoutePattern.RawText,
                methods = endpoint.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods,
                name = endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName,
                anonymous = isAnonymous,
                permission = endpoint.Metadata.GetMetadata<RequiredPermissionMetadata>()?.Permission.ToString(),
                anyPermissions = endpoint.Metadata.GetMetadata<RequiredAnyPermissionMetadata>()?.Permissions.Select(permission => permission.ToString()),
                policies = endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().Select(data => data.Policy),
                membershipExemption = endpoint.Metadata.GetMetadata<AllowWithoutTenantMembershipAttribute>() is not null
            }));
        }
    }

    [Fact]
    public async Task Every_registered_protected_api_route_rejects_unauthenticated_requests()
    {
        using var client = _factory.CreateClient();
        foreach (var endpoint in ApiEndpoints().Where(endpoint => endpoint.Metadata.GetMetadata<IAllowAnonymous>() is null))
        {
            var path = SyntheticPath(endpoint);

            foreach (var method in endpoint.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods)
            {
                using var request = new HttpRequestMessage(new HttpMethod(method), path);
                using var response = await client.SendAsync(request);
                Assert.Equal(endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()!.EndpointName,
                    Assert.Single(response.Headers.GetValues("X-Gccs-Test-Matched-Endpoint")));
                Assert.True(response.StatusCode == HttpStatusCode.Unauthorized,
                    $"{method} {endpoint.RoutePattern.RawText} returned {(int)response.StatusCode} for an unauthenticated request.");
            }
        }
    }

    private RouteEndpoint[] ApiEndpoints() => _factory.Services
        .GetServices<EndpointDataSource>()
        .SelectMany(source => source.Endpoints)
        .OfType<RouteEndpoint>()
        .Where(endpoint => endpoint.RoutePattern.RawText?.StartsWith("/api/", StringComparison.Ordinal) is true &&
            endpoint.Metadata.GetMetadata<HttpMethodMetadata>() is not null)
        .OrderBy(endpoint => endpoint.RoutePattern.RawText, StringComparer.Ordinal)
        .ToArray();

    [Fact]
    public async Task Every_registered_protected_policy_matches_each_role_and_identity_plane()
    {
        using var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var policies = scope.ServiceProvider.GetRequiredService<IAuthorizationPolicyProvider>();
        var authorization = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();
        foreach (var endpoint in ApiEndpoints().Where(e => e.Metadata.GetMetadata<IAllowAnonymous>() is null))
        {
            var policy = await AuthorizationPolicy.CombineAsync(policies, endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>(),
                endpoint.Metadata.GetOrderedMetadata<AuthorizationPolicy>());
            Assert.NotNull(policy);
            var required = endpoint.Metadata.GetMetadata<RequiredPermissionMetadata>()?.Permission;
            var any = endpoint.Metadata.GetMetadata<RequiredAnyPermissionMetadata>()?.Permissions;
            var platformPolicies = endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>()
                .Select(d => d.Policy).Where(p => p?.StartsWith("Platform.", StringComparison.Ordinal) is true).ToArray();
            foreach (var role in RoleCatalog.Roles.Append("unknown-role"))
            foreach (var plane in new[] { ApiSecurityExtensions.CustomerAuthenticationPlane, ApiSecurityExtensions.WorkforceAuthenticationPlane })
            foreach (var operatorClaim in new[] { false, true })
            {
                var permissions = RoleCatalog.GetPermissions(role);
                var claims = permissions.Select(p => new Claim(ApiSecurityExtensions.PermissionClaimType, p.ToString())).ToList();
                claims.Add(new Claim(ApiSecurityExtensions.AuthenticationPlaneClaimType, plane));
                claims.Add(new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()));
                // A customer token claiming the operator role must not gain workforce authority.
                if (operatorClaim) claims.Add(new Claim(ClaimTypes.Role, PlatformAuthorization.PlatformOperatorRole));
                var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "synthetic-validated-token"));
                var resource = new DefaultHttpContext { RequestServices = scope.ServiceProvider, User = principal };
                resource.SetEndpoint(endpoint);
                var expected = (required is null || permissions.Contains(required.Value)) &&
                    (any is null || any.Any(permissions.Contains)) &&
                    (platformPolicies.Length == 0 || (plane == ApiSecurityExtensions.WorkforceAuthenticationPlane && operatorClaim)) &&
                    (endpoint.RoutePattern.RawText?.StartsWith("/api/external-portal/", StringComparison.Ordinal) is not true ||
                        plane == ApiSecurityExtensions.CustomerAuthenticationPlane);
                var result = await authorization.AuthorizeAsync(principal, resource, policy.Requirements);
                Assert.True(result.Succeeded == expected, $"{endpoint.DisplayName}: {role}/{plane} expected {expected}.");
                _output.WriteLine(JsonSerializer.Serialize(new { kind = "role-policy", name = endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()!.EndpointName, role, plane, operatorClaim, allowed = result.Succeeded }));
            }
        }
    }

    [Fact]
    public async Task Every_membership_scoped_route_rejects_cross_tenant_selection_and_forged_permissions_without_mutation()
    {
        var tenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var database = "endpoint-membership-" + Guid.NewGuid().ToString("N");
        await using var host = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Security:MembershipAuthorization:Enforce", "true");
            builder.ConfigureServices(services =>
            {
                services.AddDbContext<GccsDbContext>(options => options.UseInMemoryDatabase(database));
                services.AddScoped<ITenantMembershipRepository, EfTenantMembershipRepository>();
                services.AddScoped<IAuditEventWriter, EfAuditEventWriter>();
            });
        });
        using var client = host.CreateClient();
        using (var setup = host.Services.CreateScope())
        {
            var db = setup.ServiceProvider.GetRequiredService<GccsDbContext>();
            db.Tenants.AddRange(new TenantEntity { Id = tenantId, Name = "Synthetic A", Status = TenantStatus.Active },
                new TenantEntity { Id = otherTenantId, Name = "Synthetic B", Status = TenantStatus.Active });
            db.Users.Add(new UserEntity { Id = userId, TenantId = tenantId, Email = "boundary@example.invalid", DisplayName = "Synthetic member", Status = UserStatus.Active });
            db.TenantMemberships.Add(new TenantMembershipEntity { Id = Guid.NewGuid(), TenantId = tenantId, UserId = userId, RoleName = RoleCatalog.Auditor, Status = MembershipStatus.Active });
            await db.SaveChangesAsync();
        }
        var endpoints = host.Services.GetServices<EndpointDataSource>().SelectMany(s => s.Endpoints).OfType<RouteEndpoint>()
            .Where(e => e.RoutePattern.RawText?.StartsWith("/api/", StringComparison.Ordinal) is true &&
                e.Metadata.GetMetadata<HttpMethodMetadata>() is not null && e.Metadata.GetMetadata<IAllowAnonymous>() is null &&
                e.Metadata.GetMetadata<AllowWithoutTenantMembershipAttribute>() is null).ToArray();
        Assert.NotEmpty(endpoints);
        foreach (var endpoint in endpoints)
        foreach (var method in endpoint.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods)
        {
            for (var repeat = 0; repeat < 2; repeat++)
            {
                using var request = new HttpRequestMessage(new HttpMethod(method), SyntheticPath(endpoint));
                request.Headers.Add("X-Gccs-Dev-Auth", "true");
                request.Headers.Add("X-Gccs-Dev-Tenant", otherTenantId.ToString());
                request.Headers.Add("X-Gccs-Dev-User", userId.ToString());
                request.Headers.Add("X-Gccs-Dev-Role", RoleCatalog.Owner);
                request.Headers.Add("X-Gccs-Dev-Permissions", string.Join(',', Enum.GetNames<Permission>()));
                using var response = await client.SendAsync(request);
                Assert.Equal(endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()!.EndpointName,
                    Assert.Single(response.Headers.GetValues("X-Gccs-Test-Matched-Endpoint")));
                Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
                Assert.Contains("tenant_membership_required", await response.Content.ReadAsStringAsync());
            }
            _output.WriteLine(JsonSerializer.Serialize(new { kind = "cross-tenant-membership", name = endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()!.EndpointName, method, attempts = 2, status = 403 }));
        }
        using var verification = host.Services.CreateScope();
        var final = verification.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Equal(2, await final.Tenants.CountAsync());
        Assert.Single(await final.Users.ToArrayAsync());
        Assert.Equal(RoleCatalog.Auditor, (await final.TenantMemberships.SingleAsync()).RoleName);
        Assert.Empty(await final.AuditLogEntries.ToArrayAsync());
        Assert.Empty(await final.EvidenceItems.ToArrayAsync());
        Assert.Empty(await final.Reports.ToArrayAsync());
    }

    private static string SyntheticPath(RouteEndpoint endpoint) => "/" + string.Join("/", endpoint.RoutePattern.PathSegments.Select(segment =>
        string.Concat(segment.Parts.Select(part => part switch
        {
            RoutePatternLiteralPart literal => literal.Content,
            RoutePatternSeparatorPart separator => separator.Content,
            RoutePatternParameterPart parameter => parameter.ParameterPolicies.Any(policy => policy.Content == "guid")
                ? "11111111-1111-1111-1111-111111111111"
                : parameter.ParameterPolicies.Any(policy => policy.Content is "int" or "long") ? "1" : "synthetic",
            _ => throw new InvalidOperationException("Unreviewed route part in authorization inventory.")
        }))));

    [Fact]
    public async Task Every_membership_scoped_permission_denial_uses_the_stored_role_despite_forged_owner_claims()
    {
        var database = "endpoint-role-" + Guid.NewGuid().ToString("N");
        var tenantId = Guid.NewGuid();
        var users = RoleCatalog.Roles.ToDictionary(role => role, _ => Guid.NewGuid());
        await using var host = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Security:MembershipAuthorization:Enforce", "true");
            builder.ConfigureServices(services =>
            {
                services.AddDbContext<GccsDbContext>(options => options.UseInMemoryDatabase(database));
                services.AddScoped<ITenantMembershipRepository, EfTenantMembershipRepository>();
                services.AddScoped<IAuditEventWriter, EfAuditEventWriter>();
            });
        });
        using var client = host.CreateClient();
        using (var setup = host.Services.CreateScope())
        {
            var db = setup.ServiceProvider.GetRequiredService<GccsDbContext>();
            db.Tenants.Add(new TenantEntity { Id = tenantId, Name = "Synthetic role matrix", Status = TenantStatus.Active });
            foreach (var (role, userId) in users)
            {
                db.Users.Add(new UserEntity { Id = userId, TenantId = tenantId, Email = $"{userId:N}@example.invalid", DisplayName = role, Status = UserStatus.Active });
                db.TenantMemberships.Add(new TenantMembershipEntity { Id = Guid.NewGuid(), TenantId = tenantId, UserId = userId, RoleName = role, Status = MembershipStatus.Active });
            }
            await db.SaveChangesAsync();
        }
        var denied = 0;
        foreach (var endpoint in ApiEndpoints().Where(e => e.Metadata.GetMetadata<IAllowAnonymous>() is null &&
            e.Metadata.GetMetadata<AllowWithoutTenantMembershipAttribute>() is null))
        foreach (var (role, userId) in users)
        {
            var permissions = RoleCatalog.GetPermissions(role);
            var required = endpoint.Metadata.GetMetadata<RequiredPermissionMetadata>()?.Permission;
            var any = endpoint.Metadata.GetMetadata<RequiredAnyPermissionMetadata>()?.Permissions;
            var platform = endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().Any(d => d.Policy?.StartsWith("Platform.", StringComparison.Ordinal) is true);
            if (!platform && (required is null || permissions.Contains(required.Value)) && (any is null || any.Any(permissions.Contains)))
                continue;
            foreach (var method in endpoint.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods)
            {
                using var request = new HttpRequestMessage(new HttpMethod(method), SyntheticPath(endpoint));
                request.Headers.Add("X-Gccs-Dev-Auth", "true");
                request.Headers.Add("X-Gccs-Dev-Tenant", tenantId.ToString());
                request.Headers.Add("X-Gccs-Dev-User", userId.ToString());
                request.Headers.Add("X-Gccs-Dev-Role", RoleCatalog.Owner);
                request.Headers.Add("X-Gccs-Dev-Permissions", string.Join(',', Enum.GetNames<Permission>()));
                using var response = await client.SendAsync(request);
                Assert.Equal(endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()!.EndpointName,
                    Assert.Single(response.Headers.GetValues("X-Gccs-Test-Matched-Endpoint")));
                Assert.True(response.StatusCode == HttpStatusCode.Forbidden, $"{role}: {method} {endpoint.RoutePattern.RawText} returned {response.StatusCode}.");
                denied++;
                _output.WriteLine(JsonSerializer.Serialize(new { kind = "stored-role-denial", name = endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()!.EndpointName, method, role, status = 403 }));
            }
        }
        Assert.True(denied > 0);
        using var verification = host.Services.CreateScope();
        var final = verification.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Single(await final.Tenants.ToArrayAsync());
        Assert.Equal(users.Count, await final.Users.CountAsync());
        Assert.Equal(users.Count, await final.TenantMemberships.CountAsync());
        Assert.All(await final.TenantMemberships.ToArrayAsync(), m => Assert.Equal(users.Single(u => u.Value == m.UserId).Key, m.RoleName));
        Assert.Empty(await final.EvidenceItems.ToArrayAsync());
        Assert.Empty(await final.Reports.ToArrayAsync());
        var audit = await final.AuditLogEntries.ToArrayAsync();
        Assert.Equal(denied, audit.Length);
        Assert.All(audit, entry =>
        {
            Assert.Equal(Gccs.Domain.Audit.AuditAction.Rejected, entry.Action);
            Assert.Equal(tenantId, entry.TenantId);
            Assert.NotNull(entry.ActorUserId);
            Assert.Contains(entry.ActorUserId.Value, users.Values);
        });
    }

    private sealed class MatchedEndpointCapture : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use(async (context, nextMiddleware) =>
            {
                context.Response.OnStarting(() =>
                {
                    context.Response.Headers["X-Gccs-Test-Matched-Endpoint"] =
                        context.GetEndpoint()?.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName ?? string.Empty;
                    return Task.CompletedTask;
                });
                await nextMiddleware();
            });
            next(app);
        };
    }

    [Fact]
    public void Report_endpoint_inventory_matches_the_executable_authorization_contract()
    {
        using var client = _factory.CreateClient();
        var actual = _factory.Services
            .GetServices<EndpointDataSource>()
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .SelectMany(endpoint =>
            {
                var methods = endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? [];
                var permission = endpoint.Metadata.GetMetadata<RequiredPermissionMetadata>()?.Permission;
                return methods.Select(method => new EndpointContract(
                    method,
                    endpoint.RoutePattern.RawText ?? string.Empty,
                    endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName ?? string.Empty,
                    permission));
            })
            .Where(endpoint =>
                endpoint.Route.StartsWith("/api/reports", StringComparison.Ordinal) ||
                endpoint.Route.StartsWith("/api/report-exports", StringComparison.Ordinal))
            .OrderBy(endpoint => endpoint.Method)
            .ThenBy(endpoint => endpoint.Route)
            .ToArray();

        EndpointContract[] expected =
        [
            new("GET", "/api/reports/approved-evidence-packages", "ListApprovedEvidencePackages", Permission.ViewReports),
            new("GET", "/api/reports/evidence-packages/{reportId:guid}", "GetEvidencePackage", Permission.ViewReports),
            new("GET", "/api/reports/exports/{reportType}", "ExportSimpleReportCsv", Permission.ViewReports),
            new("GET", "/api/reports/recent", "ListRecentReports", Permission.ViewReports),
            new("GET", "/api/reports/{reportId:guid}", "GetReportArtifact", Permission.ViewReports),
            new("GET", "/api/report-exports/{exportId:guid}", "GetReportPdfExport", Permission.ExportReports),
            new("GET", "/api/report-exports/{exportId:guid}/content", "DownloadReportPdfExport", Permission.ExportReports),
            new("POST", "/api/reports/cmmc-readiness", "GenerateCmmcReadinessReport", Permission.ManageReports),
            new("POST", "/api/reports/compliance-status", "GenerateComplianceStatusReport", Permission.ManageReports),
            new("POST", "/api/reports/evidence-packages", "GenerateEvidencePackage", Permission.ManageReports),
            new("POST", "/api/reports/labor-compliance", "GenerateLaborComplianceReport", Permission.ManageReports),
            new("POST", "/api/reports/sprs-readiness", "GenerateSprsReadinessReport", Permission.ManageReports),
            new("POST", "/api/reports/subcontractor-compliance", "GenerateSubcontractorComplianceReport", Permission.ManageReports),
            new("POST", "/api/reports/{reportId:guid}/archive", "ArchiveReport", Permission.ArchiveReports),
            new("POST", "/api/reports/{reportId:guid}/exports/pdf", "RequestReportPdfExport", Permission.ExportReports),
            new("POST", "/api/reports/{reportId:guid}/restore", "RestoreReport", Permission.ArchiveReports)
        ];

        Assert.Equal(
            expected.OrderBy(endpoint => endpoint.Method).ThenBy(endpoint => endpoint.Route),
            actual);
    }

    private sealed record EndpointContract(
        string Method,
        string Route,
        string Name,
        Permission? Permission);
}
