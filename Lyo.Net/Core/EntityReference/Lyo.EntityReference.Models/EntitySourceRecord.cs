namespace Lyo.EntityReference.Models;

/// <summary>Provenance for an ingested row: which external entity it came from and optional upstream ref.</summary>
/// <param name="Source">Stable <see cref="EntityRef" /> for the provider row (e.g. Endato PS person).</param>
/// <param name="ImportedAt">When this source link was recorded (UTC).</param>
/// <param name="ImportedFrom">Optional upstream ref (e.g. geolocation address after enrichment).</param>
public readonly record struct EntitySourceRecord(EntityRef Source, DateTime ImportedAt, EntityRef? ImportedFrom = null)
{
    /// <summary>Resolves <see cref="ImportedFrom" /> when present.</summary>
    public EntityRef? From => ImportedFrom;
}
