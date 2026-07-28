using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
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
using Lyo.Scheduler;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ApiErrorCodes = Lyo.Api.Models.Constants.ApiErrorCodes;
using Constants = Lyo.Job.Models.Constants;

namespace Lyo.Job.Scheduler;

/// <summary>
/// Polls job definitions, evaluates schedules, creates job runs via the Job API, and processes completed runs (triggers). Implements <see cref="BackgroundService" /> for
/// proper hosted-service lifetime management.
/// </summary>
public sealed class JobScheduler : BackgroundService, IJobScheduler, IHealth
{
    private static readonly string[] JobRunIncludes = [
        "JobRunParameters", "JobRunLogs", "JobRunResults", "JobSchedule", "JobTrigger", "JobTrigger.JobTriggerParameters", "JobDefinition", "JobDefinition.JobParameters",
        "JobDefinition.JobTriggerJobDefinitions.JobTriggerParameters"
    ];

    private static readonly string[] JobDefinitionIncludes = [
        "JobParameters", "JobSchedules", "JobSchedules.JobScheduleParameters", "JobTriggerJobDefinitions", "JobTriggerJobDefinitions.JobTriggerParameters",
        "JobParallelRestrictionBaseJobDefinitions", "JobParallelRestrictionBaseJobDefinitions.OtherJobDefinition"
    ];

    private static readonly Regex TemplatePlaceholderRegex = new(@"\{\{(.*?)\}\}", RegexOptions.Compiled);

    private readonly IApiClient _apiClient;

    /// <summary>
    /// In-memory consecutive failure counters per definition. Reset to 0 on any successful run; incremented on failure. When the counter reaches <c>CircuitBreakerThreshold</c>,
    /// the scheduler disables the definition via the API and clears the counter.
    /// </summary>
    private readonly Dictionary<Guid, int> _consecutiveFailures = new();

    private readonly SemaphoreSlim _definitionLock = new(1, 1);
    private readonly IJobEventPublisher _eventPublisher;
    private readonly IFormatterService _formatter;
    private readonly ILogger<JobScheduler> _logger;
    private readonly IMetrics _metrics;
    private readonly IMqService? _mqService;
    private readonly JobSchedulerOptions _options;
    private Dictionary<Guid, JobBlackoutCalendarRes> _blackoutCalendars = new();

