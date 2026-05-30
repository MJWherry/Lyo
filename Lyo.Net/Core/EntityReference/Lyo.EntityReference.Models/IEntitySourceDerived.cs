namespace Lyo.EntityReference.Models;

/// <summary>Domain type imported from or linked to an external source; may diverge after local edits.</summary>
public interface IEntitySourceDerived
{
    /// <summary>Import provenance when this row was ingested from an external system; null for in-app creates.</summary>
    EntitySourceRecord? Source { get; set; }

    /// <summary>When set, content was edited after import and may no longer match the external source.</summary>
    DateTime? LocallyModifiedAt { get; set; }
}
