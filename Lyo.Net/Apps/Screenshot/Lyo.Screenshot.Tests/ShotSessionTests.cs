using System.Runtime.CompilerServices;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Lyo.Screenshot.Tests;

public class ShotSessionTests
{
    [Fact]
    public async Task Session_Still_ReturnsOnePng()
    {
        using var first = Frames.Flat(8, 8, new(10, 20, 30, 255));
        using var second = Frames.Flat(8, 8, new(200, 10, 10, 255));
        var session = new ShotSession(new FiniteSource([first, second]));
        var bytes = (await session.CaptureAsync(ShotKind.Still, new(0, 0, 8, 8), 5, false, TestCt)).Bytes;
        Assert.Equal([0x89, 0x50, 0x4E, 0x47], bytes[..4]);
        using var png = Image.Load<Rgba32>(bytes);
        Assert.Equal(first[0, 0], png[0, 0]);
        Assert.NotEqual(second[0, 0], png[0, 0]);
        Assert.Equal(8, png.Height);
    }

    [Fact]
    public async Task Session_Gif_Stop_EncodesFramesSoFar()
    {
        using var first = Frames.Rows(8, 8, 0);
        using var second = Frames.Rows(8, 8, 8);
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(TestCt);
        var session = new ShotSession(new StopAfterSource([first, second], stop));
        var bytes = (await session.CaptureAsync(ShotKind.Gif, new(0, 0, 8, 8), 5, false, stop.Token)).Bytes;
        using var gif = Image.Load<Rgba32>(bytes);
        Assert.Equal(2, gif.Frames.Count);
    }

    [Fact]
    public async Task Session_Gif_DisposesEachFrameBeforeTheNextAndKeepsAPreview()
    {
        using var first = Frames.Rows(8, 8, 0);
        using var second = Frames.Rows(8, 8, 8);
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(TestCt);
        var probe = new DisposeProbe([first, second], stop);
        var session = new ShotSession(probe);

        var shot = await session.CaptureAsync(ShotKind.Gif, new(0, 0, 8, 8), 5, false, stop.Token);

        Assert.True(probe.FirstDisposedBeforeSecond);
        Assert.True(probe.LastDisposedBeforeNextPull);
        using var gif = Image.Load<Rgba32>(shot.Bytes);
        Assert.Equal(2, gif.Frames.Count);
        Assert.Equal(20, gif.Frames.RootFrame.Metadata.GetGifMetadata().FrameDelay);
        using var preview = Image.Load<Rgba32>(shot.PreviewPng);
        Assert.Equal(1, preview.Frames.Count);
        Assert.Equal(first[0, 0], preview[0, 0]);
        Assert.Equal(first[7, 7], preview[7, 7]);
    }

    [Fact]
    public async Task Session_Receipt_Stop_ReturnsTallerPng()
    {
        const int height = 100;
        const int shift = 40;
        using var first = Frames.Rows(16, height, 0);
        using var second = Frames.Rows(16, height, shift);
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(TestCt);
        var session = new ShotSession(new StopAfterSource([first, second], stop));
        var bytes = (await session.CaptureAsync(ShotKind.Receipt, new(0, 0, 16, height), 5, false, stop.Token)).Bytes;
        using var png = Image.Load<Rgba32>(bytes);
        Assert.Equal(height + shift, png.Height);
    }

    [Fact]
    public async Task Session_Receipt_SpillsEachFrameBeforeTheNextAndDeletesTheFile()
    {
        const int width = 16;
        const int height = 100;
        const int shift = 40;
        using var first = Frames.Rows(width, height, 0);
        using var second = Frames.Rows(width, height, shift);
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(TestCt);
        var probe = new SpillProbe([first, second], stop, 8 + (width * height * 4));
        var session = new ShotSession(probe);

        var shot = await session.CaptureAsync(ShotKind.Receipt, new(0, 0, width, height), 5, false, stop.Token);

        Assert.True(probe.FirstDisposedBeforeSecond);
        Assert.True(probe.SpilledBeforeNext);
        Assert.True(probe.LastDisposedBeforeNextPull);
        Assert.False(File.Exists(probe.SpillPath));
        using var png = Image.Load<Rgba32>(shot.Bytes);
        Assert.Equal(height + shift, png.Height);
    }

