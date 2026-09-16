using Lyo.Common.Core.Extensions;
using Lyo.EntityReference.Models;
using Lyo.HomeInventory.Models.Enums;

namespace Lyo.HomeInventory;

/// <summary>Read-only audit line for quantity changes. Free-text comments live on <c>INoteStore</c>.</summary>
public sealed class HomeItemMovementRecord
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

    /// <summary>Actor type.</summary>
    public string? CreatedByEntityType { get; set; }

    /// <summary>Actor id.</summary>
    public string? CreatedByEntityId { get; set; }

    /// <summary>Created (UTC).</summary>
    public DateTime CreatedTimestamp { get; set; }

    /// <summary>Actor as an <see cref="EntityRef" /> when both type and id are set.</summary>
    public EntityRef? CreatedBy
        => CreatedByEntityType.IsNullOrWhitespace() || CreatedByEntityId.IsNullOrWhitespace() ? null : new EntityRef(CreatedByEntityType, CreatedByEntityId);
}
