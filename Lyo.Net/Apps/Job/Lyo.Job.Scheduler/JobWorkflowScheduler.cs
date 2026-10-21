using System.Diagnostics;
using Lyo.Api.Client;
using Lyo.Api.Models.Common.Response;
using Lyo.Exceptions;
using Lyo.Health;
using Lyo.Job.Models;
using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Request;
using Lyo.Job.Models.Response;
using Lyo.Metrics;
using Lyo.Query.Models.Builders;
using Lyo.Query.Models.Common.Request;
using Lyo.Query.Models.Enums;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Constants = Lyo.Job.Models.Constants;

namespace Lyo.Job.Scheduler;

/// <summary>
/// Starts workflow runs on a cron. Workflow schedules are ordinary <c>JobSchedule</c> rows whose owner is a workflow instead of a definition, so this reuses
/// <see cref="JobScheduleSlotCalculator" /> and <see cref="JobScheduleReference" /> and the blackout calendars unchanged — cron, DST, misfire, and blackout behave the same whichever
/// kind of schedule you write.
/// <para>
/// <see cref="JobScheduler" /> is untouched by this: it walks <c>definition.JobSchedules</c>, which a workflow-owned schedule never populates. Dedupe is the workflow run's
/// <c>(JobScheduleId, ScheduledSlotUtc)</c> unique index, so two hosts polling the same slot converge on one run rather than starting two.
/// </para>
/// </summary>
public sealed class JobWorkflowScheduler : BackgroundService, IHealth
{
    private readonly IApiClient _apiClient;

    /// <summary>
    /// Slots this process already posted. The API is the authority, but a run created seconds ago may not be the "latest run" the next query returns if the workflow was also
    /// started manually, and re-posting a known slot would burn a round trip on every poll.
    /// </summary>
    private readonly HashSet<(Guid ScheduleId, DateTime Slot)> _knownSlots = [];

    private readonly ILogger<JobWorkflowScheduler> _logger;
    private readonly IMetrics _metrics;
    private readonly JobWorkflowSchedulerOptions _options;
    private DateTime? _lastCheckUtc;

    public bool IsRunning => !ExecuteTask?.IsCompleted ?? false;

    public JobWorkflowScheduler(JobWorkflowSchedulerOptions options, IApiClient apiClient, ILogger<JobWorkflowScheduler>? logger = null, IMetrics? metrics = null)
    {
        ArgumentHelpers.ThrowIfNull(options);
        ArgumentHelpers.ThrowIfNull(apiClient);
        options.Validate();
        _options = options;
        _apiClient = apiClient;
        _logger = logger ?? NullLogger<JobWorkflowScheduler>.Instance;
        _metrics = metrics ?? NullMetrics.Instance;
    }

    /// <inheritdoc />
    public string HealthCheckName => "job-workflow-scheduler";

    /// <inheritdoc />
    public Task<HealthResult> CheckHealthAsync(CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var metadata = new Dictionary<string, object?> { ["is_running"] = IsRunning, ["last_check_utc"] = _lastCheckUtc };
        var result = IsRunning
            ? HealthResult.Healthy(sw.Elapsed, "Workflow scheduler running", metadata)
            : HealthResult.Unhealthy(sw.Elapsed, "Workflow scheduler is not running", metadata);

        return Task.FromResult(result);
    }

