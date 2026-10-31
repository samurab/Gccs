using System.Text.Json;
using Gccs.Application.Compliance;
using Gccs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Compliance;

public sealed class EfApplicabilityFactRepository(GccsDbContext dbContext) : IApplicabilityFactRepository
{
    private const string Unknown = "unknown";

    public async Task<IReadOnlyList<ApplicabilityFactDto>> ListAsync(
        ApplicabilityFactQuery query,
        CancellationToken cancellationToken = default)
    {
        var facts = new List<ApplicabilityFactDto>();
        var company = await dbContext.CompanyProfiles
            .AsNoTracking()
            .Include(profile => profile.NaicsCodes)
            .Include(profile => profile.Certifications)
            .Include(profile => profile.Locations)
            .FirstOrDefaultAsync(profile => profile.TenantId == query.TenantId, cancellationToken);

        if (company is null)
        {
            facts.Add(UnknownFact(query.TenantId, "company.profile", "CompanyProfile", query.TenantId.ToString()));
        }
        else
        {
            facts.Add(Fact(query.TenantId, "company.contractor_role", company.ContractorRole.ToString(), "CompanyProfile", company.Id.ToString(), company.UpdatedAt ?? company.CreatedAt));
            facts.Add(Fact(query.TenantId, "company.data_type", company.DataHandlingPosture.ToString(), "CompanyProfile", company.Id.ToString(), company.UpdatedAt ?? company.CreatedAt));
            facts.AddRange(company.NaicsCodes.Select(naics => Fact(query.TenantId, "company.naics", naics.Code, "CompanyNaicsCode", naics.Id.ToString(), naics.LastCheckedAt is null ? null : new DateTimeOffset(naics.LastCheckedAt.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero))));
            facts.AddRange(company.Certifications.Select(certification => Fact(query.TenantId, "company.certification", certification.Type.ToString(), "CompanyCertification", certification.Id.ToString(), certification.ExpiresAt is null ? null : new DateTimeOffset(certification.ExpiresAt.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero))));
            facts.AddRange(ReadStringArray(company.AgencyCustomersJson).Select(agency => Fact(query.TenantId, "company.agency_customer", agency, "CompanyProfile", company.Id.ToString(), company.UpdatedAt ?? company.CreatedAt)));
            facts.AddRange(company.Locations.Select(location => Fact(query.TenantId, "company.performance_location", $"{location.City}, {location.StateOrProvince}, {location.Country}", "CompanyLocation", location.Id.ToString(), company.UpdatedAt ?? company.CreatedAt)));
        }

        if (query.ContractId is { } contractId)
        {
            var contract = await dbContext.Contracts
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.TenantId == query.TenantId && item.Id == contractId, cancellationToken);
            if (contract is null)
            {
                facts.Add(UnknownFact(query.TenantId, "contract.record", "Contract", contractId.ToString()));
            }
            else
            {
                var updatedAt = contract.UpdatedAt ?? contract.CreatedAt;
                facts.Add(Fact(query.TenantId, "contract.agency", contract.AgencyOrPrimeName, "Contract", contract.Id.ToString(), updatedAt));
                facts.Add(Fact(query.TenantId, "contract.type", contract.Kind.ToString(), "Contract", contract.Id.ToString(), updatedAt));
                facts.Add(Fact(query.TenantId, "contract.role", contract.Relationship.ToString(), "Contract", contract.Id.ToString(), updatedAt));
                facts.Add(Fact(query.TenantId, "contract.performance_location", contract.PlaceOfPerformance, "Contract", contract.Id.ToString(), updatedAt));
                facts.Add(Fact(query.TenantId, "contract.data_type", contract.DataHandlingPosture.ToString(), "Contract", contract.Id.ToString(), updatedAt));
            }
        }

        if (query.ClauseId is { } clauseId)
        {
            var clause = await dbContext.Set<Infrastructure.Persistence.Models.ContractClauseEntity>()
                .AsNoTracking()
                .Include(item => item.Contract)
                .FirstOrDefaultAsync(item => item.Id == clauseId && item.Contract != null && item.Contract.TenantId == query.TenantId, cancellationToken);
            facts.Add(clause is null
                ? UnknownFact(query.TenantId, "clause.citation", "ContractClause", clauseId.ToString())
                : Fact(query.TenantId, "clause.citation", clause.ClauseNumber, "ContractClause", clause.Id.ToString(), clause.UpdatedAt ?? clause.CreatedAt));
        }

        if (query.SubcontractorId is { } subcontractorId)
        {
            var subcontractor = await dbContext.Subcontractors
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.TenantId == query.TenantId && item.Id == subcontractorId, cancellationToken);
            if (subcontractor is null)
            {
                facts.Add(UnknownFact(query.TenantId, "subcontractor.role", "Subcontractor", subcontractorId.ToString()));
            }
            else
            {
                var updatedAt = subcontractor.UpdatedAt ?? subcontractor.CreatedAt;
                facts.Add(Fact(query.TenantId, "subcontractor.role", subcontractor.RoleDescription, "Subcontractor", subcontractor.Id.ToString(), updatedAt));
                facts.Add(Fact(query.TenantId, "subcontractor.has_fci_access", subcontractor.HasFciAccess.ToString(), "Subcontractor", subcontractor.Id.ToString(), updatedAt));
                facts.Add(Fact(query.TenantId, "subcontractor.has_cui_access", subcontractor.HasCuiAccess.ToString(), "Subcontractor", subcontractor.Id.ToString(), updatedAt));
            }
        }

        if (!facts.Any(fact => fact.Key.EndsWith("data_type", StringComparison.Ordinal)))
        {
            facts.Add(UnknownFact(query.TenantId, "data_type", "Tenant", query.TenantId.ToString()));
        }

        return facts.OrderBy(fact => fact.Key, StringComparer.Ordinal).ThenBy(fact => fact.Value, StringComparer.Ordinal).ToArray();
    }

    private static ApplicabilityFactDto Fact(
        Guid tenantId,
        string key,
        string? value,
        string sourceType,
        string sourceId,
        DateTimeOffset? lastUpdatedAt) =>
        string.IsNullOrWhiteSpace(value) || string.Equals(value, Unknown, StringComparison.OrdinalIgnoreCase)
            ? UnknownFact(tenantId, key, sourceType, sourceId, lastUpdatedAt)
            : new ApplicabilityFactDto(tenantId, key, value, false, sourceType, sourceId, lastUpdatedAt);

    private static ApplicabilityFactDto UnknownFact(
        Guid tenantId,
        string key,
        string sourceType,
        string sourceId,
        DateTimeOffset? lastUpdatedAt = null) =>
        new(tenantId, key, Unknown, true, sourceType, sourceId, lastUpdatedAt);

    private static IReadOnlyList<string> ReadStringArray(string json) =>
        JsonSerializer.Deserialize<string[]>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web)) ?? [];
}
