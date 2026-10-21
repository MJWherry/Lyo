using BenchmarkDotNet.Attributes;
using Lyo.Benchmark;
using Lyo.StateMachine.Models;

namespace Lyo.StateMachine.Benchmarks;

public enum BenchState
{
    Idle,
    Busy,
    Level1,
    Level2,
    Level3,
    Other
}

public enum BenchTrigger
{
    Go,
    Back,
    Leave,
    Unknown
}

public sealed class BenchContext
{
    public int Counter;
}

[BenchmarkDescription("One Fire/FireAsync call per operation against a prebuilt definition. Snapshots are created in setup, so only the transition itself is measured.")]
public class FireBenchmarks
{
    private readonly BenchContext _context = new();
    private StateMachineDefinition<BenchState, BenchTrigger, BenchContext> _dynamic = null!;
    private StateMachineDefinition<BenchState, BenchTrigger, BenchContext> _flat = null!;
    private StateSnapshot<BenchState> _idle = null!;
    private StateSnapshot<BenchState> _level3 = null!;
    private StateMachineDefinition<BenchState, BenchTrigger, BenchContext> _nested = null!;
    private StateMachineDefinition<BenchState, BenchTrigger, BenchContext> _withHooks = null!;

    [GlobalSetup]
    public void Setup()
    {
        var clock = DateTime.UnixEpoch;
        _idle = new(BenchState.Idle, 0, clock);
        _level3 = new(BenchState.Level3, 0, clock);
        var flat = new StateMachineBuilder<BenchState, BenchTrigger, BenchContext>("flat", BenchState.Idle).UseClock(() => clock);
        flat.Configure(BenchState.Idle).Permit(BenchTrigger.Go, BenchState.Busy);
        flat.Configure(BenchState.Busy).Permit(BenchTrigger.Back, BenchState.Idle);
        _flat = flat.Build();
        var hooks = new StateMachineBuilder<BenchState, BenchTrigger, BenchContext>("hooks", BenchState.Idle).UseClock(() => clock);
        hooks.Configure(BenchState.Idle).Permit(BenchTrigger.Go, BenchState.Busy).OnExit(t => t.Context.Counter++);
        hooks.Configure(BenchState.Busy).Permit(BenchTrigger.Back, BenchState.Idle).OnEntry(t => t.Context.Counter++);
        _withHooks = hooks.Build();
        var nested = new StateMachineBuilder<BenchState, BenchTrigger, BenchContext>("nested", BenchState.Level3).UseClock(() => clock);
        nested.Configure(BenchState.Level1).Permit(BenchTrigger.Leave, BenchState.Other).OnExit(t => t.Context.Counter++);
        nested.Configure(BenchState.Level2).SubstateOf(BenchState.Level1).OnExit(t => t.Context.Counter++);
        nested.Configure(BenchState.Level3).SubstateOf(BenchState.Level2).OnExit(t => t.Context.Counter++);
        nested.Configure(BenchState.Other).OnEntry(t => t.Context.Counter++);
        _nested = nested.Build();
        var dynamic = new StateMachineBuilder<BenchState, BenchTrigger, BenchContext>("dynamic", BenchState.Idle).UseClock(() => clock);
        dynamic.Configure(BenchState.Idle).PermitDynamic(BenchTrigger.Go, c => c.Counter >= 0 ? BenchState.Busy : BenchState.Other, BenchState.Busy, BenchState.Other);
        _dynamic = dynamic.Build();
    }

    [Benchmark(Baseline = true)]
    public TransitionResult<BenchState> Fire_Flat() => _flat.Fire(_context, _idle, BenchTrigger.Go);

    [Benchmark]
    public TransitionResult<BenchState> Fire_ThreeLevelHierarchy() => _nested.Fire(_context, _level3, BenchTrigger.Leave);

    [Benchmark]
    public TransitionResult<BenchState> Fire_Rejected() => _flat.Fire(_context, _idle, BenchTrigger.Unknown);

    [Benchmark]
    public Task<TransitionResult<BenchState>> FireAsync_SyncHooks() => _withHooks.FireAsync(_context, _idle, BenchTrigger.Go);

    [Benchmark]
    public TransitionResult<BenchState> Fire_Dynamic() => _dynamic.Fire(_context, _idle, BenchTrigger.Go);
}