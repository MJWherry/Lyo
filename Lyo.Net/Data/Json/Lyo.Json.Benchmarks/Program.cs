using Lyo.Benchmark;

[assembly:
    BenchmarkReport(
        "json", "JSON",
        Description = "JSON write, read, nested, NDJSON, and encoding paths for Lyo.Json over a flat 7-column SampleRecord (int/string/decimal/bool/DateTime). " +
            "Write covers bytes, string, stream, file, async, a pre-sized span, and NDJSON. Read covers the same sources plus NDJSON. " +
            "Encoding compares UTF-8, UTF-16, and UTF-32. NestedRecord round-trips a depth-2 address. RowCount is the number of records.")]

BenchmarkEntry.Run(args);
