using Lyo.Exceptions;
using Lyo.Metrics;

namespace Lyo.StateMachine;

/// <summary>Publishes state machine activity to <see cref="IMetrics" />. Register with <c>AddStateMachineMetrics</c>.</summary>
/// <remarks>
/// Emits counter <c>statemachine.transitions</c> (tags machine, from, to, trigger, kind), timing <c>statemachine.fire</c> (tags machine, trigger) for accepted
/// triggers, and counter <c>statemachine.rejections</c> (tags machine, state, trigger, reason). Tag values come from <c>ToString()</c>, so keep state and trigger
/// sets small (enums) to bound cardinality.
/// </remarks>
public sealed class MetricsStateMachineObserver<TState, TTrigger> : IStateMachineObserver<TState, TTrigger>
    where TState : notnull where TTrigger : notnull
{
    private readonly IMetrics _metrics;

    /// <summary>Creates the observer.</summary>
    public MetricsStateMachineObserver(IMetrics metrics)
    {
        ArgumentHelpers.ThrowIfNull(metrics);
        _metrics = metrics;
    }

    /// <inheritdoc />
    public void OnTransitioned(StateTransitioned<TState, TTrigger> transition)
    {
        var trigger = transition.Trigger.ToString() ?? string.Empty;
        _metrics.IncrementCounter(
            "statemachine.transitions", tags: [
                ("machine", transition.MachineName), ("from", transition.Source.ToString() ?? string.Empty), ("to", transition.Destination.ToString() ?? string.Empty),
                ("trigger", trigger), ("kind", transition.Kind.ToString())
            ]);

        _metrics.RecordTiming("statemachine.fire", transition.Duration, [("machine", transition.MachineName), ("trigger", trigger)]);
    }

    /// <inheritdoc />
    public void OnRejected(StateTransitionRejected<TState, TTrigger> rejection)
        => _metrics.IncrementCounter(
            "statemachine.rejections", tags: [
                ("machine", rejection.MachineName), ("state", rejection.State.ToString() ?? string.Empty), ("trigger", rejection.Trigger.ToString() ?? string.Empty),
                ("reason", rejection.Rejection.ToString())
            ]);
}
