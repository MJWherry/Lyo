namespace Lyo.HomeInventory.Models.Request;

/// <summary>Replace quantity buckets for an item at a location.</summary>
public sealed class HomeItemStockReq
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
}