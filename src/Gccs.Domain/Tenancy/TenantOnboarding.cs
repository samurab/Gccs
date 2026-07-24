namespace Gccs.Domain.Tenancy;

public enum TenantOnboardingType
{
    Pilot,
    Paid
}

public enum TenantOnboardingStatus
{
    PendingOwnerAcceptance,
    Active,
    Cancelled
}
