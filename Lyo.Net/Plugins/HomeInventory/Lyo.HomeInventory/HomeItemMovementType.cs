namespace Lyo.HomeInventory;

/// <summary>Audit history type of stock movement.</summary>
public enum HomeItemMovementType
{
    /// <summary>First receipt / purchase-in.</summary>
    Receipt = 0,

    /// <summary>Stock consumed, sold, or issued.</summary>
    Issue = 1,

    /// <summary>Quantity leaving a location (paired with transfer in).</summary>
    TransferOut = 2,

    /// <summary>Quantity arriving at a location (paired with transfer out).</summary>
    TransferIn = 3,

    /// <summary>Cycle count / quantity correction.</summary>
    Adjustment = 4,

    /// <summary>Return from a customer or vendor.</summary>
    Return = 5,

    /// <summary>Shrink, spoilage, or write-off.</summary>
    Loss = 6,

    /// <summary>Single move between two locations (both endpoints set on the movement row).</summary>
    StockTransfer = 7
}