using Lyo.EntityReference.Models;

namespace Lyo.HomeInventory.Models.Request;

/// <summary>Move quantity between two locations in one transaction.</summary>
public sealed class HomeItemStockTransferReq
{
    /// <summary>Source location.</summary>
    public Guid FromLocationId { get; set; }

    /// <summary>Destination location.</summary>
    public Guid ToLocationId { get; set; }

    /// <summary>Quantity to move (must be positive).</summary>
    public decimal Quantity { get; set; }

    /// <summary>Transfer ticket or similar.</summary>
    public string? ReferenceNumber { get; set; }

    /// <summary>Actor that performed the transfer.</summary>
    public EntityRef? CreatedBy { get; set; }
}