    /// <summary>Starts a run for every workflow-owned schedule that is due now. Public so a host, or a test, can drive one pass without the timer.</summary>
    public async Task CheckWorkflowSchedulesAsync(CancellationToken ct = default)
    {
        _lastCheckUtc = DateTime.UtcNow;
        foreach (var schedule in await LoadWorkflowSchedulesAsync(ct).ConfigureAwait(false)) {
            if (schedule.JobWorkflowId is not { } workflowId)
                continue;

            using (_logger.BeginScope("WorkflowId={WorkflowId} ScheduleId={ScheduleId}", workflowId, schedule.Id)) {
                var slot = await GetDueScheduledSlotAsync(schedule, ct).ConfigureAwait(false);
                if (slot is null)
                    continue;

                await StartWorkflowRunAsync(workflowId, schedule, slot.Value, ct).ConfigureAwait(false);
            }
        }
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.ScheduleCheckIntervalSeconds));
        while (!stoppingToken.IsCancellationRequested) {
            try {
                await CheckWorkflowSchedulesAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {
                return;
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Workflow schedule check failed; retrying on the next tick");
            }

            try {
                await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) {
                return;
            }
        }
    }

    private async Task<IReadOnlyList<JobScheduleRes>> LoadWorkflowSchedulesAsync(CancellationToken ct)
    {
        var query = new QueryConcreteReqBuilder().AddIncludes("JobScheduleParameters", "JobBlackoutCalendar", "JobBlackoutCalendar.JobBlackoutWindows")
            .AddWhere(w => w.NotEquals("JobWorkflowId", null))
            .Build();

        var result = await _apiClient.PostAsAsync<QueryConcreteReq, QueryRes<JobScheduleRes>>(BuildUri($"{Constants.Rest.Job.Schedules}/QueryConcrete"), query, ct: ct)
            .ConfigureAwait(false);

        return result.Items ?? [];
    }

    /// <summary>
    /// The slot this schedule is due for, or null when nothing is due. Mirrors the definition scheduler's gating, but the "already running" and "already covered" checks read the
    /// workflow run rather than the job run.
    /// </summary>
    private async Task<DateTime?> GetDueScheduledSlotAsync(JobScheduleRes schedule, CancellationToken ct)
    {
        if (!schedule.Enabled)
            return null;

        var now = DateTime.UtcNow;
        if (!JobScheduleSlotCalculator.IsWithinWindow(schedule, now))
            return null;

        var latestRun = await GetLatestRunAsync(schedule.Id, ct).ConfigureAwait(false);
        if (latestRun?.State is JobWorkflowRunState.Pending or JobWorkflowRunState.Running) {
            _logger.LogDebug("Workflow run already pending or running");
            return null;
        }

        var timeZone = ResolveTimeZone(schedule);
        var definition = schedule.ToScheduleDefinition() with { TimeZone = timeZone };
        var lookbackStart = now.AddMinutes(-_options.MisfireLookbackMinutes);
        var reference = JobScheduleReference.Resolve(
            latestRun?.FinishedTimestamp, latestRun?.ScheduledSlotUtc, latestRun?.StartedTimestamp, latestRun?.CreatedTimestamp, schedule.StartDateUtc, now,
            _options.MisfireLookbackMinutes);

        // Newest-first, not oldest-first. A schedule that has never run anchors its reference at the lookback start, and walking forward from there would fire a day-old slot
        // and then grind through every slot since. There is no separate misfire pass here: the newest missed slot is the run.
        var slot = JobScheduleSlotCalculator.FindMostRecentMissedSlot(definition, schedule, schedule.JobBlackoutCalendar, timeZone, reference, lookbackStart, now);

        // A blackout Defer moves the slot past now. Firing it anyway would run the workflow inside the window it was told to avoid; a later check picks it up.
        if (slot is null || slot > now)
            return null;

        if (latestRun?.ScheduledSlotUtc == slot || !_knownSlots.Add((schedule.Id, slot.Value)))
            return null;

        _knownSlots.RemoveWhere(known => known.Slot < lookbackStart);
        return slot;
    }

    private async Task<JobWorkflowRunRes?> GetLatestRunAsync(Guid scheduleId, CancellationToken ct)
    {
        var query = new QueryConcreteReqBuilder().AddWhere(WhereClauseBuilder.Condition("JobScheduleId", ComparisonOperatorEnum.Equals, scheduleId.ToString()))
            .AddSort("CreatedTimestamp")
            .First()
            .Build();

        var result = await _apiClient.PostAsAsync<QueryConcreteReq, QueryRes<JobWorkflowRunRes>>(BuildUri($"{Constants.Rest.Job.WorkflowRuns}/QueryConcrete"), query, ct: ct)
            .ConfigureAwait(false);

        return result.Items?.FirstOrDefault();
    }

    private async Task StartWorkflowRunAsync(Guid workflowId, JobScheduleRes schedule, DateTime slot, CancellationToken ct)
    {
        var request = new JobWorkflowStartReq { JobScheduleId = schedule.Id, ScheduledSlotUtc = slot };
        try {
            var created = await _apiClient
                .PostAsAsync<JobWorkflowStartReq, CreateResult<JobWorkflowRunRes>>(BuildUri(Constants.Rest.Job.WorkflowStart(workflowId)), request, ct: ct)
                .ConfigureAwait(false);

            if (!created.IsSuccess) {
                _logger.LogWarning("Failed to start scheduled workflow run: {Error}", created.Error?.GetFullMessage());
                return;
            }

            _metrics.IncrementCounter(Constants.Metrics.Workflow.ScheduledRunStarted);
            _logger.LogInformation("Started workflow run {WorkflowRunId} for slot {Slot:u}", created.Data!.Id, slot);
        }
        catch (Exception ex) {
            // Forget the slot so the next tick retries it; a transient API failure must not silently drop a scheduled run.
            _knownSlots.Remove((schedule.Id, slot));
            _logger.LogError(ex, "Failed to start scheduled workflow run for slot {Slot:u}", slot);
        }
    }

    private TimeZoneInfo? ResolveTimeZone(JobScheduleRes schedule)
    {
        if (string.IsNullOrWhiteSpace(schedule.TimeZoneId))
            return _options.TimeZone;

        try {
            return TimeZoneInfo.FindSystemTimeZoneById(schedule.TimeZoneId);
        }
        catch (TimeZoneNotFoundException ex) {
            _logger.LogWarning(ex, "Unknown schedule time zone {TimeZoneId}", schedule.TimeZoneId);
        }
        catch (InvalidTimeZoneException ex) {
            _logger.LogWarning(ex, "Invalid schedule time zone {TimeZoneId}", schedule.TimeZoneId);
        }

        return _options.TimeZone;
    }

    private string BuildUri(string path) => $"{_options.ApiBaseUrl.TrimEnd('/')}/{path.TrimStart('/')}";
}