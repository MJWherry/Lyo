namespace Lyo.Images.Models;

/// <summary>Where a caption strip sits relative to the image in <see cref="IImageDecorationService.AddCaptionAsync" />.</summary>
public enum CaptionPlacement
{
    /// <summary>Caption band above the image (header; often with a downward notch).</summary>
    HeaderAbove = 0,

    /// <summary>Caption band below the image (footer).</summary>
    FooterBelow = 1
}