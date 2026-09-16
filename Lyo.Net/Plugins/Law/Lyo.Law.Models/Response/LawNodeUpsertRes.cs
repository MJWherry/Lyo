namespace Lyo.Law.Models.Response;

/// <summary>Result of a node upsert.</summary>
public sealed class LawNodeUpsertRes
{
    /// <summary>Stored node id. Stable across repeats of the same source key.</summary>
    public Guid Id { get; set; }

    /// <summary>True when the content hash matched and the row was left as it was.</summary>
    public bool Unchanged { get; set; }
}
