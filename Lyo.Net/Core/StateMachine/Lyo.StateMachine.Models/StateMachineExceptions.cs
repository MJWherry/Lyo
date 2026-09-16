namespace Lyo.StateMachine.Models;

/// <summary>Raised when a state machine definition is invalid (for example a substate cycle) or used in a way it does not support (sync fire with async hooks).</summary>
public sealed class StateMachineConfigurationException(string message) : Exception(message);

/// <summary>Raised for a trigger with no rule when the machine uses <c>UnhandledTriggerPolicy.Throw</c>.</summary>
public sealed class UnhandledTriggerException(string message) : Exception(message);

/// <summary>Raised when an instance (or, with reentrancy guarding on, a context object) is fired again while a fire is already running for it.</summary>
public sealed class StateMachineReentrancyException(string message) : InvalidOperationException(message);

/// <summary>Wraps an exception thrown by a guard, hook, action, or selector. The message names the machine, phase, state, and trigger; the original is the inner exception.</summary>
public sealed class StateMachineHookException(string message, Exception inner) : Exception(message, inner);
