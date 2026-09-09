using System.Diagnostics;
using System.Text.Json;
using Lyo.Api.Client;
using Lyo.Api.Models.Builders;
using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Common.Response;
using Lyo.Exceptions;
using Lyo.Formatter;
using Lyo.Health;
using Lyo.Job.Models;
using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Events;
using Lyo.Job.Models.Request;
using Lyo.Job.Models.Response;
using Lyo.MessageQueue;
using Lyo.Metrics;
using Lyo.Query.Models.Builders;
using Lyo.Query.Models.Common.Request;
using Lyo.Query.Models.Enums;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ApiErrorCodes = Lyo.Api.Models.Constants.ApiErrorCodes;
using Constants = Lyo.Job.Models.Constants;

namespace Lyo.Job.Scheduler;

/// <summary>
/// Polls job definitions, evaluates schedules, creates job runs via the Job API, and handles completed runs (triggers). Implements <see cref="BackgroundService" /> so
/// hosted-service lifetime is correct.
/// </summary>
public sealed class JobScheduler : BackgroundService, IJobScheduler, IHealth
{
    /// <summary>Max retries for a failing completion message before it is dropped. Capped so a poison message cannot requeue forever.</summary>
    internal const int MaxRequeueCount = 3;

    private const int RequeueDelaySeconds = 2;

    private static readonly string[] JobRunIncludes = [
        "JobRunParameters", "JobRunLogs", "JobRunResults", "JobSchedule", "JobTrigger", "JobTrigger.JobTriggerParameters", "JobDefinition", "JobDefinition.JobParameters",
        "JobDefinition.JobTriggerJobDefinitions.JobTriggerParameters"
    ];

    private static readonly string[] JobDefinitionIncludes = [
        "JobParameters", "JobSchedules", "JobSchedules.JobScheduleParameters", "JobTriggerJobDefinitions", "JobTriggerJobDefinitions.JobTriggerParameters",
        "JobParallelRestrictionBaseJobDefinitions", "JobParallelRestrictionBaseJobDefinitions.OtherJobDefinition"
    ];

    private readonly IApiClient _apiClient;

    /// <summary>
    /// In-memory consecutive failure counters per definition. Reset to 0 on any successful run; incremented on failure. When the counter reaches <c>CircuitBreakerThreshold</c>,
    /// the scheduler disables the definition via the API and clears the counter.
    /// </summary>
    private readonly Dictionary<Guid, int> _consecutiveFailures = new();

    private readonly SemaphoreSlim _definitionLock = new(1, 1);
    private readonly IJobEventPublisher _eventPublisher;
    private readonly ILogger<JobScheduler> _logger;
    private readonly IMetrics _metrics;
    private readonly IMqService? _mqService;
    private readonly JobSchedulerOptions _options;
    private readonly JobRunRequestFactory _runRequestFactory;

    /// <summary>
    /// Slots this process has already created (or seen as duplicates). Survives definition refresh, which reloads <c>LastRun</c> as the newest run for the definition — often a
    /// retry with no <c>JobScheduleId</c> — and would otherwise re-POST the same cron slot every check.
    /// </summary>
    private readonly Dictionary<(Guid ScheduleId, DateTime Slot), byte> _knownScheduleSlots = new();

    private Dictionary<Guid, JobBlackoutCalendarRes> _blackoutCalendars = new();

    private Dictionary<Guid, JobInfo> _jobs = new();
    private DateTime? _lastDefinitionsRefreshUtc;
    private DateTime? _lastScheduleCheckUtc;

    /// <summary>Host shutdown token, captured so the MQ callbacks (which the broker invokes without one) can honor cancellation.</summary>
    private CancellationToken _stoppingToken = CancellationToken.None;

    public JobScheduler(
        JobSchedulerOptions options,
        IApiClient apiClient,
        IFormatterService formatter,
        IJobEventPublisher eventPublisher,
        ILogger<JobScheduler>? logger = null,
        IMetrics? metrics = null,
        IMqService? mqService = null)
    {
        ArgumentHelpers.ThrowIfNull(options);
        ArgumentHelpers.ThrowIfNull(apiClient);
        options.Validate();
        _options = options;
        _apiClient = apiClient;
        _eventPublisher = eventPublisher;
        _logger = logger ?? NullLogger<JobScheduler>.Instance;
        _metrics = metrics ?? NullMetrics.Instance;
        _mqService = mqService;
        _runRequestFactory = new(formatter, options.CreatedBy, _logger);
    }

    /// <inheritdoc />
    public string HealthCheckName => "job-scheduler";

    /// <inheritdoc />
    public Task<HealthResult> CheckHealthAsync(CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var metadata = new Dictionary<string, object?> {
            ["is_running"] = IsRunning,
            ["loaded_job_count"] = _jobs.Count,
            ["last_definitions_refresh_utc"] = _lastDefinitionsRefreshUtc,
            ["last_schedule_check_utc"] = _lastScheduleCheckUtc
        };

        var isHealthy = IsRunning;
        var result = isHealthy ? HealthResult.Healthy(sw.Elapsed, "Scheduler running", metadata) : HealthResult.Unhealthy(sw.Elapsed, "Scheduler is not running", metadata);
        return Task.FromResult(result);
    }

    public bool IsRunning => !ExecuteTask?.IsCompleted ?? false;

