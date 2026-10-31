using System.Text.Json;
using Gccs.Application.Audit;
using Gccs.Application.Security;
using Gccs.Domain.Audit;
using Gccs.Domain.Identity;
using Gccs.Domain.Tenancy;

namespace Gccs.Application.Identity;

public sealed class SsoSignInEnforcementService(
    ISsoSignInEnforcementRepository repository,
    ICurrentTenantContext tenantContext,
    IAuditEventWriter auditEventWriter)
{
    private const string ConfirmationPhrase = "CONFIRM SSO POLICY";

    public async Task<TenantSsoPolicyDto> GetPolicyAsync(CancellationToken cancellationToken = default) =>
        await repository.GetPolicyForCurrentTenantAsync(cancellationToken) ??
        new TenantSsoPolicyDto(
            Guid.Empty,
            tenantContext.TenantId,
            SsoEnforcementMode.Optional,
            null,
            null,
            new Dictionary<string, string>(),
            null,
            null,
            null);

    public async Task<TenantSsoPolicyDto> UpdatePolicyAsync(
        UpdateTenantSsoPolicyRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        ValidateConfirmation(request.ConfirmationText);
        var policy = await repository.UpsertPolicyForCurrentTenantAsync(
            request.Mode,
            request.SamlConfigurationId,
            NormalizeOptional(request.RequiredEmailDomain, 160)?.ToLowerInvariant(),
            NormalizeAttributes(request.RequiredAttributes ?? new Dictionary<string, string>()),
            actorUserId,
            cancellationToken);

        await auditEventWriter.WriteAsync(
            tenantContext.TenantId,
            actorUserId,
            AuditAction.Updated,
            "TenantSsoPolicy",
            policy.Id.ToString(),
            $"Tenant SSO enforcement mode changed to {policy.Mode}.",
            new Dictionary<string, string>
            {
                ["mode"] = policy.Mode.ToString(),
                ["samlConfigurationId"] = policy.SamlConfigurationId?.ToString() ?? string.Empty
            },
            cancellationToken);

        return policy;
    }

    public async Task<SamlAccountLinkDto> LinkAccountAsync(
        CreateSamlAccountLinkRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var subject = NormalizeRequired(request.SamlSubject, nameof(request.SamlSubject), 512);
        var email = NormalizeEmail(request.Email);
        var member = await repository.GetCurrentTenantMemberByEmailAsync(email, cancellationToken);
        if (member is null)
        {
            throw new SsoSignInValidationException("SAML account can only be linked to an existing current-tenant member.");
        }

        EnsureMemberCanUseSso(member);
        await EnsureRequiredAttributesAsync(request.Attributes ?? new Dictionary<string, string>(), cancellationToken);

        var link = await repository.UpsertAccountLinkForCurrentTenantAsync(
            member.MembershipId,
            member.UserId,
            subject,
            email,
            request.SamlConfigurationId,
            NormalizeAttributes(request.Attributes ?? new Dictionary<string, string>()),
            actorUserId,
            cancellationToken);

        await auditEventWriter.WriteAsync(
            tenantContext.TenantId,
            actorUserId,
            AuditAction.Created,
            "SamlAccountLink",
            link.Id.ToString(),
            $"SAML account link was created for '{link.Email}'.",
            new Dictionary<string, string>
            {
                ["userId"] = link.UserId.ToString(),
                ["membershipId"] = link.MembershipId.ToString()
            },
            cancellationToken);

        return link;
    }

    public async Task<SsoSignInEvaluationResult> EvaluateSamlSignInAsync(
        SsoSignInEvaluationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var tenantStatus = await repository.GetCurrentTenantStatusAsync(cancellationToken);
        if (tenantStatus is not TenantStatus.Active and not TenantStatus.Trialing)
        {
            return await DenySignInAsync(actorUserId, "tenant_inactive", "Tenant is not active.", request.SamlSubject, cancellationToken);
        }

        var policy = await GetPolicyAsync(cancellationToken);
        if (policy.Mode is SsoEnforcementMode.Disabled)
        {
            return await DenySignInAsync(actorUserId, "sso_disabled", "SSO sign-in is disabled for this tenant.", request.SamlSubject, cancellationToken);
        }

        var subject = NormalizeRequired(request.SamlSubject, nameof(request.SamlSubject), 512);
        var email = NormalizeEmail(request.Email);
        if (!RequiredAttributesMatch(policy, request.Attributes ?? new Dictionary<string, string>(), out var missingAttributeReason))
        {
            return await DenySignInAsync(actorUserId, "missing_required_attribute", missingAttributeReason, subject, cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(policy.RequiredEmailDomain) &&
            !email.EndsWith($"@{policy.RequiredEmailDomain}", StringComparison.OrdinalIgnoreCase))
        {
            return await DenySignInAsync(actorUserId, "email_domain_mismatch", "SAML email does not match the tenant SSO policy domain.", subject, cancellationToken);
        }

        var link = await repository.GetAccountLinkBySubjectForCurrentTenantAsync(subject, cancellationToken);
        if (link is null)
        {
            var member = await repository.GetCurrentTenantMemberByEmailAsync(email, cancellationToken);
            if (member is null)
            {
                return await DenySignInAsync(actorUserId, "unmapped_saml_account", "SAML account is not mapped to a current-tenant member.", subject, cancellationToken);
            }

            if (!MemberCanUseSso(member))
            {
                return await DenySignInAsync(actorUserId, "inactive_member", "SAML account maps to an inactive tenant member.", subject, cancellationToken);
            }

            link = await repository.UpsertAccountLinkForCurrentTenantAsync(
                member.MembershipId,
                member.UserId,
                subject,
                email,
                request.SamlConfigurationId,
                NormalizeAttributes(request.Attributes ?? new Dictionary<string, string>()),
                actorUserId,
                cancellationToken);

            await auditEventWriter.WriteAsync(
                tenantContext.TenantId,
                actorUserId,
                AuditAction.Created,
                "SamlAccountLink",
                link.Id.ToString(),
                $"SAML account link was created during sign-in for '{link.Email}'.",
                new Dictionary<string, string> { ["userId"] = link.UserId.ToString() },
                cancellationToken);
        }
        else if (!string.Equals(link.Email, email, StringComparison.OrdinalIgnoreCase))
        {
            return await DenySignInAsync(actorUserId, "subject_email_mismatch", "SAML subject is linked to a different member email.", subject, cancellationToken);
        }

        var linkedMember = await repository.GetCurrentTenantMemberByIdAsync(link.MembershipId, cancellationToken);
        if (linkedMember is null || !MemberCanUseSso(linkedMember))
        {
            return await DenySignInAsync(actorUserId, "inactive_member", "SAML account maps to an inactive tenant member.", subject, cancellationToken);
        }

        var signedIn = await repository.RecordSuccessfulSamlSignInAsync(link.Id, cancellationToken);
        await auditEventWriter.WriteAsync(
            tenantContext.TenantId,
            actorUserId,
            AuditAction.SignedIn,
            "SamlAccountLink",
            link.Id.ToString(),
            $"SSO sign-in succeeded for '{linkedMember.Email}'.",
            new Dictionary<string, string>
            {
                ["userId"] = linkedMember.UserId.ToString(),
                ["membershipId"] = linkedMember.MembershipId.ToString()
            },
            cancellationToken);

        return new SsoSignInEvaluationResult(
            true,
            "sso_sign_in_allowed",
            "SSO sign-in is allowed.",
            linkedMember.UserId,
            linkedMember.MembershipId,
            signedIn?.Id ?? link.Id,
            false,
            signedIn?.LastSuccessfulSignInAt);
    }

    public async Task<BreakGlassAccessGrantDto> CreateBreakGlassGrantAsync(
        CreateBreakGlassAccessRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (request.UserId == Guid.Empty)
        {
            throw new SsoSignInValidationException("Break-glass user ID is required.");
        }

        var reason = NormalizeRequired(request.Reason, nameof(request.Reason), 800);
        var approvalReference = NormalizeRequired(request.ApprovalReference, nameof(request.ApprovalReference), 240);
        if (request.ApprovedByUserId == Guid.Empty)
        {
            throw new SsoSignInValidationException("Break-glass approval user ID is required.");
        }

        if (request.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            throw new SsoSignInValidationException("Break-glass expiration must be in the future.");
        }

        var member = await repository.GetCurrentTenantMemberByUserIdAsync(request.UserId, cancellationToken);
        if (member is null || !MemberCanUseSso(member))
        {
            throw new SsoSignInValidationException("Break-glass access requires an active current-tenant member.");
        }

        var grant = await repository.CreateBreakGlassGrantForCurrentTenantAsync(
            request.UserId,
            reason,
            request.ApprovedByUserId,
            approvalReference,
            request.ExpiresAt,
            actorUserId,
            cancellationToken);

        await auditEventWriter.WriteAsync(
            tenantContext.TenantId,
            actorUserId,
            AuditAction.Approved,
            "BreakGlassAccessGrant",
            grant.Id.ToString(),
            $"Break-glass access was approved for user '{grant.UserId}'.",
            new Dictionary<string, string>
            {
                ["approvedByUserId"] = grant.ApprovedByUserId.ToString(),
                ["expiresAt"] = grant.ExpiresAt.ToString("O")
            },
            cancellationToken);

        return grant;
    }

    public async Task<SsoSignInEvaluationResult?> UseBreakGlassGrantAsync(
        Guid grantId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var grant = await repository.GetBreakGlassGrantForCurrentTenantAsync(grantId, cancellationToken);
        if (grant is null)
        {
            return null;
        }

        if (grant.Status is not BreakGlassGrantStatus.Active || grant.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            await repository.MarkBreakGlassGrantExpiredAsync(grant.Id, actorUserId, cancellationToken);
            await auditEventWriter.WriteAsync(
                tenantContext.TenantId,
                actorUserId,
                AuditAction.Rejected,
                "BreakGlassAccessGrant",
                grant.Id.ToString(),
                "Expired break-glass access use was denied.",
                new Dictionary<string, string> { ["reasonCode"] = "break_glass_expired" },
                cancellationToken);

            return new SsoSignInEvaluationResult(false, "break_glass_expired", "Break-glass access is expired.", grant.UserId, null, null, true, null);
        }

        await repository.RecordBreakGlassUseAsync(grant.Id, actorUserId, cancellationToken);
        await auditEventWriter.WriteAsync(
            tenantContext.TenantId,
            actorUserId,
            AuditAction.SignedIn,
            "BreakGlassAccessGrant",
            grant.Id.ToString(),
            "Break-glass access was used.",
            new Dictionary<string, string> { ["userId"] = grant.UserId.ToString() },
            cancellationToken);

        return new SsoSignInEvaluationResult(true, "break_glass_allowed", "Break-glass access is allowed.", grant.UserId, null, null, true, DateTimeOffset.UtcNow);
    }

    private async Task EnsureRequiredAttributesAsync(
        IReadOnlyDictionary<string, string> attributes,
        CancellationToken cancellationToken)
    {
        var policy = await GetPolicyAsync(cancellationToken);
        if (!RequiredAttributesMatch(policy, attributes, out var reason))
        {
            throw new SsoSignInValidationException(reason);
        }
    }

    private async Task<SsoSignInEvaluationResult> DenySignInAsync(
        Guid actorUserId,
        string reasonCode,
        string message,
        string? samlSubject,
        CancellationToken cancellationToken)
    {
        await auditEventWriter.WriteAsync(
            tenantContext.TenantId,
            actorUserId,
            AuditAction.Rejected,
            "SsoSignInAttempt",
            string.IsNullOrWhiteSpace(samlSubject) ? Guid.NewGuid().ToString() : samlSubject,
            $"SSO sign-in failed: {message}",
            new Dictionary<string, string> { ["reasonCode"] = reasonCode },
            cancellationToken);

        return new SsoSignInEvaluationResult(false, reasonCode, message, null, null, null, false, null);
    }

    private static bool RequiredAttributesMatch(
        TenantSsoPolicyDto policy,
        IReadOnlyDictionary<string, string> actualAttributes,
        out string reason)
    {
        var attributes = actualAttributes.ToDictionary(
            pair => pair.Key.Trim(),
            pair => pair.Value.Trim(),
            StringComparer.OrdinalIgnoreCase);

        foreach (var required in policy.RequiredAttributes)
        {
            if (!attributes.TryGetValue(required.Key, out var actual) ||
                !string.Equals(actual, required.Value, StringComparison.OrdinalIgnoreCase))
            {
                reason = $"SAML assertion is missing required attribute '{required.Key}'.";
                return false;
            }
        }

        reason = string.Empty;
        return true;
    }

    private static void EnsureMemberCanUseSso(TenantMemberDto member)
    {
        if (!MemberCanUseSso(member))
        {
            throw new SsoSignInValidationException("SAML account can only be linked to an active user with an active tenant membership.");
        }
    }

    private static bool MemberCanUseSso(TenantMemberDto member) =>
        member.UserStatus is UserStatus.Active && member.MembershipStatus is MembershipStatus.Active;

    private static void ValidateConfirmation(string confirmationText)
    {
        if (!string.Equals(confirmationText?.Trim(), ConfirmationPhrase, StringComparison.Ordinal))
        {
            throw new SsoSignInValidationException($"Confirmation text must be '{ConfirmationPhrase}'.");
        }
    }

    private static IReadOnlyDictionary<string, string> NormalizeAttributes(IReadOnlyDictionary<string, string> attributes)
    {
        var normalized = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in attributes)
        {
            var key = NormalizeRequired(pair.Key, nameof(attributes), 160);
            var value = NormalizeRequired(pair.Value, nameof(attributes), 400);
            normalized[key] = value;
        }

        return normalized;
    }

    private static string NormalizeEmail(string email)
    {
        var normalized = NormalizeRequired(email, nameof(email), 320).ToLowerInvariant();
        if (!normalized.Contains('@', StringComparison.Ordinal))
        {
            throw new SsoSignInValidationException("SAML email must be a valid email address.");
        }

        return normalized;
    }

    private static string NormalizeRequired(string? value, string fieldName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new SsoSignInValidationException($"{fieldName} is required.");
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new SsoSignInValidationException($"{fieldName} must be {maxLength} characters or fewer.");
        }

        return normalized;
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new SsoSignInValidationException($"Value must be {maxLength} characters or fewer.");
        }

        return normalized;
    }
}