    private sealed class FiniteSource(IReadOnlyList<Image<Rgba32>> frames) : IFrameSource
    {
        public async IAsyncEnumerable<Image<Rgba32>> CaptureAsync(CaptureRegion region, int framesPerSecond, bool includeCursor, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            foreach (var frame in frames) {
                cancellationToken.ThrowIfCancellationRequested();
                yield return frame.Clone();
                await Task.Yield();
            }
        }
    }

    private sealed class DisposeProbe(IReadOnlyList<Image<Rgba32>> frames, CancellationTokenSource stop) : IFrameSource
    {
        public bool FirstDisposedBeforeSecond { get; private set; }
        public bool LastDisposedBeforeNextPull { get; private set; }

        public async IAsyncEnumerable<Image<Rgba32>> CaptureAsync(CaptureRegion region, int framesPerSecond, bool includeCursor, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            Image<Rgba32>? previous = null;
            for (var i = 0; i < frames.Count; i++) {
                if (i == 1 && previous != null)
                    FirstDisposedBeforeSecond = Disposed(previous);
                previous = frames[i].Clone();
                yield return previous;
            }

            if (previous != null)
                LastDisposedBeforeNextPull = Disposed(previous);
            stop.Cancel();
            try {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) {
                yield break;
            }
        }

        private static bool Disposed(Image<Rgba32> image)
        {
            try {
                _ = image[0, 0];
                return false;
            }
            catch (ObjectDisposedException) {
                return true;
            }
        }
    }

    private sealed class SpillProbe(IReadOnlyList<Image<Rgba32>> frames, CancellationTokenSource stop, long minimumSpillBytes) : IFrameSource
    {
        private readonly HashSet<string> _before = ReceiptSpills();

        public bool FirstDisposedBeforeSecond { get; private set; }
        public bool LastDisposedBeforeNextPull { get; private set; }
        public bool SpilledBeforeNext { get; private set; }
        public string? SpillPath { get; private set; }

        public async IAsyncEnumerable<Image<Rgba32>> CaptureAsync(CaptureRegion region, int framesPerSecond, bool includeCursor, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            Image<Rgba32>? previous = null;
            for (var i = 0; i < frames.Count; i++) {
                if (i == 1 && previous != null) {
                    FirstDisposedBeforeSecond = Disposed(previous);
                    var created = ReceiptSpills();
                    created.ExceptWith(_before);
                    SpillPath = Assert.Single(created);
                    SpilledBeforeNext = new FileInfo(SpillPath).Length >= minimumSpillBytes;
                }

                previous = frames[i].Clone();
                yield return previous;
            }

            if (previous != null)
                LastDisposedBeforeNextPull = Disposed(previous);
            stop.Cancel();
            try {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) {
                yield break;
            }
        }

        private static bool Disposed(Image<Rgba32> image)
        {
            try {
                _ = image[0, 0];
                return false;
            }
            catch (ObjectDisposedException) {
                return true;
            }
        }

        private static HashSet<string> ReceiptSpills()
        {
            var directory = Path.Combine(Path.GetTempPath(), "lyo-shot");
            if (!Directory.Exists(directory))
                return [];
            return Directory.GetFiles(directory, "receipt-*.bin").ToHashSet(StringComparer.Ordinal);
        }
    }

    private sealed class StopAfterSource(IReadOnlyList<Image<Rgba32>> frames, CancellationTokenSource stop) : IFrameSource
    {
        public async IAsyncEnumerable<Image<Rgba32>> CaptureAsync(CaptureRegion region, int framesPerSecond, bool includeCursor, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            foreach (var frame in frames) {
                cancellationToken.ThrowIfCancellationRequested();
                yield return frame.Clone();
            }

            stop.Cancel();
            try {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) {
                yield break;
            }
        }
    }
}
