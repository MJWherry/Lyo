namespace Lyo.HomeInventory;

/// <summary>An item at a specific location quantity buckets.</summary>
public sealed class HomeItemStockRecord
{
    /// <summary>Item id.</summary>
    public Guid ItemId { get; set; }

    /// <summary>Location id (stock bin; independent of <see cref="HomeItemRecord.LocationId" />).</summary>
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