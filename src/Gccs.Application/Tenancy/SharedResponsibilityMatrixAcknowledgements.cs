using Gccs.Application.Audit;
using Gccs.Domain.Audit;

namespace Gccs.Application.Tenancy;

public sealed class SharedResponsibilityMatrixAcknowledgementService(
    ISharedResponsibilityMatrixAcknowledgementRepository repository,
    IAuditEventWriter auditEventWriter)
{
    public async Task<IReadOnlyList<SharedResponsibilityMatrixAcknowledgementDto>> ListAsync(
        Guid tenantId,
        SharedResponsibilityMatrixDto currentMatrix,
        CancellationToken cancellationToken = default)
    {
        var history = await repository.ListAsync(tenantId, cancellationToken);
        return history.Select(acknowledgement => WithCurrentStatus(acknowledgement, currentMatrix)).ToArray();
    }

    public async Task<SharedResponsibilityMatrixAcknowledgementDto> AcknowledgeAsync(
        Guid tenantId,
        SharedResponsibilityMatrixDto currentMatrix,
        AcknowledgeSharedResponsibilityMatrixRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(currentMatrix, request);
        var existing = await repository.FindAsync(tenantId, currentMatrix.MatrixId, currentMatrix.Version, cancellationToken);
        if (existing is not null)
        {
            return WithCurrentStatus(existing, currentMatrix);
        }

        var acknowledgedAt = DateTimeOffset.UtcNow;
        var acknowledgement = await repository.AddAsync(
            tenantId,
            currentMatrix.MatrixId,
            currentMatrix.Version,
            currentMatrix.Title,
            actorUserId,
            acknowledgedAt,
            cancellationToken);

        await auditEventWriter.WriteAsync(
            tenantId,
            actorUserId,
            AuditAction.Created,
            "SharedResponsibilityMatrixAcknowledgement",
            acknowledgement.Id.ToString(),
            "Shared responsibility matrix was acknowledged by tenant admin.",
            new Dictionary<string, string>
            {
                ["tenantId"] = tenantId.ToString(),
                ["actorUserId"] = actorUserId.ToString(),
                ["matrixId"] = currentMatrix.MatrixId,
                ["matrixVersion"] = currentMatrix.Version,
                ["acknowledgedAt"] = acknowledgedAt.ToString("O"),
                ["result"] = "acknowledged"
            },
            cancellationToken);

        return WithCurrentStatus(acknowledgement, currentMatrix);
    }

    public async Task EnsureCurrentAcknowledgedAsync(
        Guid tenantId,
        SharedResponsibilityMatrixDto currentMatrix,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var acknowledgement = await repository.FindAsync(tenantId, currentMatrix.MatrixId, currentMatrix.Version, cancellationToken);
        if (acknowledgement is not null)
        {
            return;
        }

        await auditEventWriter.WriteAsync(
            tenantId,
            actorUserId,
            AuditAction.Rejected,
            "SharedResponsibilityMatrixAcknowledgement",
            $"{tenantId}:{currentMatrix.MatrixVersionKey()}",
            "CUI-ready approval failed because the current shared responsibility matrix was not acknowledged.",
            new Dictionary<string, string>
            {
                ["tenantId"] = tenantId.ToString(),
                ["matrixId"] = currentMatrix.MatrixId,
                ["matrixVersion"] = currentMatrix.Version,
                ["result"] = "failed",
                ["reason"] = "current_matrix_not_acknowledged"
            },
            cancellationToken);

        throw new SharedResponsibilityMatrixAcknowledgementException(
            "Current shared responsibility matrix acknowledgement is required before CUI-ready approval.");
    }

    private static void ValidateRequest(SharedResponsibilityMatrixDto currentMatrix, AcknowledgeSharedResponsibilityMatrixRequest request)
    {
        if (!request.Acknowledged)
        {
            throw new SharedResponsibilityMatrixAcknowledgementException("Shared responsibility matrix acknowledgement is required.");
        }

        if (!string.Equals(request.MatrixId, currentMatrix.MatrixId, StringComparison.Ordinal) ||
            !string.Equals(request.MatrixVersion, currentMatrix.Version, StringComparison.Ordinal))
        {
            throw new SharedResponsibilityMatrixAcknowledgementException("Shared responsibility matrix acknowledgement must reference the current published version.");
        }
    }

    private static SharedResponsibilityMatrixAcknowledgementDto WithCurrentStatus(
        SharedResponsibilityMatrixAcknowledgementDto acknowledgement,
        SharedResponsibilityMatrixDto currentMatrix)
    {
        var status = string.Equals(acknowledgement.MatrixId, currentMatrix.MatrixId, StringComparison.Ordinal) &&
            string.Equals(acknowledgement.MatrixVersion, currentMatrix.Version, StringComparison.Ordinal)
                ? SharedResponsibilityMatrixAcknowledgementStatus.Current
                : SharedResponsibilityMatrixAcknowledgementStatus.Outdated;

        return acknowledgement with { Status = status };
    }
}

public interface ISharedResponsibilityMatrixAcknowledgementRepository
{
    Task<IReadOnlyList<SharedResponsibilityMatrixAcknowledgementDto>> ListAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task<SharedResponsibilityMatrixAcknowledgementDto?> FindAsync(
        Guid tenantId,
        string matrixId,
        string matrixVersion,
        CancellationToken cancellationToken = default);

    Task<SharedResponsibilityMatrixAcknowledgementDto> AddAsync(
        Guid tenantId,
        string matrixId,
        string matrixVersion,
        string matrixTitle,
        Guid actorUserId,
        DateTimeOffset acknowledgedAt,
        CancellationToken cancellationToken = default);
}

public sealed class SharedResponsibilityMatrixAcknowledgementException(string message) : InvalidOperationException(message);

public sealed record AcknowledgeSharedResponsibilityMatrixRequest(
    string MatrixId,
    string MatrixVersion,
    bool Acknowledged);

public sealed record SharedResponsibilityMatrixAcknowledgementDto(
    Guid Id,
    Guid TenantId,
    string MatrixId,
    string MatrixVersion,
    string MatrixTitle,
    Guid AcknowledgedByUserId,
    DateTimeOffset AcknowledgedAt,
    SharedResponsibilityMatrixAcknowledgementStatus Status);

public enum SharedResponsibilityMatrixAcknowledgementStatus
{
    Current,
    Outdated
}

internal static class SharedResponsibilityMatrixVersionExtensions
{
    public static string MatrixVersionKey(this SharedResponsibilityMatrixDto matrix) =>
        $"{matrix.MatrixId}:{matrix.Version}";
}
