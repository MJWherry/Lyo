namespace Lyo.Config;

/// <summary>Represents a config definition plus the current value resolved for an entity.</summary>
public sealed class ResolvedConfigItemRecord
{
    /// <summary>Gets or sets the config definition.</summary>
    public ConfigDefinitionRecord Definition { get; set; } = new();

    /// <summary>Gets or sets the entity-specific binding, if one exists.</summary>
    public ConfigBindingRecord? Binding { get; set; }

    /// <summary>Gets the effective value, preferring a binding over the default value.</summary>
    public ConfigValue? Value => Binding?.Value ?? Definition.DefaultValue;
}