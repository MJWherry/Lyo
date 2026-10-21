using System.Text.Json;
using Lyo.Common.Core.Identifiers;
using Lyo.Exceptions;
using Lyo.Web.Automation.Abstractions;
using Lyo.Web.Automation.Models;
using Lyo.Web.Automation.Plan;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lyo.Web.Automation.Api;

/// <summary>
/// Accepts plan runs up to <see cref="AutomationApiOptions.MaxConcurrentRuns" />, executes them on a background task, and keeps copied logs and snapshots until
/// <see cref="AutomationApiOptions.RunTtl" />. Disposing the host cancels active runs.
/// </summary>
public sealed class AutomationRunRegistry : IHostedService, IDisposable
{
    private readonly object _gate = new();
    private readonly ILogger<AutomationRunRegistry> _logger;
    private readonly AutomationApiOptions _options;
    private readonly Dictionary<Guid, RunSlot> _runs = [];
    private readonly IServiceScopeFactory _scopes;
    private readonly CancellationTokenSource _sweepCts = new();
    private int _active;
    private int _disposed;
    private Task? _sweep;

    /// <summary>Creates the registry. <paramref name="options" /> is validated by <c>AddAutomationApi</c>.</summary>
    public AutomationRunRegistry(IServiceScopeFactory scopes, AutomationApiOptions options, ILogger<AutomationRunRegistry> logger)
    {
        ArgumentHelpers.ThrowIfNull(scopes);
        ArgumentHelpers.ThrowIfNull(options);
        ArgumentHelpers.ThrowIfNull(logger);
        _scopes = scopes;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        _sweepCts.Cancel();
        _sweepCts.Dispose();
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_options.ArtifactRoot);
        _sweep = Task.Run(() => SweepAsync(_sweepCts.Token));
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _sweepCts.CancelAsync().ConfigureAwait(false);
        RunSlot[] slots;
        lock (_gate)
            slots = _runs.Values.ToArray();

        foreach (var slot in slots)
            slot.Cancel.Cancel();

        var pending = slots.Select(slot => slot.Work).OfType<Task>().ToArray();
        if (pending.Length == 0)
            return;

        try {
            await Task.WhenAll(pending).WaitAsync(TimeSpan.FromSeconds(5), cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is OperationCanceledException or TimeoutException or AggregateException) {
            _logger.LogDebug(ex, "Automation runs stopped with the host.");
        }
    }

    /// <summary>Starts <paramref name="request" /> when a slot is free. Returns null when <see cref="AutomationApiOptions.MaxConcurrentRuns" /> is already in use.</summary>
    public AutomationRunAccepted? TryStart(AutomationRunReq request)
    {
        ArgumentHelpers.ThrowIfNull(request);
        ArgumentHelpers.ThrowIfNull(request.Plan);
        lock (_gate) {
            if (_active >= _options.MaxConcurrentRuns)
                return null;

            _active++;
            var id = LyoGuid.CreateV7();
            var slot = new RunSlot(id);
            _runs[id] = slot;
            var plan = request.Plan;
            var kind = request.BrowserKind ?? AutomationBrowserKind.Chromium;
            var headless = request.Headless ?? true;
            slot.Work = Task.Run(() => ExecuteAsync(slot, plan, kind, headless));
            return new() { RunId = id, Status = AutomationRunStatus.Queued };
        }
    }

    /// <summary>The run, or null when the id is unknown or its TTL has elapsed.</summary>
    public AutomationRunRes? Get(Guid runId)
    {
        lock (_gate) {
            if (!_runs.TryGetValue(runId, out var slot) || IsExpired(slot))
                return null;

            return slot.ToResponse();
        }
    }

    /// <summary>Cancels the run token. Returns false when the id is unknown.</summary>
    public bool Cancel(Guid runId)
    {
        lock (_gate) {
            if (!_runs.TryGetValue(runId, out var slot) || IsExpired(slot))
                return false;

            slot.Cancel.Cancel();
            return true;
        }
    }

