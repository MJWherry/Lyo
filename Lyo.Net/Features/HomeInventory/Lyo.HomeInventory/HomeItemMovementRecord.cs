using Lyo.Common;

namespace Lyo.HomeInventory;

/// <summary>Immutable audit line for quantity changes.</summary>
public sealed class HomeItemMovementRecord
{
    public Guid Id { get; set; }

    public Guid ItemId { get; set; }

    public HomeItemMovementType MovementType { get; set; }

    public decimal Quantity { get; set; }

    public Guid? FromLocationId { get; set; }

    public Guid? ToLocationId { get; set; }

    /// <summary>Purchase order, sales order, transfer ticket, etc.</summary>
    public string? ReferenceNumber { get; set; }

    public string? Notes { get; set; }

    public string? CreatedByEntityType { get; set; }

    public string? CreatedByEntityId { get; set; }

    public DateTime CreatedTimestamp { get; set; }

    public EntityRef? CreatedBy
        => string.IsNullOrWhiteSpace(CreatedByEntityType) || string.IsNullOrWhiteSpace(CreatedByEntityId) ? null : new EntityRef(CreatedByEntityType, CreatedByEntityId);
}