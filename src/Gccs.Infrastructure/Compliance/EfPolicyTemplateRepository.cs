using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Gccs.Application.Compliance;
using Gccs.Application.Security;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Compliance;

public sealed class EfPolicyTemplateRepository(
    GccsDbContext dbContext,
    ICurrentTenantContext tenantContext) : IPolicyTemplateRepository
{
    private static readonly Regex PlaceholderPattern = new(@"\{\{\s*(?<name>[a-zA-Z0-9_]+)\s*\}\}", RegexOptions.Compiled);

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<IReadOnlyList<PolicyTemplateDto>> ListAsync(bool includeReviewStates, CancellationToken cancellationToken = default)
    {
        var query = dbContext.PolicyTemplates
            .AsNoTracking()
            .Where(template => template.TenantId == tenantContext.TenantId);
        if (!includeReviewStates)
        {
            query = query.Where(template => template.Status == PolicyTemplateStatus.Approved.ToString());
        }

        var templates = await query
            .OrderBy(template => template.Category)
            .ThenBy(template => template.Title)
            .ToArrayAsync(cancellationToken);
        return templates.Select(ToDto).ToArray();
    }

    public async Task<PolicyTemplateDto?> FindAsync(Guid templateId, bool includeReviewStates, CancellationToken cancellationToken = default)
    {
        var query = dbContext.PolicyTemplates
            .AsNoTracking()
            .Where(template => template.Id == templateId && template.TenantId == tenantContext.TenantId);
        if (!includeReviewStates)
        {
            query = query.Where(template => template.Status == PolicyTemplateStatus.Approved.ToString());
        }

        var template = await query.SingleOrDefaultAsync(cancellationToken);
        return template is null ? null : ToDto(template);
    }

    public async Task<IReadOnlyList<PolicyTemplateVersionDto>> ListVersionsAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        var templateExists = await dbContext.PolicyTemplates
            .AnyAsync(template => template.Id == templateId && template.TenantId == tenantContext.TenantId, cancellationToken);
        if (!templateExists)
        {
            return [];
        }

        var versions = await dbContext.PolicyTemplateVersions
            .AsNoTracking()
            .Where(version => version.TemplateId == templateId)
            .OrderByDescending(version => version.CreatedAt)
            .ToArrayAsync(cancellationToken);
        return versions.Select(ToDto).ToArray();
    }

    public async Task<PolicyTemplateDto> CreateAsync(
        UpsertPolicyTemplateRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var entity = new PolicyTemplateEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantContext.TenantId,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByUserId = actorUserId
        };
        Apply(entity, request);
        dbContext.PolicyTemplates.Add(entity);
        AddVersion(entity, actorUserId);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<PolicyTemplateDto?> ChangeLifecycleAsync(
        Guid templateId,
        PolicyTemplateStatus status,
        Guid? reviewerUserId,
        DateOnly? reviewedAt,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.PolicyTemplates
            .SingleOrDefaultAsync(template => template.Id == templateId && template.TenantId == tenantContext.TenantId, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        entity.Status = status.ToString();
        entity.ReviewerUserId = reviewerUserId ?? entity.ReviewerUserId;
        entity.LastReviewedAt = reviewedAt ?? entity.LastReviewedAt;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        entity.UpdatedByUserId = actorUserId;
        AddVersion(entity, actorUserId);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<GeneratedPolicyDto?> GenerateDraftPolicyAsync(
        Guid templateId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var template = await dbContext.PolicyTemplates
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate =>
                candidate.Id == templateId &&
                candidate.TenantId == tenantContext.TenantId &&
                candidate.Status == PolicyTemplateStatus.Approved.ToString(),
                cancellationToken);
        if (template is null)
        {
            return null;
        }

        var company = await dbContext.CompanyProfiles
            .AsNoTracking()
            .SingleOrDefaultAsync(profile => profile.TenantId == tenantContext.TenantId, cancellationToken);
        var values = BuildPlaceholderValues(company);
        var missing = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        var body = PlaceholderPattern.Replace(template.Body, match =>
        {
            var name = match.Groups["name"].Value;
            if (values.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            missing.Add(name);
            return match.Value;
        });

        var entity = new GeneratedPolicyEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantContext.TenantId,
            SourceTemplateId = template.Id,
            SourceTemplateVersion = template.Version,
            GeneratedAt = DateTimeOffset.UtcNow,
            Title = template.Title,
            Body = body,
            Status = GeneratedPolicyStatus.Draft.ToString(),
            PlaceholderValuesJson = JsonSerializer.Serialize(
                values
                    .Where(pair => !string.IsNullOrWhiteSpace(pair.Value))
                    .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase),
                JsonOptions),
            MissingPlaceholdersJson = JsonSerializer.Serialize(missing.ToArray(), JsonOptions),
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByUserId = actorUserId
        };
        dbContext.GeneratedPolicies.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<GeneratedPolicyDto?> FindGeneratedPolicyAsync(Guid policyId, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.GeneratedPolicies
            .AsNoTracking()
            .SingleOrDefaultAsync(policy => policy.Id == policyId && policy.TenantId == tenantContext.TenantId, cancellationToken);
        return entity is null ? null : ToDto(entity);
    }

    public async Task<GeneratedPolicyDto?> UpdateGeneratedPolicyAsync(
        Guid policyId,
        UpdateGeneratedPolicyRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.GeneratedPolicies
            .SingleOrDefaultAsync(policy => policy.Id == policyId && policy.TenantId == tenantContext.TenantId, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        entity.Title = request.Title;
        entity.Body = request.Body;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        entity.UpdatedByUserId = actorUserId;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    private static void Apply(PolicyTemplateEntity entity, UpsertPolicyTemplateRequest request)
    {
        entity.Title = request.Title;
        entity.Category = request.Category;
        entity.Body = request.Body;
        entity.PlaceholdersJson = JsonSerializer.Serialize(request.Placeholders, JsonOptions);
        entity.SourceReferencesJson = JsonSerializer.Serialize(request.SourceReferences, JsonOptions);
        entity.Version = request.Version;
        entity.Status = request.Status.ToString();
        entity.OwnerFunction = request.OwnerFunction;
        entity.LastReviewedAt = request.LastReviewedAt;
        entity.ReviewerUserId = request.ReviewerUserId;
        entity.RequiresExpertReview = request.RequiresExpertReview;
    }

    private void AddVersion(PolicyTemplateEntity entity, Guid actorUserId)
    {
        dbContext.PolicyTemplateVersions.Add(new PolicyTemplateVersionEntity
        {
            Id = Guid.NewGuid(),
            TemplateId = entity.Id,
            Version = entity.Version,
            BodyPreview = entity.Body.Length <= 500 ? entity.Body : entity.Body[..500],
            Status = entity.Status,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByUserId = actorUserId
        });
    }

    private static PolicyTemplateDto ToDto(PolicyTemplateEntity entity) =>
        new(
            entity.Id,
            entity.TenantId,
            entity.Title,
            entity.Category,
            entity.Body,
            ReadArray<string>(entity.PlaceholdersJson),
            ReadArray<PolicyTemplateSourceReferenceDto>(entity.SourceReferencesJson),
            entity.Version,
            Enum.Parse<PolicyTemplateStatus>(entity.Status),
            entity.OwnerFunction,
            entity.LastReviewedAt,
            entity.ReviewerUserId,
            entity.RequiresExpertReview,
            entity.CreatedAt,
            entity.UpdatedAt);

    private static PolicyTemplateVersionDto ToDto(PolicyTemplateVersionEntity entity) =>
        new(
            entity.Id,
            entity.TemplateId,
            entity.Version,
            entity.BodyPreview,
            Enum.Parse<PolicyTemplateStatus>(entity.Status),
            entity.CreatedAt,
            entity.CreatedByUserId);

    private static GeneratedPolicyDto ToDto(GeneratedPolicyEntity entity) =>
        new(
            entity.Id,
            entity.TenantId,
            entity.SourceTemplateId,
            entity.SourceTemplateVersion,
            entity.GeneratedAt,
            entity.Title,
            entity.Body,
            Enum.Parse<GeneratedPolicyStatus>(entity.Status),
            ReadDictionary(entity.PlaceholderValuesJson),
            ReadArray<string>(entity.MissingPlaceholdersJson),
            entity.CreatedAt,
            entity.UpdatedAt);

    private static Dictionary<string, string> BuildPlaceholderValues(CompanyProfileEntity? company)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (company is null)
        {
            return values;
        }

        values["company_name"] = company.LegalEntityName;
        values["doing_business_as"] = company.DoingBusinessAs ?? string.Empty;
        values["uei"] = company.Uei ?? string.Empty;
        values["cage_code"] = company.CageCode ?? string.Empty;
        values["products_and_services"] = company.ProductsAndServices;
        values["it_environment"] = company.ItEnvironmentDescription;
        return values;
    }

    private static IReadOnlyList<T> ReadArray<T>(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<T[]>(json, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static IReadOnlyDictionary<string, string> ReadDictionary(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOptions) ?? new Dictionary<string, string>();
        }
        catch (JsonException)
        {
            return new Dictionary<string, string>();
        }
    }
}
