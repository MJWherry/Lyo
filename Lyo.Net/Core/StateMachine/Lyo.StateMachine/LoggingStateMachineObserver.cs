using Lyo.Exceptions;
using Microsoft.Extensions.Logging;

namespace Lyo.StateMachine;

/// <summary>Logs state machine activity. Register with <c>AddStateMachineLogging</c>.</summary>
/// <remarks>Accepted triggers log at <see cref="LogLevel.Debug" />, rejections at <see cref="LogLevel.Information" />. Nothing is formatted when the level is disabled.</remarks>
public sealed class LoggingStateMachineObserver<TState, TTrigger> : IStateMachineObserver<TState, TTrigger>
    where TState : notnull where TTrigger : notnull
{
    private readonly ILogger<LoggingStateMachineObserver<TState, TTrigger>> _logger;

    /// <summary>Creates the observer.</summary>
    public LoggingStateMachineObserver(ILogger<LoggingStateMachineObserver<TState, TTrigger>> logger)
    {
        ArgumentHelpers.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <inheritdoc />
    public void OnTransitioned(StateTransitioned<TState, TTrigger> transition)
    {
        if (!_logger.IsEnabled(LogLevel.Debug))
            return;

        _logger.LogDebug(
            "State machine {Machine}: {Source} -> {Destination} on {Trigger} ({Kind}, v{Version})", transition.MachineName, transition.Source, transition.Destination,
            transition.Trigger, transition.Kind, transition.Version);
    }

    /// <inheritdoc />
    public void OnRejected(StateTransitionRejected<TState, TTrigger> rejection)
    {
        if (!_logger.IsEnabled(LogLevel.Information))
            return;

        _logger.LogInformation("State machine {Machine}: rejected {Trigger} in {State} ({Rejection})", rejection.MachineName, rejection.Trigger, rejection.State, rejection.Rejection);
    }
}
