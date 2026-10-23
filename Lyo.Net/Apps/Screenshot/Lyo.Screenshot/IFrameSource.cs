using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Lyo.Screenshot;

/// <summary>Supplies grabbed frames until the caller stops the capture.</summary>
public interface IFrameSource
{
    /// <summary>Yields one image per tick at <paramref name="framesPerSecond" /> until <paramref name="cancellationToken" /> is cancelled.</summary>
    IAsyncEnumerable<Image<Rgba32>> CaptureAsync(CaptureRegion region, int framesPerSecond, bool includeCursor, CancellationToken cancellationToken);
}