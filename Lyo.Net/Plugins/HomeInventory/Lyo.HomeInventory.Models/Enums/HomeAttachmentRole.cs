namespace Lyo.HomeInventory.Models.Enums;

/// <summary>How an attached FileStorage file relates to the inventory subject.</summary>
public enum HomeAttachmentRole
{
    /// <summary>Photograph of the item or location.</summary>
    Photo = 0,

    /// <summary>Purchase receipt or invoice scan.</summary>
    Receipt = 1,

    /// <summary>Owner manual or spec sheet.</summary>
    Manual = 2,

    /// <summary>Warranty card or certificate.</summary>
    Warranty = 3,

    /// <summary>Other document.</summary>
    Document = 4
}