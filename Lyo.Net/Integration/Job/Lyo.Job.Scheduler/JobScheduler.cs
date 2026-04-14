using System.Text.Json;
using System.Text.RegularExpressions;
using Lyo.Api.Client;
using Lyo.Api.Models.Common.Response;
using Lyo.Common.Enums;
using Lyo.Exceptions;
using Lyo.Formatter;
using Lyo.Job.Models;
using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Request;
using Lyo.Job.Models.Response;
using Lyo.MessageQueue.RabbitMq;
using Lyo.Query.Models.Builders;
using Lyo.Query.Models.Common.Request;
using Lyo.Query.Models.Enums;
using Lyo.Schedule.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Constants = Lyo.Job.Models.Constants;

namespace Lyo.Job.Scheduler;

/// <summary>
/// Polls job definitions, evaluates schedules, creates job runs via the Job API, and processes completed runs (triggers). Uses ApiClient for HTTP, DateAndTime for schedule
/// checks, Formatter for parameter templates, and MessageQueue for job completion events.
/// </summary>
public sealed class JobScheduler
{
    private static readonly string[] JobRunIncludes = [
        "JobRunParameters", "JobRunLogs", "JobRunResults", "JobSchedule", "JobTrigger", "JobTriggerTriggersJobDefinitions", "JobDefinition", "JobDefinition.JobParameters",
        "JobDefinition.JobTriggerJobDefinitions.JobTriggerParameters"
    ];

    private static readonly string[] JobDefinitionIncludes = ["JobParameters", "JobSchedules", "JobTriggerJobDefinitions", "JobTriggerJobDefinitions.JobTriggerParameters"];

    private static readonly Regex TemplatePlaceholderRegex = new(@"\{\{(.*?)\}\}", RegexOptions.Compiled);
    private readonly IApiClient _apiClient;

    private readonly SemaphoreSlim _definitionLock = new(1, 1);
    private readonly IFormatterService _formatter;
    private readonly Dictionary<Guid, JobInfo> _jobs = new();
    private readonly ILogger<JobScheduler> _logger;
    private readonly IRabbitMqService _mqService;
    private readonly JobSchedulerOptions _options;
    private PeriodicTimer? _definitionRefreshTimer;
    private CancellationTokenSource? _runCts;
    private Task? _runTask;
    private PeriodicTimer? _scheduleCheckTimer;

    public JobScheduler(JobSchedulerOptions options, IApiClient apiClient, IFormatterService formatter, IRabbitMqService mqService, ILogger<JobScheduler>? logger = null)
    {
        ArgumentHelpers.ThrowIfNull(options, nameof(options));
        ArgumentHelpers.ThrowIfNull(apiClient, nameof(apiClient));
        _options = options;
        _apiClient = apiClient;
        _formatter = formatter;
        _mqService = mqService;
        _logger = logger ?? NullLogger<JobScheduler>.Instance;
    }

    /// <summary>Starts the scheduler: connects to MQ, loads definitions, and begins background polling and completion processing.</summary>
    public async Task StartAsync(CancellationToken ct = default)
    {
        await _mqService.ConnectAsync(ct).ConfigureAwait(false);
        await UpdateDefinitionsAsync(ct).ConfigureAwait(false);
        await CheckSchedulesAsync(ct).ConfigureAwait(false);
        _runCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var token = _runCts.Token;
        _definitionRefreshTimer = new(TimeSpan.FromSeconds(_options.DefinitionRefreshIntervalSeconds));
        _scheduleCheckTimer = new(TimeSpan.FromSeconds(_options.ScheduleCheckIntervalSeconds));
        await SetupDefinitionUpdateQueueAsync(token).ConfigureAwait(false);
        await _mqService.SubscribeToQueue(Constants.Mq.QueueJobRunFinish, OnJobRunCompleteAsync, token).ConfigureAwait(false);
        _runTask = Task.WhenAll(RunDefinitionRefreshLoopAsync(token), RunScheduleCheckLoopAsync(token));
    }

    /// <summary>Stops the scheduler.</summary>
    public async Task StopAsync(CancellationToken ct = default)
    {
        _runCts?.Cancel();
        if (_runTask != null)
            await _runTask.ConfigureAwait(false);

        _definitionRefreshTimer?.Dispose();
        _scheduleCheckTimer?.Dispose();
        _runCts?.Dispose();
    }

    private async Task RunDefinitionRefreshLoopAsync(CancellationToken ct)
    {
        while (await _definitionRefreshTimer!.WaitForNextTickAsync(ct).ConfigureAwait(false)) {
            try {
                await _definitionLock.WaitAsync(ct).ConfigureAwait(false);
                try {
                    await UpdateDefinitionsAsync(ct).ConfigureAwait(false);
                }
                finally {
                    _definitionLock.Release();
                }
            }
            catch (OperationCanceledException) {
                break;
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Definition refresh failed");
            }
        }
    }

