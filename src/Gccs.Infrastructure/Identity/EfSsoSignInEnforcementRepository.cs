using System.Text.Json;
using Gccs.Application.Identity;
using Gccs.Application.Security;
using Gccs.Domain.Identity;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Identity;

public sealed class EfSsoSignInEnforcementRepository(
    GccsDbContext dbContext,
    ICurrentTenantContext tenantContext) : ISsoSignInEnforcementRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task<TenantStatus?> GetCurrentTenantStatusAsync(CancellationToken cancellationToken = default) =>
        dbContext.Tenants
            .AsNoTracking()
            .Where(tenant => tenant.Id == tenantContext.TenantId)
            .Select(tenant => (TenantStatus?)tenant.Status)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<TenantSsoPolicyDto?> GetPolicyForCurrentTenantAsync(CancellationToken cancellationToken = default)
    {
        var policy = await dbContext.TenantSsoPolicies
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.TenantId == tenantContext.TenantId, cancellationToken);

        return policy is null ? null : ToDto(policy);
    }

    public async Task<TenantSsoPolicyDto> UpsertPolicyForCurrentTenantAsync(
        SsoEnforcementMode mode,
        Guid? samlConfigurationId,
        string? requiredEmailDomain,
        IReadOnlyDictionary<string, string> requiredAttributes,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var policy = await dbContext.TenantSsoPolicies
            .SingleOrDefaultAsync(candidate => candidate.TenantId == tenantContext.TenantId, cancellationToken);

        if (policy is null)
        {
            policy = new TenantSsoPolicyEntity
            {
                Id = Guid.NewGuid(),
                TenantId = tenantContext.TenantId,
                CreatedAt = now,
                CreatedByUserId = actorUserId
            };
            dbContext.TenantSsoPolicies.Add(policy);
        }
        else
        {
            policy.UpdatedAt = now;
            policy.UpdatedByUserId = actorUserId;
        }

        policy.Mode = mode;
        policy.SamlConfigurationId = samlConfigurationId;
        policy.RequiredEmailDomain = requiredEmailDomain;
        policy.RequiredAttributesJson = JsonSerializer.Serialize(requiredAttributes, JsonOptions);

        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(policy);
    }

    public Task<TenantMemberDto?> GetCurrentTenantMemberByEmailAsync(
        string email,
        CancellationToken cancellationToken = default) =>
        dbContext.TenantMemberships
            .AsNoTracking()
            .Include(membership => membership.User)
            .Where(membership => membership.TenantId == tenantContext.TenantId && membership.User!.Email == email)
            .Select(membership => ToMemberDto(membership))
            .SingleOrDefaultAsync(cancellationToken);

    public Task<TenantMemberDto?> GetCurrentTenantMemberByIdAsync(
        Guid membershipId,
        CancellationToken cancellationToken = default) =>
        dbContext.TenantMemberships
            .AsNoTracking()
            .Include(membership => membership.User)
            .Where(membership => membership.Id == membershipId && membership.TenantId == tenantContext.TenantId)
            .Select(membership => ToMemberDto(membership))
            .SingleOrDefaultAsync(cancellationToken);

    public Task<TenantMemberDto?> GetCurrentTenantMemberByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        dbContext.TenantMemberships
            .AsNoTracking()
            .Include(membership => membership.User)
            .Where(membership => membership.UserId == userId && membership.TenantId == tenantContext.TenantId)
            .Select(membership => ToMemberDto(membership))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<SamlAccountLinkDto?> GetAccountLinkBySubjectForCurrentTenantAsync(
        string samlSubject,
        CancellationToken cancellationToken = default)
    {
        var link = await dbContext.SamlAccountLinks
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate => candidate.TenantId == tenantContext.TenantId && candidate.SamlSubject == samlSubject,
                cancellationToken);

        return link is null ? null : ToDto(link);
    }

    public async Task<SamlAccountLinkDto> UpsertAccountLinkForCurrentTenantAsync(
        Guid membershipId,
        Guid userId,
        string samlSubject,
        string email,
        Guid? samlConfigurationId,
        IReadOnlyDictionary<string, string> attributes,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var link = await dbContext.SamlAccountLinks
            .SingleOrDefaultAsync(
                candidate => candidate.TenantId == tenantContext.TenantId && candidate.SamlSubject == samlSubject,
                cancellationToken);

        if (link is null)
        {
            link = new SamlAccountLinkEntity
            {
                Id = Guid.NewGuid(),
                TenantId = tenantContext.TenantId,
                CreatedAt = now,
                CreatedByUserId = actorUserId
            };
            dbContext.SamlAccountLinks.Add(link);
        }
        else
        {
            link.UpdatedAt = now;
            link.UpdatedByUserId = actorUserId;
        }

        link.MembershipId = membershipId;
        link.UserId = userId;
        link.SamlSubject = samlSubject;
        link.Email = email;
        link.SamlConfigurationId = samlConfigurationId;
        link.AttributesJson = JsonSerializer.Serialize(attributes, JsonOptions);

        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(link);
    }

    public async Task<SamlAccountLinkDto?> RecordSuccessfulSamlSignInAsync(
        Guid linkId,
        CancellationToken cancellationToken = default)
    {
        var link = await dbContext.SamlAccountLinks
            .SingleOrDefaultAsync(
                candidate => candidate.Id == linkId && candidate.TenantId == tenantContext.TenantId,
                cancellationToken);

        if (link is null)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        link.LastSuccessfulSignInAt = now;
        link.UpdatedAt = now;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(link);
    }

    public async Task<BreakGlassAccessGrantDto> CreateBreakGlassGrantForCurrentTenantAsync(
        Guid userId,
        string reason,
        Guid approvedByUserId,
        string approvalReference,
        DateTimeOffset expiresAt,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var grant = new BreakGlassAccessGrantEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantContext.TenantId,
            UserId = userId,
            Reason = reason,
            ApprovedByUserId = approvedByUserId,
            ApprovalReference = approvalReference,
            ExpiresAt = expiresAt,
            Status = BreakGlassGrantStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByUserId = actorUserId
        };

        dbContext.BreakGlassAccessGrants.Add(grant);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(grant);
    }

    public async Task<BreakGlassAccessGrantDto?> GetBreakGlassGrantForCurrentTenantAsync(
        Guid grantId,
        CancellationToken cancellationToken = default)
    {
        var grant = await dbContext.BreakGlassAccessGrants
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == grantId && candidate.TenantId == tenantContext.TenantId, cancellationToken);

        return grant is null ? null : ToDto(grant);
    }

    public Task<BreakGlassAccessGrantDto?> RecordBreakGlassUseAsync(
        Guid grantId,
        Guid actorUserId,
        CancellationToken cancellationToken = default) =>
        UpdateGrantAsync(
            grantId,
            actorUserId,
            grant =>
            {
                grant.Status = BreakGlassGrantStatus.Used;
                grant.LastUsedAt = DateTimeOffset.UtcNow;
                grant.LastUsedByUserId = actorUserId;
            },
            cancellationToken);

    public Task<BreakGlassAccessGrantDto?> MarkBreakGlassGrantExpiredAsync(
        Guid grantId,
        Guid actorUserId,
        CancellationToken cancellationToken = default) =>
        UpdateGrantAsync(
            grantId,
            actorUserId,
            grant => grant.Status = BreakGlassGrantStatus.Expired,
            cancellationToken);

    private async Task<BreakGlassAccessGrantDto?> UpdateGrantAsync(
        Guid grantId,
        Guid actorUserId,
        Action<BreakGlassAccessGrantEntity> update,
        CancellationToken cancellationToken)
    {
        var grant = await dbContext.BreakGlassAccessGrants
            .SingleOrDefaultAsync(candidate => candidate.Id == grantId && candidate.TenantId == tenantContext.TenantId, cancellationToken);

        if (grant is null)
        {
            return null;
        }

        update(grant);
        grant.UpdatedAt = DateTimeOffset.UtcNow;
        grant.UpdatedByUserId = actorUserId;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(grant);
    }

    private static TenantSsoPolicyDto ToDto(TenantSsoPolicyEntity policy) =>
        new(
            policy.Id,
            policy.TenantId,
            policy.Mode,
            policy.SamlConfigurationId,
            policy.RequiredEmailDomain,
            DeserializeDictionary(policy.RequiredAttributesJson),
            policy.CreatedAt,
            policy.UpdatedAt,
            policy.UpdatedByUserId);

    private static SamlAccountLinkDto ToDto(SamlAccountLinkEntity link) =>
        new(
            link.Id,
            link.TenantId,
            link.MembershipId,
            link.UserId,
            link.SamlSubject,
            link.Email,
            link.SamlConfigurationId,
            DeserializeDictionary(link.AttributesJson),
            link.LastSuccessfulSignInAt,
            link.CreatedAt,
            link.UpdatedAt);

    private static BreakGlassAccessGrantDto ToDto(BreakGlassAccessGrantEntity grant) =>
        new(
            grant.Id,
            grant.TenantId,
            grant.UserId,
            grant.Reason,
            grant.ApprovedByUserId,
            grant.ApprovalReference,
            grant.ExpiresAt,
            grant.Status,
            grant.LastUsedAt,
            grant.LastUsedByUserId,
            grant.CreatedAt,
            grant.UpdatedAt);

    private static TenantMemberDto ToMemberDto(TenantMembershipEntity membership) =>
        new(
            membership.Id,
            membership.TenantId,
            membership.UserId,
            membership.User!.Email,
            membership.User.DisplayName,
            membership.User.Status,
            membership.Status,
            membership.RoleName,
            membership.User.MfaEnabled,
            membership.User.LastSignedInAt,
            membership.LastAccessedAt,
            membership.CreatedAt,
            membership.UpdatedAt);

    private static IReadOnlyDictionary<string, string> DeserializeDictionary(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, string>();
        }

        return JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOptions) ?? new Dictionary<string, string>();
    }
}
