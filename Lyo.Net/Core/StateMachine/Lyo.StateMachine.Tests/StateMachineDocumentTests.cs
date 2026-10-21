using static Lyo.StateMachine.Tests.TestMachines;

namespace Lyo.StateMachine.Tests;

[Trait("Category", "Fast")]
public class StateMachineDocumentTests
{
    private static readonly IStateKeyConverter<JobState> States = EnumStateKeyConverter<JobState>.Instance;
    private static readonly IStateKeyConverter<JobTrigger> Triggers = EnumStateKeyConverter<JobTrigger>.Instance;

    private static StateMachineDocument Document()
        => new() {
            Name = "job",
            InitialState = "Queued",
            OnTransition = ["audit"],
            States = [
                new() {
                    Name = "Queued",
                    Transitions = [
                        new() { Kind = TransitionDocumentKind.Permit, Trigger = "Start", Destination = "Running" },
                        new() {
                            Kind = TransitionDocumentKind.Permit,
                            Trigger = "Fail",
                            Destination = "Failed",
                            Guard = "can fail"
                        }
                    ]
                },
                new() {
                    Name = "Running",
                    InitialSubstate = "Working",
                    OnEntry = ["log-enter"],
                    OnExit = ["log-exit"],
                    Timers = [new() { After = TimeSpan.FromMinutes(5), Trigger = "Complete" }],
                    Transitions = [
                        new() {
                            Kind = TransitionDocumentKind.Dynamic,
                            Trigger = "Complete",
                            Destinations = ["Done", "Failed"],
                            Selector = "pick"
                        },
                        new() {
                            Kind = TransitionDocumentKind.Permit,
                            Trigger = "Retry",
                            Destination = "Queued",
                            Guard = "attempts left"
                        },
                        new() { Kind = TransitionDocumentKind.Internal, Trigger = "Heartbeat", Action = "beat" },
                        new() { Kind = TransitionDocumentKind.Reentry, Trigger = "Resume" },
                        new() { Kind = TransitionDocumentKind.Ignore, Trigger = "Throttle" }
                    ]
                },
                new() { Name = "Working", Parent = "Running" }, new() { Name = "Done", IsFinal = true }, new() { Name = "Failed", IsFinal = true }
            ]
        };

    private static StateMachineRegistry<string, JobTrigger, JobContext> Registry()
        => new StateMachineRegistry<string, JobTrigger, JobContext>().Guard("can fail", c => c.CanFail)
            .Guard<RetryPayload>("attempts left", (c, p) => p.Attempt < c.MaxAttempts)
            .Payload<RetryPayload>(JobTrigger.Retry)
            .Action("audit", t => t.Context.Log.Add($"audit:{t.Source}->{t.Destination}"))
            .Action("log-enter", t => t.Context.Log.Add("enter:Running"))
            .Action("log-exit", t => t.Context.Log.Add("exit:Running"))
            .Action("beat", t => t.Context.Log.Add("beat"))
            .Selector("pick", c => c.CanFail ? "Failed" : "Done");

    private static StateMachineDefinition<string, JobTrigger, JobContext> Load(StateMachineDocument? document = null)
        => StateMachineDocumentLoader.Load(document ?? Document(), Registry(), StringStateKeyConverter.Instance, Triggers);

    private static StateSnapshot<string> AtState(string state) => new(state, 0, DateTime.UnixEpoch);

    [Fact]
    public async Task Load_ResolvesNamedGuardsActionsSelectors()
    {
        var definition = Load();
        var context = new JobContext();
        var started = await definition.FireAsync(context, AtState("Queued"), JobTrigger.Start, TestCt);
        var beat = await definition.FireAsync(context, started.Snapshot, JobTrigger.Heartbeat, TestCt);
        var ignored = await definition.FireAsync(context, started.Snapshot, JobTrigger.Throttle, TestCt);
        var done = await definition.FireAsync(context, started.Snapshot, JobTrigger.Complete, TestCt);
        var failGuarded = await definition.FireAsync(context, AtState("Queued"), JobTrigger.Fail, TestCt);
        Assert.Equal("Working", started.To);
        Assert.Equal(TransitionKind.Internal, beat.Kind);
        Assert.Equal(TransitionKind.Ignored, ignored.Kind);
        Assert.Equal("Done", done.To);
        Assert.Equal(TransitionRejection.GuardFailed, failGuarded.Rejection);
        Assert.Equal(["audit:Queued->Working", "enter:Running", "beat", "exit:Running", "audit:Working->Done"], context.Log);
        Assert.Equal(TimeSpan.FromMinutes(5), definition.GetDueTrigger(AtState("Running"), DateTime.UnixEpoch.AddMinutes(5))!.DueAtUtc - DateTime.UnixEpoch);
    }

