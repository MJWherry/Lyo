using System.Diagnostics;

namespace Lyo.Csv.Models;

/// <summary>Options for CSV parsing behavior.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class CsvParseOptions
{
    /// <summary>Gets or sets a value indicating whether to continue parsing when errors occur. Default is false.</summary>
    public bool ContinueOnError { get; set; }

    /// <summary>Gets or sets an optional callback invoked when a parsing error occurs.</summary>
    public Action<CsvParseError>? OnError { get; set; }

    /// <summary>Gets or sets an optional filter function to skip certain rows during parsing.</summary>
    public Func<Dictionary<string, string>, bool>? RowFilter { get; set; }

    /// <summary>Gets or sets the maximum number of rows to parse. If null, all rows are parsed.</summary>
    public int? MaxRows { get; set; }

    public override string ToString()
        => $"CsvParseOptions: ContinueOnError={ContinueOnError}, HasOnError={OnError != null}, HasRowFilter={RowFilter != null}, MaxRows={MaxRows?.ToString() ?? "All"}";
}