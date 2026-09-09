using System.Diagnostics;

namespace Lyo.Parameters;

/// <summary>One picker choice for <see cref="ParameterOptionsKind.Static" /> lists.</summary>
[DebuggerDisplay("{Key}={Label}")]
public sealed class ParameterOptionsItem
{
    /// <summary>Value stored when this item is chosen.</summary>
    public string Key { get; set; } = "";

    /// <summary>Label shown in the picker UI.</summary>
    public string Label { get; set; } = "";

    public ParameterOptionsItem() { }

    public ParameterOptionsItem(string key, string label)
    {
        Key = key;
        Label = label;
    }

    public override string ToString() => $"{Key}={Label}";
}