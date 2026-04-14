using Lyo.Images;
using Lyo.Images.Models;
using Lyo.QRCode.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.QRCode.Tests;

public class BuiltInQRCodeServiceTests
{
    private static BuiltInQRCodeService CreateService()
        => new(
            new() {
                DefaultFormat = QRCodeFormat.Png,
                DefaultSize = 8,
                MinSize = 1,
                DefaultErrorCorrectionLevel = QRCodeErrorCorrectionLevel.Medium,
                EnableMetrics = false
            }, NullLogger<BuiltInQRCodeService>.Instance);

    private static BuiltInQRCodeService CreateServiceWithFrame()
        => new(
            new() {
                DefaultFormat = QRCodeFormat.Png,
                DefaultSize = 8,
                MinSize = 1,
                DefaultErrorCorrectionLevel = QRCodeErrorCorrectionLevel.Medium,
                EnableMetrics = false
            },
            NullLogger<BuiltInQRCodeService>.Instance,
            metrics: null,
            imageService: null,
            qrFrameLayout: new QrFrameLayoutService());

    [Fact]
    public async Task GenerateAsync_Png_HasValidHeader()
    {
        var service = CreateService();
        var result = await service.GenerateAsync("https://example.com", new() { Format = QRCodeFormat.Png, Size = 8 }, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsSuccess);
        var qr = Assert.IsType<QRCodeResult>(result);
        Assert.NotNull(qr.ImageBytes);
        Assert.True(qr.ImageBytes!.Length > 32);
        Assert.Equal(0x89, qr.ImageBytes[0]);
        Assert.Equal(0x50, qr.ImageBytes[1]);
        Assert.Equal(0x4E, qr.ImageBytes[2]);
        Assert.Equal(0x47, qr.ImageBytes[3]);
    }

    [Fact]
    public async Task GenerateAsync_Png_WithRoundedPanelFrame_IsLargerThanBarePng()
    {
        var service = CreateServiceWithFrame();
        var bare = await service.GenerateAsync("https://example.com", new() { Format = QRCodeFormat.Png, Size = 8 }, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(bare.IsSuccess);
        var bareQr = Assert.IsType<QRCodeResult>(bare);
        var framed = await service.GenerateAsync(
            "https://example.com",
            new() {
                Format = QRCodeFormat.Png,
                Size = 8,
                Frame = new QrFrameLayoutOptions { Style = QrFrameStyle.SimpleRoundedPanel, CaptionText = "Scan" }
            },
            TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(framed.IsSuccess);
        var framedQr = Assert.IsType<QRCodeResult>(framed);
        Assert.NotNull(bareQr.ImageBytes);
        Assert.NotNull(framedQr.ImageBytes);
        Assert.True(framedQr.ImageBytes!.Length > bareQr.ImageBytes!.Length);
    }

    [Fact]
    public async Task GenerateAsync_Svg_ContainsSvgRoot()
    {
        var service = CreateService();
        var result = await service.GenerateAsync("hello", new() { Format = QRCodeFormat.Svg, Size = 6 }, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsSuccess);
        var qr = Assert.IsType<QRCodeResult>(result);
        Assert.NotNull(qr.ImageBytes);
        var s = System.Text.Encoding.UTF8.GetString(qr.ImageBytes!);
        Assert.Contains("<svg", s, StringComparison.Ordinal);
        Assert.Contains("http://www.w3.org/2000/svg", s);
    }
}