namespace Lyo.Config;

/// <summary>Read-only snapshot of a <see cref="ConfigDefinitionRecord" /> at a point in time (monotonic <see cref="Revision" /> per definition).</summary>
public sealed class ConfigDefinitionRevisionRecord
{
    /// <summary>Owning definition id (with <see cref="Revision" />, forms the store key).</summary>
    public Guid DefinitionId { get; set; }

    /// <summary>This definition 1-based revision number.</summary>
    public int Revision { get; set; }

    /// <summary>Key stored on this revision.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>CLR type name stored on this revision.</summary>
    public string ForValueType { get; set; } = string.Empty;

    /// <summary>Description stored on this revision.</summary>
    public string? Description { get; set; }

    /// <summary>Flag: the key was required.</summary>
    public bool IsRequired { get; set; }

    /// <summary>True when values for this key were stored encrypted.</summary>
    public bool IsEncrypted { get; set; }

    /// <summary>Default captured on this revision (decrypted for store callers when encryption is enabled).</summary>
    public ConfigValue? DefaultValue { get; set; }

    /// <summary>Timestamp of when this revision was written.</summary>
    public DateTime CreatedTimestamp { get; set; }
}
