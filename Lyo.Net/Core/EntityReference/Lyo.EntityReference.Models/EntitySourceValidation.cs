namespace Lyo.EntityReference.Models;

/// <summary>Validates external source keys on provenance records.</summary>
public static class EntitySourceValidation
{
    /// <summary>Requires a non-empty external source reference.</summary>
    public static void RequireSource(EntityRef? source)
    {
        if (source is null)
            throw new ArgumentException("Source link requires a non-null EntityRef source.");
    }

    /// <summary>Requires a non-empty external source on a provenance record.</summary>
    public static void RequireSource(EntitySourceRecord record)
        => RequireSource(record.Source);
}
