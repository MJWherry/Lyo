using Lyo.Json;

namespace Lyo.Yaml;

/// <summary>Options for <see cref="YamlService" />. The JSON contract fields control names, numbers, and omission. <see cref="JsonOptions.WriteIndented" /> selects block YAML when true and flow YAML when false.</summary>
public sealed class YamlOptions : JsonOptions
{
    /// <summary>Configuration section name. Nothing in this package binds it.</summary>
    public new const string SectionName = "Lyo:Yaml";

    /// <summary>Builds options. YAML starts indented, which means a block document.</summary>
    public YamlOptions() => WriteIndented = true;

    /// <summary>When true, a document starts with <c>---</c> and ends with <c>...</c>. Starts false.</summary>
    public bool ExplicitMarkers { get; set; }

    /// <summary>When true, <c>---</c> separates documents in a <see cref="List{T}" />. Export of a list writes one document per item. Starts false.</summary>
    public bool MultiDocument { get; set; }

    /// <summary>When true, anchors and aliases expand. Starts false.</summary>
    public bool AllowAliases { get; set; }

    /// <summary>How strings are quoted. Starts as plain.</summary>
    public YamlQuoteStyle QuoteStrings { get; set; } = YamlQuoteStyle.Plain;
}

/// <summary>Quoting for YAML strings.</summary>
public enum YamlQuoteStyle
{
    /// <summary>Leave strings plain when YAML allows it.</summary>
    Plain,

    /// <summary>Wrap strings in single quotes.</summary>
    SingleQuoted,

    /// <summary>Wrap strings in double quotes.</summary>
    DoubleQuoted
}
