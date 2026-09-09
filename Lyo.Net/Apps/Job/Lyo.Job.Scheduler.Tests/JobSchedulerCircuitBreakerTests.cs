using System.Collections;
using System.Reflection;
using Lyo.Formatter;
using Lyo.Job.Models.Enums;
using Lyo.Query.Models.Enums;
using Lyo.Job.Models.Events;
using Lyo.Job.Models.Response;

namespace Lyo.Job.Scheduler.Tests;

/// <summary>
/// Exercises the circuit breaker and failure-alert accounting in completion handling, including the fail-open regression: clearing the failure counter after a failed disable patch
/// left the definition running and restarted counting from zero on the next failure.
/// </summary>
public class JobSchedulerCircuitBreakerTests
{
    [Fact]
    public async Task FailuresBelowTheThreshold_DoNotTripTheBreaker()
    {
        var definition = BuildDefinition(circuitBreakerThreshold: 3);
        var api = new FakeSchedulerApiClient(definition);
        var (scheduler, events) = await CreateSchedulerAsync(api);
        await CompleteFailedRunAsync(scheduler, definition.Id);
        await CompleteFailedRunAsync(scheduler, definition.Id);
        Assert.Empty(api.DefinitionPatches);
        Assert.Equal(2, FailureCount(scheduler, definition.Id));
        Assert.True(CachedJobs(scheduler).Contains(definition.Id));
        Assert.Empty(events.Alerts);
    }

    [Fact]
    public async Task ReachingTheThreshold_DisablesTheDefinitionAndAlerts()
    {
        var definition = BuildDefinition(circuitBreakerThreshold: 2);
        var api = new FakeSchedulerApiClient(definition);
        var (scheduler, events) = await CreateSchedulerAsync(api);
        await CompleteFailedRunAsync(scheduler, definition.Id);
        await CompleteFailedRunAsync(scheduler, definition.Id);

        var patch = Assert.Single(api.DefinitionPatches);
        Assert.Equal(false, patch.Properties["Enabled"]);
        Assert.True(patch.Properties.ContainsKey("CircuitBreakerTrippedAt"));

        // A tripped definition must leave the cache so the scheduler stops firing it, and the counter resets only after a successful trip.
        Assert.False(CachedJobs(scheduler).Contains(definition.Id));
        Assert.Null(FailureCount(scheduler, definition.Id));
        Assert.Contains(events.Alerts, a => a.AlertType == JobAlertType.CircuitBreakerTripped);
    }

    [Fact]
    public async Task WhenTheTripPatchFails_TheCounterIsKeptSoTheNextFailureRetries()
    {
        var definition = BuildDefinition(circuitBreakerThreshold: 2);
        var api = new FakeSchedulerApiClient(definition) { ThrowOnDefinitionPatch = true };
        var (scheduler, events) = await CreateSchedulerAsync(api);
        await CompleteFailedRunAsync(scheduler, definition.Id);
        await CompleteFailedRunAsync(scheduler, definition.Id);

        // Clearing the counter here was the fail-open bug: the definition kept running and counting restarted from zero.
        Assert.Equal(2, FailureCount(scheduler, definition.Id));
        Assert.True(CachedJobs(scheduler).Contains(definition.Id));
        Assert.DoesNotContain(events.Alerts, a => a.AlertType == JobAlertType.CircuitBreakerTripped);

        api.ThrowOnDefinitionPatch = false;
        await CompleteFailedRunAsync(scheduler, definition.Id);
        Assert.Single(api.DefinitionPatches);
        Assert.Null(FailureCount(scheduler, definition.Id));
    }

    [Fact]
    public async Task ASuccessfulRun_ResetsTheFailureCounter()
    {
        var definition = BuildDefinition(circuitBreakerThreshold: 3);
        var api = new FakeSchedulerApiClient(definition);
        var (scheduler, _) = await CreateSchedulerAsync(api);
        await CompleteFailedRunAsync(scheduler, definition.Id);
        await CompleteRunAsync(scheduler, BuildRun(definition.Id, JobRunResult.Success));
        Assert.Null(FailureCount(scheduler, definition.Id));
        await CompleteFailedRunAsync(scheduler, definition.Id);
        Assert.Equal(1, FailureCount(scheduler, definition.Id));
    }

    [Fact]
    public async Task TimeoutsCountTowardTheBreaker()
    {
        var definition = BuildDefinition(circuitBreakerThreshold: 2);
        var api = new FakeSchedulerApiClient(definition);
        var (scheduler, _) = await CreateSchedulerAsync(api);
        await CompleteRunAsync(scheduler, BuildRun(definition.Id, JobRunResult.Timeout));
        await CompleteRunAsync(scheduler, BuildRun(definition.Id, JobRunResult.Timeout));
        Assert.Single(api.DefinitionPatches);
    }

    [Fact]
    public async Task FailureAlerts_HonourTheConsecutiveFailureThreshold()
    {
        var definition = BuildDefinition(circuitBreakerThreshold: 0, alertOnFailure: true, alertAfterConsecutiveFailures: 2);
        var api = new FakeSchedulerApiClient(definition);
        var (scheduler, events) = await CreateSchedulerAsync(api);
        await CompleteFailedRunAsync(scheduler, definition.Id);
        Assert.Empty(events.Alerts);
        await CompleteFailedRunAsync(scheduler, definition.Id);
        Assert.Single(events.Alerts, a => a.AlertType == JobAlertType.Failure);
    }

