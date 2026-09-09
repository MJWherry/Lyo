namespace Lyo.Config;

/// <summary>Model for a config definition plus the current value resolved for an entity.</summary>
public sealed class ResolvedConfigItemRecord
{
    /// <summary>Value of the config definition.</summary>
    public ConfigDefinitionRecord Definition { get; set; } = new();

    /// <summary>Holds the entity-specific binding, if one exists.</summary>
    public ConfigBindingRecord? Binding { get; set; }

    /// <summary>Returns the effective value, preferring a binding over the default value.</summary>
    public ConfigValue? Value => Binding?.Value ?? Definition.DefaultValue;
}