using System.Diagnostics;

namespace Lyo.Query.Models.Parameters;

/// <summary>One selectable option for <see cref="ParameterOptionsKind.Static" /> pickers.</summary>
[DebuggerDisplay("{Key}={Label}")]
public sealed class ParameterOptionsItem
{
    /// <summary>Stored parameter value when this item is selected.</summary>
    public string Key { get; set; } = "";

    /// <summary>Display text in the picker UI.</summary>
    public string Label { get; set; } = "";

    public ParameterOptionsItem() { }

    public ParameterOptionsItem(string key, string label)
    {
        Key = key;
        Label = label;
    }

    public override string ToString() => $"{Key}={Label}";
}