    [Fact]
    public async Task Trigger_FiresTheTargetDefinitionWithADeterministicIdempotencyKey()
    {
        var trigger = BuildTrigger(Guid.NewGuid(), "Result", "Success");
        var definition = BuildDefinition(circuitBreakerThreshold: 0);

        // The trigger targets the same definition so the fake's definition GET can answer for it.
        definition = definition with { JobTriggers = [trigger with { TriggersJobDefinitionId = definition.Id }] };
        var api = new FakeSchedulerApiClient(definition);
        var (scheduler, _) = await CreateSchedulerAsync(api);
        var run = BuildRun(definition.Id, JobRunResult.Success, allowTriggers: true, resultKey: "Result", resultValue: "Success");
        await CompleteRunAsync(scheduler, run);

        var created = Assert.Single(api.CreatedRunRequests);
        Assert.Equal(definition.Id, created.JobDefinitionId);
        Assert.Equal($"trigger:{trigger.Id:N}:{run.Id:N}", created.IdempotencyKey);
    }

    [Fact]
    public async Task Trigger_DoesNotFireWhenDisabledOrWhenTheResultDoesNotMatch()
    {
        var disabled = BuildTrigger(Guid.NewGuid(), "Result", "Success", enabled: false);
        var mismatch = BuildTrigger(Guid.NewGuid(), "Result", "Warning");
        var definition = BuildDefinition(circuitBreakerThreshold: 0);
        definition = definition with {
            JobTriggers = [disabled with { TriggersJobDefinitionId = definition.Id }, mismatch with { TriggersJobDefinitionId = definition.Id }]
        };

        var api = new FakeSchedulerApiClient(definition);
        var (scheduler, _) = await CreateSchedulerAsync(api);
        await CompleteRunAsync(scheduler, BuildRun(definition.Id, JobRunResult.Success, allowTriggers: true, resultKey: "Result", resultValue: "Success"));
        Assert.Empty(api.CreatedRunRequests);
    }

    [Fact]
    public async Task Triggers_AreSkippedWhenTheRunDisallowsThem()
    {
        var trigger = BuildTrigger(Guid.NewGuid(), "Result", "Success");
        var definition = BuildDefinition(circuitBreakerThreshold: 0);
        definition = definition with { JobTriggers = [trigger with { TriggersJobDefinitionId = definition.Id }] };
        var api = new FakeSchedulerApiClient(definition);
        var (scheduler, _) = await CreateSchedulerAsync(api);
        await CompleteRunAsync(scheduler, BuildRun(definition.Id, JobRunResult.Success, resultKey: "Result", resultValue: "Success"));
        Assert.Empty(api.CreatedRunRequests);
    }

    private static async Task<(JobScheduler Scheduler, FakeEventPublisher Events)> CreateSchedulerAsync(FakeSchedulerApiClient api)
    {
        var events = new FakeEventPublisher();
        var scheduler = new JobScheduler(
            new() {
                ApiBaseUrl = "http://localhost/api",
                DefinitionRefreshIntervalSeconds = 3600,
                ScheduleCheckIntervalSeconds = 3600,
                EnableMisfireCatchUp = false
            }, api, new FormatterService(), events);

        await scheduler.RefreshDefinitionsAsync(TestContext.Current.CancellationToken);
        return (scheduler, events);
    }

    private static JobDefinitionRes BuildDefinition(
        int circuitBreakerThreshold,
        bool alertOnFailure = false,
        int alertAfterConsecutiveFailures = 0,
        IReadOnlyList<JobTriggerRes>? triggers = null)
        => new(
            Guid.NewGuid(), "BreakerDef", null, "Test", "cs", true, [], [], triggers ?? [], null, CircuitBreakerThreshold: circuitBreakerThreshold,
            AlertOnFailure: alertOnFailure, AlertAfterConsecutiveFailures: alertAfterConsecutiveFailures);

    private static JobTriggerRes BuildTrigger(Guid targetDefinitionId, string key, string? value, bool enabled = true)
        => new(Guid.NewGuid(), targetDefinitionId, key, ComparisonOperatorEnum.Equals, value, null, enabled, null, null, null);

    private static JobRunRes BuildRun(
        Guid definitionId,
        JobRunResult result,
        bool allowTriggers = false,
        string? resultKey = null,
        string? resultValue = null)
    {
        var runId = Guid.NewGuid();
        return new() {
            Id = runId,
            JobDefinitionId = definitionId,
            State = JobState.Finished,
            Result = result,
            AllowTriggers = allowTriggers,
            CreatedTimestamp = DateTime.UtcNow.AddMinutes(-1),
            FinishedTimestamp = DateTime.UtcNow,
            JobRunResults = resultKey is null ? [] : [new(Guid.NewGuid(), runId, resultKey, "String", resultValue)]
        };
    }

    private static Task CompleteFailedRunAsync(JobScheduler scheduler, Guid definitionId) => CompleteRunAsync(scheduler, BuildRun(definitionId, JobRunResult.Failure));

    private static async Task CompleteRunAsync(JobScheduler scheduler, JobRunRes run)
    {
        var method = typeof(JobScheduler).GetMethod("ProcessCompletedJobRunAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
        await (Task<bool>)method.Invoke(scheduler, [run])!;
    }

    private static int? FailureCount(JobScheduler scheduler, Guid definitionId)
    {
        var counters = (Dictionary<Guid, int>)typeof(JobScheduler).GetField("_consecutiveFailures", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(scheduler)!;
        return counters.TryGetValue(definitionId, out var count) ? count : null;
    }

    private static IDictionary CachedJobs(JobScheduler scheduler)
        => (IDictionary)typeof(JobScheduler).GetField("_jobs", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(scheduler)!;
}
