using System.Text.Json;
using Gccs.Application.Contracts;
using Gccs.Application.Security;
using Gccs.Domain.Common;
using Gccs.Domain.Compliance;
using Gccs.Domain.Contracts;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Contracts;

public sealed class EfContractRepository(GccsDbContext dbContext, ICurrentTenantContext tenantContext) : IContractRepository
{
    public async Task<IReadOnlyList<ContractDto>> ListCurrentTenantAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Contracts
            .AsNoTracking()
            .Where(contract => contract.TenantId == tenantContext.TenantId)
            .OrderByDescending(contract => contract.UpdatedAt ?? contract.CreatedAt)
            .ThenBy(contract => contract.ContractNumber)
            .Select(contract => ToDto(contract))
            .ToArrayAsync(cancellationToken);

    public async Task<ContractDto?> FindCurrentTenantAsync(Guid contractId, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Contracts
            .AsNoTracking()
            .SingleOrDefaultAsync(
                contract => contract.TenantId == tenantContext.TenantId && contract.Id == contractId,
                cancellationToken);

        return entity is null ? null : ToDto(entity);
    }

    public async Task<IReadOnlyList<ContractDocumentDto>?> ListDocumentsAsync(
        Guid contractId,
        CancellationToken cancellationToken = default)
    {
        var exists = await dbContext.Contracts.AnyAsync(
            contract => contract.TenantId == tenantContext.TenantId && contract.Id == contractId,
            cancellationToken);

        if (!exists)
        {
            return null;
        }

        return await dbContext.Set<ContractDocumentEntity>()
            .AsNoTracking()
            .Where(document => document.ContractId == contractId)
            .OrderByDescending(document => document.UploadedAt)
            .Select(document => ToDocumentDto(document))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ContractDeliverableDto>?> ListDeliverablesAsync(
        Guid contractId,
        CancellationToken cancellationToken = default)
    {
        var exists = await dbContext.Contracts.AnyAsync(
            contract => contract.TenantId == tenantContext.TenantId && contract.Id == contractId,
            cancellationToken);

        if (!exists)
        {
            return null;
        }

        return await dbContext.Set<ContractDeliverableEntity>()
            .AsNoTracking()
            .Where(deliverable => deliverable.ContractId == contractId)
            .OrderBy(deliverable => deliverable.DueAt ?? DateOnly.MaxValue)
            .ThenBy(deliverable => deliverable.Name)
            .Select(deliverable => ToDeliverableDto(deliverable))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ContractClauseDto>?> ListClausesAsync(
        Guid contractId,
        CancellationToken cancellationToken = default)
    {
        var exists = await dbContext.Contracts.AnyAsync(
            contract => contract.TenantId == tenantContext.TenantId && contract.Id == contractId,
            cancellationToken);

        if (!exists)
        {
            return null;
        }

        return await dbContext.Set<ContractClauseEntity>()
            .AsNoTracking()
            .Where(clause => clause.ContractId == contractId && clause.RemovedAt == null)
            .OrderBy(clause => clause.Source)
            .ThenBy(clause => clause.ClauseNumber)
            .Select(clause => ToClauseDto(clause))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<ContractDto> CreateCurrentTenantAsync(
        UpsertContractRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var entity = new ContractEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantContext.TenantId,
            CreatedAt = now,
            CreatedByUserId = actorUserId
        };

        Apply(entity, request);
        dbContext.Contracts.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<ContractDto?> UpdateCurrentTenantAsync(
        Guid contractId,
        UpsertContractRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Contracts
            .SingleOrDefaultAsync(
                contract => contract.TenantId == tenantContext.TenantId && contract.Id == contractId,
                cancellationToken);

        if (entity is null)
        {
            return null;
        }

        Apply(entity, request);
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        entity.UpdatedByUserId = actorUserId;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<ContractDocumentDto?> CreateDocumentMetadataAsync(
        Guid contractId,
        ContractDocumentUploadRequest request,
        Guid actorUserId,
        string noticeVersion,
        CancellationToken cancellationToken = default)
    {
        var contractExists = await dbContext.Contracts.AnyAsync(
            contract => contract.TenantId == tenantContext.TenantId && contract.Id == contractId,
            cancellationToken);

        if (!contractExists)
        {
            return null;
        }

        var documentId = Guid.NewGuid();
        var fileName = request.FileName.Trim();
        var document = new ContractDocumentEntity
        {
            Id = documentId,
            ContractId = contractId,
            Type = request.Type,
            FileName = fileName,
            ContentType = request.ContentType.Trim().ToLowerInvariant(),
            SizeBytes = request.SizeBytes,
            StorageUri = $"pending://contracts/{contractId}/documents/{documentId}/{Uri.EscapeDataString(fileName)}",
            ExtractedTextHash = null,
            ValidationStatus = "accepted",
            MalwareScanStatus = "scan-pending",
            NoticeVersion = noticeVersion,
            UploadedAt = DateTimeOffset.UtcNow,
            UploadedByUserId = actorUserId,
            ContainsPotentialCui = request.ContainsPotentialCui
        };

        dbContext.Set<ContractDocumentEntity>().Add(document);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDocumentDto(document);
    }

    public async Task<ContractDocumentDto?> DeleteDocumentAsync(
        Guid contractId,
        Guid documentId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var document = await dbContext.Set<ContractDocumentEntity>()
            .Include(item => item.Contract)
            .SingleOrDefaultAsync(
                item =>
                    item.Id == documentId &&
                    item.ContractId == contractId &&
                    item.Contract != null &&
                    item.Contract.TenantId == tenantContext.TenantId,
                cancellationToken);

        if (document is null)
        {
            return null;
        }

        var dto = ToDocumentDto(document);
        dbContext.Set<ContractDocumentEntity>().Remove(document);
        await dbContext.SaveChangesAsync(cancellationToken);
        return dto;
    }

    public async Task<ExtractionJobDto?> CreateExtractionJobAsync(
        Guid contractId,
        Guid documentId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var documentExists = await dbContext.Set<ContractDocumentEntity>()
            .AnyAsync(
                document =>
                    document.Id == documentId &&
                    document.ContractId == contractId &&
                    document.Contract != null &&
                    document.Contract.TenantId == tenantContext.TenantId,
                cancellationToken);

        if (!documentExists)
        {
            return null;
        }

        var job = new ExtractionJobEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantContext.TenantId,
            SourceDocumentId = documentId,
            RequestedByUserId = actorUserId,
            Status = ExtractionJobStatus.Queued,
            RequestedAt = DateTimeOffset.UtcNow
        };

        dbContext.Set<ExtractionJobEntity>().Add(job);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToExtractionJobDto(job);
    }

    public async Task<ExtractionJobDto?> MarkExtractionJobCompletedAsync(
        Guid extractionJobId,
        CancellationToken cancellationToken = default)
    {
        var job = await dbContext.Set<ExtractionJobEntity>()
            .SingleOrDefaultAsync(
                item => item.Id == extractionJobId && item.TenantId == tenantContext.TenantId,
                cancellationToken);

        if (job is null)
        {
            return null;
        }

        job.Status = ExtractionJobStatus.Completed;
        job.CompletedAt = DateTimeOffset.UtcNow;
        job.FailureReason = null;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToExtractionJobDto(job);
    }

    public async Task<ExtractionJobDto?> MarkExtractionJobProcessingAsync(
        Guid extractionJobId,
        CancellationToken cancellationToken = default)
    {
        var job = await dbContext.Set<ExtractionJobEntity>()
            .SingleOrDefaultAsync(
                item => item.Id == extractionJobId && item.TenantId == tenantContext.TenantId,
                cancellationToken);

        if (job is null)
        {
            return null;
        }

        job.Status = ExtractionJobStatus.Processing;
        job.StartedAt ??= DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToExtractionJobDto(job);
    }

    public async Task<ExtractionJobDto?> MarkExtractionJobFailedAsync(
        Guid extractionJobId,
        string failureReason,
        CancellationToken cancellationToken = default)
    {
        var job = await dbContext.Set<ExtractionJobEntity>()
            .SingleOrDefaultAsync(
                item => item.Id == extractionJobId && item.TenantId == tenantContext.TenantId,
                cancellationToken);

        if (job is null)
        {
            return null;
        }

        job.Status = ExtractionJobStatus.Failed;
        job.CompletedAt = DateTimeOffset.UtcNow;
        job.FailureReason = string.IsNullOrWhiteSpace(failureReason)
            ? "Extraction failed without a detailed reason."
            : failureReason.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToExtractionJobDto(job);
    }

    public async Task<ExtractionJobProcessingInputDto?> FindExtractionJobInputAsync(
        Guid extractionJobId,
        CancellationToken cancellationToken = default)
    {
        var job = await dbContext.Set<ExtractionJobEntity>()
            .AsNoTracking()
            .Include(item => item.SourceDocument)
            .SingleOrDefaultAsync(
                item =>
                    item.Id == extractionJobId &&
                    item.TenantId == tenantContext.TenantId &&
                    item.SourceDocument != null,
                cancellationToken);

        return job?.SourceDocument is null
            ? null
            : new ExtractionJobProcessingInputDto(ToExtractionJobDto(job), ToDocumentDto(job.SourceDocument));
    }

    public async Task<ClauseLibraryMatchDto?> FindPublishedClauseLibraryMatchAsync(
        Guid tenantId,
        string clauseNumber,
        CancellationToken cancellationToken = default)
    {
        var normalizedNumber = clauseNumber.Trim();
        var clause = await dbContext.Clauses
            .AsNoTracking()
            .Where(item =>
                item.ReviewState == ReviewState.Published &&
                item.Number == normalizedNumber &&
                (item.TenantId == null || item.TenantId == tenantId))
            .OrderByDescending(item => item.TenantId == tenantId)
            .ThenBy(item => item.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return clause is null ? null : new ClauseLibraryMatchDto(clause.Id, clause.Number, clause.Title);
    }

    public async Task<IReadOnlyList<ClauseCandidateDto>> ReplaceClauseCandidatesAsync(
        Guid extractionJobId,
        Guid sourceDocumentId,
        IReadOnlyList<ClauseCandidateCreateDto> candidates,
        CancellationToken cancellationToken = default)
    {
        var job = await dbContext.Set<ExtractionJobEntity>()
            .SingleOrDefaultAsync(
                item =>
                    item.Id == extractionJobId &&
                    item.SourceDocumentId == sourceDocumentId &&
                    item.TenantId == tenantContext.TenantId,
                cancellationToken);

        if (job is null)
        {
            return [];
        }

        var existing = await dbContext.Set<ClauseCandidateEntity>()
            .Where(candidate => candidate.ExtractionJobId == extractionJobId)
            .ToArrayAsync(cancellationToken);
        dbContext.Set<ClauseCandidateEntity>().RemoveRange(existing);

        var now = DateTimeOffset.UtcNow;
        var entities = candidates
            .Select(candidate => new ClauseCandidateEntity
            {
                Id = Guid.NewGuid(),
                TenantId = tenantContext.TenantId,
                ExtractionJobId = extractionJobId,
                SourceDocumentId = sourceDocumentId,
                NormalizedCitation = candidate.NormalizedCitation,
                RawExtractedText = candidate.RawExtractedText,
                DetectedTitle = candidate.DetectedTitle,
                Confidence = candidate.Confidence,
                LocationMetadata = candidate.LocationMetadata,
                MatchMethod = candidate.MatchMethod,
                ClauseLibraryId = candidate.ClauseLibraryId,
                ReviewStatus = "pending_review",
                CreatedAt = now
            })
            .ToArray();

        dbContext.Set<ClauseCandidateEntity>().AddRange(entities);
        await dbContext.SaveChangesAsync(cancellationToken);
        return entities.Select(ToClauseCandidateDto).ToArray();
    }

    public async Task<ContractDocumentExtractionResultsDto?> ListExtractionResultsAsync(
        Guid contractId,
        Guid documentId,
        string? reviewStatus,
        CancellationToken cancellationToken = default)
    {
        var documentExists = await dbContext.Set<ContractDocumentEntity>()
            .AnyAsync(
                document =>
                    document.Id == documentId &&
                    document.ContractId == contractId &&
                    document.Contract != null &&
                    document.Contract.TenantId == tenantContext.TenantId,
                cancellationToken);

        if (!documentExists)
        {
            return null;
        }

        var latestJob = await dbContext.Set<ExtractionJobEntity>()
            .AsNoTracking()
            .Where(job => job.TenantId == tenantContext.TenantId && job.SourceDocumentId == documentId)
            .OrderByDescending(job => job.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var query = dbContext.Set<ClauseCandidateEntity>()
            .AsNoTracking()
            .Where(candidate => candidate.TenantId == tenantContext.TenantId && candidate.SourceDocumentId == documentId);

        if (!string.IsNullOrWhiteSpace(reviewStatus))
        {
            var normalizedStatus = reviewStatus.Trim();
            query = query.Where(candidate => candidate.ReviewStatus == normalizedStatus);
        }

        var candidates = await query
            .OrderByDescending(candidate => candidate.Confidence)
            .ThenBy(candidate => candidate.NormalizedCitation)
            .Select(candidate => ToClauseCandidateDto(candidate))
            .ToArrayAsync(cancellationToken);

        return new ContractDocumentExtractionResultsDto(
            contractId,
            documentId,
            latestJob?.Status,
            latestJob?.FailureReason,
            candidates.Length,
            candidates);
    }

    public async Task<ClauseCandidateDto?> EditClauseCandidateAsync(
        Guid contractId,
        Guid documentId,
        Guid candidateId,
        ClauseCandidateEditRequest request,
        CancellationToken cancellationToken = default)
    {
        var candidate = await FindCandidateForCurrentTenantAsync(contractId, documentId, candidateId, cancellationToken);
        if (candidate is null)
        {
            return null;
        }

        candidate.NormalizedCitation = request.NormalizedCitation;
        candidate.ClauseLibraryId = request.ClauseLibraryId;
        candidate.ReviewStatus = "edited";
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToClauseCandidateDto(candidate);
    }

    public async Task<ClauseCandidateDto?> AcceptClauseCandidateAsync(
        Guid contractId,
        Guid documentId,
        Guid candidateId,
        ClauseCandidateReviewRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var candidate = await FindCandidateForCurrentTenantAsync(contractId, documentId, candidateId, cancellationToken);
        var libraryClause = await dbContext.Clauses
            .AsNoTracking()
            .SingleOrDefaultAsync(
                clause =>
                    clause.Id == request.ClauseLibraryId &&
                    clause.ReviewState == ReviewState.Published &&
                    (clause.TenantId == null || clause.TenantId == tenantContext.TenantId),
                cancellationToken);

        if (candidate is null || libraryClause is null)
        {
            return null;
        }

        EnsureCandidateCanTransition(candidate, "accepted");

        var duplicateExists = await dbContext.Set<ContractClauseEntity>().AnyAsync(
            clause =>
                clause.ContractId == contractId &&
                clause.ClauseLibraryId == libraryClause.Id &&
                clause.RemovedAt == null,
            cancellationToken);

        if (!duplicateExists)
        {
            var now = DateTimeOffset.UtcNow;
            dbContext.Set<ContractClauseEntity>().Add(new ContractClauseEntity
            {
                Id = Guid.NewGuid(),
                ContractId = contractId,
                ClauseLibraryId = libraryClause.Id,
                ClauseNumber = libraryClause.Number,
                Title = libraryClause.Title,
                FullText = libraryClause.PlainEnglishSummary,
                Source = ToClauseSource(libraryClause),
                SourceUrl = libraryClause.SourceUrl,
                SourceHash = libraryClause.SourceHash,
                AttachmentReason = $"Accepted extraction candidate: {request.Reason}",
                SourceDocumentReference = candidate.LocationMetadata,
                RequiresFlowDown = libraryClause.UsuallyRequiresFlowDown,
                LastReviewedAt = libraryClause.LastReviewedAt,
                ReviewedByUserId = actorUserId,
                NextReviewDueAt = libraryClause.NextReviewDueAt,
                Confidence = libraryClause.Confidence,
                RequiresExpertReview = libraryClause.RequiresExpertReview,
                ReviewState = ReviewState.Published,
                CreatedAt = now,
                CreatedByUserId = actorUserId
            });
        }

        candidate.ClauseLibraryId = libraryClause.Id;
        candidate.NormalizedCitation = $"{libraryClause.Source.ToUpperInvariant()} {libraryClause.Number}".Trim();
        candidate.ReviewStatus = "accepted";
        candidate.ReviewedByUserId = actorUserId;
        candidate.ReviewedAt = DateTimeOffset.UtcNow;
        candidate.DecisionReason = request.Reason;
        candidate.DecisionNote = request.DecisionNote;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToClauseCandidateDto(candidate);
    }

    public async Task<ClauseCandidateDto?> RejectClauseCandidateAsync(
        Guid contractId,
        Guid documentId,
        Guid candidateId,
        ClauseCandidateReviewRequest request,
        CancellationToken cancellationToken = default)
    {
        var candidate = await FindCandidateForCurrentTenantAsync(contractId, documentId, candidateId, cancellationToken);
        if (candidate is null)
        {
            return null;
        }

        EnsureCandidateCanTransition(candidate, "rejected");
        candidate.ReviewStatus = "rejected";
        candidate.ReviewedByUserId = tenantContext.UserId;
        candidate.ReviewedAt = DateTimeOffset.UtcNow;
        candidate.DecisionReason = request.Reason;
        candidate.DecisionNote = request.DecisionNote;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToClauseCandidateDto(candidate);
    }

    public async Task<ClauseCandidateDto?> MarkClauseCandidateNeedsClarificationAsync(
        Guid contractId,
        Guid documentId,
        Guid candidateId,
        ClauseCandidateStateChangeRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var candidate = await FindCandidateForCurrentTenantAsync(contractId, documentId, candidateId, cancellationToken);
        if (candidate is null)
        {
            return null;
        }

        EnsureCandidateCanTransition(candidate, "needs_clarification");
        candidate.ReviewStatus = "needs_clarification";
        candidate.ReviewedByUserId = actorUserId;
        candidate.ReviewedAt = DateTimeOffset.UtcNow;
        candidate.DecisionReason = request.Reason;
        candidate.DecisionNote = request.DecisionNote;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToClauseCandidateDto(candidate);
    }

    public async Task<ClauseCandidateDto?> SupersedeClauseCandidateAsync(
        Guid contractId,
        Guid documentId,
        Guid candidateId,
        ClauseCandidateStateChangeRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var candidate = await FindCandidateForCurrentTenantAsync(contractId, documentId, candidateId, cancellationToken);
        if (candidate is null)
        {
            return null;
        }

        EnsureCandidateCanTransition(candidate, "superseded");
        candidate.ReviewStatus = "superseded";
        candidate.ReviewedByUserId = actorUserId;
        candidate.ReviewedAt = DateTimeOffset.UtcNow;
        candidate.DecisionReason = request.Reason;
        candidate.DecisionNote = request.DecisionNote;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToClauseCandidateDto(candidate);
    }

    public async Task<ContractDeliverableDto?> CreateDeliverableAsync(
        Guid contractId,
        UpsertContractDeliverableRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var contractExists = await dbContext.Contracts.AnyAsync(
            contract => contract.TenantId == tenantContext.TenantId && contract.Id == contractId,
            cancellationToken);

        if (!contractExists)
        {
            return null;
        }

        var deliverable = new ContractDeliverableEntity
        {
            Id = Guid.NewGuid(),
            ContractId = contractId
        };

        Apply(deliverable, request);
        dbContext.Set<ContractDeliverableEntity>().Add(deliverable);
        SyncDeliverableTask(contractId, deliverable, actorUserId);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDeliverableDto(deliverable);
    }

    public async Task<ContractDeliverableDto?> UpdateDeliverableAsync(
        Guid contractId,
        Guid deliverableId,
        UpsertContractDeliverableRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var deliverable = await dbContext.Set<ContractDeliverableEntity>()
            .Include(item => item.Contract)
            .SingleOrDefaultAsync(
                item =>
                    item.Id == deliverableId &&
                    item.ContractId == contractId &&
                    item.Contract != null &&
                    item.Contract.TenantId == tenantContext.TenantId,
                cancellationToken);

        if (deliverable is null)
        {
            return null;
        }

        Apply(deliverable, request);
        SyncDeliverableTask(contractId, deliverable, actorUserId);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDeliverableDto(deliverable);
    }

    public async Task<ContractClauseDto?> AttachClauseAsync(
        Guid contractId,
        AttachContractClauseRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var contractExists = await dbContext.Contracts.AnyAsync(
            contract => contract.TenantId == tenantContext.TenantId && contract.Id == contractId,
            cancellationToken);
        var libraryClause = await dbContext.Clauses
            .AsNoTracking()
            .SingleOrDefaultAsync(
                clause =>
                    clause.Id == request.ClauseLibraryId &&
                    clause.ReviewState == ReviewState.Published &&
                    (clause.TenantId == null || clause.TenantId == tenantContext.TenantId),
                cancellationToken);

        if (!contractExists || libraryClause is null)
        {
            return null;
        }

        var duplicateExists = await dbContext.Set<ContractClauseEntity>().AnyAsync(
            clause =>
                clause.ContractId == contractId &&
                clause.ClauseLibraryId == request.ClauseLibraryId &&
                clause.RemovedAt == null,
            cancellationToken);

        if (duplicateExists)
        {
            throw new ContractValidationException(new Dictionary<string, string[]>
            {
                ["clauseLibraryId"] = ["This published clause is already attached to the contract."]
            });
        }

        var now = DateTimeOffset.UtcNow;
        var contractClause = new ContractClauseEntity
        {
            Id = Guid.NewGuid(),
            ContractId = contractId,
            ClauseLibraryId = libraryClause.Id,
            ClauseNumber = libraryClause.Number,
            Title = libraryClause.Title,
            FullText = libraryClause.PlainEnglishSummary,
            Source = ToClauseSource(libraryClause),
            SourceUrl = libraryClause.SourceUrl,
            SourceHash = libraryClause.SourceHash,
            AttachmentReason = request.AttachmentReason,
            SourceDocumentReference = request.SourceDocumentReference,
            RequiresFlowDown = libraryClause.UsuallyRequiresFlowDown,
            LastReviewedAt = libraryClause.LastReviewedAt,
            ReviewedByUserId = libraryClause.ReviewedByUserId,
            NextReviewDueAt = libraryClause.NextReviewDueAt,
            Confidence = libraryClause.Confidence,
            RequiresExpertReview = libraryClause.RequiresExpertReview,
            ReviewState = ReviewState.Published,
            CreatedAt = now,
            CreatedByUserId = actorUserId
        };

        dbContext.Set<ContractClauseEntity>().Add(contractClause);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToClauseDto(contractClause);
    }

    public async Task<ContractClauseDto?> RemoveClauseAsync(
        Guid contractId,
        Guid contractClauseId,
        RemoveContractClauseRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var contractClause = await dbContext.Set<ContractClauseEntity>()
            .Include(clause => clause.Contract)
            .SingleOrDefaultAsync(
                clause =>
                    clause.Id == contractClauseId &&
                    clause.ContractId == contractId &&
                    clause.RemovedAt == null &&
                    clause.Contract != null &&
                    clause.Contract.TenantId == tenantContext.TenantId,
                cancellationToken);

        if (contractClause is null)
        {
            return null;
        }

        var dto = ToClauseDto(contractClause);
        contractClause.RemovedAt = DateTimeOffset.UtcNow;
        contractClause.RemovedByUserId = actorUserId;
        contractClause.RemovalReason = request.Reason;
        contractClause.UpdatedAt = contractClause.RemovedAt;
        contractClause.UpdatedByUserId = actorUserId;
        await dbContext.SaveChangesAsync(cancellationToken);
        return dto;
    }

    public async Task<GeneratedContractObligationsDto?> GenerateObligationsForClauseAsync(
        Guid contractId,
        Guid contractClauseId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var contractClause = await dbContext.Set<ContractClauseEntity>()
            .Include(clause => clause.Contract)
            .SingleOrDefaultAsync(
                clause =>
                    clause.Id == contractClauseId &&
                    clause.ContractId == contractId &&
                    clause.RemovedAt == null &&
                    clause.Contract != null &&
                    clause.Contract.TenantId == tenantContext.TenantId,
                cancellationToken);

        if (contractClause is null)
        {
            return null;
        }

        var libraryClause = await dbContext.Clauses
            .AsNoTracking()
            .SingleOrDefaultAsync(
                clause =>
                    clause.Id == contractClause.ClauseLibraryId &&
                    clause.ReviewState == ReviewState.Published &&
                    (clause.TenantId == null || clause.TenantId == tenantContext.TenantId),
                cancellationToken);

        if (libraryClause is null)
        {
            return new GeneratedContractObligationsDto(contractClauseId, [], 0);
        }

        var mappedObligationIds = await dbContext.ClauseObligationMappings
            .AsNoTracking()
            .Where(mapping =>
                mapping.ClauseId == libraryClause.Id &&
                mapping.ReviewState == ReviewState.Published &&
                (mapping.TenantId == null || mapping.TenantId == tenantContext.TenantId))
            .Select(mapping => mapping.ObligationId)
            .Distinct()
            .ToArrayAsync(cancellationToken);
        IReadOnlyCollection<string> obligationIds = mappedObligationIds.Length > 0
            ? mappedObligationIds
            : ReadRequiredActionIds(libraryClause.RequiredActionIdsJson);
        if (obligationIds.Count == 0)
        {
            return new GeneratedContractObligationsDto(contractClauseId, [], 0);
        }

        var obligations = await dbContext.Obligations
            .Where(obligation => obligationIds.Contains(obligation.Id) && obligation.ReviewState == ReviewState.Published)
            .ToArrayAsync(cancellationToken);
        var mappedIds = new List<string>();
        var tasksCreated = 0;
        var now = DateTimeOffset.UtcNow;

        foreach (var obligation in obligations)
        {
            var mappingExists = await dbContext.Set<ContractClauseObligationEntity>().AnyAsync(
                mapping => mapping.ContractClauseId == contractClauseId && mapping.ObligationId == obligation.Id,
                cancellationToken);

            if (!mappingExists)
            {
                dbContext.Set<ContractClauseObligationEntity>().Add(new ContractClauseObligationEntity
                {
                    ContractClauseId = contractClauseId,
                    ObligationId = obligation.Id
                });
            }

            mappedIds.Add(obligation.Id);

            var taskExists = await dbContext.ComplianceTasks.AnyAsync(
                task =>
                    task.TenantId == tenantContext.TenantId &&
                    task.ContractId == contractId &&
                    task.ObligationId == obligation.Id &&
                    task.Type == ComplianceTaskType.ObligationAction,
                cancellationToken);

            if (taskExists)
            {
                continue;
            }

            dbContext.ComplianceTasks.Add(new ComplianceTaskEntity
            {
                Id = Guid.NewGuid(),
                TenantId = tenantContext.TenantId,
                ContractId = contractId,
                ObligationId = obligation.Id,
                Title = obligation.Title,
                Description = obligation.RequiredAction,
                Type = ComplianceTaskType.ObligationAction,
                Status = ComplianceTaskStatus.Open,
                RiskLevel = obligation.RiskLevel,
                OwnerFunction = obligation.OwnerFunction,
                CreatedAt = now,
                CreatedByUserId = actorUserId
            });
            tasksCreated++;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return new GeneratedContractObligationsDto(contractClauseId, mappedIds.Order().ToArray(), tasksCreated);
    }

    private static void Apply(ContractEntity entity, UpsertContractRequest request)
    {
        entity.ContractNumber = request.ContractNumber;
        entity.Title = request.Title;
        entity.AgencyOrPrimeName = request.AgencyOrPrimeName;
        entity.Relationship = request.Relationship;
        entity.Kind = request.Kind;
        entity.Status = request.Status;
        entity.AwardedAt = request.AwardedAt;
        entity.PeriodOfPerformanceStart = request.PeriodOfPerformanceStart;
        entity.PeriodOfPerformanceEnd = request.PeriodOfPerformanceEnd;
        entity.PlaceOfPerformance = request.PlaceOfPerformance;
        entity.Description = request.Description;
        entity.DataHandlingPosture = request.DataHandlingPosture;
    }

    private static void Apply(ContractDeliverableEntity entity, UpsertContractDeliverableRequest request)
    {
        entity.Name = request.Name;
        entity.Description = request.Description;
        entity.DueAt = request.DueAt;
        entity.OwnerFunction = request.OwnerFunction;
        entity.Status = request.Status;
    }

    private void SyncDeliverableTask(Guid contractId, ContractDeliverableEntity deliverable, Guid actorUserId)
    {
        if (deliverable.DueAt is null)
        {
            return;
        }

        var task = dbContext.ComplianceTasks.Local.FirstOrDefault(task => task.ContractId == contractId && task.Title == deliverable.Name) ??
            dbContext.ComplianceTasks.FirstOrDefault(task => task.ContractId == contractId && task.Title == deliverable.Name);
        var now = DateTimeOffset.UtcNow;

        if (task is null)
        {
            task = new ComplianceTaskEntity
            {
                Id = Guid.NewGuid(),
                TenantId = tenantContext.TenantId,
                ContractId = contractId,
                Type = ComplianceTaskType.CalendarReminder,
                RiskLevel = RiskLevel.Medium,
                CreatedAt = now,
                CreatedByUserId = actorUserId
            };
            dbContext.ComplianceTasks.Add(task);
        }
        else
        {
            task.UpdatedAt = now;
            task.UpdatedByUserId = actorUserId;
        }

        task.Title = deliverable.Name;
        task.Description = deliverable.Description;
        task.OwnerFunction = deliverable.OwnerFunction;
        task.DueAt = deliverable.DueAt;
        task.Status = IsComplete(deliverable.Status) ? ComplianceTaskStatus.Done : ComplianceTaskStatus.Open;
    }

    private static ContractDto ToDto(ContractEntity entity) =>
        new(
            entity.Id,
            entity.TenantId,
            entity.ContractNumber,
            entity.Title,
            entity.AgencyOrPrimeName,
            entity.Relationship,
            entity.Kind,
            entity.Status,
            entity.AwardedAt,
            entity.PeriodOfPerformanceStart,
            entity.PeriodOfPerformanceEnd,
            entity.PlaceOfPerformance,
            entity.Description,
            entity.DataHandlingPosture,
            entity.CreatedAt,
            entity.UpdatedAt);

    private static ContractDocumentDto ToDocumentDto(ContractDocumentEntity entity) =>
        new(
            entity.Id,
            entity.ContractId,
            entity.Type,
            entity.FileName,
            entity.ContentType,
            entity.SizeBytes,
            entity.StorageUri,
            entity.ExtractedTextHash,
            entity.ValidationStatus,
            entity.MalwareScanStatus,
            entity.NoticeVersion,
            entity.UploadedAt,
            entity.UploadedByUserId,
            entity.ContainsPotentialCui);

    private async Task<ClauseCandidateEntity?> FindCandidateForCurrentTenantAsync(
        Guid contractId,
        Guid documentId,
        Guid candidateId,
        CancellationToken cancellationToken) =>
        await dbContext.Set<ClauseCandidateEntity>()
            .Include(candidate => candidate.SourceDocument)
            .ThenInclude(document => document!.Contract)
            .SingleOrDefaultAsync(
                candidate =>
                    candidate.Id == candidateId &&
                    candidate.TenantId == tenantContext.TenantId &&
                    candidate.SourceDocumentId == documentId &&
                    candidate.SourceDocument != null &&
                    candidate.SourceDocument.ContractId == contractId &&
                    candidate.SourceDocument.Contract != null &&
                    candidate.SourceDocument.Contract.TenantId == tenantContext.TenantId,
                cancellationToken);

    private static ExtractionJobDto ToExtractionJobDto(ExtractionJobEntity entity) =>
        new(
            entity.Id,
            entity.TenantId,
            entity.SourceDocumentId,
            entity.RequestedByUserId,
            entity.Status,
            entity.RequestedAt,
            entity.StartedAt,
            entity.CompletedAt,
            entity.FailureReason);

    private static ClauseCandidateDto ToClauseCandidateDto(ClauseCandidateEntity entity) =>
        new(
            entity.Id,
            entity.TenantId,
            entity.ExtractionJobId,
            entity.SourceDocumentId,
            entity.NormalizedCitation,
            entity.RawExtractedText,
            entity.DetectedTitle,
            entity.Confidence,
            entity.LocationMetadata,
            entity.MatchMethod,
            entity.ClauseLibraryId,
            entity.ReviewStatus,
            entity.ReviewedByUserId,
            entity.ReviewedAt,
            entity.DecisionNote,
            entity.DecisionReason,
            entity.CreatedAt);

    private static void EnsureCandidateCanTransition(ClauseCandidateEntity candidate, string targetStatus)
    {
        var allowed = candidate.ReviewStatus is "pending_review" or "edited" or "needs_clarification";
        if (!allowed)
        {
            throw new ContractValidationException(new Dictionary<string, string[]>
            {
                ["reviewStatus"] = [$"Clause candidate cannot transition from '{candidate.ReviewStatus}' to '{targetStatus}'."]
            });
        }
    }

    private static ContractDeliverableDto ToDeliverableDto(ContractDeliverableEntity entity) =>
        new(
            entity.Id,
            entity.ContractId,
            entity.Name,
            entity.Description,
            entity.DueAt,
            entity.OwnerFunction,
            entity.Status,
            entity.DueAt < DateOnly.FromDateTime(DateTime.UtcNow) && !IsComplete(entity.Status));

    private static ContractClauseDto ToClauseDto(ContractClauseEntity entity) =>
        new(
            entity.Id,
            entity.ContractId,
            entity.ClauseLibraryId,
            entity.ClauseNumber,
            entity.Title,
            entity.Source,
            entity.SourceUrl,
            entity.LastReviewedAt,
            entity.AttachmentReason,
            entity.SourceDocumentReference,
            entity.CreatedAt,
            entity.CreatedByUserId ?? Guid.Empty);

    private static ClauseSource ToClauseSource(ClauseEntity clause)
    {
        var combined = $"{clause.Source} {clause.Number}";
        if (combined.Contains("DFARS", StringComparison.OrdinalIgnoreCase))
        {
            return ClauseSource.Dfars;
        }

        if (combined.Contains("FAR", StringComparison.OrdinalIgnoreCase) ||
            combined.StartsWith("52.", StringComparison.OrdinalIgnoreCase))
        {
            return ClauseSource.Far;
        }

        if (combined.Contains("CUSTOM", StringComparison.OrdinalIgnoreCase))
        {
            return ClauseSource.Local;
        }

        return ClauseSource.AgencySupplement;
    }

    private static IReadOnlyList<string> ReadRequiredActionIds(string value)
    {
        try
        {
            using var document = JsonDocument.Parse(value);
            return document.RootElement.ValueKind == JsonValueKind.Array
                ? document.RootElement
                    .EnumerateArray()
                    .Where(item => item.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(item.GetString()))
                    .Select(item => item.GetString()!)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray()
                : [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static bool IsComplete(DeliverableStatus status) =>
        status is DeliverableStatus.Submitted or DeliverableStatus.Accepted or DeliverableStatus.Waived;
}
