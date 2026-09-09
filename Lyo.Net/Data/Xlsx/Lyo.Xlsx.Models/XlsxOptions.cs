using System.Diagnostics;
using Lyo.DataTable.Models;

namespace Lyo.Xlsx.Models;

/// <summary>Tunable XLSX read/write settings, including DataTable value/format pooling.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class XlsxOptions
{
    /// <summary>Configuration section name used when binding.</summary>
    public const string SectionName = "Xlsx";

    /// <summary>Pooling used on DataTable import paths. Starts with pooling on above the default cell threshold.</summary>
    public DataTablePoolingOptions Pooling { get; set; } = new();

    /// <summary>Checks nested options.</summary>
    public void Validate() => Pooling.Validate();

    /// <inheritdoc />
    public override string ToString() => $"XlsxOptions: Pooling=({Pooling})";
}