    private async Task RunScheduleCheckLoopAsync(CancellationToken ct)
    {
        while (await _scheduleCheckTimer!.WaitForNextTickAsync(ct).ConfigureAwait(false)) {
            try {
                await _definitionLock.WaitAsync(ct).ConfigureAwait(false);
                try {
                    await CheckSchedulesAsync(ct).ConfigureAwait(false);
                }
                finally {
                    _definitionLock.Release();
                }
            }
            catch (OperationCanceledException) {
                break;
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Schedule check failed");
            }
        }
    }

    private async Task SetupDefinitionUpdateQueueAsync(CancellationToken token)
    {
        var queueName = Constants.Mq.JobDefinitionChangeKey;
        await _mqService.CreateQueue(queueName, true, false, false, null, token).ConfigureAwait(false);
        await _mqService.BindQueueToExchange(queueName, Constants.Mq.JobEventExchange, Constants.Mq.JobDefinitionChangeKey, token).ConfigureAwait(false);
        await _mqService.SubscribeToQueue(queueName, OnDefinitionUpdatedAsync, token).ConfigureAwait(false);
        _logger.LogInformation("Subscribed to definition updates via {QueueName}", queueName);
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
                _jobs.Remove(definitionId.Value);
                _logger.LogDebug("Removed disabled definition {DefinitionId}", definitionId);
                return true;
            }

            var jobInfo = await LoadJobInfoAsync(definition).ConfigureAwait(false);
            _jobs[definitionId.Value] = jobInfo;
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

    /// <summary>Loads job definitions from the API and refreshes _jobs.</summary>
    public async Task UpdateDefinitionsAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Updating job definitions");
        _jobs.Clear();
        var query = new QueryReqBuilder().AddIncludes(JobDefinitionIncludes).Build();
        var results = await _apiClient.PostAsAsync<QueryReq, QueryRes<JobDefinitionRes>>(BuildUri(Constants.Rest.Job.DefinitionsQuery), query, null, ct)
            .ConfigureAwait(false);

        if (results?.Items == null || !results.IsSuccess) {
            _logger.LogWarning("No definitions loaded or query failed");
            return;
        }

