namespace Lyo.HomeInventory;

/// <summary>Quantity buckets for an item at a specific location.</summary>
public sealed class HomeItemStockRecord
{
    public Guid ItemId { get; set; }

    public Guid LocationId { get; set; }

    public decimal QuantityOnHand { get; set; }

    public decimal QuantityReserved { get; set; }

    public decimal? ReorderPoint { get; set; }

    public DateTime UpdatedTimestamp { get; set; }
}