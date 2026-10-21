using Lyo.Benchmark;

[assembly:
    BenchmarkReport(
        "state-machine", "State machine",
        Description = "Single-trigger fire cost for Lyo.StateMachine definitions: a flat two-state toggle, a transition inherited across a three-level " +
            "hierarchy (three exits, one entry), a rejected unknown trigger, FireAsync on a machine with only sync hooks, and a dynamic destination.")]

BenchmarkEntry.Run(args);