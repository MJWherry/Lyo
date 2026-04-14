using Lyo.Common;

namespace Lyo.Config;

/// <summary>Binds a config definition to a specific entity instance.</summary>
public sealed class ConfigBindingRecord
{
    /// <summary>Gets or sets the unique identifier of the binding.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the definition identifier this binding is for.</summary>
    public Guid DefinitionId { get; set; }

    /// <summary>Gets or sets the config key. Duplicated for convenience when loading bindings directly.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Gets or sets the target entity type.</summary>
    public string ForEntityType { get; set; } = string.Empty;

    /// <summary>Gets or sets the target entity id.</summary>
    public string ForEntityId { get; set; } = string.Empty;

    /// <summary>Gets or sets the typed value bound to this entity.</summary>
    public ConfigValue Value { get; set; } = new();

    /// <summary>Gets or sets when the binding was created.</summary>
    public DateTime CreatedTimestamp { get; set; }

    /// <summary>Gets or sets when the binding was last updated.</summary>
    public DateTime? UpdatedTimestamp { get; set; }

    /// <summary>Gets the referenced entity.</summary>
    public EntityRef ForEntity => new(ForEntityType, ForEntityId);
}