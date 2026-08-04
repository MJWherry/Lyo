using Lyo.Benchmarking;

[assembly:
    BenchmarkReport(
        "compression", "Compression",
        Description = "Compress/decompress throughput for Lyo.Compression across GZip, Deflate, Zstd, Snappier, LZ4, LZMA, BZip2, " +
            "XZ (and Brotli/ZLib on net10.0). Buffered suites use seeded deterministic (incompressible) buffers at " +
            "100/250/500 MiB. Large suites (100 MiB–2 GiB) cover stream APIs (DeterministicPayloadStream → NullingStream) " +
            "and file APIs (IOTemp paths). Ratio on random data is not meaningful — these measure speed and framing.")]

BenchmarkEntry.Run(args);
