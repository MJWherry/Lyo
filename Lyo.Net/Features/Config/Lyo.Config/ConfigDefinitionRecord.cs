using Lyo.Exceptions;

namespace Lyo.Config;

/// <summary>Defines an allowed config key for a given entity type.</summary>
public sealed class ConfigDefinitionRecord
{
    /// <summary>Gets or sets the unique identifier of the definition.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the entity type this definition applies to.</summary>
    public string ForEntityType { get; set; } = string.Empty;

    /// <summary>Gets or sets the config key.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the CLR type name for JSON values under this key, same form as <see cref="ForEntityType" /> (<see cref="Type.FullName" />; use
    /// <see cref="ConfigValue.GetTypeName(System.Type)" /> when seeding).
    /// </summary>
    public string ForValueType { get; set; } = string.Empty;

    /// <summary>Gets or sets an optional human-readable description.</summary>
    public string? Description { get; set; }

    /// <summary>
    /// When true, each entity must have a resolved value: a binding and/or a <see cref="DefaultValue" />. If there is no default, a binding is mandatory. Enforced when loading
    /// config and when deleting bindings.
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>Gets or sets the optional default value for the key.</summary>
    public ConfigValue? DefaultValue { get; set; }

    /// <summary>Gets or sets when the definition was created.</summary>
    public DateTime CreatedTimestamp { get; set; }

    /// <summary>Gets or sets when the definition was last updated.</summary>
    public DateTime? UpdatedTimestamp { get; set; }

    /// <summary>Returns true when the provided config value is allowed by this definition.</summary>
    public bool Accepts(ConfigValue value)
    {
        ArgumentHelpers.ThrowIfNull(value, nameof(value));
        return value.MatchesType(ForValueType);
    }

    /// <summary>Validates that the configured default value matches the configured value type.</summary>
    public void Validate()
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(ForEntityType, nameof(ForEntityType));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(Key, nameof(Key));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(ForValueType, nameof(ForValueType));
        // Do not pass a single ThrowIf(condition, $"…{DefaultValue.TypeName}…"): the message is evaluated before ThrowIf runs, so DefaultValue null still dereferenced.
        if (DefaultValue != null && !Accepts(DefaultValue))
            OperationHelpers.ThrowIf(true, $"Default value type '{DefaultValue.TypeName}' does not match definition type '{ForValueType}'.");
    }
}