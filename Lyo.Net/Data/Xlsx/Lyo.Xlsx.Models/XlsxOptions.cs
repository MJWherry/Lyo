using System.Diagnostics;
using Lyo.Common.Core.Enums;
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

    /// <summary>Column order for a typed export and a DataTable export. Starts as declaration order. A caller-supplied column list stays in the caller's order.</summary>
    public MemberOrder Order { get; set; } = MemberOrder.Defined;

    /// <summary>Header names used when <see cref="Order" /> is <see cref="MemberOrder.Specified" />. Names in this list are written first, in this sequence.</summary>
    public List<string> OrderNames { get; } = [];

    /// <summary>Checks nested options.</summary>
    public void Validate() => Pooling.Validate();

    /// <inheritdoc />
    public override string ToString() => $"XlsxOptions: Pooling=({Pooling})";
}