using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Lyo.Screenshot;

/// <summary>Copies one rectangle from the desktop into an image. One instance is used from the capture loop only.</summary>
public interface IScreenGrabber : IDisposable
{
    /// <summary>The full desktop rectangle, in pixels. A zero size means the grabber captures every output when asked for the full screen.</summary>
    CaptureRegion VirtualScreen { get; }

    /// <summary>Grabs <paramref name="region" />. When <paramref name="includeCursor" /> is set, the pointer is composited if this platform can.</summary>
    Image<Rgba32> Grab(CaptureRegion region, bool includeCursor);
}