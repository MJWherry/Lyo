using Lyo.Images.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Lyo.Images.Tests;

public class QrFrameLayoutServiceTests
{
    private static byte[] CreateSquarePng(int side)
    {
        using var image = new Image<Rgba32>(side, side, Color.DodgerBlue);
        using var ms = new MemoryStream();
        image.SaveAsPng(ms);
        return ms.ToArray();
    }

    [Fact]
    public async Task BadgeWithHeader_LargeExplicitCaption_IncreasesCanvasHeightBeyondThin220PxEra()
    {
        var svc = new QrFrameLayoutService();
        var qr = CreateSquarePng(2000);
        var opts = new QrFrameLayoutOptions {
            Style = QrFrameStyle.BadgeWithHeader,
            CaptionText = "Scan Me",
            CaptionFontSizePx = 320,
            HeaderHeightPx = 52,
            AutoSizeHeaderToCaption = true
        };

        var result = await svc.CompositeQrFramePngAsync(qr, opts, CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);

        await using var readMs = new MemoryStream(result.Data);
        using var img = await Image.LoadAsync<Rgba32>(readMs, CancellationToken.None);
        // Header grows to fit large type (old layout capped caption at 220px and header ~900px max).
        Assert.True(img.Height >= 2550, $"Expected tall framed output for large caption font, got height {img.Height}.");
    }

    [Fact]
    public async Task SimpleRoundedPanel_CutsBoundingCorner_NotFilledSquare()
    {
        var svc = new QrFrameLayoutService();
        var qr = CreateSquarePng(128);
        var opts = new QrFrameLayoutOptions {
            Style = QrFrameStyle.SimpleRoundedPanel,
            PanelBackgroundHex = "#FFFFFF",
            ShadowOffsetPx = 0,
            OuterMarginPx = 24,
            PaddingAroundQrPx = 24,
            CornerRadiusPx = 24
        };

        var result = await svc.CompositeQrFramePngAsync(qr, opts, CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);

        await using var readMs = new MemoryStream(result.Data);
        using var img = await Image.LoadAsync<Rgba32>(readMs, CancellationToken.None);
        // Card origin (margin, margin): axis-aligned fill paints #FFFFFF here; rounded path leaves default canvas (#FFF3F4F6) visible outside the arc.
        var corner = img[24, 24];
        var inset = img[36, 36];
        Assert.True(corner.R < 252 || corner.G < 252 || corner.B < 252, $"Expected canvas color at bbox corner (not panel white), got R={corner.R} G={corner.G} B={corner.B}.");
        Assert.True(inset.R > 248 && inset.G > 248 && inset.B > 248, $"Expected white panel just inside the arc, got R={inset.R} G={inset.G} B={inset.B}.");
    }

    [Fact]
    public async Task BorderOnly_LargeExplicitCaption_ReservesFooterUsingMeasuredBlock()
    {
        var svc = new QrFrameLayoutService();
        var qr = CreateSquarePng(800);
        var opts = new QrFrameLayoutOptions {
            Style = QrFrameStyle.BorderOnly,
            CaptionText = "Footer caption text",
            CaptionFontSizePx = 120,
            AutoSizeHeaderToCaption = false
        };

        var result = await svc.CompositeQrFramePngAsync(qr, opts, CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);

        await using var readMs = new MemoryStream(result.Data);
        using var img = await Image.LoadAsync<Rgba32>(readMs, CancellationToken.None);
        // Footer band must exceed a single 120px line estimate when wrapping adds height.
        Assert.True(img.Height > 800 + 200, $"Expected extra footer space for wrapped caption, got height {img.Height}.");
    }
}
