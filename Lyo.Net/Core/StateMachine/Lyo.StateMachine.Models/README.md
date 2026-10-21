# Lyo.StateMachine.Models

Dependency-free contracts shared by [`Lyo.StateMachine`](../Lyo.StateMachine/README.md) and its stores such as [
`Lyo.StateMachine.Postgres`](../Lyo.StateMachine.Postgres/README.md). Reference this package when you only need to pass snapshots around or implement a store.

## Features

- **`StateSnapshot<TState>`.** Immutable saved state: current state, version, entry time, optional timer due time, and optional instance data JSON. All times are UTC. New instances
  start at version 0.
- **`TransitionResult<TState>`.** `Succeeded`, `Changed`, `Kind` (`Transition`, `Reentry`, `Internal`, `Ignored`), `From`, `To`, and the `Snapshot`. Invalid transitions return a
  `TransitionRejection` (`NoTransition`, `GuardFailed`, `ConcurrencyConflict`) instead of throwing.
- **`TransitionRecord<TState>`.** History entry (from, to, trigger name, version, time) written by stores next to the snapshot.
- **`IStateStore<TState>`.** `LoadAsync` returns null for unsaved instances. `SaveAsync` writes only when the stored version equals `expectedVersion` (0 for a new instance) and
  returns false otherwise.
- **`IStateInstanceQuery<TState>`.** List instances filtered by states, due time (for timer polling), or update time; count by state; page history; delete with an optional version
  check.
- **`IStateKeyConverter<T>`.** Turns states and triggers into stable string keys. `EnumStateKeyConverter<TEnum>` (member names) and `StringStateKeyConverter` ship here.
- **`StateMachineDocument` and `IStateMachineDefinitionStore`.** A machine as data (states, parents, initial substates, final flags, timers, hook names, and
  Permit/Reentry/Internal/Ignore/Dynamic rules) saved as append-only revisions.
- **Exceptions.** `StateMachineConfigurationException` (invalid definition), `UnhandledTriggerException`, `StateMachineHookException` (wraps guard/hook failures), and
  `StateMachineReentrancyException`.

## Examples

### Implementing a store

```csharp
public sealed class MyStore : IStateStore<JobState>
{
    public Task<StateSnapshot<JobState>?> LoadAsync(string machineName, string instanceKey, CancellationToken ct = default) => ...;

    // Write only if the stored version is still expectedVersion; return false otherwise.
    public Task<bool> SaveAsync(string machineName, string instanceKey, StateSnapshot<JobState> snapshot, long expectedVersion,
        TransitionRecord<JobState>? transition = null, CancellationToken ct = default) => ...;
}
```

### A definition document

```csharp
var document = new StateMachineDocument {
    Name = "job",
    InitialState = "queued",
    States = [
        new() { Name = "queued", Transitions = [new() { Kind = TransitionDocumentKind.Permit, Trigger = "start", Destination = "running" }] },
        new() {
            Name = "running",
            Timers = [new() { After = TimeSpan.FromMinutes(30), Trigger = "complete" }],
            Transitions = [new() { Kind = TransitionDocumentKind.Dynamic, Trigger = "complete", Destinations = ["done", "failed"], Selector = "pick" }]
        },
        new() { Name = "done", IsFinal = true },
        new() { Name = "failed", IsFinal = true }
    ]
};
```