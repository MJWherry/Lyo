# Lyo.StateMachine

Build a `StateMachineDefinition<TState, TTrigger, TContext>` once and share it. The definition holds rules only; each entity keeps a small `StateSnapshot<TState>` (from [`Lyo.StateMachine.Models`](../Lyo.StateMachine.Models/README.md)) in memory or in a store. `TContext` is your host object (character, job row, worker), so guards and hooks stay typed without casts. Definitions can also be loaded from data (`StateMachineDocument`) with guards and hooks resolved by name.

## Features

- **Precompiled and allocation-light.** `Build()` compiles a trigger table per state with inherited rules and flattened exit/entry hook paths. A flat `Fire` is one dictionary lookup (about 55 ns and 152 B). With no async hooks, `FireAsync` runs synchronously and returns a completed task.
- **Typed payload triggers.** `Trigger<TTrigger, TPayload>` makes the compiler require the payload when firing. Guards, selectors, internal actions, and `OnEntryFrom` hooks receive it typed.
- **Results, not exceptions.** An unknown trigger or a failed guard returns a `TransitionResult` rejection. `OnUnhandledTrigger(UnhandledTriggerPolicy.Throw)` or a callback changes the unknown-trigger case.
- **Rule kinds.** `Permit`, `PermitReentry`, `PermitDynamic` (selector picks from declared destinations), `InternalTransition` (action only; same snapshot and version), and `Ignore` (accepted, nothing runs). `TransitionResult.Kind` says which happened.
- **Hierarchical states.** `SubstateOf(parent)` inherits the parent's transitions (a child rule overrides). `InitialSubstate(child)` drills into a child whenever the parent is entered. Exit runs child to parent, entry runs parent to child, skipping the common ancestor.
- **Timers.** `After(TimeSpan, trigger)` sets `StateSnapshot.DueAtUtc` on entry. The host drives it with `GetDueTrigger` or `StateMachineInstance.FireDueAsync`.
- **Validation.** `Analyze()` reports unreachable and dead-end states (states marked `AsFinal()` are allowed to be dead ends). `Strict()` makes `Build()` throw for either.
- **Hardening.** Guard, hook, and selector exceptions are wrapped in `StateMachineHookException` naming the machine, phase, state, and trigger. Cancellation is checked before every hook. Snapshot times and the clock must be UTC. `GuardReentrancy()` detects a hook firing the same context again.
- **Observers.** `IStateMachineObserver` gets `OnTransitioned` (with kind and measured duration) and `OnRejected`. Built-in `MetricsStateMachineObserver` (`Lyo.Metrics`) and `LoggingStateMachineObserver` (`ILogger`).
- **Persistence.** `StateMachineInstance` loads, fires, and saves through any `IStateStore<TState>` with optimistic concurrency, optional conflict retries, typed instance data (`GetDataAsync`/`UpdateDataAsync`), and one operation at a time. `InMemoryStateStore` also implements `IStateInstanceQuery`; see [`Lyo.StateMachine.Postgres`](../Lyo.StateMachine.Postgres/README.md) for a database store.
- **Data-driven definitions.** `StateMachineRegistry` holds named guards, actions, selectors, and payload declarations. `StateMachineDocumentLoader` builds a definition from a `StateMachineDocument`, reporting every missing name at once. `ToDocument` exports, `StateMachineDocumentJson` serializes, `StateMachineDefinitionCache` loads revisions from an `IStateMachineDefinitionStore`.
- **Diagrams.** `ToMermaid()` (stateDiagram-v2 with nested states, initial substates, final states, dynamic and internal edges) and `ToDot()` (Graphviz clusters).

## Examples

### Define and fire

```csharp
static readonly Trigger<JobTrigger, RetryPayload> Retry = new(JobTrigger.Retry);

var definition = new StateMachineBuilder<JobState, JobTrigger, JobContext>("job", JobState.Queued)
    .Configure(JobState.Queued).Permit(JobTrigger.Start, JobState.Running)
    .Configure(JobState.Running)
        .Permit(JobTrigger.Complete, JobState.Done)
        .PermitIf(Retry, JobState.Queued, (ctx, p) => p.Attempt < ctx.MaxAttempts, "attempts left")
        .OnEntryAsync((t, ct) => t.Context.Log.StartedAsync(ct))
    .Configure(JobState.Paused).SubstateOf(JobState.Running)
    .Build();

var snapshot = definition.CreateInitialSnapshot();
var result = await definition.FireAsync(ctx, snapshot, JobTrigger.Start, ct);
if (result.Succeeded)
    snapshot = result.Snapshot;
```

