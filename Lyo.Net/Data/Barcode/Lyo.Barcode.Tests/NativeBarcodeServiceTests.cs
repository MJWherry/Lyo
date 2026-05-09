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
            }, TestContext.Current.CancellationToken);

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
            }, TestContext.Current.CancellationToken);

        var br = Assert.IsType<BarcodeResult>(result);
        Assert.True(result.IsSuccess);
        var text = Encoding.UTF8.GetString(br.ImageBytes!);
        Assert.StartsWith("<svg", text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GenerateAsync_InvalidCharacter_Fails()
    {
        var service = CreateService();
        var result = await service.GenerateAsync("bad\u0001", BarcodeSymbology.Code128, new() { Format = BarcodeFormat.Bmp }, TestContext.Current.CancellationToken);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task GenerateAsync_UnsupportedSymbology_Fails()
    {
        var service = CreateService();
        var result = await service.GenerateAsync("123", (BarcodeSymbology)999, new(), TestContext.Current.CancellationToken);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task GenerateAsync_Code128Bmp_WithHumanReadable_IncreasesHeight()
    {
        var service = CreateService();
        const int quiet = 10; // ISO minimum; request of 8 is clamped upward in the renderer.
        const int moduleW = 2;
        const int barH = 60;
        var baseQuietPx = quiet * moduleW;
        var baseH = barH + 2 * baseQuietPx;
        var captionBand = 6 + 14 + 4;

        var without = await service.GenerateAsync(
            "TEST1234", BarcodeSymbology.Code128, new() {
                Format = BarcodeFormat.Bmp,
                ModuleWidthPixels = moduleW,
                BarHeightPixels = barH,
                QuietZoneModules = 8
            }, TestContext.Current.CancellationToken);

        var with = await service.GenerateAsync(
            "TEST1234", BarcodeSymbology.Code128, new() {
                Format = BarcodeFormat.Bmp,
                ModuleWidthPixels = moduleW,
                BarHeightPixels = barH,
                QuietZoneModules = 8,
                ShowHumanReadableTextBelow = true,
                HumanReadableFontSizePixels = 14,
                HumanReadableMarginTopPixels = 6,
                HumanReadableMarginBottomPixels = 4
            }, TestContext.Current.CancellationToken);

        var br0 = Assert.IsType<BarcodeResult>(without);
        var br1 = Assert.IsType<BarcodeResult>(with);
        Assert.True(without.IsSuccess && with.IsSuccess);
        Assert.Equal(baseH, br0.ImageHeightPixels);
        Assert.Equal(baseH + captionBand, br1.ImageHeightPixels);
    }

    [Fact]
    public async Task GenerateAsync_Code128Svg_WithHumanReadable_ContainsTextElement()
    {
        var service = CreateService();
        var result = await service.GenerateAsync(
            "HI", BarcodeSymbology.Code128, new() {
                Format = BarcodeFormat.Svg,
                ModuleWidthPixels = 2,
                BarHeightPixels = 40,
                QuietZoneModules = 10,
                ShowHumanReadableTextBelow = true
            }, TestContext.Current.CancellationToken);

        var br = Assert.IsType<BarcodeResult>(result);
        Assert.True(result.IsSuccess);
        var text = Encoding.UTF8.GetString(br.ImageBytes!);
        Assert.Contains("<text", text, StringComparison.Ordinal);
        Assert.Contains("HI", text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GenerateAsync_Code128Bmp_WithBorder_IncreasesDimensions()
    {
        var service = CreateService();
        const int quiet = 10;
        const int moduleW = 2;
        const int barH = 40;
        const int border = 5;

        var plain = await service.GenerateAsync(
            "X", BarcodeSymbology.Code128, new() {
                Format = BarcodeFormat.Bmp,
                ModuleWidthPixels = moduleW,
                BarHeightPixels = barH,
                QuietZoneModules = quiet
            }, TestContext.Current.CancellationToken);

        var bordered = await service.GenerateAsync(
            "X", BarcodeSymbology.Code128, new() {
                Format = BarcodeFormat.Bmp,
                ModuleWidthPixels = moduleW,
                BarHeightPixels = barH,
                QuietZoneModules = quiet,
                ShowBorder = true,
                BorderWidthPixels = border,
                BorderColorHex = "#FF0000"
            }, TestContext.Current.CancellationToken);

        var p0 = Assert.IsType<BarcodeResult>(plain);
        var p1 = Assert.IsType<BarcodeResult>(bordered);
        Assert.True(plain.IsSuccess && bordered.IsSuccess);
        Assert.Equal(p0.ImageWidthPixels + 2 * border, p1.ImageWidthPixels);
        Assert.Equal(p0.ImageHeightPixels + 2 * border, p1.ImageHeightPixels);
    }

    [Fact]
    public async Task GenerateAsync_Code128Svg_WithHumanReadable_EscapesMarkupInCaption()
    {
        var service = CreateService();
        var result = await service.GenerateAsync(
            "X", BarcodeSymbology.Code128, new() {
                Format = BarcodeFormat.Svg,
                ModuleWidthPixels = 1,
                BarHeightPixels = 24,
                QuietZoneModules = 10,
                ShowHumanReadableTextBelow = true,
                HumanReadableText = "a<b&\"'"
            }, TestContext.Current.CancellationToken);

        var br = Assert.IsType<BarcodeResult>(result);
        Assert.True(result.IsSuccess);
        var svg = Encoding.UTF8.GetString(br.ImageBytes!);
        Assert.DoesNotContain("<b", svg, StringComparison.Ordinal);
        Assert.Contains("&lt;b", svg, StringComparison.Ordinal);
    }
}