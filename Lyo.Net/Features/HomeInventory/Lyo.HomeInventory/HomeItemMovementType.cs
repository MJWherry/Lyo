namespace Lyo.HomeInventory;

/// <summary>Type of stock movement for audit history.</summary>
public enum HomeItemMovementType
{
    /// <summary>Initial receipt or purchase-in.</summary>
    Receipt = 0,

    /// <summary>Consumption, sale, or issue from stock.</summary>
    Issue = 1,

    /// <summary>Stock leaving a location (paired with transfer in).</summary>
    TransferOut = 2,

    /// <summary>Stock arriving at a location (paired with transfer out).</summary>
    TransferIn = 3,

    /// <summary>Cycle count or correction.</summary>
    Adjustment = 4,

    /// <summary>Customer or vendor return.</summary>
    Return = 5,

    /// <summary>Shrinkage, spoilage, or write-off.</summary>
    Loss = 6,

    /// <summary>Atomic move between two locations (both endpoints set on the movement row).</summary>
    StockTransfer = 7
}