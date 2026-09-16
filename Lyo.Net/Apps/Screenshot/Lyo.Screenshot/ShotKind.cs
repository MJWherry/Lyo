namespace Lyo.Screenshot;

/// <summary>What a finished capture writes.</summary>
public enum ShotKind
{
    /// <summary>One PNG of the first frame.</summary>
    Still,

    /// <summary>Looping GIF of every frame gathered before stop.</summary>
    Gif,

    /// <summary>One tall PNG stitched from the scroll.</summary>
    Receipt
}
