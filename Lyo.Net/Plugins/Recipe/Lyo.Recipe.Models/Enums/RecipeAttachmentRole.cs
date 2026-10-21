namespace Lyo.Recipe.Models.Enums;

/// <summary>How an attached FileStorage file relates to the recipe subject.</summary>
public enum RecipeAttachmentRole
{
    /// <summary>Any document that is not a scan, card, or image.</summary>
    Document = 0,

    /// <summary>A scanned page (source clipping, handwritten card, magazine).</summary>
    Scan = 1,

    /// <summary>A generated or printed recipe card.</summary>
    Card = 2,

    /// <summary>A photograph or illustration.</summary>
    Image = 3,

    /// <summary>Anything that does not fit the other roles.</summary>
    Other = 4
}