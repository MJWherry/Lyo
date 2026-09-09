namespace Lyo.HomeInventory;

/// <summary>An item at a specific location quantity buckets.</summary>
public sealed class HomeItemStockRecord
{
    public Guid ItemId { get; set; }

    public Guid LocationId { get; set; }

    public decimal QuantityOnHand { get; set; }

    public decimal QuantityReserved { get; set; }

    public decimal? ReorderPoint { get; set; }

    public DateTime UpdatedTimestamp { get; set; }
}