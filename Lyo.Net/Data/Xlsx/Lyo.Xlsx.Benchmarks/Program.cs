using Lyo.Benchmarking;

[assembly:
    BenchmarkReport(
        "xlsx", "XLSX",
        Description = "XLSX write (ClosedXML) and read (ExcelDataReader) paths for Lyo.Xlsx over a flat 7-column SampleRecord. " +
            "Row counts are smaller than CSV because XLSX is markedly heavier per row. RowCount is the number of " +
            "records; the column set and types are captured in each class's data shape.")]

BenchmarkEntry.Run(args);