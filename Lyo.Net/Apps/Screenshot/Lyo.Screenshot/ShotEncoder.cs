using Lyo.Exceptions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;

namespace Lyo.Screenshot;

/// <summary>Writes a PNG with ImageSharp, or a looping GIF one frame at a time.</summary>
public static class ShotEncoder
{
    /// <summary>Encodes <paramref name="image" /> as PNG.</summary>
    public static async Task<byte[]> EncodePngAsync(Image<Rgba32> image, CancellationToken cancellationToken = default)
    {
        ArgumentHelpers.ThrowIfNull(image);
        await using var stream = new MemoryStream();
        await image.SaveAsPngAsync(stream, cancellationToken).ConfigureAwait(false);
        return stream.ToArray();
    }

    /// <summary>
    /// Encodes <paramref name="frames" /> as a GIF that loops forever. Each frame is indexed and written before the next one is read.
    /// Frame delay follows <paramref name="framesPerSecond" /> (GIF delay is in centiseconds).
    /// </summary>
    public static Task<byte[]> EncodeGifAsync(IReadOnlyList<Image<Rgba32>> frames, int framesPerSecond, CancellationToken cancellationToken = default)
    {
        try {
            ArgumentHelpers.ThrowIfNullOrEmpty(frames);
            ArgumentHelpers.ThrowIfLessThan(framesPerSecond, 1);
            using var output = new MemoryStream();
            for (var i = 0; i < frames.Count; i++) {
                cancellationToken.ThrowIfCancellationRequested();
                WriteGifFrame(output, frames[i], framesPerSecond, i == 0);
            }

            output.WriteByte(ScreenGif.Trailer);
            return Task.FromResult(output.ToArray());
        }
        catch (Exception exception) {
            return Task.FromException<byte[]>(exception);
        }
    }

    /// <summary>
    /// Encodes PNG files at <paramref name="framePaths" /> as a GIF that loops forever. One file is decoded, written into the GIF, and disposed before the next file is opened.
    /// Frame delay follows <paramref name="framesPerSecond" /> (GIF delay is in centiseconds).
    /// </summary>
    public static async Task<byte[]> EncodeGifAsync(IReadOnlyList<string> framePaths, int framesPerSecond, CancellationToken cancellationToken = default)
    {
        ArgumentHelpers.ThrowIfNullOrEmpty(framePaths);
        ArgumentHelpers.ThrowIfLessThan(framesPerSecond, 1);
        using var output = new MemoryStream();
        for (var i = 0; i < framePaths.Count; i++) {
            cancellationToken.ThrowIfCancellationRequested();
            using var frame = await Image.LoadAsync<Rgba32>(framePaths[i], cancellationToken).ConfigureAwait(false);
            WriteGifFrame(output, frame, framesPerSecond, i == 0);
        }

        output.WriteByte(ScreenGif.Trailer);
        ReleaseScratchMemory();
        return output.ToArray();
    }

    /// <summary>PNG of the first picture in <paramref name="gif" />. Later frames are not decoded.</summary>
    public static byte[] FirstFramePng(byte[] gif)
    {
        ArgumentHelpers.ThrowIfNullOrEmpty(gif);
        byte[] png;
        using (var image = Image.Load<Rgba32>(new DecoderOptions { MaxFrames = 1 }, gif)) {
            using var stream = new MemoryStream();
            image.SaveAsPng(stream);
            png = stream.ToArray();
        }

        ReleaseScratchMemory();
        return png;
    }

    /// <summary>Returns ImageSharp's retained unmanaged buffers to the operating system. Call this only after every image from the shot has been disposed.</summary>
    public static void ReleaseScratchMemory() => Configuration.Default.MemoryAllocator.ReleaseRetainedResources();

    internal static void WriteGifFrame(Stream output, Image<Rgba32> frame, int framesPerSecond, bool first)
    {
        ArgumentHelpers.ThrowIfNull(output);
        ArgumentHelpers.ThrowIfNull(frame);
        ArgumentHelpers.ThrowIfLessThan(framesPerSecond, 1);
        ScreenGif.WriteFrame(output, frame, GifDelay(framesPerSecond), first);
    }

    private static int GifDelay(int framesPerSecond) => Math.Max(1, (int)Math.Round(100d / framesPerSecond));
}