    /// <summary>Absolute path of the copied transcript, or null when the run or the file is missing.</summary>
    public string? LogPath(Guid runId) => ArtifactFile(runId, "logs", "run.log");

    /// <summary>PNG file names copied for the run. Empty when the run exists and captured nothing. Null when the run is unknown.</summary>
    public IReadOnlyList<string>? ListSnapshots(Guid runId)
    {
        var dir = ArtifactDirectory(runId, "snapshots");
        if (dir is null)
            return null;

        if (!Directory.Exists(dir))
            return [];

        return Directory.GetFiles(dir, "*.png").Select(Path.GetFileName).Where(name => name != null).Cast<string>().OrderBy(name => name, StringComparer.Ordinal).ToArray();
    }

    /// <summary>Storage id of the PNG for a finished step, or null when the run or the step has none.</summary>
    public string? ScreenshotFileId(Guid runId, int index)
    {
        lock (_gate) {
            if (!_runs.TryGetValue(runId, out var slot) || IsExpired(slot))
                return null;

            return slot.ScreenshotFileId(index);
        }
    }

    /// <summary>Absolute path of one copied PNG, or null when the name is safe but the file or run is missing.</summary>
    public string? SnapshotPath(Guid runId, string name)
    {
        if (!AutomationSnapshotNames.IsSingleFileName(name))
            return null;

        return ArtifactFile(runId, "snapshots", name);
    }

    private string? ArtifactFile(Guid runId, string folder, string name)
    {
        var dir = ArtifactDirectory(runId, folder);
        if (dir is null)
            return null;

        var path = Path.Combine(dir, name);
        return File.Exists(path) ? path : null;
    }

    private string? ArtifactDirectory(Guid runId, string folder)
    {
        lock (_gate) {
            if (!_runs.TryGetValue(runId, out var slot) || IsExpired(slot))
                return null;

            return Path.Combine(RunRoot(runId), folder);
        }
    }

    private bool IsExpired(RunSlot slot) => slot.FinishedUtc is { } finished && DateTime.UtcNow - finished > _options.RunTtl;

    private string RunRoot(Guid runId) => Path.Combine(_options.ArtifactRoot, runId.ToString("N"));

    private async Task ExecuteAsync(RunSlot slot, AutomationPlan plan, AutomationBrowserKind kind, bool headless)
    {
        IWebAutomationSession? session = null;
        Exception? error = null;
        var cancelled = false;
        try {
            slot.Status = AutomationRunStatus.Running;
            await using var scope = _scopes.CreateAsyncScope();
            var factory = scope.ServiceProvider.GetRequiredService<IAutomationBrowserSessionFactory>();
            var runner = scope.ServiceProvider.GetRequiredService<IAutomationPlanRunner>();
            session = factory.CreateSession(kind, headless);
            var runtime = new AutomationPlanRuntimeOptions { Instrumentation = new SlotInstrumentation(slot) };
            var result = await runner.RunWithResultAsync(session, plan, runtime, _logger, slot.Cancel.Token).ConfigureAwait(false);
            slot.Strings = result.Snapshot.Strings;
            slot.StringLists = result.Snapshot.StringLists;
        }
        catch (OperationCanceledException) {
            cancelled = true;
        }
        catch (Exception ex) {
            _logger.LogWarning(ex, "Automation run {RunId} failed.", slot.RunId);
            error = ex;
        }
        finally {
            try {
                CopyArtifacts(session?.SessionDirectory, slot.RunId);
            }
            catch (Exception ex) {
                _logger.LogWarning(ex, "Automation run {RunId} artifacts were not copied.", slot.RunId);
            }

            if (session != null) {
                try {
                    await session.DisposeAsync().ConfigureAwait(false);
                }
                catch (Exception ex) {
                    _logger.LogDebug(ex, "Automation session dispose failed for {RunId}.", slot.RunId);
                }
            }
        }

        if (cancelled)
            slot.Status = AutomationRunStatus.Cancelled;
        else if (error != null) {
            slot.Error = error.Message;
            slot.Status = AutomationRunStatus.Failed;
        }
        else
            slot.Status = AutomationRunStatus.Completed;

        lock (_gate) {
            _active--;
            slot.FinishedUtc = DateTime.UtcNow;
        }
    }

