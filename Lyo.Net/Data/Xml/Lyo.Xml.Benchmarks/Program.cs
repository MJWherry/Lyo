using Lyo.Benchmark;

[assembly:
    BenchmarkReport(
        "xml", "XML",
        Description = "XML write, read, nested, encoding, and JSON conversion for Lyo.Xml over a flat 7-column SampleRecord (int/string/decimal/bool/DateTime). " +
            "Write covers bytes, string, stream, file, async, and a pre-sized span. Read covers the same sources. " +
            "Encoding compares UTF-8, UTF-16, and UTF-32. NestedRecord round-trips a depth-2 address. RowCount is the number of records.")]

BenchmarkEntry.Run(args);
