namespace Lyo.StateMachine.Tests;

public enum JobState
{
    Queued,
    Running,
    Paused,
    Throttled,
    Done,
    Failed
}

public enum JobTrigger
{
    Start,
    Pause,
    Throttle,
    Resume,
    Complete,
    Retry,
    Fail,
    Heartbeat
}

public sealed record RetryPayload(int Attempt);

public sealed class JobContext
{
    public int MaxAttempts { get; init; } = 3;

    public bool CanFail { get; init; }

    public List<string> Log { get; } = [];

    public Func<Task>? DuringTransition { get; init; }
}

public enum CharacterState
{
    Idle,
    Combat,
    Attacking,
    Defending,
    Dead
}

public enum CharacterTrigger
{
    Engage,
    Disengage,
    Hit,
    Block,
    Strike
}

public sealed class Character
{
    public int Health { get; set; } = 10;
}

internal sealed class RecordingObserver(List<string> log) : IStateMachineObserver<JobState, JobTrigger>
{
    public void OnTransitioned(StateTransitioned<JobState, JobTrigger> transition) => log.Add($"observer:{transition.Source}->{transition.Destination}@{transition.Version}");

    public void OnRejected(StateTransitionRejected<JobState, JobTrigger> rejection) => log.Add($"rejected:{rejection.State}:{rejection.Trigger}:{rejection.Rejection}");
}

internal static class TestMachines
{
    public static readonly Trigger<JobTrigger, RetryPayload> Retry = new(JobTrigger.Retry);

    /// <summary>Queued, Running (with Paused and Throttled substates), Done, Failed. Every state logs enter/exit into <see cref="JobContext.Log" />.</summary>
    public static StateMachineBuilder<JobState, JobTrigger, JobContext> Job()
    {
        var builder = new StateMachineBuilder<JobState, JobTrigger, JobContext>("job", JobState.Queued);
        builder.Configure(JobState.Queued)
            .Permit(JobTrigger.Start, JobState.Running)
            .Permit(JobTrigger.Pause, JobState.Paused)
            .PermitIf(JobTrigger.Fail, JobState.Failed, c => c.CanFail, "can fail");

        builder.Configure(JobState.Running)
            .Permit(JobTrigger.Pause, JobState.Paused)
            .Permit(JobTrigger.Throttle, JobState.Throttled)
            .Permit(JobTrigger.Complete, JobState.Done)
            .PermitIf(Retry, JobState.Queued, (c, p) => p.Attempt < c.MaxAttempts, "attempts left")
            .PermitReentry(JobTrigger.Heartbeat);

        builder.Configure(JobState.Paused).SubstateOf(JobState.Running).Permit(JobTrigger.Resume, JobState.Running);
        builder.Configure(JobState.Throttled).SubstateOf(JobState.Running).Permit(JobTrigger.Complete, JobState.Queued);
        foreach (var state in Enum.GetValues<JobState>()) {
            builder.Configure(state).OnEntry(t => t.Context.Log.Add($"enter:{state}")).OnExit(t => t.Context.Log.Add($"exit:{state}"));
        }

        return builder;
    }

    public static StateSnapshot<JobState> At(JobState state, long version = 0) => new(state, version, DateTime.UnixEpoch);

    public static StateMachineBuilder<CharacterState, CharacterTrigger, Character> Character()
    {
        var builder = new StateMachineBuilder<CharacterState, CharacterTrigger, Character>("character", CharacterState.Idle);
        builder.Configure(CharacterState.Idle).Permit(CharacterTrigger.Engage, CharacterState.Attacking);
        builder.Configure(CharacterState.Combat)
            .Permit(CharacterTrigger.Disengage, CharacterState.Idle)
            .PermitIf(CharacterTrigger.Hit, CharacterState.Dead, c => c.Health <= 0, "health depleted");

        builder.Configure(CharacterState.Attacking).SubstateOf(CharacterState.Combat).Permit(CharacterTrigger.Block, CharacterState.Defending);
        builder.Configure(CharacterState.Defending).SubstateOf(CharacterState.Combat).Permit(CharacterTrigger.Strike, CharacterState.Attacking);
        return builder;
    }
}