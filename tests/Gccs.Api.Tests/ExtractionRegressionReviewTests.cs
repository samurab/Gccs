using System.Text.Json;
using Gccs.Application.Audit;
using Gccs.Application.Compliance;
using Gccs.Application.Notifications;
using Gccs.Application.Tasks;
using Gccs.Domain.Audit;
using Gccs.Domain.Compliance;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class ExtractionRegressionReviewTests
{
    private static readonly HashSet<string> AllowedClassifications = new(StringComparer.OrdinalIgnoreCase)
    {
        "parser",
        "matcher",
        "library",
        "label",
        "source_quality",
        "expected_limitation"
    };

    private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "open",
        "in_progress",
        "resolved",
        "accepted_risk"
    };

    private static readonly HashSet<string> UpdateLinkTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "matcher",
        "library",
        "parser",
        "label"
    };

    [Fact]
    public void TC_28_3_1_Reviewed_failures_have_classification_owner_status_and_resolution_note()
    {
        using var records = LoadReviewRecords();

        foreach (var record in records.RootElement.GetProperty("records").EnumerateArray())
        {
            Assert.Contains(record.GetProperty("classification").GetString() ?? string.Empty, AllowedClassifications);
            Assert.False(string.IsNullOrWhiteSpace(record.GetProperty("owner").GetString()));
            Assert.Contains(record.GetProperty("status").GetString() ?? string.Empty, AllowedStatuses);
            Assert.False(string.IsNullOrWhiteSpace(record.GetProperty("resolutionNote").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(record.GetProperty("failureType").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(record.GetProperty("citation").GetString()));
        }
    }

    [Fact]
    public void TC_28_3_2_Follow_up_tasks_can_be_created_from_failures()
    {
        using var records = LoadReviewRecords();

        foreach (var record in records.RootElement.GetProperty("records").EnumerateArray())
        {
            var task = record.GetProperty("followUpTask");
            Assert.False(string.IsNullOrWhiteSpace(task.GetProperty("taskId").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(task.GetProperty("title").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(task.GetProperty("status").GetString()));
        }
    }

    [Fact]
    public void TC_28_3_3_Resolved_failures_link_to_applicable_updates()
    {
        using var records = LoadReviewRecords();
        var resolved = records.RootElement.GetProperty("records")
            .EnumerateArray()
            .Where(record => string.Equals(record.GetProperty("status").GetString(), "resolved", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        Assert.NotEmpty(resolved);
        foreach (var record in resolved)
        {
            var links = record.GetProperty("resolutionLinks").EnumerateArray().ToArray();
            Assert.NotEmpty(links);
            Assert.Contains(links, link => UpdateLinkTypes.Contains(link.GetProperty("type").GetString() ?? string.Empty));
            Assert.All(links, link => Assert.False(string.IsNullOrWhiteSpace(link.GetProperty("reference").GetString())));
        }
    }

    [Fact]
    public void TC_28_3_4_Release_summary_shows_open_risks_and_metric_trends()
    {
        var root = FindReviewRoot();
        using var summary = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "release-summary.json")));
        var markdown = File.ReadAllText(Path.Combine(root, "release-summary.md"));

        Assert.NotEmpty(summary.RootElement.GetProperty("metricTrends").EnumerateArray());
        Assert.NotEmpty(summary.RootElement.GetProperty("openRisks").EnumerateArray());
        Assert.False(string.IsNullOrWhiteSpace(summary.RootElement.GetProperty("releaseReadinessNote").GetString()));
        Assert.Contains("Metric Trends", markdown, StringComparison.Ordinal);
        Assert.Contains("Open Risks", markdown, StringComparison.Ordinal);
    }

    [Fact]
    public void TC_28_3_5_Regression_review_records_are_traceable()
    {
        using var records = LoadReviewRecords();

        foreach (var record in records.RootElement.GetProperty("records").EnumerateArray())
        {
            var auditTrail = record.GetProperty("auditTrail").EnumerateArray().ToArray();
            Assert.NotEmpty(auditTrail);
            Assert.Contains(auditTrail, audit => audit.GetProperty("action").GetString() == "created");
            Assert.All(auditTrail, audit =>
            {
                Assert.False(string.IsNullOrWhiteSpace(audit.GetProperty("actor").GetString()));
                Assert.True(DateTimeOffset.TryParse(audit.GetProperty("at").GetString(), out _));
            });
        }
    }

    [Fact]
    public async Task Service_reviews_missed_clause_and_false_positive_classifications_and_creates_follow_up_tasks()
    {
        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var repository = new InMemoryExtractionRegressionReviewRepository();
        var taskRepository = new CapturingComplianceTaskRepository(tenantId);
        var auditWriter = new CapturingAuditEventWriter();
        var service = CreateService(repository, taskRepository, auditWriter);

        foreach (var classification in Enum.GetValues<ExtractionFailureClassification>())
        {
            var record = await service.ReviewFailureAsync(
                new ReviewExtractionFailureRequest(
                    "run-2026-06-18",
                    $"doc-{classification}",
                    classification is ExtractionFailureClassification.Label
                        ? ExtractionRegressionFailureType.FalsePositive
                        : ExtractionRegressionFailureType.MissedClause,
                    "FAR 52.204-21",
                    classification,
                    "Compliance Content Owner",
                    ExtractionRegressionReviewStatus.InProgress,
                    "Needs deliberate review before release.",
                    true,
                    $"Review {classification} extraction failure",
                    RiskLevel.High,
                    new DateOnly(2026, 6, 25)),
                tenantId,
                actorUserId);

            Assert.Equal(classification, record.Classification);
            Assert.Equal(tenantId, record.TenantId);
            Assert.NotNull(record.FollowUpTaskId);
        }

        Assert.Equal(Enum.GetValues<ExtractionFailureClassification>().Length, taskRepository.Tasks.Count);
        Assert.All(taskRepository.Tasks, task =>
        {
            Assert.Equal("extraction_regression_review", task.LinkedEntityType);
            Assert.False(string.IsNullOrWhiteSpace(task.LinkedEntityId));
        });
        Assert.Contains(auditWriter.Events, audit =>
            audit.EntityType == "ExtractionRegressionReview" &&
            audit.Action == AuditAction.Created &&
            audit.Metadata["classification"] == ExtractionFailureClassification.Matcher.ToString());
    }

    [Fact]
    public async Task Service_requires_update_links_when_resolving_matcher_library_parser_or_label_failures()
    {
        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var repository = new InMemoryExtractionRegressionReviewRepository();
        var auditWriter = new CapturingAuditEventWriter();
        var service = CreateService(repository, new CapturingComplianceTaskRepository(tenantId), auditWriter);
        var record = await service.ReviewFailureAsync(
            CreateRequest(ExtractionFailureClassification.Matcher, createTask: false),
            tenantId,
            actorUserId);

        await Assert.ThrowsAsync<ExtractionRegressionReviewValidationException>(() =>
            service.ResolveAsync(
                record.Id,
                new ResolveExtractionRegressionReviewRequest(
                    ExtractionFailureClassification.Matcher,
                    ExtractionRegressionReviewStatus.Resolved,
                    "Resolved without a link should be rejected.",
                    []),
                actorUserId));

        var resolved = await service.ResolveAsync(
            record.Id,
            new ResolveExtractionRegressionReviewRequest(
                ExtractionFailureClassification.Matcher,
                ExtractionRegressionReviewStatus.Resolved,
                "Matcher pattern updated and verified against the corpus.",
                [new ExtractionRegressionResolutionLinkDto(ExtractionRegressionUpdateLinkType.Matcher, "tools/extraction-evaluation/evaluate_corpus.py")]),
            actorUserId);

        Assert.NotNull(resolved);
        Assert.Equal(ExtractionRegressionReviewStatus.Resolved, resolved.Status);
        Assert.Single(resolved.ResolutionLinks);
        Assert.Contains(auditWriter.Events, audit =>
            audit.EntityType == "ExtractionRegressionReview" &&
            audit.Action == AuditAction.Updated &&
            audit.Metadata["resolutionLinkCount"] == "1");
    }

    [Fact]
    public async Task Service_generates_release_summary_with_metric_trends_and_current_tenant_open_risks()
    {
        var tenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var repository = new InMemoryExtractionRegressionReviewRepository();
        var auditWriter = new CapturingAuditEventWriter();
        var service = CreateService(repository, new CapturingComplianceTaskRepository(tenantId), auditWriter);
        await service.ReviewFailureAsync(CreateRequest(ExtractionFailureClassification.Library, createTask: false), tenantId, actorUserId);
        await service.ReviewFailureAsync(CreateRequest(ExtractionFailureClassification.Parser, createTask: false), otherTenantId, actorUserId);

        var summary = await service.GenerateReleaseSummaryAsync(
            new GenerateExtractionRegressionReleaseSummaryRequest(
                [
                    new ExtractionRegressionMetricTrendDto("run-1", 0.90m, 0.75m, 1, 2),
                    new ExtractionRegressionMetricTrendDto("run-2", 0.98m, 0.92m, 0, 1)
                ],
                0.95m,
                0.95m),
            tenantId,
            actorUserId);

        Assert.Equal(2, summary.MetricTrends.Count);
        Assert.Single(summary.OpenRisks);
        Assert.Equal(tenantId, summary.TenantId);
        Assert.Contains("open risks", summary.ReleaseReadinessNote, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(auditWriter.Events, audit =>
            audit.EntityType == "ExtractionRegressionReleaseSummary" &&
            audit.Metadata["openRiskCount"] == "1" &&
            audit.Metadata["metricTrendCount"] == "2");
    }

    [Fact]
    public async Task Service_rejects_review_records_without_traceability_fields()
    {
        var service = CreateService(
            new InMemoryExtractionRegressionReviewRepository(),
            new CapturingComplianceTaskRepository(Guid.NewGuid()),
            new CapturingAuditEventWriter());

        await Assert.ThrowsAsync<ExtractionRegressionReviewValidationException>(() =>
            service.ReviewFailureAsync(
                CreateRequest(ExtractionFailureClassification.SourceQuality, createTask: false) with
                {
                    Owner = " ",
                    ResolutionNote = " "
                },
                Guid.NewGuid(),
                Guid.NewGuid()));
    }

    private static JsonDocument LoadReviewRecords() =>
        JsonDocument.Parse(File.ReadAllText(Path.Combine(FindReviewRoot(), "review-records.json")));

    private static ExtractionRegressionReviewService CreateService(
        IExtractionRegressionReviewRepository repository,
        IComplianceTaskRepository taskRepository,
        IAuditEventWriter auditWriter) =>
        new(
            repository,
            new ComplianceTaskService(taskRepository, auditWriter, Enumerable.Empty<IAssignmentNotificationRepository>()),
            auditWriter);

    private static ReviewExtractionFailureRequest CreateRequest(ExtractionFailureClassification classification, bool createTask) =>
        new(
            "run-2026-06-18",
            "synthetic-dod-safeguarding-001",
            ExtractionRegressionFailureType.MissedClause,
            "FAR 52.204-21",
            classification,
            "Compliance Content Owner",
            ExtractionRegressionReviewStatus.InProgress,
            "Failure is under review with release risk noted.",
            createTask,
            null,
            RiskLevel.High,
            null);

    private static string FindReviewRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "tests", "fixtures", "extraction-regression-review");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate tests/fixtures/extraction-regression-review.");
    }

    private sealed class InMemoryExtractionRegressionReviewRepository : IExtractionRegressionReviewRepository
    {
        private readonly List<ExtractionRegressionReviewRecordDto> _records = [];

        public Task<ExtractionRegressionReviewRecordDto> CreateAsync(
            ReviewExtractionFailureRequest request,
            Guid tenantId,
            Guid actorUserId,
            CancellationToken cancellationToken = default)
        {
            var record = new ExtractionRegressionReviewRecordDto(
                Guid.NewGuid(),
                tenantId,
                request.EvaluationRunId,
                request.DocumentId,
                request.FailureType,
                request.Citation,
                request.Classification,
                request.Owner,
                request.Status,
                request.ResolutionNote,
                null,
                [],
                actorUserId,
                DateTimeOffset.UtcNow,
                null,
                null);
            _records.Add(record);
            return Task.FromResult(record);
        }

        public Task<ExtractionRegressionReviewRecordDto?> LinkFollowUpTaskAsync(
            Guid reviewRecordId,
            Guid taskId,
            Guid actorUserId,
            CancellationToken cancellationToken = default)
        {
            var index = _records.FindIndex(record => record.Id == reviewRecordId);
            if (index < 0)
            {
                return Task.FromResult<ExtractionRegressionReviewRecordDto?>(null);
            }

            _records[index] = _records[index] with
            {
                FollowUpTaskId = taskId,
                UpdatedByUserId = actorUserId,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            return Task.FromResult<ExtractionRegressionReviewRecordDto?>(_records[index]);
        }

        public Task<ExtractionRegressionReviewRecordDto?> ResolveAsync(
            Guid reviewRecordId,
            ResolveExtractionRegressionReviewRequest request,
            Guid actorUserId,
            CancellationToken cancellationToken = default)
        {
            var index = _records.FindIndex(record => record.Id == reviewRecordId);
            if (index < 0)
            {
                return Task.FromResult<ExtractionRegressionReviewRecordDto?>(null);
            }

            _records[index] = _records[index] with
            {
                Classification = request.Classification,
                Status = request.Status,
                ResolutionNote = request.ResolutionNote,
                ResolutionLinks = request.ResolutionLinks,
                UpdatedByUserId = actorUserId,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            return Task.FromResult<ExtractionRegressionReviewRecordDto?>(_records[index]);
        }

        public Task<IReadOnlyList<ExtractionRegressionReviewRecordDto>> ListOpenRisksAsync(
            Guid tenantId,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<ExtractionRegressionReviewRecordDto> records = _records
                .Where(record => record.TenantId == tenantId &&
                    record.Status is ExtractionRegressionReviewStatus.Open or ExtractionRegressionReviewStatus.InProgress)
                .ToArray();
            return Task.FromResult(records);
        }
    }

    private sealed class CapturingComplianceTaskRepository(Guid tenantId) : IComplianceTaskRepository
    {
        public List<ComplianceTaskDto> Tasks { get; } = [];

        public Task<IReadOnlyList<ComplianceTaskDto>> ListCurrentTenantAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ComplianceTaskDto>>(Tasks);

        public Task<ComplianceTaskDto?> CreateAsync(
            CreateComplianceTaskRequest request,
            ComplianceTaskStatus status,
            Guid actorUserId,
            CancellationToken cancellationToken = default)
        {
            var task = new ComplianceTaskDto(
                Guid.NewGuid(),
                tenantId,
                request.Title,
                request.Description,
                ComplianceTaskType.CorrectiveAction,
                status.ToString().Replace("InProgress", "in_progress", StringComparison.Ordinal).ToLowerInvariant(),
                request.Priority,
                request.AssignedToUserId,
                request.OwnerFunction,
                request.DueAt,
                request.LinkedEntityType,
                request.LinkedEntityId,
                DateTimeOffset.UtcNow,
                null);
            Tasks.Add(task);
            return Task.FromResult<ComplianceTaskDto?>(task);
        }

        public Task<ComplianceTaskDto?> UpdateAsync(
            Guid taskId,
            UpdateComplianceTaskRequest request,
            ComplianceTaskStatus? status,
            Guid actorUserId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<ComplianceTaskDto?>(null);
    }

    private sealed class CapturingAuditEventWriter : IAuditEventWriter
    {
        public List<CapturedAuditEvent> Events { get; } = [];

        public Task WriteAsync(
            Guid tenantId,
            Guid actorUserId,
            AuditAction action,
            string entityType,
            string entityId,
            string summary,
            IReadOnlyDictionary<string, string>? metadata = null,
            CancellationToken cancellationToken = default)
        {
            Events.Add(new CapturedAuditEvent(tenantId, actorUserId, action, entityType, entityId, summary, metadata ?? new Dictionary<string, string>()));
            return Task.CompletedTask;
        }
    }

    private sealed record CapturedAuditEvent(
        Guid TenantId,
        Guid ActorUserId,
        AuditAction Action,
        string EntityType,
        string EntityId,
        string Summary,
        IReadOnlyDictionary<string, string> Metadata);
}
