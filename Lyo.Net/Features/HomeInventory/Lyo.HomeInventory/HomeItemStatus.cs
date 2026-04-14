namespace Lyo.HomeInventory;

/// <summary>Lifecycle status for an inventory item record.</summary>
public enum HomeItemStatus
{
    /// <summary>Active and available for use.</summary>
    Active = 0,

    /// <summary>No longer stocked but retained for history.</summary>
    Discontinued = 1,

    /// <summary>Soft-archived; excluded from default listings.</summary>
    Archived = 2
}