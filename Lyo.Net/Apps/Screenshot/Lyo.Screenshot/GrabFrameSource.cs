using Lyo.Exceptions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Lyo.Screenshot;

/// <summary>Polls an <see cref="IScreenGrabber" /> until the capture is stopped.</summary>
public sealed class GrabFrameSource(IScreenGrabber grabber) : IFrameSource
{
    /// <inheritdoc />
    public async IAsyncEnumerable<Image<Rgba32>> CaptureAsync(
        CaptureRegion region,
        int framesPerSecond,
        bool includeCursor,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentHelpers.ThrowIfNull(grabber);
        ArgumentHelpers.ThrowIfLessThan(framesPerSecond, 1);
        var delay = TimeSpan.FromSeconds(1d / framesPerSecond);
        while (!cancellationToken.IsCancellationRequested) {
            yield return grabber.Grab(region, includeCursor);
            try {
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) {
                yield break;
            }
        }
    }
}
