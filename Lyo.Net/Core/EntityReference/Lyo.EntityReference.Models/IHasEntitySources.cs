namespace Lyo.EntityReference.Models;

/// <summary>Domain type that carries import provenance via <see cref="EntitySourceRecord" /> rows.</summary>
public interface IHasEntitySources
{
    /// <summary>Import lineage for this entity (may be empty for in-app creates).</summary>
    ICollection<EntitySourceRecord> Sources { get; }
}
