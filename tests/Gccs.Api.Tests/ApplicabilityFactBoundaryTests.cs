using System.Net;
using System.Net.Http.Json;
using Gccs.Application.Compliance;
using Gccs.Application.Identity;
using Gccs.Domain.Common;
using Gccs.Domain.Companies;
using Gccs.Domain.Contracts;
using Gccs.Domain.Identity;
using Gccs.Domain.Tenancy;
using Gccs.Domain.Vendors;
using Gccs.Infrastructure.Compliance;
using Gccs.Infrastructure.Identity;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class ApplicabilityFactBoundaryTests
{
    [Fact]
    public async Task Http_queries_isolate_indirect_ids_and_repeated_reads_without_business_mutations()
    {
        var tenant = Guid.NewGuid();
        var foreignTenant = Guid.NewGuid();
        var user = Guid.NewGuid();
        var own = (Contract: Guid.NewGuid(), Clause: Guid.NewGuid(), Subcontractor: Guid.NewGuid());
        var foreign = (Contract: Guid.NewGuid(), Clause: Guid.NewGuid(), Subcontractor: Guid.NewGuid());
        var database = $"applicability-http-{Guid.NewGuid():N}";
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.UseSetting("ConnectionStrings:GccsDatabase", string.Empty);
            builder.UseSetting("Security:MembershipAuthorization:Enforce", "true");
            builder.ConfigureServices(services =>
            {
                services.AddDbContext<GccsDbContext>(options => options.UseInMemoryDatabase(database));
                services.AddScoped<ITenantMembershipRepository, EfTenantMembershipRepository>();
                services.AddScoped<IApplicabilityFactRepository, EfApplicabilityFactRepository>();
                using var provider = services.BuildServiceProvider();
                using var scope = provider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
                foreach (var id in new[] { tenant, foreignTenant })
                    db.Tenants.Add(new() { Id = id, Name = "Synthetic facts", Status = TenantStatus.Active,
                        DataPosture = TenantDataPosture.NoCui });
                db.Users.Add(new() { Id = user, TenantId = tenant, Email = "facts@example.invalid",
                    DisplayName = "Synthetic facts owner", Status = UserStatus.Active, MfaEnabled = true });
                db.TenantMemberships.Add(new() { Id = Guid.NewGuid(), TenantId = tenant, UserId = user,
                    Status = MembershipStatus.Active, RoleName = RoleCatalog.Owner });
                foreach (var item in new[] { (Tenant: tenant, Ids: own, Marker: "OWN-SYNTHETIC"),
                             (Tenant: foreignTenant, Ids: foreign, Marker: "FOREIGN-SYNTHETIC") })
                {
                    db.Contracts.Add(new() { Id = item.Ids.Contract, TenantId = item.Tenant,
                        ContractNumber = item.Marker, Title = item.Marker, AgencyOrPrimeName = item.Marker,
                        Status = ContractStatus.Active, DataHandlingPosture = DataHandlingPosture.FciOnly });
                    db.Set<ContractClauseEntity>().Add(new() { Id = item.Ids.Clause, ContractId = item.Ids.Contract,
                        ClauseLibraryId = item.Marker, ClauseNumber = item.Marker, Title = item.Marker,
                        FullText = "Synthetic public content", Source = ClauseSource.Far, ReviewState = ReviewState.Published });
                    db.Subcontractors.Add(new() { Id = item.Ids.Subcontractor, TenantId = item.Tenant,
                        Name = item.Marker, RoleDescription = item.Marker, Status = SubcontractorStatus.Active });
                }
                db.SaveChanges();
            });
        });
        using var client = factory.CreateClient();

        HttpRequestMessage Request(Guid selected, (Guid Contract, Guid Clause, Guid Subcontractor)? ids)
        {
            var path = "/api/applicability-facts" + (ids is { } value
                ? $"?contractId={value.Contract}&clauseId={value.Clause}&subcontractorId={value.Subcontractor}" : "");
            var request = new HttpRequestMessage(HttpMethod.Get, path);
            request.Headers.Add("X-Gccs-Dev-Auth", "true");
            request.Headers.Add("X-Gccs-Dev-Tenant", selected.ToString());
            request.Headers.Add("X-Gccs-Dev-User", user.ToString());
            request.Headers.Add("X-Gccs-Dev-Role", RoleCatalog.Owner);
            return request;
        }

        async Task<ApplicabilityFactDto[]> Read((Guid Contract, Guid Clause, Guid Subcontractor)? ids)
        {
            using var request = Request(tenant, ids);
            using var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var facts = await response.Content.ReadFromJsonAsync<ApplicabilityFactDto[]>();
            Assert.NotNull(facts);
            Assert.All(facts, fact => Assert.Equal(tenant, fact.TenantId));
            Assert.DoesNotContain(facts, fact => fact.Value == "FOREIGN-SYNTHETIC");
            return facts;
        }

        var ownFacts = await Read(own);
        Assert.Contains(ownFacts, fact => fact.Key == "contract.agency" && fact.Value == "OWN-SYNTHETIC");
        Assert.Contains(ownFacts, fact => fact.Key == "clause.citation" && fact.Value == "OWN-SYNTHETIC");
        Assert.Contains(ownFacts, fact => fact.Key == "subcontractor.role" && fact.Value == "OWN-SYNTHETIC");
        foreach (var ids in new[] { foreign, (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()) })
        {
            var facts = await Read(ids);
            foreach (var key in new[] { "contract.record", "clause.citation", "subcontractor.role" })
                Assert.Contains(facts, fact => fact.Key == key && fact.IsUnknown && fact.Value == "unknown");
            Assert.DoesNotContain(facts, fact => fact.Key == "contract.agency");
        }
        Assert.All(await Read(null), fact => Assert.True(fact.IsUnknown));
        foreach (var repeated in await Task.WhenAll(Read(own), Read(own)))
            Assert.Equal(ownFacts, repeated);

        using (var request = Request(foreignTenant, foreign))
        using (var response = await client.SendAsync(request))
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        using (var response = await client.GetAsync("/api/applicability-facts"))
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        using var finalScope = factory.Services.CreateScope();
        var final = finalScope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Equal(2, await final.Contracts.CountAsync());
        Assert.Equal(2, await final.Set<ContractClauseEntity>().CountAsync());
        Assert.Equal(2, await final.Subcontractors.CountAsync());
        Assert.Equal(1, await final.TenantMemberships.CountAsync());
        Assert.All(await final.Contracts.ToArrayAsync(), item =>
            Assert.Equal(item.TenantId == tenant ? "OWN-SYNTHETIC" : "FOREIGN-SYNTHETIC", item.Title));
    }
}
