using Lyo.Benchmark;

[assembly:
    BenchmarkReport(
        "xlsx", "XLSX",
        Description = "XLSX write (streaming OpenXML), read (ExcelDataReader), convert, split, and merge paths for Lyo.Xlsx over a flat 7-column SampleRecord. " +
            "Split benchmarks cover row chunks and per-worksheet outputs; merge benchmarks cover PreserveSheets and ConcatenateRows. Row counts are smaller than CSV because XLSX is markedly heavier per row. RowCount is the number of " +
            "records; the column set and types are captured in each class's data shape.")]

BenchmarkEntry.Run(args);