public interface ISsoSignInEnforcementRepository
{
    Task<TenantStatus?> GetCurrentTenantStatusAsync(CancellationToken cancellationToken = default);

    Task<TenantSsoPolicyDto?> GetPolicyForCurrentTenantAsync(CancellationToken cancellationToken = default);

    Task<TenantSsoPolicyDto> UpsertPolicyForCurrentTenantAsync(
        SsoEnforcementMode mode,
        Guid? samlConfigurationId,
        string? requiredEmailDomain,
        IReadOnlyDictionary<string, string> requiredAttributes,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<TenantMemberDto?> GetCurrentTenantMemberByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<TenantMemberDto?> GetCurrentTenantMemberByIdAsync(Guid membershipId, CancellationToken cancellationToken = default);

    Task<TenantMemberDto?> GetCurrentTenantMemberByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<SamlAccountLinkDto?> GetAccountLinkBySubjectForCurrentTenantAsync(string samlSubject, CancellationToken cancellationToken = default);

    Task<SamlAccountLinkDto> UpsertAccountLinkForCurrentTenantAsync(
        Guid membershipId,
        Guid userId,
        string samlSubject,
        string email,
        Guid? samlConfigurationId,
        IReadOnlyDictionary<string, string> attributes,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<SamlAccountLinkDto?> RecordSuccessfulSamlSignInAsync(Guid linkId, CancellationToken cancellationToken = default);

    Task<BreakGlassAccessGrantDto> CreateBreakGlassGrantForCurrentTenantAsync(
        Guid userId,
        string reason,
        Guid approvedByUserId,
        string approvalReference,
        DateTimeOffset expiresAt,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<BreakGlassAccessGrantDto?> GetBreakGlassGrantForCurrentTenantAsync(Guid grantId, CancellationToken cancellationToken = default);

    Task<BreakGlassAccessGrantDto?> RecordBreakGlassUseAsync(Guid grantId, Guid actorUserId, CancellationToken cancellationToken = default);

    Task<BreakGlassAccessGrantDto?> MarkBreakGlassGrantExpiredAsync(Guid grantId, Guid actorUserId, CancellationToken cancellationToken = default);
}

public sealed record TenantSsoPolicyDto(
    Guid Id,
    Guid TenantId,
    SsoEnforcementMode Mode,
    Guid? SamlConfigurationId,
    string? RequiredEmailDomain,
    IReadOnlyDictionary<string, string> RequiredAttributes,
    DateTimeOffset? CreatedAt,
    DateTimeOffset? UpdatedAt,
    Guid? UpdatedByUserId);

public sealed record UpdateTenantSsoPolicyRequest(
    SsoEnforcementMode Mode,
    string ConfirmationText,
    Guid? SamlConfigurationId = null,
    string? RequiredEmailDomain = null,
    IReadOnlyDictionary<string, string>? RequiredAttributes = null);

public sealed record CreateSamlAccountLinkRequest(
    string SamlSubject,
    string Email,
    Guid? SamlConfigurationId = null,
    IReadOnlyDictionary<string, string>? Attributes = null);

public sealed record SsoSignInEvaluationRequest(
    string SamlSubject,
    string Email,
    Guid? SamlConfigurationId = null,
    IReadOnlyDictionary<string, string>? Attributes = null);

public sealed record SsoSignInEvaluationResult(
    bool Allowed,
    string ReasonCode,
    string Message,
    Guid? UserId,
    Guid? MembershipId,
    Guid? SamlAccountLinkId,
    bool UsedBreakGlass,
    DateTimeOffset? OccurredAt);

public sealed record SamlAccountLinkDto(
    Guid Id,
    Guid TenantId,
    Guid MembershipId,
    Guid UserId,
    string SamlSubject,
    string Email,
    Guid? SamlConfigurationId,
    IReadOnlyDictionary<string, string> Attributes,
    DateTimeOffset? LastSuccessfulSignInAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record CreateBreakGlassAccessRequest(
    Guid UserId,
    string Reason,
    Guid ApprovedByUserId,
    string ApprovalReference,
    DateTimeOffset ExpiresAt);

public sealed record BreakGlassAccessGrantDto(
    Guid Id,
    Guid TenantId,
    Guid UserId,
    string Reason,
    Guid ApprovedByUserId,
    string ApprovalReference,
    DateTimeOffset ExpiresAt,
    BreakGlassGrantStatus Status,
    DateTimeOffset? LastUsedAt,
    Guid? LastUsedByUserId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public enum SsoEnforcementMode
{
    Optional,
    RequiredForMembers,
    RequiredForAllExceptBreakGlass,
    Disabled
}

public enum BreakGlassGrantStatus
{
    Active,
    Used,
    Expired,
    Revoked
}

public sealed class SsoSignInValidationException(string message) : InvalidOperationException(message);
