namespace Lyo.HomeInventory.Models.Enums;

/// <summary>Item condition (physical or commercial).</summary>
public enum HomeItemCondition
{
    /// <summary>Condition was not recorded.</summary>
    Unknown = 0,

    /// <summary>Unused / factory sealed.</summary>
    New = 1,

    /// <summary>Previously used.</summary>
    Used = 2,

    /// <summary>Refurbished by vendor or owner.</summary>
    Refurbished = 3,

    /// <summary>Opened packaging, otherwise unused.</summary>
    OpenBox = 4,

    /// <summary>Damaged.</summary>
    Damaged = 5
}