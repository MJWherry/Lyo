using System.Diagnostics;

namespace Lyo.Csv.Models;

/// <summary>Maps a CSV column to a target property with an optional transform.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class ColumnMapping
{
    /// <summary>Source column name from the CSV.</summary>
    public string SourceColumn { get; set; } = string.Empty;

    /// <summary>Target property name on the destination object.</summary>
    public string TargetProperty { get; set; } = string.Empty;

    /// <summary>Optional transformer that converts the CSV value.</summary>
    public Func<string, object>? Transformer { get; set; }

    /// <summary>Default value when the source column is missing or empty.</summary>
    public object? DefaultValue { get; set; }

    /// <inheritdoc />
    public override string ToString()
        => $"ColumnMapping: SourceColumn='{SourceColumn}', TargetProperty='{TargetProperty}', HasTransformer={Transformer != null}, DefaultValue='{DefaultValue}'";
}