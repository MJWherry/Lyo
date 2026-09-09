namespace Lyo.HomeInventory;

/// <summary>An inventory item record lifecycle status.</summary>
public enum HomeItemStatus
{
    /// <summary>Use active and available.</summary>
    Active = 0,

    /// <summary>History no longer stocked but retained.</summary>
    Discontinued = 1,

    /// <summary>Soft-archived and omitted from default lists.</summary>
    Archived = 2
}