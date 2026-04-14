using System.Diagnostics;

namespace Lyo.Csv.Models;

/// <summary>Maps a CSV column to a target property with optional transformation.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class ColumnMapping
{
    /// <summary>Gets or sets the source column name from the CSV.</summary>
    public string SourceColumn { get; set; } = string.Empty;

    /// <summary>Gets or sets the target property name on the destination object.</summary>
    public string TargetProperty { get; set; } = string.Empty;

    /// <summary>Gets or sets an optional transformer function to convert the CSV value.</summary>
    public Func<string, object>? Transformer { get; set; }

    /// <summary>Gets or sets a default value to use if the source column is missing or empty.</summary>
    public object? DefaultValue { get; set; }

    public override string ToString()
        => $"ColumnMapping: SourceColumn='{SourceColumn}', TargetProperty='{TargetProperty}', HasTransformer={Transformer != null}, DefaultValue='{DefaultValue}'";
}