    private void CopyArtifacts(string? sessionDirectory, Guid runId)
    {
        if (string.IsNullOrWhiteSpace(sessionDirectory))
            return;

        CopyTree(Path.Combine(sessionDirectory, "logs"), Path.Combine(RunRoot(runId), "logs"));
        CopyTree(Path.Combine(sessionDirectory, "snapshots"), Path.Combine(RunRoot(runId), "snapshots"));
    }

    private static void CopyTree(string source, string destination)
    {
        if (!Directory.Exists(source))
            return;

        Directory.CreateDirectory(destination);
        foreach (var file in Directory.GetFiles(source)) {
            var target = Path.Combine(destination, Path.GetFileName(file));
            File.Copy(file, target, true);
        }
    }

    private async Task SweepAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested) {
            try {
                await Task.Delay(TimeSpan.FromSeconds(30), ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) {
                break;
            }

            List<Guid> expired;
            lock (_gate) {
                expired = _runs.Where(pair => IsExpired(pair.Value)).Select(pair => pair.Key).ToList();
                foreach (var id in expired)
                    _runs.Remove(id);
            }

            foreach (var id in expired) {
                var root = RunRoot(id);
                if (Directory.Exists(root))
                    Directory.Delete(root, true);
            }
        }
    }

    private sealed class RunSlot(Guid runId)
    {
        private readonly List<AutomationRunStepRes> _steps = [];
        private readonly Lock _stepsGate = new();

        public Guid RunId { get; } = runId;

        public CancellationTokenSource Cancel { get; } = new();

        public string Status { get; set; } = AutomationRunStatus.Queued;

        public string? Error { get; set; }

        public IReadOnlyDictionary<string, string>? Strings { get; set; }

        public IReadOnlyDictionary<string, IReadOnlyList<string>>? StringLists { get; set; }

        public DateTime? FinishedUtc { get; set; }

        public Task? Work { get; set; }

        public void AddStep(AutomationRunStepRes step)
        {
            lock (_stepsGate)
                _steps.Add(step);
        }

        public string? ScreenshotFileId(int index)
        {
            lock (_stepsGate)
                return _steps.FirstOrDefault(step => step.Index == index)?.ScreenshotFileId;
        }

        public AutomationRunRes ToResponse()
        {
            AutomationRunStepRes[] steps;
            lock (_stepsGate)
                steps = _steps.ToArray();

            return new() {
                RunId = RunId,
                Status = Status,
                Error = Error,
                Strings = Strings,
                StringLists = StringLists,
                Steps = steps
            };
        }
    }

    private sealed class SlotInstrumentation(RunSlot slot) : AutomationPlanInstrumentationBase
    {
        public override void OnStepOutcome(in AutomationPlanStepOutcomeRecord e)
            => slot.AddStep(
                new() {
                    Index = e.StepIndex,
                    Name = e.Step.Name,
                    Type = StepType(e.Step),
                    Outcome = e.Outcome.ToString(),
                    DurationMs = (long)e.Duration.TotalMilliseconds,
                    Error = e.Error?.Message,
                    ScreenshotFileId = e.ScreenshotFileId,
                    Context = e.Context
                });

        private static string StepType(AutomationStepDefinition step)
        {
            var json = JsonSerializer.Serialize(step, typeof(AutomationStepDefinition));
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("type", out var type) ? type.GetString() ?? "" : "";
        }
    }
}