using Lyo.Benchmarking;

[assembly:
    BenchmarkReport(
        "compression", "Compression",
        Description = "Compress/decompress throughput for Lyo.Compression across GZip, Deflate, Zstd, Snappier, LZ4, LZMA, BZip2, " +
            "XZ (and Brotli/ZLib on net10.0). Important caveat: the input is cryptographically random bytes of the " +
            "given DataSize, which is effectively incompressible - these benchmarks measure raw algorithm speed and " +
            "framing overhead, NOT achievable compression ratio on real data. Decompress cases reuse output produced " + "once in setup.")]

BenchmarkEntry.Run(args);