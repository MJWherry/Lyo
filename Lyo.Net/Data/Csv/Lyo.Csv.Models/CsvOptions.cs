using System.Diagnostics;
using Lyo.DataTable.Models;

namespace Lyo.Csv.Models;

/// <summary>
/// Service-level options for CSV, including DataTable value pooling.
/// CSV defaults disable value pooling (unique-heavy grids); enable via <see cref="Pooling" /> or config when duplication is high.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class CsvOptions
{
    /// <summary>Configuration section name for binding.</summary>
    public const string SectionName = "Csv";

    /// <summary>
    /// Pooling options for CSV → DataTable imports.
    /// Defaults: <see cref="DataTablePoolingOptions.PoolValues" /> = false (format pooling unused for CSV).
    /// </summary>
    public DataTablePoolingOptions Pooling { get; set; } = CreateDefaultPooling();

    /// <summary>Creates CSV-oriented pooling defaults (<c>PoolValues=false</c>, <c>PoolFormats=false</c>).</summary>
    public static DataTablePoolingOptions CreateDefaultPooling()
        => new() { PoolValues = false, PoolFormats = false };

    /// <summary>Validates nested options.</summary>
    public void Validate() => Pooling.Validate();

    /// <inheritdoc />
    public override string ToString() => $"CsvOptions: Pooling=({Pooling})";
}
