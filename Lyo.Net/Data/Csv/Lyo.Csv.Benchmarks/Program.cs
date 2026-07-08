using Lyo.Benchmarking;

[assembly:
    BenchmarkReport(
        "csv", "CSV",
        Description = "CSV write, read, split, and merge paths for Lyo.Csv over a flat 7-column SampleRecord (int/string/decimal/bool/DateTime). " +
            "Covers typed object lists (bytes and string), streaming parse, DataTable round-trips, row-based split/combine (bytes and files), and multi-part merge. RowCount is the " +
            "number of records; the data structure (columns and types) is captured in each class's data shape. The " +
            "SampleRecord here is flat - nested objects/collections would add per-row flattening cost not represented " + "by row count alone.")]

BenchmarkEntry.Run(args);