    [Fact]
    public void Load_MissingNames_ThrowsListingAll()
    {
        var document = Document() with {
            States = [
                new() {
                    Name = "Queued",
                    OnEntry = ["nope-hook"],
                    Transitions = [
                        new() {
                            Kind = TransitionDocumentKind.Permit,
                            Trigger = "Start",
                            Destination = "Running",
                            Guard = "nope-guard"
                        },
                        new() {
                            Kind = TransitionDocumentKind.Dynamic,
                            Trigger = "Complete",
                            Destinations = ["Done"],
                            Selector = "nope-selector"
                        },
                        new() { Kind = TransitionDocumentKind.Permit, Trigger = "Explode", Destination = "Done" }
                    ]
                },
                new() { Name = "Running" }, new() { Name = "Done" }
            ]
        };

        var ex = Assert.Throws<StateMachineConfigurationException>(() => StateMachineDocumentLoader.Load(document, Registry(), StringStateKeyConverter.Instance, Triggers));
        Assert.Contains("nope-hook", ex.Message);
        Assert.Contains("nope-guard", ex.Message);
        Assert.Contains("nope-selector", ex.Message);
        Assert.Contains("Explode", ex.Message);
    }

    [Fact]
    public void Load_UnknownEnumState_Throws()
    {
        var document = new StateMachineDocument { Name = "job", InitialState = "Queued", States = [new() { Name = "Queued" }, new() { Name = "Sleeping" }] };
        var ex = Assert.Throws<StateMachineConfigurationException>(() => StateMachineDocumentLoader.Load(
            document, new StateMachineRegistry<JobState, JobTrigger, JobContext>(), States, Triggers));

        Assert.Contains("Sleeping", ex.Message);
    }

    [Fact]
    public void Load_PayloadTriggerDeclared_GuardGetsTypedPayload()
    {
        var definition = Load();
        var running = AtState("Working");
        Assert.Equal("Queued", definition.Fire(new(), running, Retry, new(1)).To);
        Assert.Equal(TransitionRejection.GuardFailed, definition.Fire(new(), running, Retry, new(3)).Rejection);
        Assert.Throws<ArgumentException>(() => definition.Fire(new(), running, JobTrigger.Retry));
    }

    [Fact]
    public void ToDocument_LoaderBuilt_RoundTripsJson()
    {
        var original = Document() with { Revision = 4 };
        var json = StateMachineDocumentJson.Serialize(original);
        var exported = Load(StateMachineDocumentJson.Deserialize(json)).ToDocument(StringStateKeyConverter.Instance, Triggers, 4);
        Assert.Equal(json, StateMachineDocumentJson.Serialize(exported));
    }

    [Fact]
    public void ToDocument_CodeBuilt_UsesGuardDescriptionsAndSkipsUnnamedHooks()
    {
        var document = Job().Build().ToDocument(States, Triggers);
        var queued = document.States.Single(s => s.Name == "Queued");
        Assert.Empty(queued.OnEntry);
        Assert.Equal("can fail", queued.Transitions.Single(t => t.Trigger == "Fail").Guard);
        Assert.Equal("Running", document.States.Single(s => s.Name == "Paused").Parent);
    }

    [Fact]
    public void FindRemovedStates_ReportsDroppedStates()
    {
        var updated = Document() with { States = Document().States.Where(s => s.Name != "Failed" && s.Name != "Working").ToArray() };
        Assert.Equal(["Working", "Failed"], StateMachineDocumentCompatibility.FindRemovedStates(Document(), updated));
    }

    [Fact]
    public async Task DefinitionCache_ReturnsSameInstancePerRevision()
    {
        var store = new InMemoryStateMachineDefinitionStore();
        await store.SaveAsync(Document(), 0, TestCt);
        var cache = new StateMachineDefinitionCache<string, JobTrigger, JobContext>(store, Registry(), StringStateKeyConverter.Instance, Triggers);
        var first = await cache.GetAsync("job", ct: TestCt);
        var again = await cache.GetAsync("job", ct: TestCt);
        await store.SaveAsync(Document() with { InitialState = "Running" }, 1, TestCt);
        var latest = await cache.GetAsync("job", ct: TestCt);
        var pinned = await cache.GetAsync("job", 1, TestCt);
        Assert.Same(first, again);
        Assert.NotSame(first, latest);
        Assert.Equal("Running", latest.InitialState);
        Assert.Same(first, pinned);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => cache.GetAsync("missing", ct: TestCt));
    }
}