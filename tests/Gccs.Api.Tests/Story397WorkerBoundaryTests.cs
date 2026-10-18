using System.Reflection;
using Gccs.Api;
using Gccs.Api.Security;
using Gccs.Application.Ai;
using Gccs.Application.Common;
using Gccs.Application.Contracts;
using Gccs.Application.Identity;
using Gccs.Application.Marketing;
using Gccs.Application.Notifications;
using Gccs.Application.Portals;
using Gccs.Application.Reports;
using Gccs.Application.Storage;
using Gccs.Infrastructure.Identity;
using Gccs.Infrastructure.Marketing;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class Story397WorkerBoundaryTests
{
    public static IEnumerable<object[]> Workers => new[]
    {
        "ai", "assignment", "cleanup", "demo", "extraction", "invitation", "portal", "report", "reminder"
    }.SelectMany(name => new[] { new object[] { name, false }, new object[] { name, true } });

    [Theory, MemberData(nameof(Workers))]
    public async Task Actual_worker_dispatches_empty_or_faulted_work_and_stops_on_cancellation(string name, bool fault)
    {
        using var cancellation = new CancellationTokenSource();
        var dispatched = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var errors = 0;
        var calls = 0;
        object? Dispatch(MethodInfo method, object?[] args, object? emptyResult)
        {
            // BackgroundService supplies its linked stopping token, not the StartAsync token instance.
            Assert.True(Assert.IsType<CancellationToken>(args[^1]).CanBeCanceled);
            Interlocked.Increment(ref calls);
            dispatched.TrySetResult();
            if (fault) throw new InvalidOperationException("Synthetic worker dependency failure");
            return emptyResult;
        }
        var services = new ServiceCollection();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IApplicationTransaction, InlineTransaction>();
        services.AddSingleton(P<Gccs.Application.Audit.IAuditEventWriter>((m, a) => throw new InvalidOperationException("Empty work must not audit")));
        switch (name)
        {
            case "ai":
                services.AddScoped(_ => new AiOutputReviewService(null!, null!, new InlineTransaction(), TimeProvider.System,
                    P<IAiOutputRetentionRepository>((m, a) => Dispatch(m, a,
                        Task.FromResult<IReadOnlyList<AiOutputRetentionArchiveDto>>([])))));
                break;
            case "assignment":
                services.AddScoped(_ => new AssignmentEmailDeliveryService(
                    P<IAssignmentEmailDeliveryRepository>((m, a) => m.Name == "TryClaimNextAsync"
                        ? Dispatch(m, a, Task.FromResult<ClaimedAssignmentEmailDelivery?>(null)) : Unexpected(m)),
                    P<IAssignmentEmailSender>((m, a) => m.Name == "get_IsConfigured" ? true : Unexpected(m)),
                    new("https://synthetic.example.invalid", TimeSpan.FromMinutes(1), 3)));
                break;
            case "cleanup":
                services.AddScoped(_ => P<IObjectCleanupQueue>((m, a) => m.Name == "ProcessNextAsync"
                    ? Dispatch(m, a, Task.FromResult(false)) : Unexpected(m)));
                break;
            case "demo":
                services.AddScoped(_ => P<IDemoRequestRepository>((m, a) => m.Name switch
                {
                    // The retention call is a strict no-op fake; no records or files are removed.
                    "DeleteExpiredAsync" => Task.FromResult(0),
                    "TryClaimNextDeliveryAsync" => Dispatch(m, a, NullTask(m)),
                    _ => Unexpected(m)
                }));
                services.AddScoped(p => new DemoRequestDeliveryService(p.GetRequiredService<IDemoRequestRepository>(),
                    P<IDemoRequestDeliveryTransport>((m, a) => m.Name == "get_IsConfigured" ? true : Unexpected(m)),
                    new(TimeSpan.FromMinutes(1), 3), TimeProvider.System));
                break;
            case "extraction":
                services.AddScoped(_ => P<IExtractionJobWorkRepository>((m, a) =>
                    Dispatch(m, a, Task.FromResult<ClaimedExtractionJob?>(null))));
                break;
            case "invitation":
                services.AddScoped(_ => new InvitationDeliveryService(
                    P<IInvitationDeliveryRepository>((m, a) => m.Name == "TryClaimNextAsync"
                        ? Dispatch(m, a, Task.FromResult<ClaimedInvitationDelivery?>(null)) : Unexpected(m)),
                    P<IInvitationEmailSender>((m, a) => m.Name == "get_IsConfigured" ? true : Unexpected(m)),
                    new("https://synthetic.example.invalid", TimeSpan.FromMinutes(1), 3)));
                break;
            case "portal":
                services.AddScoped(_ => new PortalPackageLifecycleService(
                    P<IPortalPackageLifecycleRepository>((m, a) => m.Name == "ListDueForMaintenanceAsync"
                        ? Dispatch(m, a, Task.FromResult<IReadOnlyList<SharedPortalPackageDto>>([])) : Unexpected(m)),
                    null!, null!, new InlineTransaction(), TimeProvider.System));
                break;
            case "report":
                services.AddScoped(_ => P<IReportExportRepository>((m, a) => m.Name == "TryClaimNextAsync"
                    ? Dispatch(m, a, Task.FromResult<ClaimedReportExport?>(null)) : Unexpected(m)));
                break;
            case "reminder":
                services.AddScoped(_ => P<IDueDateReminderRepository>((m, a) => m.Name == "RunAutomatedAsync"
                    ? Dispatch(m, a, Task.FromResult(0)) : Unexpected(m)));
                break;
        }
        using var provider = services.BuildServiceProvider();
        var scopes = provider.GetRequiredService<IServiceScopeFactory>();
        ILogger<T> Logger<T>() => new ErrorCounter<T>(() => Interlocked.Increment(ref errors));
        using BackgroundService worker = name switch
        {
            "ai" => new AiOutputRetentionWorker(scopes, Logger<AiOutputRetentionWorker>()),
            "assignment" => new AssignmentEmailDeliveryWorker(scopes, Options.Create(new InvitationEmailOptions { Enabled = true }), Logger<AssignmentEmailDeliveryWorker>()),
            "cleanup" => new ObjectCleanupWorker(scopes, Logger<ObjectCleanupWorker>()),
            "demo" => new DemoRequestDeliveryWorker(scopes, Options.Create(new DemoRequestOptions { Enabled = true }), Logger<DemoRequestDeliveryWorker>()),
            "extraction" => new ExtractionJobWorker(scopes, Options.Create(new ExtractionProcessingOptions()), Logger<ExtractionJobWorker>()),
            "invitation" => new InvitationDeliveryWorker(scopes, Options.Create(new InvitationEmailOptions { Enabled = true }), Logger<InvitationDeliveryWorker>()),
            "portal" => new PortalPackageLifecycleWorker(scopes, TimeProvider.System, Options.Create(new PortalPackageLifecycleProcessingOptions()), Logger<PortalPackageLifecycleWorker>()),
            "report" => new ReportExportWorker(scopes, Options.Create(new ReportExportProcessingOptions()), Logger<ReportExportWorker>()),
            _ => new DueDateReminderWorker(scopes, Options.Create(new DueDateReminderProcessingOptions { PollIntervalMinutes = 1 }), Logger<DueDateReminderWorker>())
        };
        await worker.StartAsync(cancellation.Token);
        await dispatched.Task.WaitAsync(TimeSpan.FromSeconds(75));
        if (fault)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            while (Volatile.Read(ref errors) == 0) await Task.Delay(10, timeout.Token);
        }
        cancellation.Cancel();
        await worker.StopAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(calls > 0);
        Assert.Equal(fault, errors > 0);
        Assert.True(worker.ExecuteTask!.IsCompleted);
    }

    [Theory]
    [InlineData(false, 1)]
    [InlineData(true, 1)]
    [InlineData(false, 2)]
    [InlineData(true, 2)]
    public async Task Claimed_extraction_or_report_initializes_processing_and_failure_scopes_from_the_claim(bool report, int iterations)
    {
        var tenants = Enumerable.Range(0, iterations).Select(_ => Guid.NewGuid()).ToArray();
        var user = Guid.NewGuid(); var claims = 0;
        var processingTenants = new List<Guid>(); var failureTenants = new List<Guid>();
        using var cancellation = new CancellationTokenSource();
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var processScopes = 0; var failureScopes = 0;
        var services = new ServiceCollection();
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.AddScoped<HttpTenantContext>();
        void Check(HttpTenantContext context)
        {
            Assert.Contains(context.TenantId, tenants); Assert.Equal(user, context.UserId);
            Assert.Equal("synthetic@example.invalid", context.UserEmail);
        }
        services.AddScoped(_ => P<IExtractionJobWorkRepository>((m, a) => Task.FromResult<ClaimedExtractionJob?>(
            new(Guid.NewGuid(), tenants[Interlocked.Increment(ref claims) - 1], user, "synthetic@example.invalid", Guid.NewGuid(), 1))));
        services.AddScoped(p => new ContractService(
            P<IContractRepository>((m, a) =>
            {
                Check(p.GetRequiredService<HttpTenantContext>());
                if (m.Name == "FindExtractionJobInputAsync") { processScopes++; processingTenants.Add(p.GetRequiredService<HttpTenantContext>().TenantId); throw new InvalidOperationException("Synthetic processing failure"); }
                if (m.Name == "MarkExtractionJobFailedAsync") { failureScopes++; failureTenants.Add(p.GetRequiredService<HttpTenantContext>().TenantId); if (failureScopes == iterations) completed.TrySetResult(); return NullTask(m); }
                return Unexpected(m);
            }), null!, null!, null!, null!, null!, new InlineTransaction(), p.GetRequiredService<HttpTenantContext>()));
        services.AddScoped(p => P<IReportExportRepository>((m, a) =>
        {
            if (m.Name == "TryClaimNextAsync") return Task.FromResult<ClaimedReportExport?>(
                new(Guid.NewGuid(), tenants[Interlocked.Increment(ref claims) - 1], Guid.NewGuid(), user, "synthetic@example.invalid", Guid.NewGuid(), 1));
            Check(p.GetRequiredService<HttpTenantContext>());
            if (m.Name == "RecordProcessingFailureAsync") { failureScopes++; failureTenants.Add(p.GetRequiredService<HttpTenantContext>().TenantId); if (failureScopes == iterations) completed.TrySetResult(); return NullTask(m); }
            return Unexpected(m);
        }));
        services.AddScoped(p => new ReportExportService(p.GetRequiredService<IReportExportRepository>(),
            P<IReportRepository>((m, a) =>
            {
                Check(p.GetRequiredService<HttpTenantContext>()); processScopes++; processingTenants.Add(p.GetRequiredService<HttpTenantContext>().TenantId);
                throw new InvalidOperationException("Synthetic rendering input failure");
            }), null!, null!, null!, new InlineTransaction(), null!, null!));
        using var provider = services.BuildServiceProvider();
        var scopes = provider.GetRequiredService<IServiceScopeFactory>();
        using BackgroundService worker = report
            ? new ReportExportWorker(scopes, Options.Create(new ReportExportProcessingOptions()), new ErrorCounter<ReportExportWorker>(() => { }))
            : new ExtractionJobWorker(scopes, Options.Create(new ExtractionProcessingOptions()), new ErrorCounter<ExtractionJobWorker>(() => { }));
        await worker.StartAsync(cancellation.Token);
        await completed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        cancellation.Cancel();
        await worker.StopAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(iterations, processScopes); Assert.Equal(iterations, failureScopes);
        Assert.Equal(tenants, processingTenants); Assert.Equal(tenants, failureTenants);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Cancellation_during_claim_is_graceful_and_does_not_dispatch_processing_or_failure(bool report)
    {
        using var cancellation = new CancellationTokenSource();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken observed = default;
        async Task<T?> Block<T>(CancellationToken token) where T : class
        {
            observed = token;
            entered.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
            return null;
        }
        var services = new ServiceCollection();
        services.AddScoped(_ => P<IExtractionJobWorkRepository>((m, a) =>
            Block<ClaimedExtractionJob>(Assert.IsType<CancellationToken>(a[^1]))));
        services.AddScoped(_ => P<IReportExportRepository>((m, a) => m.Name == "TryClaimNextAsync"
            ? Block<ClaimedReportExport>(Assert.IsType<CancellationToken>(a[^1])) : Unexpected(m)));
        // Processing services deliberately are absent: reaching them would log a failure and fail this test.
        using var provider = services.BuildServiceProvider();
        var errors = 0;
        using BackgroundService worker = report
            ? new ReportExportWorker(provider.GetRequiredService<IServiceScopeFactory>(), Options.Create(new ReportExportProcessingOptions()), new ErrorCounter<ReportExportWorker>(() => errors++))
            : new ExtractionJobWorker(provider.GetRequiredService<IServiceScopeFactory>(), Options.Create(new ExtractionProcessingOptions()), new ErrorCounter<ExtractionJobWorker>(() => errors++));
        await worker.StartAsync(cancellation.Token);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        cancellation.Cancel();
        await worker.StopAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(observed.IsCancellationRequested);
        Assert.True(worker.ExecuteTask!.IsCompletedSuccessfully);
        Assert.Equal(0, errors);
    }

    private static object Unexpected(MethodInfo method) => throw new InvalidOperationException($"Unexpected dependency call: {method.Name}");
    private static object NullTask(MethodInfo method) => typeof(Task).GetMethod(nameof(Task.FromResult))!
        .MakeGenericMethod(method.ReturnType.GetGenericArguments()[0]).Invoke(null, [null])!;
    private static T P<T>(Func<MethodInfo, object?[], object?> handler) where T : class
    {
        var result = DispatchProxy.Create<T, StrictProxy>();
        ((StrictProxy)(object)result).Handler = handler;
        return result;
    }
    public class StrictProxy : DispatchProxy
    {
        public Func<MethodInfo, object?[], object?> Handler { get; set; } = null!;
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => Handler(targetMethod!, args ?? []);
    }
    private sealed class InlineTransaction : IApplicationTransaction
    {
        public Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default) => operation(cancellationToken);
    }
    private sealed class ErrorCounter<T>(Action error) : ILogger<T>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel level, EventId id, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        { if (level >= LogLevel.Error) error(); }
    }
}
