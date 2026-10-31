namespace Gccs.Domain.Compliance;

public sealed record MvpModule(
    string Key,
    string Name,
    string Purpose,
    string Status);
