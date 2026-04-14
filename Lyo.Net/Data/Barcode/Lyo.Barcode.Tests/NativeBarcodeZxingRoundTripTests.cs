using Lyo.Barcode.Models;
using Lyo.Barcode.Native;
using Lyo.Codes.ZXing;
using Lyo.Metrics;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Barcode.Tests;

public class NativeBarcodeZxingRoundTripTests
{
    private static NativeBarcodeService CreateService() => new(new(), NullLogger<NativeBarcodeService>.Instance, NullMetrics.Instance);

    [Theory]
    [InlineData("12345678")]
    [InlineData("TEST1234")]
    [InlineData("HELLO-128")]
    public async Task GeneratedBmp_DecodesWithZxing(string data)
    {
        var service = CreateService();
        var result = await service.GenerateAsync(
            data, BarcodeSymbology.Code128, new() {
                Format = BarcodeFormat.Bmp,
                ModuleWidthPixels = 2,
                BarHeightPixels = 80,
                QuietZoneModules = 10
            }, TestContext.Current.CancellationToken).ConfigureAwait(false);

        var br = Assert.IsType<BarcodeResult>(result);
        Assert.True(result.IsSuccess, result.Errors?[0].Message);
        Assert.NotNull(br.ImageBytes);
        var decode = ZxingCodeImageDecoder.DecodeBarcode(br.ImageBytes!);
        Assert.True(decode.IsSuccess, decode.Errors?[0].Message);
        Assert.Equal(data, decode.Data!.Text);
    }
}