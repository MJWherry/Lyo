using Lyo.Benchmark;

[assembly:
    BenchmarkReport(
        "lock", "Locking",
        Description = "Distributed-lock primitives for Lyo.Lock: uncontended acquire/release and execute-with-lock for the " +
            "in-process LocalLockService vs the distributed RedisLockService (throwaway Docker Redis), plus a bounded " +
            "contended local scenario. Each operation uses a unique key (GUID) so cases are uncontended unless stated; " +
            "the contended case measures coordination overhead under N contenders, not raw throughput.")]

BenchmarkEntry.Run(args);