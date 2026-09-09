using System.Diagnostics;
using Lyo.Common.Metadata.Records;

namespace Lyo.Reporting.Models.Models;

/// <summary>
/// Design-time parameter declared on a report composition. The workbench uses this for the schema and example values; generation still
/// persists the same keys on the definition / generation rows.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class ParameterSpec
{
    /// <summary>Parameter key referenced from composition text as <c>{Key}</c>.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>CLR FullName (or catalog alias) for the JSON value. Defaults to string.</summary>
    public string Type { get; set; } = LyoTypeInfo.String.FullName;

    /// <summary>Optional human-readable description shown in the workbench and run dialog.</summary>
    public string? Description { get; set; }

    /// <summary>When true, generate / preview requires a value.</summary>
    public bool Required { get; set; }

    /// <summary>Literal default used when a caller omits the parameter.</summary>
    public string? DefaultValue { get; set; }

    /// <summary>Sample value used by the design workbench to preview interpolation without a real generate.</summary>
    public string? ExampleValue { get; set; }

    /// <summary>JSON array of allowed values, same contract as definition parameters.</summary>
    public string? AllowedValues { get; set; }

    public override string ToString() => $"{Key} ({Type}) example={ExampleValue ?? DefaultValue ?? "-"}";
}