    private Dictionary<Guid, JobInfo> _jobs = new();
    private DateTime? _lastDefinitionsRefreshUtc;
    private DateTime? _lastScheduleCheckUtc;

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
        _formatter = formatter;
        _eventPublisher = eventPublisher;
        _logger = logger ?? NullLogger<JobScheduler>.Instance;
        _metrics = metrics ?? NullMetrics.Instance;
        _mqService = mqService;
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
        await _eventPublisher.SetupAsync(stoppingToken).ConfigureAwait(false);
        await _eventPublisher.SubscribeToDefinitionUpdatesAsync(Constants.Mq.JobDefinitionChangeKey, OnDefinitionUpdatedAsync, stoppingToken).ConfigureAwait(false);
        await _eventPublisher.SubscribeToRunCompletionsAsync(OnJobRunCompleteAsync, stoppingToken).ConfigureAwait(false);
        try {
            await RefreshDefinitionsInternalAsync(stoppingToken).ConfigureAwait(false);
            await CheckSchedulesInternalAsync(stoppingToken).ConfigureAwait(false);
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

        await _definitionLock.WaitAsync().ConfigureAwait(false);
        try {
            var definition = await GetJobDefinitionAsync(definitionId.Value).ConfigureAwait(false);
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

            var jobInfo = await LoadJobInfoAsync(definition).ConfigureAwait(false);
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
        Guid? jobRunId;
        try {
            jobRunId = JsonSerializer.Deserialize<Guid>(body);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Could not parse job run complete message");
            return false;
        }

        var run = await GetJobRunAsync(jobRunId!.Value).ConfigureAwait(false);
        if (run != null)
            return await ProcessCompletedJobRunAsync(run).ConfigureAwait(false);

        _logger.LogWarning("Job run {JobRunId} not found", jobRunId);
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
            if (latestRuns is not null && latestRuns.TryGetValue(def.Id, out var latest))
                updated[def.Id] = new(def, latest.LastRun, latest.LastSuccessfulRun, latest.LastFailedRun);
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

            return latest?.ToDictionary(l => l.JobDefinitionId);
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
        if (!_eventPublisher.IsConnected()) {
            _logger.LogWarning("Skipping schedule check: event publisher disconnected");
            return;
        }

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
        if (!IsWithinScheduleWindow(schedule, now))
            return null;

        if (jobInfo.LastRun?.State is JobState.Queued or JobState.Running) {
            _logger.LogDebug("Job already queued or running");
            return null;
        }

        if (IsBlockedByParallelRestrictions(jobInfo))
            return null;

        var definition = schedule.ToScheduleDefinition() with { TimeZone = _options.TimeZone };
        var reference = JobScheduleReference.Resolve(
            jobInfo.LastSuccessfulRun?.StartedTimestamp, jobInfo.LastRun?.ScheduledSlotUtc, jobInfo.LastRun?.StartedTimestamp, jobInfo.LastRun?.CreatedTimestamp,
            schedule.StartDateUtc, now, _options.MisfireLookbackMinutes);

        var nextDue = ScheduleCalculator.GetNextRun(definition, reference);
        if (!nextDue.HasValue || nextDue.Value > now)
            return null;

        if (!IsWithinScheduleWindow(schedule, nextDue.Value))
            return null;

        var calendar = ResolveBlackoutCalendar(schedule);
        var adjusted = JobBlackoutCalendarEvaluator.AdjustSlotForBlackout(nextDue.Value, calendar, ResolveTimeZone(schedule));
        if (!adjusted.HasValue)
            return null;

        if (adjusted.Value > now)
            return null;

        _logger.LogInformation("Schedule due for definition {Name} (slot {Slot:u})", jobInfo.Definition.Name, adjusted.Value);
        return adjusted.Value;
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
                    if (!IsWithinScheduleWindow(schedule, now)) {
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

    private DateTime? FindMostRecentMissedSlot(JobInfo jobInfo, JobScheduleRes schedule, DateTime lookbackStart, DateTime now)
    {
        var definition = schedule.ToScheduleDefinition() with { TimeZone = _options.TimeZone };
        var reference = jobInfo.LastSuccessfulRun?.StartedTimestamp ?? lookbackStart;
        if (reference < lookbackStart)
            reference = lookbackStart;

        if (schedule.StartDateUtc.HasValue && schedule.StartDateUtc.Value > reference)
            reference = schedule.StartDateUtc.Value;

        DateTime? mostRecent = null;
        var cursor = reference;
        for (var i = 0; i < 10_000; i++) {
            var next = ScheduleCalculator.GetNextRun(definition, cursor);
            if (!next.HasValue || next.Value > now)
                break;

            if (next.Value >= lookbackStart && IsWithinScheduleWindow(schedule, next.Value)) {
                var calendar = ResolveBlackoutCalendar(schedule);
                var adjusted = JobBlackoutCalendarEvaluator.AdjustSlotForBlackout(next.Value, calendar, ResolveTimeZone(schedule));
                if (adjusted.HasValue)
                    mostRecent = adjusted.Value;
            }

            cursor = next.Value.AddMilliseconds(1);
        }

        return mostRecent;
    }

    private static bool IsWithinScheduleWindow(JobScheduleRes schedule, DateTime utcTime)
    {
        if (schedule.StartDateUtc.HasValue && utcTime < schedule.StartDateUtc.Value)
            return false;

        if (schedule.EndDateUtc.HasValue && utcTime > schedule.EndDateUtc.Value)
            return false;

        return true;
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
        return new(definition, lastRunTask.Result.Items?.FirstOrDefault(), lastSuccessTask.Result.Items?.FirstOrDefault(), lastFailedTask.Result?.Items?.FirstOrDefault());
    }

    private async Task ProcessScheduledJobDefinitionAsync(JobDefinitionRes definition, JobScheduleRes schedule, DateTime scheduledSlot, CancellationToken ct)
        => await CreateScheduledRunAsync(_jobs[definition.Id], schedule, scheduledSlot, ct).ConfigureAwait(false);

    /// <summary>Posts a scheduled job run and updates in-memory state. Returns true when a new run was created.</summary>
    private async Task<bool> CreateScheduledRunAsync(JobInfo jobInfo, JobScheduleRes schedule, DateTime scheduledSlot, CancellationToken ct)
    {
        var definition = jobInfo.Definition;
        if (!_jobs.TryGetValue(definition.Id, out jobInfo))
            return false;

        var runReq = BuildRunRequest(definition.Id, schedule, null, null);
        runReq.JobScheduleId = schedule.Id;
        runReq.ScheduledSlotUtc = scheduledSlot;
        _logger.LogDebug("Creating job run: {Request}", runReq);
        try {
            var created = await _apiClient.PostAsAsync<JobRunReq, CreateResult<JobRunRes>>(BuildUri(Constants.Rest.Job.RunsCreate), runReq, null, ct).ConfigureAwait(false);
            if (created.IsSuccess && created.Data != null) {
                _logger.LogInformation("Created job run {JobRunId}", created.Data.Id);
                _metrics.IncrementCounter(Constants.Metrics.Scheduler.RunsCreated);
                _jobs = new(_jobs) { [definition.Id] = jobInfo with { LastRun = created.Data } };
                return true;
            }

            if (created.Error?.Errors?.Any(e => e.Code == ApiErrorCodes.Conflict) == true) {
                _metrics.IncrementCounter(Constants.Metrics.Scheduler.SlotConflicts);
                _logger.LogDebug("Job run for slot {Slot:u} already exists (created by another instance)", scheduledSlot);
                MarkSlotAttempted(definition.Id, jobInfo, scheduledSlot);
                return false;
            }

            _metrics.IncrementCounter(Constants.Metrics.Scheduler.RunCreateFailed);
            _logger.LogWarning("Failed to create job run: {Error}", created?.Error);
            MarkSlotAttempted(definition.Id, jobInfo, scheduledSlot);
            return false;
        }
        catch (ApiException ex) {
            // Do not let a single create failure abort the whole schedule-check loop (past-due spam).
            if (ex.StatusCode is 409) {
                _metrics.IncrementCounter(Constants.Metrics.Scheduler.SlotConflicts);
                _logger.LogDebug(ex, "Job run for slot {Slot:u} already exists (created by another instance)", scheduledSlot);
                MarkSlotAttempted(definition.Id, jobInfo, scheduledSlot);
                return false;
            }

            _metrics.IncrementCounter(Constants.Metrics.Scheduler.RunCreateFailed);
            _logger.LogWarning(ex, "Failed to create job run for slot {Slot:u}", scheduledSlot);
            MarkSlotAttempted(definition.Id, jobInfo, scheduledSlot);
            return false;
        }
    }

    /// <summary>Advances in-memory schedule progress past <paramref name="scheduledSlot" /> when create fails, so the next check does not retry the same past-due slot forever.</summary>
    private void MarkSlotAttempted(Guid definitionId, JobInfo jobInfo, DateTime scheduledSlot)
    {
        var placeholder = (jobInfo.LastRun ?? new JobRunRes { JobDefinitionId = definitionId, State = JobState.Finished }) with {
            ScheduledSlotUtc = scheduledSlot, State = JobState.Finished, Result = JobRunResult.Failure
        };

        _jobs = new(_jobs) { [definitionId] = jobInfo with { LastRun = placeholder } };
    }

    /// <returns>false to ack the MQ message; true to requeue. Missing cache entries are acked so they do not loop forever.</returns>
    private async Task<bool> ProcessCompletedJobRunAsync(JobRunRes run)
    {
        // Serialize with definition refresh/schedule checks: completion handling mutates _jobs and _consecutiveFailures, and a concurrent
        // refresh could otherwise overwrite (or race with) the LastRun/LastFailedRun updates made here.
        await _definitionLock.WaitAsync().ConfigureAwait(false);
        try {
            return await ProcessCompletedJobRunLockedAsync(run).ConfigureAwait(false);
        }
        finally {
            _definitionLock.Release();
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
        if (run.Result is JobRunResult.Success or JobRunResult.SuccessWithWarnings or JobRunResult.PartialSuccess)
            _consecutiveFailures.Remove(run.JobDefinitionId);
        else if (IsFailureOutcome(run.Result)) {
            _consecutiveFailures.TryGetValue(run.JobDefinitionId, out var prev);
            var next = prev + 1;
            _consecutiveFailures[run.JobDefinitionId] = next;
            var threshold = jobInfo.Definition.CircuitBreakerThreshold;
            if (threshold > 0 && next >= threshold) {
                _logger.LogWarning(
                    "Circuit breaker tripped for {Name} ({DefinitionId}) after {Failures} consecutive failure(s)", jobInfo.Definition.Name, run.JobDefinitionId, next);

                await TripCircuitBreakerAsync(run.JobDefinitionId).ConfigureAwait(false);
                _metrics.IncrementCounter(Constants.Metrics.Scheduler.CircuitBreakerTripped);
                if (_eventPublisher.IsConnected()) {
                    await _eventPublisher.PublishAlertAsync(
                            run.JobDefinitionId, run.Id, JobAlertType.CircuitBreakerTripped,
                            $"Circuit breaker tripped for '{jobInfo.Definition.Name}' after {next} consecutive failure(s)")
                        .ConfigureAwait(false);
                }

                _consecutiveFailures.Remove(run.JobDefinitionId);
            }
            else if (jobInfo.Definition is { AlertOnFailure: true } && _eventPublisher.IsConnected()) {
                var alertThreshold = jobInfo.Definition.AlertAfterConsecutiveFailures;
                if (alertThreshold <= 0 || next >= alertThreshold) {
                    await _eventPublisher.PublishAlertAsync(
                            run.JobDefinitionId, run.Id, JobAlertType.Failure, $"Job '{jobInfo.Definition.Name}' failed ({next} consecutive failure(s))")
                        .ConfigureAwait(false);
                }
            }
        }

        // Schedule a retry if the run failed (or timed out) and the definition allows it.
        if (IsFailureOutcome(run.Result) && jobInfo.Definition.MaxRetryCount > 0 && run.RetryAttempt < jobInfo.Definition.MaxRetryCount)
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

        var created = await _apiClient.PostAsAsync<JobRunReq, CreateResult<JobRunRes>>(BuildUri(Constants.Rest.Job.RunsCreate), retryReq).ConfigureAwait(false);
        if (created.IsSuccess && created.Data != null) {
            _metrics.IncrementCounter(Constants.Metrics.Scheduler.RetriesScheduled);
            _logger.LogInformation("Created retry job run {JobRunId} (attempt {Attempt})", created.Data.Id, nextAttempt);
            if (useDelayedMq && _mqService is IDelayedMqService delayedMq) {
                var queue = Constants.Mq.QueueGetJobRunCreated(jobInfo.Definition.WorkerType);
                var envelope = new QueueMessageEnvelope<Guid>(created.Data.Id, 0, Guid.NewGuid().ToString("D"), DateTime.UtcNow);
                var bytes = JsonSerializer.SerializeToUtf8Bytes(envelope);
                await delayedMq.SendToQueueDelayed(queue, bytes, TimeSpan.FromSeconds(backoffSeconds)).ConfigureAwait(false);
            }
        }
        else if (created.Error?.Errors?.Any(e => e.Code == ApiErrorCodes.Conflict) == true)
            _metrics.IncrementCounter(Constants.Metrics.Scheduler.SlotConflicts);
        else {
            _metrics.IncrementCounter(Constants.Metrics.Scheduler.RunCreateFailed);
            _logger.LogWarning("Failed to create retry job run for definition {Name}: {Error}", jobInfo.Definition.Name, created.Error);
        }
    }

    /// <summary>Whether a run outcome counts as a failure for retry, circuit breaker, and alerting purposes (Failure and Timeout; not Cancelled).</summary>
    private static bool IsFailureOutcome(JobRunResult? result) => result is JobRunResult.Failure or JobRunResult.Timeout;

    private async Task TripCircuitBreakerAsync(Guid definitionId)
    {
        try {
            var patch = PatchRequestBuilder.ForId(definitionId).SetProperty("Enabled", false).SetProperty("CircuitBreakerTrippedAt", DateTime.UtcNow).Build();
            await _apiClient.PatchAsAsync<PatchRequest, object>(BuildUri($"{Constants.Rest.Job.Definitions}/{definitionId}"), patch).ConfigureAwait(false);

            // Remove from in-memory cache so the scheduler stops firing this definition.
            var updated = new Dictionary<Guid, JobInfo>(_jobs);
            updated.Remove(definitionId);
            _jobs = updated;
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to trip circuit breaker for definition {DefinitionId}", definitionId);
        }
    }

    private async Task ProcessChildRunCompletionAsync(JobRunRes childRun)
    {
        var parentId = childRun.ParentJobRunId!.Value;
        var siblings = await GetChildRunsAsync(parentId).ConfigureAwait(false);
        if (siblings.Count == 0)
            return;

        var finished = siblings.Where(r => r.State == JobState.Finished).ToList();
        var progress = (int)Math.Round(finished.Count * 100.0 / siblings.Count);
        await PatchRunProgressAsync(parentId, progress, $"{finished.Count}/{siblings.Count} children complete").ConfigureAwait(false);
        if (finished.Count < siblings.Count)
            return;

        var parent = await GetJobRunAsync(parentId).ConfigureAwait(false);
        if (parent is null || parent.State == JobState.Finished)
            return;

        var aggregated = AggregateChildResults(siblings);
        if (!await FinalizeParentRunAsync(parentId, aggregated).ConfigureAwait(false))
            return;
    }

    private async Task<IReadOnlyList<JobRunRes>> GetChildRunsAsync(Guid parentRunId)
    {
        var where = WhereClauseBuilder.Condition("ParentJobRunId", ComparisonOperatorEnum.Equals, parentRunId.ToString());
        var result = await _apiClient.PostAsAsync<QueryConcreteReq, QueryRes<JobRunRes>>(
                BuildUri(Constants.Rest.Job.RunsQuery), new QueryConcreteReqBuilder().AddIncludes("JobRunResults").AddWhere(where).Build())
            .ConfigureAwait(false);

        return result.Items ?? [];
    }

    private async Task PatchRunProgressAsync(Guid runId, int progressPercent, string? message)
    {
        var patch = PatchRequestBuilder.ForId(runId).SetProperty("ProgressPercent", progressPercent).SetProperty("LastHeartbeatUtc", DateTime.UtcNow);
        if (message is not null)
            patch.SetProperty("ProgressMessage", message);

        await _apiClient.PatchAsAsync<PatchRequest, object>(BuildUri($"{Constants.Rest.Job.Runs}/{runId}"), patch.Build()).ConfigureAwait(false);
    }

    private static IReadOnlyList<JobRunResultReq> AggregateChildResults(IReadOnlyList<JobRunRes> children)
    {
        var anyFailure = children.Any(c => c.Result == JobRunResult.Failure);
        var anyPartial = children.Any(c => c.Result == JobRunResult.PartialSuccess);
        var anyWarning = children.Any(c => c.Result == JobRunResult.SuccessWithWarnings);
        var outcome = anyFailure ? JobRunResult.Failure : anyPartial ? JobRunResult.PartialSuccess : anyWarning ? JobRunResult.SuccessWithWarnings : JobRunResult.Success;
        var results = new List<JobRunResultReq> {
            new(Constants.Data.JobRunResultKey.Result, JobParameterType.String, outcome.ToString()), new("ChildCount", JobParameterType.Int, children.Count)
        };

        var totalCreate = children.Sum(c => c.GetResultValueAs<int?>(Constants.Data.JobRunResultKey.CreateCount) ?? 0);
        var totalUpdate = children.Sum(c => c.GetResultValueAs<int?>(Constants.Data.JobRunResultKey.UpdateCount) ?? 0);
        var totalDelete = children.Sum(c => c.GetResultValueAs<int?>(Constants.Data.JobRunResultKey.DeleteCount) ?? 0);
        var totalFailed = children.Sum(c => c.GetResultValueAs<int?>(Constants.Data.JobRunResultKey.FailedCount) ?? 0);
        if (totalCreate > 0)
            results.Add(new(Constants.Data.JobRunResultKey.CreateCount, JobParameterType.Int, totalCreate));

        if (totalUpdate > 0)
            results.Add(new(Constants.Data.JobRunResultKey.UpdateCount, JobParameterType.Int, totalUpdate));

        if (totalDelete > 0)
            results.Add(new(Constants.Data.JobRunResultKey.DeleteCount, JobParameterType.Int, totalDelete));

        if (totalFailed > 0)
            results.Add(new(Constants.Data.JobRunResultKey.FailedCount, JobParameterType.Int, totalFailed));

        return results;
    }

    private async Task<bool> FinalizeParentRunAsync(Guid parentRunId, IReadOnlyList<JobRunResultReq> results)
    {
        try {
            var finished = await _apiClient.PostAsAsync<IReadOnlyList<JobRunResultReq>, JobRunRes>(BuildUri(Constants.Rest.Job.RunFinished(parentRunId)), results)
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

            var triggeringDef = await GetJobDefinitionAsync(trigger.TriggersJobDefinitionId).ConfigureAwait(false);
            if (triggeringDef == null || !triggeringDef.Enabled) {
                _logger.LogInformation("Triggered definition not found or disabled");
                continue;
            }

            var runReq = BuildRunRequest(triggeringDef.Id, null, trigger, triggeredByRun);

            // Deduplicate trigger firing across scheduler instances / redelivered completion messages: one triggered run per
            // (trigger, completed run) pair — a duplicate create resolves to the existing run.
            runReq.IdempotencyKey = $"trigger:{trigger.Id:N}:{triggeredByRun.Id:N}";
            var created = await _apiClient.PostAsAsync<JobRunReq, CreateResult<JobRunRes>>(BuildUri(Constants.Rest.Job.RunsCreate), runReq).ConfigureAwait(false);
            if (created.IsSuccess && created.Data != null) {
                _metrics.IncrementCounter(Constants.Metrics.Scheduler.TriggersFired);
                _metrics.IncrementCounter(Constants.Metrics.Scheduler.RunsCreated);
                _logger.LogInformation("Created triggered job run {JobRunId}", created.Data.Id);
            }
            else if (created.Error?.Errors?.Any(e => e.Code == ApiErrorCodes.Conflict) == true)
                _metrics.IncrementCounter(Constants.Metrics.Scheduler.SlotConflicts);
            else {
                _metrics.IncrementCounter(Constants.Metrics.Scheduler.RunCreateFailed);
                _logger.LogWarning("Failed to create triggered job run");
            }
        }
    }

    private JobRunReq BuildRunRequest(Guid definitionId, JobScheduleRes? schedule, JobTriggerRes? trigger, JobRunRes? triggeredBy)
    {
        var jobInfo = _jobs[definitionId];
        var req = new JobRunReq {
            JobDefinitionId = definitionId,
            JobScheduleId = schedule?.Id,
            JobTriggerId = trigger?.Id,
            TriggeredByJobRunId = triggeredBy?.Id,
            AllowTriggers = true,
            CreatedBy = _options.CreatedBy,
            JobRunParameters = []
        };

        var templateData = BuildTemplateData(jobInfo, trigger, triggeredBy, schedule);
        foreach (var p in jobInfo.Definition.JobParameters ?? [])
            req.JobRunParameters.Add(CreateRunParameterFromDefinition(p, templateData));

        // Schedule parameters override definition defaults by key (e.g. a required parameter with a null definition
        // default whose value is supplied per schedule). Replace rather than append so the API does not see a
        // duplicate key carrying the empty definition default alongside the schedule value.
        foreach (var sp in schedule?.Parameters ?? []) {
            if (!sp.Enabled)
                continue;

            var runParam = CreateRunParameterFromSchedule(sp, templateData);
            req.JobRunParameters.RemoveAll(existing => string.Equals(existing.Key, runParam.Key, StringComparison.OrdinalIgnoreCase));
            req.JobRunParameters.Add(runParam);
        }

        foreach (var tp in trigger?.TriggerParameters ?? [])
            req.JobRunParameters.Add(CreateRunParameterFromTrigger(tp, templateData));

        return req;
    }

    private Dictionary<string, object?> BuildTemplateData(JobInfo jobInfo, JobTriggerRes? trigger, JobRunRes? triggeredBy, JobScheduleRes? schedule)
    {
        var data = new Dictionary<string, object?> {
            ["Definition"] = jobInfo.Definition,
            ["LastRun"] = jobInfo.LastRun,
            ["LastSuccessfulRun"] = jobInfo.LastSuccessfulRun,
            ["LastFailedRun"] = jobInfo.LastFailedRun,
            ["Trigger"] = trigger,
            ["TriggeredByRun"] = triggeredBy,
            ["Schedule"] = schedule
        };

        AddRunTemplateData(data, "LastRun", jobInfo.LastRun);
        AddRunTemplateData(data, "LastSuccessfulRun", jobInfo.LastSuccessfulRun);
        AddRunTemplateData(data, "LastFailedRun", jobInfo.LastFailedRun);
        AddRunTemplateData(data, "TriggeredByRun", triggeredBy);
        foreach (var sp in schedule?.Parameters ?? []) {
            if (sp.Enabled)
                data[$"Schedule_Parameter_{sp.Key}"] = sp.Value;
        }

        if (trigger?.TriggerParameters == null)
            return data;

        foreach (var tp in trigger.TriggerParameters)
            data[$"Trigger_Parameter_{tp.Key}"] = tp.Value;

        return data;
    }

    private static void AddRunTemplateData(IDictionary<string, object?> data, string prefix, JobRunRes? run)
    {
        if (run == null)
            return;

        var results = run.GetResultDictionary();
        var parameters = run.GetParameterDictionary();
        foreach (var kvp in results)
            data[$"{prefix}_Result_{kvp.Key}"] = kvp.Value;

        foreach (var kvp in parameters)
            data[$"{prefix}_Parameter_{kvp.Key}"] = kvp.Value;
    }

    private JobRunParameterReq CreateRunParameterFromDefinition(JobParameterRes p, Dictionary<string, object?> templateData)
    {
        var req = new JobRunParameterReq { Key = p.Key, Description = p.Description, Type = p.Type };
        switch (p.Type) {
            case JobParameterType.String:
            case JobParameterType.Json:
                req.Value = FormatTemplateValue(p.Value ?? "", templateData);
                req.Type = p.Type == JobParameterType.Json ? JobParameterType.Json : JobParameterType.String;
                break;
            default:
                req.Value = p.Value;
                break;
        }

        return req;
    }

    private JobRunParameterReq CreateRunParameterFromSchedule(JobScheduleParameterRes p, Dictionary<string, object?> templateData)
    {
        var req = new JobRunParameterReq {
            Key = p.Key,
            Description = p.Description,
            Type = p.Type,
            EncryptedValue = p.EncryptedValue
        };

        switch (p.Type) {
            case JobParameterType.String:
            case JobParameterType.Json:
                req.Value = FormatTemplateValue(p.Value ?? "", templateData);
                break;
            default:
                req.Value = p.Value;
                break;
        }

        return req;
    }

    private JobRunParameterReq CreateRunParameterFromTrigger(JobTriggerParameterRes p, Dictionary<string, object?> templateData)
    {
        var req = new JobRunParameterReq { Key = p.Key, Description = p.Description, Type = p.Type };
        switch (p.Type) {
            case JobParameterType.String:
            case JobParameterType.Json:
                req.Value = FormatTemplateValue(p.Value ?? "", templateData);
                req.Type = p.Type == JobParameterType.Json ? JobParameterType.Json : JobParameterType.String;
                break;
            default:
                req.Value = p.Value;
                break;
        }

        return req;
    }

    private string FormatTemplateValue(string template, Dictionary<string, object?> templateData)
        => TemplatePlaceholderRegex.Replace(
            template, match => {
                var expr = match.Groups[1].Value;
                try {
                    return _formatter.Format("{" + expr + "}", templateData);
                }
                catch (Exception ex) {
                    _logger.LogWarning(ex, "Template format failed: {Expression}", expr);
                    return match.Value;
                }
            });

    private string BuildUri(string path)
    {
        var baseUrl = _options.ApiBaseUrl.TrimEnd('/');
        var p = path.TrimStart('/');
        return $"{baseUrl}/{p}";
    }

    private async Task<JobRunRes?> GetJobRunAsync(Guid id)
    {
        var include = string.Join("&include=", JobRunIncludes);
        try {
            return await _apiClient.GetAsAsync<JobRunRes>($"{BuildUri(Constants.Rest.Job.Runs)}/{id}?include={include}").ConfigureAwait(false);
        }
        catch (ApiException ex) when (ex.StatusCode == 404) {
            return null;
        }
    }

    private async Task<JobDefinitionRes?> GetJobDefinitionAsync(Guid id)
    {
        var include = string.Join("&include=", JobDefinitionIncludes);
        try {
            return await _apiClient.GetAsAsync<JobDefinitionRes>($"{BuildUri(Constants.Rest.Job.Definitions)}/{id}?include={include}").ConfigureAwait(false);
        }
        catch (ApiException ex) when (ex.StatusCode == 404) {
            return null;
        }
    }
}