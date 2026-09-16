using Lyo.EntityReference.Models;
using Lyo.HomeInventory.Models.Enums;

namespace Lyo.HomeInventory.Models.Request;

/// <summary>Apply an on-hand delta and append a movement row.</summary>
public sealed class HomeItemStockAdjustReq
{
    /// <summary>Location whose on-hand quantity changes.</summary>
    public Guid LocationId { get; set; }

    /// <summary>Signed delta (positive receives, negative issues).</summary>
    public decimal QuantityDelta { get; set; }

    /// <summary>Movement type recorded on the audit row.</summary>
    public HomeItemMovementType MovementType { get; set; } = HomeItemMovementType.Adjustment;

    /// <summary>PO, ticket, or similar.</summary>
    public string? ReferenceNumber { get; set; }

    /// <summary>Actor that performed the adjustment.</summary>
    public EntityRef? CreatedBy { get; set; }
}
