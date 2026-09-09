using Lyo.Exceptions;

namespace Lyo.Config;

/// <summary>A given entity type defines an allowed config key.</summary>
public sealed class ConfigDefinitionRecord
{
    /// <summary>Unique identifier of the definition.</summary>
    public Guid Id { get; set; }

    /// <summary>Holds the entity type this definition applies to.</summary>
    public string SubjectEntityType { get; set; } = string.Empty;

    /// <summary>Value of the config key.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// CLR type name for JSON values under this key, same form as <see cref="ForEntityType" /> (<see cref="Type.FullName" />; use.
    /// <see cref="ConfigValue.GetTypeName(System.Type)" /> during seed).
    /// </summary>
    public string ForValueType { get; set; } = string.Empty;

    /// <summary>Optional field: an optional human-readable description.</summary>
    public string? Description { get; set; }

    /// <summary>
    /// If set, each entity must have a resolved value: a binding and/or a <see cref="DefaultValue" />. If there is no default, a binding is mandatory. Enforced when loading
    /// config, and when bindings are deleted.
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>Optional default value for the key for this record.</summary>
    public ConfigValue? DefaultValue { get; set; }

    /// <summary>
    /// If set, the API host encrypts the default and all binding values at rest. Clients send plaintext plus this flag; they do not encrypt.
    /// </summary>
    public bool IsEncrypted { get; set; }

    /// <summary>When the definition was created.</summary>
    public DateTime CreatedTimestamp { get; set; }

    /// <summary>When the definition was last updated.</summary>
    public DateTime? UpdatedTimestamp { get; set; }

    /// <summary>True when the provided config value is allowed by this definition.</summary>
    public bool Accepts(ConfigValue value)
    {
        ArgumentHelpers.ThrowIfNull(value);
        return value.MatchesType(ForValueType);
    }

    /// <summary>Checks that the configured default value matches the configured value type.</summary>
    public void Validate() => ConfigValidators.Definition.Validate(this).ValueOrThrow();
}