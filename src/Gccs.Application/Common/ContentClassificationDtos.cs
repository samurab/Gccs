using Gccs.Domain.Common;

namespace Gccs.Application.Common;

public sealed record ContentClassificationRequest(
    ContentClassification Classification,
    ContentClassificationSource Source = ContentClassificationSource.UserSelected,
    decimal? Confidence = null,
    Guid? ReviewedByUserId = null,
    DateTimeOffset? ReviewedAt = null,
    string? Reason = null,
    bool IsApprovedDemoContent = false);

public sealed record ContentClassificationDto(
    ContentClassification Classification,
    ContentClassificationSource Source,
    decimal? Confidence,
    Guid? ReviewedByUserId,
    DateTimeOffset? ReviewedAt,
    string? Reason,
    bool IsApprovedDemoContent);

public sealed record ContentClassificationHistoryDto(
    Guid Id,
    Guid TenantId,
    string EntityType,
    string EntityId,
    ContentClassification? PreviousClassification,
    ContentClassification NewClassification,
    ContentClassificationSource Source,
    decimal? Confidence,
    Guid? ReviewedByUserId,
    DateTimeOffset? ReviewedAt,
    string? Reason,
    Guid ChangedByUserId,
    DateTimeOffset ChangedAt);
