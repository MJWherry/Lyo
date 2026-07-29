using System.Diagnostics;

namespace Lyo.DataTable.Models;

/// <summary>
/// Options for parse-scoped value and format pooling when building a <see cref="DataTable" />.
/// Create one <see cref="DataTableValueInterner" /> per parse (single-threaded). Pools are not process-wide string interning.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class DataTablePoolingOptions
{
    /// <summary>Configuration section name for binding.</summary>
    public const string SectionName = "DataTablePooling";

    /// <summary>Default estimated cell count at/above which enabled pools activate.</summary>
    public const int DefaultPoolingCellThreshold = 512;

    /// <summary>When true, intern duplicate cell string values via a parse-scoped pool.</summary>
    public bool PoolValues { get; set; } = true;

    /// <summary>When true, intern duplicate <see cref="DataTableCellFormat" /> instances via a parse-scoped pool.</summary>
    public bool PoolFormats { get; set; } = true;

    /// <summary>
    /// Estimated cell count at/above which enabled pools activate. Use <c>0</c> to always pool when the corresponding flag is on.
    /// Default is <see cref="DefaultPoolingCellThreshold" />.
    /// </summary>
    public int PoolingCellThreshold { get; set; } = DefaultPoolingCellThreshold;

    /// <summary>Validates options. Throws when <see cref="PoolingCellThreshold" /> is negative.</summary>
    public void Validate()
    {
        if (PoolingCellThreshold < 0)
            throw new ArgumentOutOfRangeException(nameof(PoolingCellThreshold), PoolingCellThreshold, "PoolingCellThreshold must be >= 0.");
    }

    /// <inheritdoc />
    public override string ToString()
        => $"DataTablePoolingOptions: PoolValues={PoolValues}, PoolFormats={PoolFormats}, PoolingCellThreshold={PoolingCellThreshold}";
}
