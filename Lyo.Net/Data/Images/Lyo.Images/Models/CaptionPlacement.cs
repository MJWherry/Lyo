namespace Lyo.Images.Models;

/// <summary>Where a caption strip sits relative to the input image when composited by <see cref="IImageDecorationService.AddCaptionAsync" />.</summary>
public enum CaptionPlacement
{
    /// <summary>Caption band rendered above the image (header style; commonly paired with a downward notch).</summary>
    HeaderAbove = 0,

    /// <summary>Caption band rendered below the image (footer style).</summary>
    FooterBelow = 1
}