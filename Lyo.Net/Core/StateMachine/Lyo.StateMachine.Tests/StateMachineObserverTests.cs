using Microsoft.Extensions.Logging;
using static Lyo.StateMachine.Tests.TestMachines;

namespace Lyo.StateMachine.Tests;

[Trait("Category", "Fast")]
public class StateMachineObserverTests
{
    [Fact]
    public void Fire_Rejected_CallsOnRejected()
    {
        var log = new List<string>();
        var definition = Job().AddObserver(new RecordingObserver(log)).Build();
        definition.Fire(new(), At(JobState.Queued), JobTrigger.Complete);
        definition.Fire(new() { CanFail = false }, At(JobState.Queued), JobTrigger.Fail);
        Assert.Equal(["rejected:Queued:Complete:NoTransition", "rejected:Queued:Fail:GuardFailed"], log);
    }

    [Fact]
    public void MetricsObserver_RecordsCounterWithTags()
    {
        var metrics = new RecordingMetrics();
        var definition = Job().AddObserver(new MetricsStateMachineObserver<JobState, JobTrigger>(metrics)).Build();
        definition.Fire(new(), At(JobState.Queued), JobTrigger.Start);
        definition.Fire(new(), At(JobState.Queued), JobTrigger.Complete);
        var transition = Assert.Single(metrics.Calls, c => c.Name == "statemachine.transitions");
        Assert.Equal("counter", transition.Kind);
        Assert.Equal(
            new() {
                ["machine"] = "job",
                ["from"] = "Queued",
                ["to"] = "Running",
                ["trigger"] = "Start",
                ["kind"] = "Transition"
            }, transition.Tags);

        Assert.Single(metrics.Calls, c => c.Kind == "timing" && c.Name == "statemachine.fire");
        var rejection = Assert.Single(metrics.Calls, c => c.Name == "statemachine.rejections");
        Assert.Equal("NoTransition", rejection.Tags["reason"]);
    }

    [Fact]
    public void LoggingObserver_LogsTransition()
    {
        var logger = new ListLogger<LoggingStateMachineObserver<JobState, JobTrigger>>();
        var definition = Job().AddObserver(new LoggingStateMachineObserver<JobState, JobTrigger>(logger)).Build();
        definition.Fire(new(), At(JobState.Queued), JobTrigger.Start);
        definition.Fire(new(), At(JobState.Queued), JobTrigger.Complete);
        Assert.Collection(
            logger.Entries, e => {
                Assert.Equal(LogLevel.Debug, e.Level);
                Assert.Equal("State machine job: Queued -> Running on Start (Transition, v1)", e.Message);
            }, e => {
                Assert.Equal(LogLevel.Information, e.Level);
                Assert.Equal("State machine job: rejected Complete in Queued (NoTransition)", e.Message);
            });
    }
}