using System.Diagnostics;
using Lyo.DataTable.Models;

namespace Lyo.Xlsx.Models;

/// <summary>Options for XLSX read/write, including DataTable value/format pooling.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class XlsxOptions
{
    /// <summary>Configuration section name for binding.</summary>
    public const string SectionName = "Xlsx";

    /// <summary>Pooling options for DataTable import paths. Defaults enable pooling above the default cell threshold.</summary>
    public DataTablePoolingOptions Pooling { get; set; } = new();

    /// <summary>Validates nested options.</summary>
    public void Validate() => Pooling.Validate();

    /// <inheritdoc />
    public override string ToString() => $"XlsxOptions: Pooling=({Pooling})";
}