        foreach (var def in results.Items) {
            if (!def.Enabled) {
                _logger.LogDebug("Skipping disabled definition {Name}", def.Name);
                continue;
            }

            var jobInfo = await LoadJobInfoAsync(def, ct).ConfigureAwait(false);
            _jobs[def.Id] = jobInfo;
        }
    }

    /// <summary>Evaluates schedules and creates job runs where due.</summary>
    public async Task CheckSchedulesAsync(CancellationToken ct = default)
    {
        if (!_mqService.IsConnected()) {
            _logger.LogWarning("Skipping schedule check: MessageQueue disconnected");
            return;
        }

        foreach (var kvp in _jobs) {
            var jobInfo = kvp.Value;
            var schedules = jobInfo.Definition.JobSchedules;
            if (schedules == null)
                continue;

            foreach (var schedule in schedules) {
                using (_logger.BeginScope("DefinitionId={DefinitionId} ScheduleId={ScheduleId}", jobInfo.Definition.Id, schedule.Id)) {
                    if (!ShouldProcessSchedule(jobInfo, schedule))
                        continue;

                    await ProcessScheduledJobDefinitionAsync(jobInfo.Definition, schedule, ct).ConfigureAwait(false);
                }
            }
        }
    }

    private bool ShouldProcessSchedule(JobInfo jobInfo, JobScheduleRes schedule)
    {
        if (!schedule.Enabled)
            return false;

        if (jobInfo.LastRun?.State is JobState.Queued or JobState.Running) {
            _logger.LogDebug("Job already queued or running");
            return false;
        }

        var lastRunTime = jobInfo.LastSuccessfulRun?.StartedTimestamp;
        var shouldRun = schedule.Type switch {
            ScheduleType.SetTimes => ShouldRunSetTimes(schedule, lastRunTime),
            ScheduleType.Interval => ShouldRunInterval(schedule, lastRunTime),
            var _ => false
        };

        if (shouldRun)
            _logger.LogInformation("Schedule due for definition {Name}", jobInfo.Definition.Name);

        return shouldRun;
    }

    private bool ShouldRunSetTimes(JobScheduleRes schedule, DateTime? lastRunUtc)
    {
        if (schedule.Times == null || schedule.Times.Count == 0)
            return false;

        if (!IsScheduledMonth(schedule))
            return false;

        return DateAndTime.DateAndTime.IsPastDue(_options.TimezoneState, schedule.Times.ToList(), schedule.DayFlags, lastRunUtc);
    }

    private bool ShouldRunInterval(JobScheduleRes schedule, DateTime? lastRunUtc)
    {
        if (schedule.StartTime == null || schedule.EndTime == null || schedule.IntervalMinutes <= 0)
            return false;

        if (!IsScheduledMonth(schedule))
            return false;

        var interval = schedule.IntervalMinutes.GetValueOrDefault();
        if (interval <= 0)
            return false;

        return DateAndTime.DateAndTime.IsPastDue(_options.TimezoneState, schedule.StartTime!.Value, schedule.EndTime!.Value, interval, schedule.DayFlags, lastRunUtc);
    }

    private static bool IsScheduledMonth(JobScheduleRes schedule)
    {
        var now = DateTime.UtcNow;
        var monthFlag = (MonthFlags)(1 << (now.Month - 1));
        return schedule.MonthFlags == MonthFlags.None || schedule.MonthFlags.HasFlag(monthFlag);
    }

    private async Task<JobInfo> LoadJobInfoAsync(JobDefinitionRes definition, CancellationToken ct = default)
    {
        var prevRunsQuery = new QueryReqBuilder().AddIncludes(JobRunIncludes)
            .AddQuery(WhereClauseBuilder.Condition("JobDefinitionId", ComparisonOperatorEnum.Equals, definition.Id.ToString()))
            .AddSort("CreatedTimestamp")
            .SetPagination(0, 10)
            .Build();

        var prevRuns = await _apiClient.PostAsAsync<QueryReq, QueryRes<JobRunRes>>(BuildUri(Constants.Rest.Job.RunsQuery), prevRunsQuery, null, ct).ConfigureAwait(false);
        var items = prevRuns?.Items ?? [];
        var lastRun = items.FirstOrDefault();
        var lastSuccessful = items.FirstOrDefault(r => r.Result is JobRunResult.Success or JobRunResult.SuccessWithWarnings);
        var lastFailed = items.FirstOrDefault(r => r.Result == JobRunResult.Failure);
        if (lastSuccessful == null) {
            var successNode = WhereClauseBuilder.CombineAs(
                GroupOperatorEnum.And, prevRunsQuery.WhereClause!,
                WhereClauseBuilder.Condition("Result", ComparisonOperatorEnum.In, new[] { nameof(JobRunResult.Success), nameof(JobRunResult.SuccessWithWarnings) }));

            var successQuery = new QueryReqBuilder(prevRunsQuery).First().AddQuery(successNode).Build();
            var successRes = await _apiClient.PostAsAsync<QueryReq, QueryRes<JobRunRes>>(BuildUri(Constants.Rest.Job.RunsQuery), successQuery, null, ct)
                .ConfigureAwait(false);

            lastSuccessful = successRes?.Items?.FirstOrDefault();
        }

        if (lastFailed != null)
            return new(definition, null, null, lastRun, lastSuccessful, lastFailed);

        var failNode = WhereClauseBuilder.CombineAs(
            GroupOperatorEnum.And, prevRunsQuery.WhereClause!, WhereClauseBuilder.Condition("Result", ComparisonOperatorEnum.Equals, nameof(JobRunResult.Failure)));

        var failQuery = new QueryReqBuilder(prevRunsQuery).First().AddQuery(failNode).Build();
        var failRes = await _apiClient.PostAsAsync<QueryReq, QueryRes<JobRunRes>>(BuildUri(Constants.Rest.Job.RunsQuery), failQuery, null, ct).ConfigureAwait(false);
        lastFailed = failRes?.Items?.FirstOrDefault();
        return new(definition, null, null, lastRun, lastSuccessful, lastFailed);
    }

    private async Task ProcessScheduledJobDefinitionAsync(JobDefinitionRes definition, JobScheduleRes schedule, CancellationToken ct)
    {
        if (!_jobs.TryGetValue(definition.Id, out var jobInfo))
            return;

        var runReq = BuildRunRequest(definition.Id, schedule, null, null);
        runReq.JobScheduleId = schedule.Id;
        _logger.LogDebug("Creating job run: {Request}", runReq);
        var created = await _apiClient.PostAsAsync<JobRunReq, CreateResult<JobRunRes>>(BuildUri(Constants.Rest.Job.Runs), runReq, null, ct).ConfigureAwait(false);
        if (created?.IsSuccess == true && created.Data != null) {
            _logger.LogInformation("Created job run {JobRunId}", created.Data.Id);
            _jobs[definition.Id] = jobInfo with { LastRun = created.Data };
        }
        else
            _logger.LogWarning("Failed to create job run: {Error}", created?.Error);
    }

    private async Task<bool> ProcessCompletedJobRunAsync(JobRunRes run)
    {
        if (!_jobs.TryGetValue(run.JobDefinitionId, out var jobInfo)) {
            _logger.LogWarning("No job info for definition {DefinitionId}", run.JobDefinitionId);
            return true;
        }

        _jobs[run.JobDefinitionId] = jobInfo with { LastRun = run };
        var resultStr = run.GetResultValueAs<string?>(Constants.Data.JobRunResultKey.Result);
        if (resultStr is "Success" or "PartialSuccess" or "SuccessWithWarnings")
            _jobs[run.JobDefinitionId] = _jobs[run.JobDefinitionId] with { LastSuccessfulRun = run };

        if (!run.AllowTriggers || jobInfo.Definition.JobTriggers?.Count == 0)
            return true;

        await ProcessTriggersAsync(jobInfo with { LastRun = run }, run).ConfigureAwait(false);
        return true;
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