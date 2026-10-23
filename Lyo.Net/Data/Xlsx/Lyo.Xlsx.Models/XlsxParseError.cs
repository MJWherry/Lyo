using System.Diagnostics;

namespace Lyo.Xlsx.Models;

/// <summary>One cell that could not be bound. The row is omitted from the good list.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class XlsxParseError
{
    /// <summary>1-based worksheet row. The header is row 1.</summary>
    public int RowNumber { get; init; }

    /// <summary>Header of the cell that failed.</summary>
    public string? ColumnName { get; init; }

    /// <summary>Cell text that failed to convert.</summary>
    public string? Raw { get; init; }

    /// <summary>Why the cell failed.</summary>
    public string Message { get; init; } = string.Empty;

    /// <inheritdoc />
    public override string ToString() => $"XlsxParseError: RowNumber={RowNumber}, ColumnName='{ColumnName}', Raw='{Raw}', Message='{Message}'";
}