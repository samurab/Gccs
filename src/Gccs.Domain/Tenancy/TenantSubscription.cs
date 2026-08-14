namespace Gccs.Domain.Tenancy;

public enum TenantKind
{
    ContractorWorkspace,
    PartnerOrganization,
    Internal
}

public enum SubscriptionPlan
{
    PilotEvaluation,
    CommercialStandard,
    Partner,
    Internal
}

public enum SubscriptionStatus
{
    Pending,
    Active,
    GracePeriod,
    Expired,
    Cancelled,
    Converted
}

public enum SubscriptionAccessLevel
{
    Full,
    ReadOnly,
    Denied
}

public sealed record TenantSubscription(
    Guid Id,
    Guid TenantId,
    TenantKind TenantKind,
    SubscriptionPlan Plan,
    string PlanCode,
    SubscriptionStatus Status,
    DateTimeOffset StartsAt,
    DateTimeOffset? EndsAt,
    DateTimeOffset? GraceEndsAt,
    string? ExternalCustomerReference,
    string? ExternalSubscriptionReference,
    string StatusReason,
    long Version)
{
    public SubscriptionStatus EffectiveStatus(DateTimeOffset now)
    {
        if (Status is SubscriptionStatus.Pending or SubscriptionStatus.Cancelled or SubscriptionStatus.Expired)
        {
            return Status;
        }

        if (Status is SubscriptionStatus.Converted)
        {
            return Plan is SubscriptionPlan.CommercialStandard
                ? SubscriptionStatus.Converted
                : SubscriptionStatus.Expired;
        }

        if (Status is SubscriptionStatus.GracePeriod)
        {
            return GraceEndsAt is not null && now < GraceEndsAt.Value
                ? SubscriptionStatus.GracePeriod
                : SubscriptionStatus.Expired;
        }

        if (now < StartsAt)
        {
            return SubscriptionStatus.Pending;
        }

        if (EndsAt is null)
        {
            return Plan is SubscriptionPlan.PilotEvaluation
                ? SubscriptionStatus.Expired
                : SubscriptionStatus.Active;
        }

        if (now < EndsAt.Value)
        {
            return SubscriptionStatus.Active;
        }

        return GraceEndsAt is not null && now < GraceEndsAt.Value
            ? SubscriptionStatus.GracePeriod
            : SubscriptionStatus.Expired;
    }

    public SubscriptionAccessLevel AccessLevel(DateTimeOffset now) => EffectiveStatus(now) switch
    {
        SubscriptionStatus.Active or SubscriptionStatus.Converted => SubscriptionAccessLevel.Full,
        SubscriptionStatus.GracePeriod => SubscriptionAccessLevel.ReadOnly,
        _ => SubscriptionAccessLevel.Denied
    };
}
