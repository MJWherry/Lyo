using System.Text;
using Lyo.Common.Enums;
using Lyo.Images.Builders;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Lyo.Images.Tests;

public class ImageDecorationServiceTests
{
    private static byte[] CreateSquarePng(int side, Color? color = null)
    {
        using var image = new Image<Rgba32>(side, side, color ?? Color.DodgerBlue);
        using var ms = new MemoryStream();
        image.SaveAsPng(ms);
        return ms.ToArray();
    }

    private static byte[] CreateLogoPng(int side) => CreateSquarePng(side, Color.OrangeRed);

    private static string CreateSquareSvg(int side, string fill = "#000000")
        => $"<?xml version=\"1.0\" encoding=\"UTF-8\"?><svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{side}\" height=\"{side}\" viewBox=\"0 0 {side} {side}\"><rect width=\"{side}\" height=\"{side}\" fill=\"{fill}\"/></svg>";

    private static async Task<(int W, int H)> GetSizeAsync(byte[] png)
    {
        await using var ms = new MemoryStream(png);
        using var img = await Image.LoadAsync<Rgba32>(ms);
        return (img.Width, img.Height);
    }

    [Fact]
    public async Task OverlayAsync_Raster_CentersOverlayWithPad()
    {
        var svc = new ImageDecorationService();
        var bg = CreateSquarePng(200);
        var logo = CreateLogoPng(64);
        await using var bgMs = new MemoryStream(bg);
        await using var logoMs = new MemoryStream(logo);
        await using var outMs = new MemoryStream();
        var result = await svc.OverlayAsync(
            bgMs, logoMs, outMs, OverlayOptionsBuilder.New().WithOverlaySizePercent(20).WithPadColor("#FFFFFF").Build(), ImageFormat.Png,
            ct: TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        var bytes = outMs.ToArray();
        var (w, h) = await GetSizeAsync(bytes);
        Assert.Equal(200, w);
        Assert.Equal(200, h);
        await using var pngMs = new MemoryStream(bytes);
        using var img = await Image.LoadAsync<Rgba32>(pngMs, TestContext.Current.CancellationToken);
        // Center of the overlay should be the orange-red logo (allow modest decode tolerance).
        var center = img[100, 100];
        Assert.True(center.R > 200 && center.G < 80, $"Expected orange-red center, got R={center.R} G={center.G} B={center.B}.");
    }

    [Fact]
    public async Task OverlayAsync_Svg_EmbedsBase64ImageElement()
    {
        var svc = new ImageDecorationService();
        var svg = CreateSquareSvg(240);
        var bgBytes = Encoding.UTF8.GetBytes(svg);
        var logo = CreateLogoPng(32);
        await using var bgMs = new MemoryStream(bgBytes);
        await using var logoMs = new MemoryStream(logo);
        await using var outMs = new MemoryStream();
        var result = await svc.OverlayAsync(
            bgMs, logoMs, outMs, OverlayOptionsBuilder.New().WithOverlaySizePercent(15).WithBorder("#000000").Build(), ImageFormat.Svg, ct: TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        var withIcon = Encoding.UTF8.GetString(outMs.ToArray());
        Assert.Contains("<image href=\"data:image/png;base64,", withIcon, StringComparison.Ordinal);
        Assert.EndsWith("</svg>", withIcon.TrimEnd());
    }

    [Fact]
    public async Task AddFrameAsync_ExpandsCanvasByStrokeAndPadding()
    {
        var svc = new ImageDecorationService();
        var qr = CreateSquarePng(160);
        await using var inMs = new MemoryStream(qr);
        await using var outMs = new MemoryStream();
        var result = await svc.AddFrameAsync(
            inMs, outMs, FrameOptionsBuilder.New().WithStrokeColor("#000000").WithStrokeWidth(4).WithPadding(12).WithCornerRadius(16).Build(), ImageFormat.Png,
            ct: TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        var (w, h) = await GetSizeAsync(outMs.ToArray());
        Assert.Equal(160 + 2 * (4 + 12), w);
        Assert.Equal(160 + 2 * (4 + 12), h);
    }

    [Fact]
    public async Task AddCaptionAsync_HeaderAbove_GrowsCanvasHeight()
    {
        var svc = new ImageDecorationService();
        var qr = CreateSquarePng(200);
        await using var inMs = new MemoryStream(qr);
        await using var outMs = new MemoryStream();
        var result = await svc.AddCaptionAsync(
            inMs, outMs, CaptionOptionsBuilder.New().WithText("Scan Me").WithBandHeight(48).Build(), ImageFormat.Png, ct: TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        var (w, h) = await GetSizeAsync(outMs.ToArray());
        Assert.Equal(200, w);
        Assert.True(h > 200, $"Expected header band to grow canvas, got height {h}.");
    }

    [Fact]
    public async Task AddOuterPaddingAsync_GrowsCanvasByMarginAndPadding()
    {
        var svc = new ImageDecorationService();
        var qr = CreateSquarePng(120);
        await using var inMs = new MemoryStream(qr);
        await using var outMs = new MemoryStream();
        var result = await svc.AddOuterPaddingAsync(
            inMs, outMs, PaddingOptionsBuilder.New().WithPadding(20).WithMargin(15).WithCornerRadius(24).WithShadow("#33000000", 8).Build(), ImageFormat.Png,
            ct: TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        var (w, h) = await GetSizeAsync(outMs.ToArray());
        // Canvas = card(120 + 2*20) + 2*margin(15) + shadowOff(8)
        Assert.Equal(120 + 2 * 20 + 2 * 15 + 8, w);
        Assert.Equal(120 + 2 * 20 + 2 * 15 + 8, h);
    }

    [Fact]
    public async Task Pipeline_ChainedBadge_OutputLargerThanBareQr()
    {
        var svc = new ImageDecorationService();
        var qr = CreateSquarePng(220);
        var logo = CreateLogoPng(32);
        var pipeline = svc.Pipeline(qr)
            .Overlay(logo, OverlayOptionsBuilder.New().WithOverlaySizePercent(18).WithPadColor("#FFFFFF").Build())
            .AddCaption(CaptionOptionsBuilder.New().WithText("Demo").WithBandHeight(40).WithNotch().Build())
            .AddOuterPadding(PaddingOptionsBuilder.New().WithPanelColor("#FFFFFF").WithCornerRadius(16).Build())
            .AddFrame(FrameOptionsBuilder.New().WithStrokeColor("#1e293b").WithStrokeWidth(2).WithCornerRadius(16).WithPadding(0).Build());

        var result = await pipeline.ToByteArrayAsync(ImageFormat.Png, ct: TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.True(result.Data!.Length > qr.Length, $"Expected chained output {result.Data!.Length} > bare {qr.Length}.");
        var (w, h) = await GetSizeAsync(result.Data!);
        Assert.True(w > 220 && h > 220, $"Expected chained output larger than bare 220x220, got {w}x{h}.");
    }
}