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

    [Fact]
    public async Task GenerateAsync_Png_HasValidHeader()
    {
        var service = CreateService();
        var result = await service.GenerateAsync("https://example.com", new() { Format = QRCodeFormat.Png, Size = 8 }, TestContext.Current.CancellationToken);
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
    public async Task GenerateAsync_DefaultOptions_AllowsWorkbenchModulePresets()
    {
        // Regression: MinSize must not assume Size is total image pixels (workbench passes 6–32 px/module).
        var service = new BuiltInQRCodeService(new(), NullLogger<BuiltInQRCodeService>.Instance);
        foreach (var px in new[] { 6, 10, 16, 24, 32 }) {
            var result = await service.GenerateAsync("https://example.com", new() { Format = QRCodeFormat.Png, Size = px }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess, $"Size={px}px/module should be valid");
        }
    }

    [Fact]
    public async Task GenerateAsync_Svg_ContainsSvgRoot()
    {
        var service = CreateService();
        var result = await service.GenerateAsync("hello", new() { Format = QRCodeFormat.Svg, Size = 6 }, TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess);
        var qr = Assert.IsType<QRCodeResult>(result);
        Assert.NotNull(qr.ImageBytes);
        var s = System.Text.Encoding.UTF8.GetString(qr.ImageBytes!);
        Assert.Contains("<svg", s, StringComparison.Ordinal);
        Assert.Contains("http://www.w3.org/2000/svg", s);
    }
}
