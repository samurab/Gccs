namespace Gccs.Domain.Compliance;

public enum SspSectionType
{
    SystemDescription,
    AuthorizationBoundary,
    Environment,
    Interconnections,
    Users,
    Roles,
    DataTypes,
    CuiHandlingPosture,
    ControlImplementationNarratives,
    InheritedResponsibilities,
    ExternalServiceProviders,
    EvidenceReferences
}

public enum SspSectionStatus { Draft, InReview, Approved, Superseded, Archived }

public enum SspLinkedRecordType
{
    CompanyProfile,
    SystemBoundary,
    Asset,
    CmmcControl,
    ResponsibilityMatrix,
    Policy,
    PoamItem,
    Evidence
}

public static class SspSectionLifecycle
{
    public static bool CanTransition(SspSectionStatus current, SspSectionStatus next) =>
        (current, next) switch
        {
            (SspSectionStatus.Draft, SspSectionStatus.InReview) => true,
            (SspSectionStatus.InReview, SspSectionStatus.Draft or SspSectionStatus.Approved) => true,
            (SspSectionStatus.Approved, SspSectionStatus.Superseded or SspSectionStatus.Archived) => true,
            (SspSectionStatus.Superseded, SspSectionStatus.Archived) => true,
            _ => false
        };

    public static bool CanEdit(SspSectionStatus status) =>
        status is SspSectionStatus.Draft or SspSectionStatus.InReview;
}
