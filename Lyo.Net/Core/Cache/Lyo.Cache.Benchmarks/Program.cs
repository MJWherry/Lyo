using Lyo.Benchmarking;

[assembly:
    BenchmarkReport(
        "cache", "Caching",
        Description = "Cache hot-path costs for Lyo.Cache: local IMemoryCache vs FusionCache for object get/set, the serialized " +
            "byte-payload path (JSON +/- compression framing) across payload sizes, and FusionCache with a Redis " +
            "backplane (Docker). Object cases use a small CachePayload; payload cases vary DataSize (the length of a " +
            "compressible string body) to show framing/serialization cost.")]

BenchmarkEntry.Run(args);