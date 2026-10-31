namespace Gccs.Infrastructure.Persistence.Models;

public sealed class CuiReadinessEvidenceEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Kind { get; set; } = "";
    public int Version { get; set; }
    public string State { get; set; } = "";
    public DateTimeOffset ReviewedAt { get; set; }
    public Guid ReviewedByUserId { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public string SourceReference { get; set; } = "";
    public string ReviewNotes { get; set; } = "";
    public string DetailsJson { get; set; } = "{}";
}
