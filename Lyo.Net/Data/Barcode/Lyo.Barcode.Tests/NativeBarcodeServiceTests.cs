using System.Text;
using Lyo.Barcode.Models;
using Lyo.Barcode.Native;
using Lyo.Metrics;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Barcode.Tests;

public class NativeBarcodeServiceTests
{
    private static NativeBarcodeService CreateService(BarcodeServiceOptions? options = null, IMetrics? metrics = null)
        => new(options ?? new BarcodeServiceOptions(), NullLogger<NativeBarcodeService>.Instance, metrics);

    [Fact]
    public async Task GenerateAsync_Code128Bmp_ReturnsBmpBytes()
    {
        var service = CreateService();
        var result = await service.GenerateAsync(
            "TEST1234", BarcodeSymbology.Code128, new() {
                Format = BarcodeFormat.Bmp,
                ModuleWidthPixels = 2,
                BarHeightPixels = 60,
                QuietZoneModules = 8
            }, TestContext.Current.CancellationToken).ConfigureAwait(false);

        var br = Assert.IsType<BarcodeResult>(result);
        Assert.True(result.IsSuccess);
        Assert.NotNull(br.ImageBytes);
        Assert.Equal('B', (char)br.ImageBytes![0]);
        Assert.Equal('M', (char)br.ImageBytes[1]);
        Assert.Equal(BarcodeFormat.Bmp, br.Format);
        Assert.True(br.ImageWidthPixels > 0);
        Assert.True(br.ImageHeightPixels > 0);
    }

    [Fact]
    public async Task GenerateAsync_Code128Svg_StartsWithSvgTag()
    {
        var service = CreateService();
        var result = await service.GenerateAsync(
            "X", BarcodeSymbology.Code128, new() {
                Format = BarcodeFormat.Svg,
                ModuleWidthPixels = 1,
                BarHeightPixels = 40,
                QuietZoneModules = 4
            }, TestContext.Current.CancellationToken).ConfigureAwait(false);

        var br = Assert.IsType<BarcodeResult>(result);
        Assert.True(result.IsSuccess);
        var text = Encoding.UTF8.GetString(br.ImageBytes!);
        Assert.StartsWith("<svg", text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GenerateAsync_InvalidCharacter_Fails()
    {
        var service = CreateService();
        var result = await service.GenerateAsync("bad\u0001", BarcodeSymbology.Code128, new() { Format = BarcodeFormat.Bmp }, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task GenerateAsync_UnsupportedSymbology_Fails()
    {
        var service = CreateService();
        var result = await service.GenerateAsync("123", (BarcodeSymbology)999, new(), TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.False(result.IsSuccess);
    }
}