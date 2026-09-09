using Lyo.EntityReference.Models;

namespace Lyo.Config;

/// <summary>Attaches a config definition to a specific entity instance.</summary>
public sealed class ConfigBindingRecord
{
    /// <summary>Unique identifier of the binding.</summary>
    public Guid Id { get; set; }

    /// <summary>Definition identifier this binding is for for this record.</summary>
    public Guid DefinitionId { get; set; }

    /// <summary>Value of the config key. Duplicated for convenience when loading bindings directly.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Value of the target entity type.</summary>
    public string SubjectEntityType { get; set; } = string.Empty;

    /// <summary>Target entity id (string — supports composite keys, e.g. Config app routes use <c>kind:id</c>) for this record.</summary>
    public string SubjectEntityId { get; set; } = string.Empty;

    /// <summary>Typed value bound to this entity for this record.</summary>
    public ConfigValue Value { get; set; } = new();

    /// <summary>Clock time for when the binding was created.</summary>
    public DateTime CreatedTimestamp { get; set; }

    /// <summary>Clock time for when the binding was last updated.</summary>
    public DateTime? UpdatedTimestamp { get; set; }

    /// <summary>Returns the referenced entity.</summary>
    public EntityRef ForEntity => EntityRef.ForKey(SubjectEntityType, SubjectEntityId);
}