### Game loop (sync)

```csharp
var result = characterMachine.Fire(character, character.Snapshot, CharacterTrigger.Hit);
if (result.Succeeded)
    character.Snapshot = result.Snapshot;
```

### Persisted instance and DI

```csharp
services.AddStateMachineMetrics<JobState, JobTrigger>();
services.AddStateMachineLogging<JobState, JobTrigger>();
services.AddStateMachine<JobState, JobTrigger, JobContext>("job", JobState.Queued, b => b
    .Configure(JobState.Queued).Permit(JobTrigger.Start, JobState.Running));

var instance = new StateMachineInstance<JobState, JobTrigger, JobContext>(definition, store, jobId.ToString(),
    new() { MaxConflictRetries = 2 });
var result = await instance.FireAsync(ctx, JobTrigger.Start, ct);
await instance.UpdateDataAsync<JobProgress>(p => new((p?.Percent ?? 0) + 10), ct);
```

### Timers, dynamic destinations, internal and ignored triggers

```csharp
builder.Configure(JobState.Running)
    .PermitDynamic(JobTrigger.Complete, c => c.HasErrors ? JobState.Failed : JobState.Done, JobState.Done, JobState.Failed)
    .InternalTransition(JobTrigger.Progress, t => t.Context.Touch())
    .Ignore(JobTrigger.Start)
    .After(TimeSpan.FromMinutes(30), JobTrigger.Complete);

// Host scheduler: poll due instances and fire their timers.
foreach (var due in await query.ListAsync("job", new() { DueBeforeUtc = DateTime.UtcNow }, ct: ct))
    await new StateMachineInstance<JobState, JobTrigger, JobContext>(definition, store, due.InstanceKey)
        .FireDueAsync(ctx, DateTime.UtcNow, ct);
```

### Definitions from a database

```csharp
var registry = new StateMachineRegistry<string, string, JobContext>()
    .Guard("can fail", c => c.CanFail)
    .Payload<RetryPayload>("retry")
    .Guard<RetryPayload>("attempts left", (c, p) => p.Attempt < c.MaxAttempts)
    .Action("notify", t => t.Context.Notify(t.Destination))
    .Selector("pick", c => c.HasErrors ? "failed" : "done");

await definitionStore.SaveAsync(document, expectedRevision: 0, ct);
var cache = new StateMachineDefinitionCache<string, string, JobContext>(definitionStore, registry,
    StringStateKeyConverter.Instance, StringStateKeyConverter.Instance);
var definition = await cache.GetAsync("job", ct: ct);
```

## Rules worth knowing

- Two rules for the same trigger on one state are allowed only when both are guarded; the first passing guard wins.
- `Permit` to the same state is rejected at build time. Use `PermitReentry` so exit and entry hooks run.
- `CanFire` and `GetPermittedTriggers` cannot evaluate payload guards and report those rules as permitted.
- `StateMachineInstance` runs hooks before saving. On a concurrency conflict nothing is saved but hook side effects already happened, and `MaxConflictRetries` runs them again, so keep hooks idempotent.
- Timers are not inherited: `DueAtUtc` comes from the earliest timer on the state actually entered (after drilling into initial substates). Timer triggers must be permitted from that state and take no payload.
- `PermitDynamic` selectors must return one of the declared destinations; anything else throws `InvalidOperationException`.
- Internal and ignored triggers return the same snapshot and are not saved by `StateMachineInstance`.
- A clock that returns a non-UTC time throws `StateMachineConfigurationException` on fire; a non-UTC snapshot throws `ArgumentException`.
- `ToDocument` exports guard descriptions as guard names and skips unnamed code hooks, so register matching names before loading an exported code-built machine.
- Before publishing a revision that drops states, check `StateMachineDocumentCompatibility.FindRemovedStates`; saved instances in a removed state can no longer fire.
- The machine name keys persisted instances. Do not rename a machine that has saved data.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Json` (direct, lyo)
- `Lyo.Metrics` (direct, lyo)
- `Lyo.StateMachine.Models` (direct, lyo)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (direct, microsoft)
- `System.Text.Json` `10.0.5` (direct, microsoft, netstandard2.0)
- `Lyo.Common.Core` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Encoding.CodePages` `10.0.5` (transitive, microsoft)