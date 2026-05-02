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
        "JobRunParameters", "JobRunLogs", "JobRunResults", "JobSchedule", "JobTrigger", "JobTriggerTriggersJobDefinitions", "JobDefinition", "JobDefinition.JobParameters",
        "JobDefinition.JobTriggerJobDefinitions.JobTriggerParameters"
    ];

    private static readonly string[] JobDefinitionIncludes = [
        "JobParameters", "JobSchedules", "JobTriggerJobDefinitions", "JobTriggerJobDefinitions.JobTriggerParameters", "JobParallelRestrictions"
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
    private readonly JobSchedulerOptions _options;

    private Dictionary<Guid, JobInfo> _jobs = new();
    private DateTime? _lastDefinitionsRefreshUtc;
    private DateTime? _lastScheduleCheckUtc;

    public JobScheduler(JobSchedulerOptions options, IApiClient apiClient, IFormatterService formatter, IJobEventPublisher eventPublisher, ILogger<JobScheduler>? logger = null)
    {
        ArgumentHelpers.ThrowIfNull(options);
        ArgumentHelpers.ThrowIfNull(apiClient);
        _options = options;
        _apiClient = apiClient;
        _formatter = formatter;
        _eventPublisher = eventPublisher;
        _logger = logger ?? NullLogger<JobScheduler>.Instance;
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
        await RefreshDefinitionsInternalAsync(stoppingToken).ConfigureAwait(false);
        await CheckSchedulesInternalAsync(stoppingToken).ConfigureAwait(false);
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
                _logger.LogError(ex, "Schedule check failed");
            }
        }
    }

    private async Task<bool> OnDefinitionUpdatedAsync(byte[] body)
    {
        Guid? definitionId = null;
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
                _logger.LogWarning("Definition {DefinitionId} not found", definitionId);
                return true;
            }

            if (!definition.Enabled) {
                var updated = new Dictionary<Guid, JobInfo>(_jobs);
                updated.Remove(definitionId.Value);
                _jobs = updated;
                _logger.LogDebug("Removed disabled definition {DefinitionId}", definitionId);
                return true;
            }

            var jobInfo = await LoadJobInfoAsync(definition).ConfigureAwait(false);
            var updatedJobs = new Dictionary<Guid, JobInfo>(_jobs) { [definitionId.Value] = jobInfo };
            _jobs = updatedJobs;
            _logger.LogInformation("Refreshed definition {DefinitionId} ({Name})", definitionId, definition.Name);
            return true;
        }
        finally {
            _definitionLock.Release();
        }
    }

    private async Task<bool> OnJobRunCompleteAsync(byte[] body)
    {
        Guid? jobRunId = null;
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
        _logger.LogInformation("Updating job definitions");
        var query = new QueryReqBuilder().AddIncludes(JobDefinitionIncludes).Build();
        var results = await _apiClient.PostAsAsync<QueryReq, QueryRes<JobDefinitionRes>>(BuildUri(Constants.Rest.Job.DefinitionsQuery), query, null, ct).ConfigureAwait(false);
        if (results?.Items == null || !results.IsSuccess) {
            _logger.LogWarning("No definitions loaded or query failed");
            return;
        }

        var updated = new Dictionary<Guid, JobInfo>();
        foreach (var def in results.Items) {
            if (!def.Enabled) {
                _logger.LogDebug("Skipping disabled definition {Name}", def.Name);
                continue;
            }

            var jobInfo = await LoadJobInfoAsync(def, ct).ConfigureAwait(false);
            updated[def.Id] = jobInfo;
        }

        _jobs = updated;
    }

    /// <summary>Evaluates schedules and creates job runs where due. Must be called under the definition lock.</summary>
    private async Task CheckSchedulesInternalAsync(CancellationToken ct = default)
    {
        _lastScheduleCheckUtc = DateTime.UtcNow;
        if (!_eventPublisher.IsConnected()) {
            _logger.LogWarning("Skipping schedule check: event publisher disconnected");
            return;
        }

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

        if (jobInfo.LastRun?.State is JobState.Queued or JobState.Running) {
            _logger.LogDebug("Job already queued or running");
            return null;
        }

        // Enforce parallel restrictions
        var restrictions = jobInfo.Definition.JobParallelRestrictions;
        if (restrictions != null) {
            foreach (var restriction in restrictions) {
                if (!restriction.Enabled)
                    continue;

                if (_jobs.TryGetValue(restriction.OtherJobDefinitionId, out var otherJob) && otherJob.LastRun?.State is JobState.Queued or JobState.Running) {
                    _logger.LogDebug(
                        "Job {Name} blocked by parallel restriction with {OtherName}", jobInfo.Definition.Name,
                        restriction.OtherJobDefinition?.Name ?? restriction.OtherJobDefinitionId.ToString());

                    return null;
                }
            }
        }

        var definition = schedule.ToScheduleDefinition() with { TimeZone = _options.TimeZone };
        var lastRunTime = jobInfo.LastSuccessfulRun?.StartedTimestamp;
        var nextDue = ScheduleCalculator.GetNextRun(definition, lastRunTime ?? DateTime.UtcNow.AddYears(-10));
        if (!nextDue.HasValue || nextDue.Value > DateTime.UtcNow)
            return null;

        _logger.LogInformation("Schedule due for definition {Name} (slot {Slot:u})", jobInfo.Definition.Name, nextDue.Value);
        return nextDue.Value;
    }

    private async Task<JobInfo> LoadJobInfoAsync(JobDefinitionRes definition, CancellationToken ct = default)
    {
        var baseWhere = WhereClauseBuilder.Condition("JobDefinitionId", ComparisonOperatorEnum.Equals, definition.Id.ToString());
        var lastRunTask = _apiClient.PostAsAsync<QueryReq, QueryRes<JobRunRes>>(
            BuildUri(Constants.Rest.Job.RunsQuery), new QueryReqBuilder().AddIncludes(JobRunIncludes).AddWhere(baseWhere).AddSort("CreatedTimestamp").First().Build(), null, ct);

        var successFilter = WhereClauseBuilder.CombineAs(
            GroupOperatorEnum.And, baseWhere,
            WhereClauseBuilder.Condition("Result", ComparisonOperatorEnum.In, new[] { nameof(JobRunResult.Success), nameof(JobRunResult.SuccessWithWarnings) }));

        var lastSuccessTask = _apiClient.PostAsAsync<QueryReq, QueryRes<JobRunRes>>(
            BuildUri(Constants.Rest.Job.RunsQuery), new QueryReqBuilder().AddIncludes(JobRunIncludes).AddWhere(successFilter).AddSort("CreatedTimestamp").First().Build(), null,
            ct);

        var failFilter = WhereClauseBuilder.CombineAs(
            GroupOperatorEnum.And, baseWhere, WhereClauseBuilder.Condition("Result", ComparisonOperatorEnum.Equals, nameof(JobRunResult.Failure)));

        var lastFailedTask = _apiClient.PostAsAsync<QueryReq, QueryRes<JobRunRes>>(
            BuildUri(Constants.Rest.Job.RunsQuery), new QueryReqBuilder().AddIncludes(JobRunIncludes).AddWhere(failFilter).AddSort("CreatedTimestamp").First().Build(), null, ct);

        await Task.WhenAll(lastRunTask, lastSuccessTask, lastFailedTask).ConfigureAwait(false);
        return new(definition, lastRunTask.Result?.Items?.FirstOrDefault(), lastSuccessTask.Result?.Items?.FirstOrDefault(), lastFailedTask.Result?.Items?.FirstOrDefault());
    }

    private async Task ProcessScheduledJobDefinitionAsync(JobDefinitionRes definition, JobScheduleRes schedule, DateTime scheduledSlot, CancellationToken ct)
    {
        if (!_jobs.TryGetValue(definition.Id, out var jobInfo))
            return;

        var runReq = BuildRunRequest(definition.Id, schedule, null, null);
        runReq.JobScheduleId = schedule.Id;
        runReq.ScheduledSlotUtc = scheduledSlot;
        _logger.LogDebug("Creating job run: {Request}", runReq);
        var created = await _apiClient.PostAsAsync<JobRunReq, CreateResult<JobRunRes>>(BuildUri(Constants.Rest.Job.Runs), runReq, null, ct).ConfigureAwait(false);
        if (created?.IsSuccess == true && created.Data != null) {
            _logger.LogInformation("Created job run {JobRunId}", created.Data.Id);
            _jobs = new(_jobs) { [definition.Id] = jobInfo with { LastRun = created.Data } };
        }
        else if (created?.Error?.Errors?.Any(e => e.Code == ApiErrorCodes.Conflict) == true)
            // Another scheduler instance already created a run for this slot — idempotent, not an error
            _logger.LogDebug("Job run for slot {Slot:u} already exists (created by another instance)", scheduledSlot);
        else
            _logger.LogWarning("Failed to create job run: {Error}", created?.Error);
    }

    private async Task<bool> ProcessCompletedJobRunAsync(JobRunRes run)
    {
        if (!_jobs.TryGetValue(run.JobDefinitionId, out var jobInfo)) {
            _logger.LogWarning("No job info for definition {DefinitionId}", run.JobDefinitionId);
            return true;
        }

        var updatedInfo = jobInfo with { LastRun = run };
        var resultStr = run.GetResultValueAs<string?>(Constants.Data.JobRunResultKey.Result);
        if (resultStr is "Success" or "PartialSuccess" or "SuccessWithWarnings")
            updatedInfo = updatedInfo with { LastSuccessfulRun = run };
        else
            updatedInfo = updatedInfo with { LastFailedRun = run };

        _jobs = new(_jobs) { [run.JobDefinitionId] = updatedInfo };

        // Update circuit breaker counter.
        if (run.Result is JobRunResult.Success or JobRunResult.SuccessWithWarnings or JobRunResult.PartialSuccess)
            _consecutiveFailures.Remove(run.JobDefinitionId);
        else if (run.Result == JobRunResult.Failure) {
            _consecutiveFailures.TryGetValue(run.JobDefinitionId, out var prev);
            var next = prev + 1;
            _consecutiveFailures[run.JobDefinitionId] = next;
            var threshold = jobInfo.Definition.CircuitBreakerThreshold;
            if (threshold > 0 && next >= threshold) {
                _logger.LogWarning(
                    "Circuit breaker tripped for {Name} ({DefinitionId}) after {Failures} consecutive failure(s)", jobInfo.Definition.Name, run.JobDefinitionId, next);

                await TripCircuitBreakerAsync(run.JobDefinitionId).ConfigureAwait(false);
                _consecutiveFailures.Remove(run.JobDefinitionId);
            }
        }

        // Schedule a retry if the run failed and the definition allows it.
        if (run.Result == JobRunResult.Failure && jobInfo.Definition.MaxRetryCount > 0 && run.RetryAttempt < jobInfo.Definition.MaxRetryCount)
            await ScheduleRetryAsync(jobInfo, run).ConfigureAwait(false);

        if (!run.AllowTriggers || jobInfo.Definition.JobTriggers?.Count == 0)
            return true;

        await ProcessTriggersAsync(updatedInfo, run).ConfigureAwait(false);
        return true;
    }

    private async Task ScheduleRetryAsync(JobInfo jobInfo, JobRunRes failedRun)
    {
        var nextAttempt = failedRun.RetryAttempt + 1;
        var backoffSeconds = jobInfo.Definition.RetryBackoffSeconds * nextAttempt;
        _logger.LogInformation(
            "Scheduling retry attempt {Attempt}/{Max} for definition {Name} (backoff {Backoff}s)", nextAttempt, jobInfo.Definition.MaxRetryCount, jobInfo.Definition.Name,
            backoffSeconds);

        var retryReq = BuildRunRequest(failedRun.JobDefinitionId, null, null, null);
        retryReq.RetryAttempt = nextAttempt;
        retryReq.ReRanFromJobRunId = failedRun.Id;

        // Use ScheduledSlotUtc as the "not-before" hint if backoff > 0; the worker can respect this or ignore it
        if (backoffSeconds > 0)
            retryReq.ScheduledSlotUtc = DateTime.UtcNow.AddSeconds(backoffSeconds);

        var created = await _apiClient.PostAsAsync<JobRunReq, CreateResult<JobRunRes>>(BuildUri(Constants.Rest.Job.Runs), retryReq).ConfigureAwait(false);
        if (created?.IsSuccess == true)
            _logger.LogInformation("Created retry job run {JobRunId} (attempt {Attempt})", created.Data!.Id, nextAttempt);
        else
            _logger.LogWarning("Failed to create retry job run for definition {Name}: {Error}", jobInfo.Definition.Name, created?.Error);
    }

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
            var created = await _apiClient.PostAsAsync<JobRunReq, CreateResult<JobRunRes>>(BuildUri(Constants.Rest.Job.Runs), runReq).ConfigureAwait(false);
            if (created?.IsSuccess == true)
                _logger.LogInformation("Created triggered job run {JobRunId}", created.Data!.Id);
            else
                _logger.LogWarning("Failed to create triggered job run");
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
        return await _apiClient.GetAsAsync<JobRunRes>($"{BuildUri(Constants.Rest.Job.Runs)}/{id}?include={include}").ConfigureAwait(false);
    }

    private async Task<JobDefinitionRes?> GetJobDefinitionAsync(Guid id)
    {
        var include = string.Join("&include=", JobDefinitionIncludes);
        return await _apiClient.GetAsAsync<JobDefinitionRes>($"{BuildUri(Constants.Rest.Job.Definitions)}/{id}?include={include}").ConfigureAwait(false);
    }
}