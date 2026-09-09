using Lyo.Common.Core.Extensions;
using Lyo.EntityReference.Models;

namespace Lyo.HomeInventory;

/// <summary>Read-only audit line for quantity changes.</summary>
public sealed class HomeItemMovementRecord
{
    public Guid Id { get; set; }

    public Guid ItemId { get; set; }

    public HomeItemMovementType MovementType { get; set; }

    public decimal Quantity { get; set; }

    public Guid? FromLocationId { get; set; }

    public Guid? ToLocationId { get; set; }

    /// <summary>PO, sales order, transfer ticket, or similar.</summary>
    public string? ReferenceNumber { get; set; }

    public string? Notes { get; set; }

    public string? CreatedByEntityType { get; set; }

    public string? CreatedByEntityId { get; set; }

    public DateTime CreatedTimestamp { get; set; }

    public EntityRef? CreatedBy
        => CreatedByEntityType.IsNullOrWhitespace() || CreatedByEntityId.IsNullOrWhitespace() ? null : new EntityRef(CreatedByEntityType, CreatedByEntityId);
}