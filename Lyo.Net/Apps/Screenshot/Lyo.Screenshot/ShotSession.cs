using Lyo.Exceptions;

namespace Lyo.Screenshot;

/// <summary>Pulls frames from an <see cref="IFrameSource" /> until stop, then encodes a still, GIF, or receipt.</summary>
/// <param name="source">Frame producer. Each frame is disposed after it is encoded.</param>
public sealed class ShotSession(IFrameSource source)
{
    /// <summary>
    /// Still returns the first frame as PNG. GIF writes each frame into the animation as it arrives, and keeps the first frame as <see cref="CapturedShot.PreviewPng" />.
    /// Receipt writes each frame to a temp file and drops it before the next, then stitches one frame at a time and deletes the file.
    /// Throws <see cref="InvalidOperationException" /> when no frame was captured.
    /// </summary>
    public async Task<CapturedShot> CaptureAsync(ShotKind kind, CaptureRegion region, int framesPerSecond, bool includeCursor, CancellationToken cancellationToken = default)
    {
        ArgumentHelpers.ThrowIfNull(source);
        ArgumentHelpers.ThrowIfLessThan(framesPerSecond, 1);
        if (kind == ShotKind.Gif)
            return await CaptureGifAsync(region, framesPerSecond, includeCursor, cancellationToken).ConfigureAwait(false);
        if (kind == ShotKind.Receipt)
            return await CaptureReceiptAsync(region, framesPerSecond, includeCursor, cancellationToken).ConfigureAwait(false);

        return await CaptureStillAsync(region, framesPerSecond, includeCursor, cancellationToken).ConfigureAwait(false);
    }

    private async Task<CapturedShot> CaptureGifAsync(CaptureRegion region, int framesPerSecond, bool includeCursor, CancellationToken cancellationToken)
    {
        using var output = new MemoryStream();
        byte[]? preview = null;
        var count = 0;
        try {
            await foreach (var frame in source.CaptureAsync(region, framesPerSecond, includeCursor, cancellationToken).ConfigureAwait(false)) {
                using (frame) {
                    ShotEncoder.WriteGifFrame(output, frame, framesPerSecond, count == 0);
                    if (count == 0)
                        preview = await ShotEncoder.EncodePngAsync(frame, CancellationToken.None).ConfigureAwait(false);
                    count++;
                }
            }
        }
        catch (OperationCanceledException) when (count > 0) {
        }

        if (count == 0 || preview is null)
            throw new InvalidOperationException("No frames were captured.");

        output.WriteByte(ScreenGif.Trailer);
        return new(output.ToArray(), preview);
    }

    private async Task<CapturedShot> CaptureStillAsync(CaptureRegion region, int framesPerSecond, bool includeCursor, CancellationToken cancellationToken)
    {
        await foreach (var frame in source.CaptureAsync(region, framesPerSecond, includeCursor, cancellationToken).ConfigureAwait(false)) {
            using (frame) {
                var png = await ShotEncoder.EncodePngAsync(frame, CancellationToken.None).ConfigureAwait(false);
                return new(png, png);
            }
        }

        throw new InvalidOperationException("No frames were captured.");
    }

    private async Task<CapturedShot> CaptureReceiptAsync(CaptureRegion region, int framesPerSecond, bool includeCursor, CancellationToken cancellationToken)
    {
        var directory = ShotScratch.Directory;
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, $"receipt-{Guid.NewGuid():N}.bin");
        try {
            await using var spill = new FileStream(path, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read, 1024 * 1024, FileOptions.SequentialScan);
            var count = 0;
            try {
                await foreach (var frame in source.CaptureAsync(region, framesPerSecond, includeCursor, cancellationToken).ConfigureAwait(false)) {
                    try {
                        ReceiptStitcher.WriteFrame(spill, frame);
                    }
                    finally {
                        frame.Dispose();
                        ShotEncoder.ReleaseScratchMemory();
                    }

                    count++;
                }
            }
            catch (OperationCanceledException) when (count > 0) {
            }

            if (count == 0)
                throw new InvalidOperationException("No frames were captured.");

            spill.Position = 0;
            using var stitched = ReceiptStitcher.Stitch(spill);
            var receipt = await ShotEncoder.EncodePngAsync(stitched, CancellationToken.None).ConfigureAwait(false);
            return new(receipt, receipt);
        }
        finally {
            try {
                File.Delete(path);
            }
            catch (IOException) {
            }
            catch (UnauthorizedAccessException) {
            }
        }
    }
}
