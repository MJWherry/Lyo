using Lyo.EntityReference.Models;
using Lyo.HomeInventory.Models.Enums;

namespace Lyo.HomeInventory.Models.Response;

/// <summary>Read-only stock movement audit line.</summary>
public sealed class HomeItemMovementRes
{
    /// <summary>Row id.</summary>
    public Guid Id { get; set; }

    /// <summary>Item id.</summary>
    public Guid ItemId { get; set; }

    /// <summary>Movement type.</summary>
    public HomeItemMovementType MovementType { get; set; }

    /// <summary>Quantity moved.</summary>
    public decimal Quantity { get; set; }

    /// <summary>Source location when applicable.</summary>
    public Guid? FromLocationId { get; set; }

    /// <summary>Destination location when applicable.</summary>
    public Guid? ToLocationId { get; set; }

    /// <summary>PO, sales order, transfer ticket, or similar.</summary>
    public string? ReferenceNumber { get; set; }

    /// <summary>Actor that wrote the movement.</summary>
    public EntityRef? CreatedBy { get; set; }

    /// <summary>Created (UTC).</summary>
    public DateTime CreatedTimestamp { get; set; }
}