    /// <inheritdoc />
    public async Task RefreshDefinitionsAsync(CancellationToken ct = default)
    {
        await _definitionLock.WaitAsync(ct).ConfigureAwait(false);
        try {
            await RefreshDefinitionsInternalAsync(ct).ConfigureAwait(false);
        }
        finally {
            _definitionLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task CheckSchedulesAsync(CancellationToken ct = default)
    {
        await _definitionLock.WaitAsync(ct).ConfigureAwait(false);
        try {
            await CheckSchedulesInternalAsync(ct).ConfigureAwait(false);
        }
        finally {
            _definitionLock.Release();
        }
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _stoppingToken = stoppingToken;
        await _eventPublisher.SetupAsync(stoppingToken).ConfigureAwait(false);
        await _eventPublisher.SubscribeToDefinitionUpdatesAsync(Constants.Mq.JobDefinitionChangeKey, OnDefinitionUpdatedAsync, stoppingToken).ConfigureAwait(false);
        await _eventPublisher.SubscribeToRunCompletionsAsync(OnJobRunCompleteAsync, stoppingToken).ConfigureAwait(false);
        try {
            // Take the definition lock for the first pass too: the MQ subscriptions above are already live, so a completion or
            // definition-update callback can mutate _jobs concurrently with this refresh.
            await RefreshDefinitionsAsync(stoppingToken).ConfigureAwait(false);
            await CheckSchedulesAsync(stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {
            return;
        }
        catch (Exception ex) {
            _metrics.IncrementCounter(Constants.Metrics.Scheduler.RefreshError);
            _logger.LogError(ex, "Initial definition refresh failed; will retry on the refresh loop");
        }

        var definitionRefreshTimer = new PeriodicTimer(TimeSpan.FromSeconds(_options.DefinitionRefreshIntervalSeconds));
        var scheduleCheckTimer = new PeriodicTimer(TimeSpan.FromSeconds(_options.ScheduleCheckIntervalSeconds));
        try {
            await Task.WhenAll(RunDefinitionRefreshLoopAsync(definitionRefreshTimer, stoppingToken), RunScheduleCheckLoopAsync(scheduleCheckTimer, stoppingToken));
        }
        catch (OperationCanceledException) {
            // Normal shutdown
        }
        finally {
            definitionRefreshTimer.Dispose();
            scheduleCheckTimer.Dispose();
        }
    }

    private async Task RunDefinitionRefreshLoopAsync(PeriodicTimer timer, CancellationToken ct)
    {
        while (await timer.WaitForNextTickAsync(ct).ConfigureAwait(false)) {
            try {
                await RefreshDefinitionsAsync(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) {
                break;
            }
            catch (Exception ex) {
                _metrics.IncrementCounter(Constants.Metrics.Scheduler.RefreshError);
                _logger.LogError(ex, "Definition refresh failed");
            }
        }
    }

    private async Task RunScheduleCheckLoopAsync(PeriodicTimer timer, CancellationToken ct)
    {
        while (await timer.WaitForNextTickAsync(ct).ConfigureAwait(false)) {
            try {
                await CheckSchedulesAsync(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) {
                break;
            }
            catch (Exception ex) {
                _metrics.IncrementCounter(Constants.Metrics.Scheduler.CheckError);
                _logger.LogError(ex, "Schedule check failed");
            }
        }
    }

    private async Task<bool> OnDefinitionUpdatedAsync(byte[] body)
    {
        Guid? definitionId;
        try {
            definitionId = JsonSerializer.Deserialize<Guid>(body);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Could not parse definition update message");
            return false;
        }

        if (!definitionId.HasValue)
            return false;

        if (!await TryAcquireDefinitionLockAsync().ConfigureAwait(false))
            return false; // ack — the scheduler is shutting down; the next startup refresh reloads everything

        try {
            var definition = await GetJobDefinitionAsync(definitionId.Value, _stoppingToken).ConfigureAwait(false);
            if (definition == null) {
                // Deleted definition: evict it from the cache, otherwise the scheduler keeps creating doomed runs for it.
                if (_jobs.ContainsKey(definitionId.Value)) {
                    var remaining = new Dictionary<Guid, JobInfo>(_jobs);
                    remaining.Remove(definitionId.Value);
                    _jobs = remaining;
                    _logger.LogInformation("Removed deleted definition {DefinitionId} from the scheduler cache", definitionId);
                }
                else
                    _logger.LogWarning("Definition {DefinitionId} not found", definitionId);

                return false; // ack — retrying won't help if the definition is gone
            }

            if (!definition.Enabled) {
                var updated = new Dictionary<Guid, JobInfo>(_jobs);
                updated.Remove(definitionId.Value);
                _jobs = updated;
                _logger.LogDebug("Removed disabled definition {DefinitionId}", definitionId);
                return false; // ack — handled
            }

            var jobInfo = await LoadJobInfoAsync(definition, _stoppingToken).ConfigureAwait(false);
            var updatedJobs = new Dictionary<Guid, JobInfo>(_jobs) { [definitionId.Value] = jobInfo };
            _jobs = updatedJobs;
            _logger.LogInformation("Refreshed definition {DefinitionId} ({Name})", definitionId, definition.Name);
            return false; // ack — handled (true = requeue)
        }
        catch (Exception ex) {
            // Do not throw: RequeueOnException would loop poison messages forever (e.g. API enum query bugs).
            _logger.LogError(ex, "Failed to refresh definition {DefinitionId}; acknowledging to avoid requeue storm", definitionId);
            return false;
        }
        finally {
            _definitionLock.Release();
        }
    }

    private async Task<bool> OnJobRunCompleteAsync(byte[] body)
    {
        if (!TryParseCompletion(body, out var jobRunId, out var envelope)) {
            _logger.LogError("Could not parse job run complete message");
            return false;
        }

        try {
            var run = await GetJobRunAsync(jobRunId, _stoppingToken).ConfigureAwait(false);
            if (run != null)
                return await ProcessCompletedJobRunAsync(run).ConfigureAwait(false);

            _logger.LogWarning("Job run {JobRunId} not found", jobRunId);
            return false;
        }
        catch (Exception ex) {
            // Do not throw: RequeueOnException would nack-requeue the same bytes forever (e.g. API 500).
            return await HandleCompletionFailureAsync(jobRunId, envelope, ex).ConfigureAwait(false);
        }
    }

    /// <summary>Parses a raw JSON <see cref="Guid" /> or a counted <see cref="QueueMessageEnvelope{T}" /> retry. Unparseable bytes are poison and must be acked.</summary>
    private static bool TryParseCompletion(byte[] body, out Guid jobRunId, out QueueMessageEnvelope<Guid>? envelope)
    {
        jobRunId = default;
        envelope = null;
        try {
            envelope = JsonSerializer.Deserialize<QueueMessageEnvelope<Guid>>(body);
            if (envelope is not null && envelope.Payload != Guid.Empty) {
                jobRunId = envelope.Payload;
                return true;
            }
        }
        catch (JsonException) {
            // Producers publish a bare Guid; fall through.
        }

        envelope = null;
        try {
            var id = JsonSerializer.Deserialize<Guid>(body);
            if (id == Guid.Empty)
                return false;

            jobRunId = id;
            return true;
        }
        catch (JsonException) {
            return false;
        }
    }

    /// <summary>Acks the delivered message and republishes a counted envelope up to <see cref="MaxRequeueCount" />. Never throws.</summary>
    private async Task<bool> HandleCompletionFailureAsync(Guid runId, QueueMessageEnvelope<Guid>? envelope, Exception ex)
    {
        var requeueCount = envelope?.RequeueCount ?? 0;
        if (_mqService is null || requeueCount >= MaxRequeueCount) {
            _logger.LogError(ex, "Scheduler giving up on run {RunId} after {Count} requeue(s)", runId, requeueCount);
            return false;
        }

        var nextCount = requeueCount + 1;
        _logger.LogWarning(ex, "Scheduler failed processing run {RunId}; requeue {Count}/{Max}", runId, nextCount, MaxRequeueCount);
        var retry = new QueueMessageEnvelope<Guid>(runId, nextCount, envelope?.MessageId ?? Guid.NewGuid().ToString("D"), envelope?.EnqueuedAt ?? DateTime.UtcNow);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(retry);
        try {
            if (_mqService is IDelayedMqService delayedMq)
                await delayedMq.SendToQueueDelayed(Constants.Mq.QueueJobRunFinish, bytes, TimeSpan.FromSeconds(nextCount * RequeueDelaySeconds)).ConfigureAwait(false);
            else
                await _mqService.SendToQueue(Constants.Mq.QueueJobRunFinish, bytes).ConfigureAwait(false);
        }
        catch (Exception publishEx) {
            _logger.LogError(publishEx, "Scheduler failed to republish run {RunId} after processing error", runId);
        }

        return false;
    }

    /// <summary>Loads job definitions from the API and atomically replaces the in-memory cache. Must be called under the definition lock.</summary>
    private async Task RefreshDefinitionsInternalAsync(CancellationToken ct = default)
    {
        _lastDefinitionsRefreshUtc = DateTime.UtcNow;
        _logger.LogTrace("Updating job definitions");
        using var timer = _metrics.StartTimer(Constants.Metrics.Scheduler.RefreshDuration);
        var query = new QueryConcreteReqBuilder().AddIncludes(JobDefinitionIncludes).Build();
        var results = await _apiClient.PostAsAsync<QueryConcreteReq, QueryRes<JobDefinitionRes>>(BuildUri(Constants.Rest.Job.DefinitionsQuery), query, null, ct)
            .ConfigureAwait(false);

        if (results.Items == null || !results.IsSuccess) {
            _metrics.IncrementCounter(Constants.Metrics.Scheduler.RefreshError);
            _logger.LogWarning("No definitions loaded or query failed");
            return;
        }

        var enabledDefinitions = results.Items.Where(d => d.Enabled).ToList();
        foreach (var def in results.Items.Where(d => !d.Enabled))
            _logger.LogDebug("Skipping disabled definition {Name}", def.Name);

        // Batch endpoint: one round trip for all definitions instead of three run queries per definition.
        var latestRuns = await LoadLatestRunsBatchAsync(enabledDefinitions, ct).ConfigureAwait(false);
        var updated = new Dictionary<Guid, JobInfo>();
        foreach (var def in enabledDefinitions) {
            if (latestRuns is not null && latestRuns.TryGetValue(def.Id, out var latest)) {
                RememberRunSlots(latest.LastRun, latest.LastSuccessfulRun, latest.LastFailedRun);
                updated[def.Id] = new(def, latest.LastRun, latest.LastSuccessfulRun, latest.LastFailedRun);
            }
            else if (latestRuns is not null)
                updated[def.Id] = new(def);
            else
                updated[def.Id] = await LoadJobInfoAsync(def, ct).ConfigureAwait(false);
        }

        _jobs = updated;
        _metrics.RecordGauge(Constants.Metrics.Scheduler.DefinitionsLoaded, updated.Count);
        await RefreshBlackoutCalendarsAsync(ct).ConfigureAwait(false);
        await ProcessMisfiresAsync(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Loads the latest/latest-successful/latest-failed run for all definitions in one call. Returns null when the batch endpoint is unavailable (fallback to per-definition
    /// queries).
    /// </summary>
    private async Task<Dictionary<Guid, JobDefinitionLatestRunsRes>?> LoadLatestRunsBatchAsync(IReadOnlyList<JobDefinitionRes> definitions, CancellationToken ct)
    {
        if (definitions.Count == 0)
            return new();

        try {
            var ids = definitions.Select(d => d.Id).ToList();
            var latest = await _apiClient.PostAsAsync<List<Guid>, List<JobDefinitionLatestRunsRes>>(BuildUri(Constants.Rest.Job.DefinitionsLatestRuns), ids, null, ct)
                .ConfigureAwait(false);

            return latest.ToDictionary(l => l.JobDefinitionId);
        }
        catch (ApiException ex) {
            _logger.LogWarning(ex, "Batch latest-runs endpoint unavailable; falling back to per-definition queries");
            return null;
        }
    }

    private async Task RefreshBlackoutCalendarsAsync(CancellationToken ct)
    {
        var query = new QueryConcreteReqBuilder().AddIncludes("JobBlackoutWindows").Build();
        var results = await _apiClient
            .PostAsAsync<QueryConcreteReq, QueryRes<JobBlackoutCalendarRes>>(BuildUri($"{Constants.Rest.Job.BlackoutCalendars}/QueryConcrete"), query, null, ct)
            .ConfigureAwait(false);

        if (results.Items == null || !results.IsSuccess) {
            _logger.LogWarning("Failed to refresh job calendars");
            return;
        }

        _blackoutCalendars = results.Items.Where(c => c.Enabled).ToDictionary(c => c.Id);
        _logger.LogDebug("Loaded {Count} enabled job blackout calendars", _blackoutCalendars.Count);
    }

    /// <summary>Evaluates schedules and creates job runs where due. Must be called under the definition lock.</summary>
    private async Task CheckSchedulesInternalAsync(CancellationToken ct = default)
    {
        _lastScheduleCheckUtc = DateTime.UtcNow;

        // Runs are created over HTTP and the API owns their dispatch, so a disconnected publisher must not stop schedules from firing —
        // it only means dispatch is delayed until the maintenance service redispatches the queued runs.
        if (!_eventPublisher.IsConnected())
            _logger.LogWarning("Event publisher disconnected; scheduling continues and queued runs will be dispatched by maintenance recovery");

        using var timer = _metrics.StartTimer(Constants.Metrics.Scheduler.CheckDuration);
        foreach (var kvp in _jobs) {
            var jobInfo = kvp.Value;
            var schedules = jobInfo.Definition.JobSchedules;
            if (schedules == null)
                continue;

            foreach (var schedule in schedules) {
                using (_logger.BeginScope("DefinitionId={DefinitionId} ScheduleId={ScheduleId}", jobInfo.Definition.Id, schedule.Id)) {
                    var scheduledSlot = GetDueScheduledSlot(jobInfo, schedule);
                    if (!scheduledSlot.HasValue)
                        continue;

                    await ProcessScheduledJobDefinitionAsync(jobInfo.Definition, schedule, scheduledSlot.Value, ct).ConfigureAwait(false);
                }
            }
        }
    }

    /// <summary>
    /// Returns the due scheduled slot UTC timestamp if this schedule should fire now, or null if not. The returned value is used as the idempotency key (<c>ScheduledSlotUtc</c>)
    /// when creating the run.
    /// </summary>
    private DateTime? GetDueScheduledSlot(JobInfo jobInfo, JobScheduleRes schedule)
    {
        if (!schedule.Enabled)
            return null;

        var now = DateTime.UtcNow;
        if (!JobScheduleSlotCalculator.IsWithinWindow(schedule, now))
            return null;

        if (jobInfo.LastRun?.State is JobState.Queued or JobState.Running) {
            _logger.LogDebug("Job already queued or running");
            return null;
        }

        if (IsBlockedByParallelRestrictions(jobInfo))
            return null;

        // One resolver for both next-run math and blackout evaluation: the schedule's own zone wins, falling back to the scheduler-level
        // option. Overwriting with the option alone made cron fire at the wrong instant and disagree with the blackout windows below.
        var timeZone = ResolveTimeZone(schedule);
        var definition = schedule.ToScheduleDefinition() with { TimeZone = timeZone };
        var reference = ResolveScheduleReference(jobInfo, schedule, now);
        var slot = JobScheduleSlotCalculator.GetDueSlot(definition, schedule, ResolveBlackoutCalendar(schedule), timeZone, reference, now);
        if (!slot.HasValue)
            return null;

        // LastRun can be a newer retry/manual run, so matching LastRun.ScheduledSlotUtc is not enough; remembered slots cover that after refresh.
        if (jobInfo.LastRun?.JobScheduleId == schedule.Id && jobInfo.LastRun.ScheduledSlotUtc == slot) {
            RememberScheduleSlot(schedule.Id, slot.Value);
            return null;
        }

        if (IsKnownScheduleSlot(schedule.Id, slot.Value))
            return null;

        _logger.LogInformation("Schedule due for definition {Name} (slot {Slot:u})", jobInfo.Definition.Name, slot.Value);
        return slot;
    }

    /// <summary>Anchor both the due-slot and misfire paths on the same reference, so they cannot disagree about which slots are already accounted for.</summary>
    private DateTime ResolveScheduleReference(JobInfo jobInfo, JobScheduleRes schedule, DateTime now)
    {
        // Only this schedule's slot is a safe cursor. A retry's ScheduledSlotUtc is a dispatch delay, not a cron instant, and would skip upcoming ticks.
        var lastSlotForThisSchedule = LatestSlotForSchedule(schedule.Id, jobInfo.LastRun, jobInfo.LastFailedRun, jobInfo.LastSuccessfulRun);
        return JobScheduleReference.Resolve(
            jobInfo.LastSuccessfulRun?.StartedTimestamp, lastSlotForThisSchedule, jobInfo.LastRun?.StartedTimestamp, jobInfo.LastRun?.CreatedTimestamp, schedule.StartDateUtc,
            now, _options.MisfireLookbackMinutes);
    }

    private static DateTime? LatestSlotForSchedule(Guid scheduleId, params JobRunRes?[] runs)
    {
        DateTime? latest = null;
        foreach (var run in runs) {
            if (run?.JobScheduleId != scheduleId || !run.ScheduledSlotUtc.HasValue)
                continue;
            if (!latest.HasValue || run.ScheduledSlotUtc.Value > latest.Value)
                latest = run.ScheduledSlotUtc;
        }

        return latest;
    }

    private JobBlackoutCalendarRes? ResolveBlackoutCalendar(JobScheduleRes schedule)
    {
        if (schedule.JobBlackoutCalendar is { Enabled: true })
            return schedule.JobBlackoutCalendar;

        if (schedule.JobBlackoutCalendarId.HasValue && _blackoutCalendars.TryGetValue(schedule.JobBlackoutCalendarId.Value, out var cached))
            return cached;

        return null;
    }

    private TimeZoneInfo? ResolveTimeZone(JobScheduleRes schedule)
    {
        if (!string.IsNullOrWhiteSpace(schedule.TimeZoneId)) {
            try {
                return TimeZoneInfo.FindSystemTimeZoneById(schedule.TimeZoneId);
            }
            catch (TimeZoneNotFoundException ex) {
                _logger.LogWarning(ex, "Unknown schedule time zone {TimeZoneId}", schedule.TimeZoneId);
            }
            catch (InvalidTimeZoneException ex) {
                _logger.LogWarning(ex, "Invalid schedule time zone {TimeZoneId}", schedule.TimeZoneId);
            }
        }

        return _options.TimeZone;
    }

    /// <summary>Creates catch-up runs for the most recent missed slot on schedules with a RunOnce misfire policy.</summary>
    private async Task ProcessMisfiresAsync(CancellationToken ct)
    {
        if (!_options.EnableMisfireCatchUp || !_eventPublisher.IsConnected())
            return;

        var now = DateTime.UtcNow;
        var lookbackStart = now.AddMinutes(-_options.MisfireLookbackMinutes);
        foreach (var jobInfo in _jobs.Values) {
            var schedules = jobInfo.Definition.JobSchedules;
            if (schedules == null)
                continue;

            foreach (var schedule in schedules) {
                if (!schedule.Enabled || schedule.MisfirePolicy != JobMisfirePolicy.RunOnce)
                    continue;

                using (_logger.BeginScope("DefinitionId={DefinitionId} ScheduleId={ScheduleId}", jobInfo.Definition.Id, schedule.Id)) {
                    if (!JobScheduleSlotCalculator.IsWithinWindow(schedule, now)) {
                        _metrics.IncrementCounter(Constants.Metrics.Scheduler.MisfiresSkipped);
                        continue;
                    }

                    if (jobInfo.LastRun?.State is JobState.Queued or JobState.Running || IsBlockedByParallelRestrictions(jobInfo)) {
                        _metrics.IncrementCounter(Constants.Metrics.Scheduler.MisfiresSkipped);
                        continue;
                    }

                    var missedSlot = FindMostRecentMissedSlot(jobInfo, schedule, lookbackStart, now);
                    if (!missedSlot.HasValue) {
                        _metrics.IncrementCounter(Constants.Metrics.Scheduler.MisfiresSkipped);
                        continue;
                    }

                    if (await RunExistsForSlotAsync(schedule.Id, missedSlot.Value, ct).ConfigureAwait(false)) {
                        RememberScheduleSlot(schedule.Id, missedSlot.Value);
                        _metrics.IncrementCounter(Constants.Metrics.Scheduler.MisfiresSkipped);
                        continue;
                    }

                    _logger.LogInformation("Misfire catch-up for definition {Name} (missed slot {Slot:u})", jobInfo.Definition.Name, missedSlot.Value);
                    var created = await CreateScheduledRunAsync(jobInfo, schedule, missedSlot.Value, ct).ConfigureAwait(false);
                    if (created)
                        _metrics.IncrementCounter(Constants.Metrics.Scheduler.MisfiresCaughtUp);
                }
            }
        }
    }

    /// <summary>Resolves the schedule's zone, calendar, and reference, then delegates the slot search to <see cref="JobScheduleSlotCalculator" />.</summary>
    private DateTime? FindMostRecentMissedSlot(JobInfo jobInfo, JobScheduleRes schedule, DateTime lookbackStart, DateTime now)
    {
        var timeZone = ResolveTimeZone(schedule);
        var definition = schedule.ToScheduleDefinition() with { TimeZone = timeZone };
        var reference = ResolveScheduleReference(jobInfo, schedule, now);
        return JobScheduleSlotCalculator.FindMostRecentMissedSlot(definition, schedule, ResolveBlackoutCalendar(schedule), timeZone, reference, lookbackStart, now);
    }

    private bool IsBlockedByParallelRestrictions(JobInfo jobInfo)
    {
        var restrictions = jobInfo.Definition.JobParallelRestrictions;
        if (restrictions == null)
            return false;

        foreach (var restriction in restrictions) {
            if (!restriction.Enabled)
                continue;

            if (_jobs.TryGetValue(restriction.OtherJobDefinitionId, out var otherJob) && otherJob.LastRun?.State is JobState.Queued or JobState.Running) {
                _logger.LogDebug(
                    "Job {Name} blocked by parallel restriction with {OtherName}", jobInfo.Definition.Name,
                    restriction.OtherJobDefinition?.Name ?? restriction.OtherJobDefinitionId.ToString());

                return true;
            }
        }

        return false;
    }

    private async Task<bool> RunExistsForSlotAsync(Guid scheduleId, DateTime scheduledSlot, CancellationToken ct)
    {
        var where = WhereClauseBuilder.CombineAs(
            GroupOperatorEnum.And, WhereClauseBuilder.Condition("JobScheduleId", ComparisonOperatorEnum.Equals, scheduleId.ToString()),
            WhereClauseBuilder.Condition("ScheduledSlotUtc", ComparisonOperatorEnum.Equals, scheduledSlot));

        var result = await _apiClient.PostAsAsync<QueryConcreteReq, QueryRes<JobRunRes>>(
                BuildUri(Constants.Rest.Job.RunsQuery), new QueryConcreteReqBuilder().AddWhere(where).First().Build(), null, ct)
            .ConfigureAwait(false);

        return result.Items?.Count > 0;
    }

    private async Task<JobInfo> LoadJobInfoAsync(JobDefinitionRes definition, CancellationToken ct = default)
    {
        var baseWhere = WhereClauseBuilder.Condition("JobDefinitionId", ComparisonOperatorEnum.Equals, definition.Id.ToString());
        var lastRunTask = _apiClient.PostAsAsync<QueryConcreteReq, QueryRes<JobRunRes>>(
            BuildUri(Constants.Rest.Job.RunsQuery), new QueryConcreteReqBuilder().AddIncludes(JobRunIncludes).AddWhere(baseWhere).AddSort("CreatedTimestamp").First().Build(), null,
            ct);

        var successFilter = WhereClauseBuilder.CombineAs(
            GroupOperatorEnum.And, baseWhere,
            WhereClauseBuilder.Condition("Result", ComparisonOperatorEnum.In, new[] { nameof(JobRunResult.Success), nameof(JobRunResult.SuccessWithWarnings) }));

        var lastSuccessTask = _apiClient.PostAsAsync<QueryConcreteReq, QueryRes<JobRunRes>>(
            BuildUri(Constants.Rest.Job.RunsQuery), new QueryConcreteReqBuilder().AddIncludes(JobRunIncludes).AddWhere(successFilter).AddSort("CreatedTimestamp").First().Build(),
            null, ct);

        var failFilter = WhereClauseBuilder.CombineAs(
            GroupOperatorEnum.And, baseWhere, WhereClauseBuilder.Condition("Result", ComparisonOperatorEnum.Equals, nameof(JobRunResult.Failure)));

        var lastFailedTask = _apiClient.PostAsAsync<QueryConcreteReq, QueryRes<JobRunRes>>(
            BuildUri(Constants.Rest.Job.RunsQuery), new QueryConcreteReqBuilder().AddIncludes(JobRunIncludes).AddWhere(failFilter).AddSort("CreatedTimestamp").First().Build(),
            null, ct);

        await Task.WhenAll(lastRunTask, lastSuccessTask, lastFailedTask).ConfigureAwait(false);
        var lastRun = lastRunTask.Result.Items?.FirstOrDefault();
        var lastSuccess = lastSuccessTask.Result.Items?.FirstOrDefault();
        var lastFailed = lastFailedTask.Result.Items?.FirstOrDefault();
        RememberRunSlots(lastRun, lastSuccess, lastFailed);
        return new(definition, lastRun, lastSuccess, lastFailed);
    }

    private async Task ProcessScheduledJobDefinitionAsync(JobDefinitionRes definition, JobScheduleRes schedule, DateTime scheduledSlot, CancellationToken ct)
        => await CreateScheduledRunAsync(_jobs[definition.Id], schedule, scheduledSlot, ct).ConfigureAwait(false);

    /// <summary>Posts a scheduled job run and updates in-memory state. Returns true when a new run was created.</summary>
    private async Task<bool> CreateScheduledRunAsync(JobInfo jobInfo, JobScheduleRes schedule, DateTime scheduledSlot, CancellationToken ct)
    {
        var definition = jobInfo.Definition;
        if (!_jobs.TryGetValue(definition.Id, out var currentJobInfo))
            return false;

        jobInfo = currentJobInfo;
        var runReq = BuildRunRequest(definition.Id, schedule, null, null);
        runReq.JobScheduleId = schedule.Id;
        runReq.ScheduledSlotUtc = scheduledSlot;
        _logger.LogDebug("Creating job run: {Request}", runReq);
        try {
            var created = await _apiClient.PostAsAsync<JobRunReq, CreateResult<JobRunRes>>(BuildUri(Constants.Rest.Job.RunsCreate), runReq, null, ct).ConfigureAwait(false);
            if (created.IsSuccess && created.Data != null) {
                _logger.LogInformation("Created job run {JobRunId}", created.Data.Id);
                _metrics.IncrementCounter(Constants.Metrics.Scheduler.RunsCreated);
                RememberScheduleSlot(schedule.Id, scheduledSlot);
                _jobs = new(_jobs) { [definition.Id] = jobInfo with { LastRun = created.Data } };
                return true;
            }

            if (created.Error?.Errors.Any(e => e.Code == ApiErrorCodes.Conflict) == true) {
                _metrics.IncrementCounter(Constants.Metrics.Scheduler.SlotConflicts);
                _logger.LogDebug("Job run for slot {Slot:u} already exists (created by another instance)", scheduledSlot);
                RememberScheduleSlot(schedule.Id, scheduledSlot);
                MarkSlotAttempted(definition.Id, schedule.Id, jobInfo, scheduledSlot);
                return false;
            }

            _metrics.IncrementCounter(Constants.Metrics.Scheduler.RunCreateFailed);
            _logger.LogWarning("Failed to create job run: {Error}", created.Error);
            MarkSlotAttempted(definition.Id, schedule.Id, jobInfo, scheduledSlot);
            return false;
        }
        catch (ApiException ex) {
            // Do not let a single create failure abort the whole schedule-check loop (past-due spam).
            if (ex.StatusCode is 409) {
                _metrics.IncrementCounter(Constants.Metrics.Scheduler.SlotConflicts);
                _logger.LogDebug(ex, "Job run for slot {Slot:u} already exists (created by another instance)", scheduledSlot);
                RememberScheduleSlot(schedule.Id, scheduledSlot);
                MarkSlotAttempted(definition.Id, schedule.Id, jobInfo, scheduledSlot);
                return false;
            }

            _metrics.IncrementCounter(Constants.Metrics.Scheduler.RunCreateFailed);
            _logger.LogWarning(ex, "Failed to create job run for slot {Slot:u}", scheduledSlot);
            MarkSlotAttempted(definition.Id, schedule.Id, jobInfo, scheduledSlot);
            return false;
        }
    }

    /// <summary>
    /// Advances in-memory schedule progress past <paramref name="scheduledSlot" /> when create fails, so the next check does not retry the
    /// same past-due slot forever.
    /// </summary>
    private void MarkSlotAttempted(Guid definitionId, Guid scheduleId, JobInfo jobInfo, DateTime scheduledSlot)
    {
        var placeholder = (jobInfo.LastRun ?? new JobRunRes { JobDefinitionId = definitionId, State = JobState.Finished }) with {
            JobScheduleId = scheduleId, ScheduledSlotUtc = scheduledSlot, State = JobState.Finished, Result = JobRunResult.Failure
        };

        _jobs = new(_jobs) { [definitionId] = jobInfo with { LastRun = placeholder } };
    }

    private bool IsKnownScheduleSlot(Guid scheduleId, DateTime slot) => _knownScheduleSlots.ContainsKey((scheduleId, slot));

    private void RememberRunSlots(params JobRunRes?[] runs)
    {
        foreach (var run in runs) {
            if (run?.JobScheduleId is { } scheduleId && run.ScheduledSlotUtc is { } slot)
                RememberScheduleSlot(scheduleId, slot);
        }
    }

    private void RememberScheduleSlot(Guid scheduleId, DateTime slot)
    {
        _knownScheduleSlots[(scheduleId, slot)] = 0;
        if (_knownScheduleSlots.Count < 256)
            return;

        var cutoff = DateTime.UtcNow.AddMinutes(-Math.Max(60, _options.MisfireLookbackMinutes));
        List<(Guid ScheduleId, DateTime Slot)>? stale = null;
        foreach (var key in _knownScheduleSlots.Keys) {
            if (key.Slot >= cutoff)
                continue;

            stale ??= [];
            stale.Add(key);
        }

        if (stale is null)
            return;

        foreach (var key in stale)
            _knownScheduleSlots.Remove(key);
    }

    /// <returns>false to ack the MQ message; true to requeue. Missing cache entries are acked so they do not loop forever.</returns>
    private async Task<bool> ProcessCompletedJobRunAsync(JobRunRes run)
    {
        // Serialize with definition refresh/schedule checks: completion handling mutates _jobs and _consecutiveFailures, and a concurrent
        // refresh could otherwise overwrite (or race with) the LastRun/LastFailedRun updates made here.
        if (!await TryAcquireDefinitionLockAsync().ConfigureAwait(false))
            return true; // requeue — shutting down before the completion was processed, so another instance (or restart) must handle it

        try {
            return await ProcessCompletedJobRunLockedAsync(run).ConfigureAwait(false);
        }
        finally {
            _definitionLock.Release();
        }
    }

    /// <summary>
    /// Acquires the definition lock while honoring host shutdown. The broker invokes MQ callbacks without a cancellation token, so without this a stopping host could block on
    /// the lock behind a slow refresh.
    /// </summary>
    /// <returns>false when the scheduler is stopping and the caller should not process the message.</returns>
    private async Task<bool> TryAcquireDefinitionLockAsync()
    {
        try {
            await _definitionLock.WaitAsync(_stoppingToken).ConfigureAwait(false);
            return true;
        }
        catch (OperationCanceledException) {
            return false;
        }
    }

    private async Task<bool> ProcessCompletedJobRunLockedAsync(JobRunRes run)
    {
        if (!_jobs.TryGetValue(run.JobDefinitionId, out var jobInfo)) {
            _logger.LogWarning("No job info for definition {DefinitionId}", run.JobDefinitionId);
            return false; // ack — requeue would spam this forever; definition may be unscheduled/disabled
        }

        var updatedInfo = jobInfo with { LastRun = run };
        var resultStr = run.GetResultValueAs<string?>(Constants.Data.JobRunResultKey.Result);
        if (resultStr is "Success" or "PartialSuccess" or "SuccessWithWarnings")
            updatedInfo = updatedInfo with { LastSuccessfulRun = run };
        else
            updatedInfo = updatedInfo with { LastFailedRun = run };

        _jobs = new(_jobs) { [run.JobDefinitionId] = updatedInfo };

        // Update circuit breaker counter. Timeouts (dead-job detection) count as failures so hung jobs also trip the breaker and retry.
        if (JobRunOutcome.IsSuccess(run.Result))
            _consecutiveFailures.Remove(run.JobDefinitionId);
        else if (JobRunOutcome.IsFailure(run.Result)) {
            _consecutiveFailures.TryGetValue(run.JobDefinitionId, out var prev);
            var next = prev + 1;
            _consecutiveFailures[run.JobDefinitionId] = next;
            var threshold = jobInfo.Definition.CircuitBreakerThreshold;
            if (threshold > 0 && next >= threshold) {
                _logger.LogWarning(
                    "Circuit breaker tripped for {Name} ({DefinitionId}) after {Failures} consecutive failure(s)", jobInfo.Definition.Name, run.JobDefinitionId, next);

                // Only clear the counter once the definition is actually disabled. Clearing it after a failed trip left the breaker open:
                // the definition kept running and the next failure started counting from zero again.
                if (await TripCircuitBreakerAsync(run.JobDefinitionId).ConfigureAwait(false)) {
                    _metrics.IncrementCounter(Constants.Metrics.Scheduler.CircuitBreakerTripped);
                    if (_eventPublisher.IsConnected()) {
                        await _eventPublisher.PublishAlertAsync(
                                run.JobDefinitionId, run.Id, JobAlertType.CircuitBreakerTripped,
                                $"Circuit breaker tripped for '{jobInfo.Definition.Name}' after {next} consecutive failure(s)", _stoppingToken)
                            .ConfigureAwait(false);
                    }

                    _consecutiveFailures.Remove(run.JobDefinitionId);
                }
                else {
                    _metrics.IncrementCounter(Constants.Metrics.Scheduler.CircuitBreakerTripFailed);
                    _logger.LogError(
                        "Could not disable definition {DefinitionId} after {Failures} consecutive failure(s); keeping the counter so the next failure retries the trip",
                        run.JobDefinitionId, next);
                }
            }
            else if (jobInfo.Definition is { AlertOnFailure: true } && _eventPublisher.IsConnected()) {
                var alertThreshold = jobInfo.Definition.AlertAfterConsecutiveFailures;
                if (alertThreshold <= 0 || next >= alertThreshold) {
                    await _eventPublisher.PublishAlertAsync(
                            run.JobDefinitionId, run.Id, JobAlertType.Failure, $"Job '{jobInfo.Definition.Name}' failed ({next} consecutive failure(s))", _stoppingToken)
                        .ConfigureAwait(false);
                }
            }
        }

        // Schedule a retry if the run failed (or timed out) and the definition allows it.
        if (JobRunOutcome.IsFailure(run.Result) && jobInfo.Definition.MaxRetryCount > 0 && run.RetryAttempt < jobInfo.Definition.MaxRetryCount)
            await ScheduleRetryAsync(jobInfo, run).ConfigureAwait(false);

        // Batch fan-in: when a child run completes, update parent progress and finalize when all siblings finish.
        if (run.ParentJobRunId.HasValue)
            await ProcessChildRunCompletionAsync(run).ConfigureAwait(false);

        if (!run.AllowTriggers || jobInfo.Definition.JobTriggers?.Count == 0)
            return false; // ack — handled

        await ProcessTriggersAsync(updatedInfo, run).ConfigureAwait(false);
        return false; // ack — handled (true = requeue)
    }

    private async Task ScheduleRetryAsync(JobInfo jobInfo, JobRunRes failedRun)
    {
        var nextAttempt = failedRun.RetryAttempt + 1;
        var backoffSeconds = JobRetryBackoff.ComputeBackoffSeconds(jobInfo.Definition.RetryBackoffSeconds, nextAttempt, jobInfo.Definition.RetryBackoffType);
        var useDelayedMq = backoffSeconds > 0 && _mqService is IDelayedMqService;
        _logger.LogInformation(
            "Scheduling retry attempt {Attempt}/{Max} for definition {Name} (backoff {Backoff}s, type {BackoffType})", nextAttempt, jobInfo.Definition.MaxRetryCount,
            jobInfo.Definition.Name, backoffSeconds, jobInfo.Definition.RetryBackoffType);

        var retryReq = BuildRunRequest(failedRun.JobDefinitionId, null, null, null);
        retryReq.RetryAttempt = nextAttempt;
        retryReq.ReRanFromJobRunId = failedRun.Id;

        // Deduplicate retry creation across scheduler instances / redelivered completion messages: the same failed run + attempt
        // always maps to the same idempotency key, so a duplicate create returns the existing retry run instead of a second one.
        retryReq.IdempotencyKey = $"retry:{failedRun.Id:N}:{nextAttempt}";

        // Exactly one dispatch path per retry:
        // - delayed MQ available: suppress the API's immediate publish; the delayed envelope below is the sole dispatch.
        // - no delayed MQ: a future ScheduledSlotUtc suppresses the immediate publish and the maintenance service dispatches when due.
        // - no backoff: the API's immediate publish dispatches as usual.
        if (backoffSeconds > 0) {
            if (useDelayedMq)
                retryReq.SuppressDispatch = true;
            else
                retryReq.ScheduledSlotUtc = DateTime.UtcNow.AddSeconds(backoffSeconds);
        }

        var created = await _apiClient.PostAsAsync<JobRunReq, CreateResult<JobRunRes>>(BuildUri(Constants.Rest.Job.RunsCreate), retryReq, null, _stoppingToken).ConfigureAwait(false);
        if (created.IsSuccess && created.Data != null) {
            _metrics.IncrementCounter(Constants.Metrics.Scheduler.RetriesScheduled);
            _logger.LogInformation("Created retry job run {JobRunId} (attempt {Attempt})", created.Data.Id, nextAttempt);

            // Make the pending retry visible to schedule gating. Without this the retry is invisible to GetDueScheduledSlot, so the next
            // cron tick would create a second concurrent run for the same definition while the retry is still queued.
            if (_jobs.TryGetValue(failedRun.JobDefinitionId, out var current))
                _jobs = new(_jobs) { [failedRun.JobDefinitionId] = current with { LastRun = created.Data } };

            if (useDelayedMq && _mqService is IDelayedMqService delayedMq) {
                var queue = Constants.Mq.QueueGetJobRunCreated(jobInfo.Definition.WorkerType);
                var envelope = new QueueMessageEnvelope<Guid>(created.Data.Id, 0, Guid.NewGuid().ToString("D"), DateTime.UtcNow);
                var bytes = JsonSerializer.SerializeToUtf8Bytes(envelope);
                await delayedMq.SendToQueueDelayed(queue, bytes, TimeSpan.FromSeconds(backoffSeconds)).ConfigureAwait(false);
            }
        }
        else if (created.Error?.Errors.Any(e => e.Code == ApiErrorCodes.Conflict) == true)
            _metrics.IncrementCounter(Constants.Metrics.Scheduler.SlotConflicts);
        else {
            _metrics.IncrementCounter(Constants.Metrics.Scheduler.RunCreateFailed);
            _logger.LogWarning("Failed to create retry job run for definition {Name}: {Error}", jobInfo.Definition.Name, created.Error);
        }
    }

    /// <summary>Disables a definition whose failure counter reached the circuit-breaker threshold.</summary>
    /// <returns>true when the definition was disabled; false when the API call failed and the breaker is still open.</returns>
    private async Task<bool> TripCircuitBreakerAsync(Guid definitionId)
    {
        try {
            var patch = PatchRequestBuilder.ForId(definitionId).SetProperty("Enabled", false).SetProperty("CircuitBreakerTrippedAt", DateTime.UtcNow).Build();
            await _apiClient.PatchAsAsync<PatchRequest, object>(BuildUri($"{Constants.Rest.Job.Definitions}/{definitionId}"), patch, null, _stoppingToken).ConfigureAwait(false);

            // Remove from in-memory cache so the scheduler stops firing this definition.
            var updated = new Dictionary<Guid, JobInfo>(_jobs);
            updated.Remove(definitionId);
            _jobs = updated;
            return true;
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to trip circuit breaker for definition {DefinitionId}", definitionId);
            return false;
        }
    }

    private async Task ProcessChildRunCompletionAsync(JobRunRes childRun)
    {
        var parentId = childRun.ParentJobRunId!.Value;
        var siblings = await GetChildRunsAsync(parentId, _stoppingToken).ConfigureAwait(false);
        if (siblings.Count == 0)
            return;

        var finished = siblings.Where(r => r.State == JobState.Finished).ToList();
        var progress = (int)Math.Round(finished.Count * 100.0 / siblings.Count);
        await PatchRunProgressAsync(parentId, progress, $"{finished.Count}/{siblings.Count} children complete").ConfigureAwait(false);
        if (finished.Count < siblings.Count)
            return;

        var parent = await GetJobRunAsync(parentId, _stoppingToken).ConfigureAwait(false);
        if (parent is null || parent.State == JobState.Finished)
            return;

        var aggregated = JobBatchResultAggregator.Aggregate(siblings);
        if (!await FinalizeParentRunAsync(parentId, aggregated).ConfigureAwait(false))
            return;
    }

    private async Task<IReadOnlyList<JobRunRes>> GetChildRunsAsync(Guid parentRunId, CancellationToken ct = default)
    {
        var where = WhereClauseBuilder.Condition("ParentJobRunId", ComparisonOperatorEnum.Equals, parentRunId.ToString());
        var result = await _apiClient.PostAsAsync<QueryConcreteReq, QueryRes<JobRunRes>>(
                BuildUri(Constants.Rest.Job.RunsQuery), new QueryConcreteReqBuilder().AddIncludes("JobRunResults").AddWhere(where).Build(), null, ct)
            .ConfigureAwait(false);

        return result.Items ?? [];
    }

    private async Task PatchRunProgressAsync(Guid runId, int progressPercent, string? message)
    {
        var patch = PatchRequestBuilder.ForId(runId).SetProperty("ProgressPercent", progressPercent).SetProperty("LastHeartbeatUtc", DateTime.UtcNow);
        if (message is not null)
            patch.SetProperty("ProgressMessage", message);

        await _apiClient.PatchAsAsync<PatchRequest, object>(BuildUri($"{Constants.Rest.Job.Runs}/{runId}"), patch.Build(), null, _stoppingToken).ConfigureAwait(false);
    }

    private async Task<bool> FinalizeParentRunAsync(Guid parentRunId, IReadOnlyList<JobRunResultReq> results)
    {
        try {
            var finished = await _apiClient
                .PostAsAsync<IReadOnlyList<JobRunResultReq>, JobRunRes>(BuildUri(Constants.Rest.Job.RunFinished(parentRunId)), results, null, _stoppingToken)
                .ConfigureAwait(false);

            return finished is not null;
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to finalize parent run {ParentRunId}", parentRunId);
            return false;
        }
    }

    private async Task ProcessTriggersAsync(JobInfo jobInfo, JobRunRes triggeredByRun)
    {
        var triggers = jobInfo.Definition.JobTriggers ?? [];
        foreach (var trigger in triggers) {
            if (!trigger.Enabled)
                continue;

            var matchValue = triggeredByRun.GetResultValueAs<string?>(trigger.JobResultKey);
            if (matchValue != trigger.JobResultValue) {
                _logger.LogDebug("Trigger criteria does not match");
                continue;
            }

            var triggeringDef = await GetJobDefinitionAsync(trigger.TriggersJobDefinitionId, _stoppingToken).ConfigureAwait(false);
            if (triggeringDef == null || !triggeringDef.Enabled) {
                _logger.LogInformation("Triggered definition not found or disabled");
                continue;
            }

            var runReq = BuildRunRequest(triggeringDef.Id, null, trigger, triggeredByRun);

            // Deduplicate trigger firing across scheduler instances / redelivered completion messages: one triggered run per
            // (trigger, completed run) pair — a duplicate create resolves to the existing run.
            runReq.IdempotencyKey = $"trigger:{trigger.Id:N}:{triggeredByRun.Id:N}";
            var created = await _apiClient.PostAsAsync<JobRunReq, CreateResult<JobRunRes>>(BuildUri(Constants.Rest.Job.RunsCreate), runReq, null, _stoppingToken)
                .ConfigureAwait(false);
            if (created.IsSuccess && created.Data != null) {
                _metrics.IncrementCounter(Constants.Metrics.Scheduler.TriggersFired);
                _metrics.IncrementCounter(Constants.Metrics.Scheduler.RunsCreated);
                _logger.LogInformation("Created triggered job run {JobRunId}", created.Data.Id);
            }
            else if (created.Error?.Errors.Any(e => e.Code == ApiErrorCodes.Conflict) == true)
                _metrics.IncrementCounter(Constants.Metrics.Scheduler.SlotConflicts);
            else {
                _metrics.IncrementCounter(Constants.Metrics.Scheduler.RunCreateFailed);
                _logger.LogWarning("Failed to create triggered job run");
            }
        }
    }

    /// <summary>Builds the run request for <paramref name="definitionId" /> through the shared factory, using the definition's currently cached runs as template data.</summary>
    private JobRunReq BuildRunRequest(Guid definitionId, JobScheduleRes? schedule, JobTriggerRes? trigger, JobRunRes? triggeredBy)
        => _runRequestFactory.Build(_jobs[definitionId], schedule, trigger, triggeredBy);

    private string BuildUri(string path)
    {
        var baseUrl = _options.ApiBaseUrl.TrimEnd('/');
        var p = path.TrimStart('/');
        return $"{baseUrl}/{p}";
    }

    private async Task<JobRunRes?> GetJobRunAsync(Guid id, CancellationToken ct = default)
    {
        var include = string.Join("&include=", JobRunIncludes);
        try {
            return await _apiClient.GetAsAsync<JobRunRes>($"{BuildUri(Constants.Rest.Job.Runs)}/{id}?include={include}", null, ct).ConfigureAwait(false);
        }
        catch (ApiException ex) when (ex.StatusCode == 404) {
            return null;
        }
    }

    private async Task<JobDefinitionRes?> GetJobDefinitionAsync(Guid id, CancellationToken ct = default)
    {
        var include = string.Join("&include=", JobDefinitionIncludes);
        try {
            return await _apiClient.GetAsAsync<JobDefinitionRes>($"{BuildUri(Constants.Rest.Job.Definitions)}/{id}?include={include}", null, ct).ConfigureAwait(false);
        }
        catch (ApiException ex) when (ex.StatusCode == 404) {
            return null;
        }
    }
}