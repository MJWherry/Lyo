namespace Lyo.HomeInventory.Models.Response;

/// <summary>Quantity buckets for an item at a location.</summary>
public sealed class HomeItemStockRes
{
    /// <summary>Item id.</summary>
    public Guid ItemId { get; set; }

    /// <summary>Location id.</summary>
    public Guid LocationId { get; set; }

    /// <summary>On-hand quantity.</summary>
    public decimal QuantityOnHand { get; set; }

    /// <summary>Reserved quantity.</summary>
    public decimal QuantityReserved { get; set; }

    /// <summary>Optional reorder threshold.</summary>
    public decimal? ReorderPoint { get; set; }

    /// <summary>Last write (UTC).</summary>
    public DateTime UpdatedTimestamp { get; set; }
}