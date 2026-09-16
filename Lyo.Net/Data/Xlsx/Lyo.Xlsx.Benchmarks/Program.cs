using Lyo.Benchmark;

[assembly:
    BenchmarkReport(
        "xlsx", "XLSX",
        Description = "XLSX write (streaming OpenXML), read (ExcelDataReader), nested, convert, split, and merge paths for Lyo.Xlsx over a flat 7-column SampleRecord. " +
            "Write covers bytes, stream, file, async, selected columns, multi-sheet, DataTable, and a dictionary source. Read covers dictionary and DataTable parse from bytes, stream, file, and async. " +
            "NestedRecord writes nested members as text. Convert covers CSV in UTF-8, UTF-16, and UTF-32 plus HTML. " +
            "Split covers row chunks and per-worksheet outputs; merge covers PreserveSheets and ConcatenateRows. RowCount is the number of records.")]

BenchmarkEntry.Run(args);