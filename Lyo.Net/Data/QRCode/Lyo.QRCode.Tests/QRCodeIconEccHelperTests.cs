using Lyo.QRCode.Models;

namespace Lyo.QRCode.Tests;

public class QRCodeIconEccHelperTests
{
    [Fact]
    public void GetEffectiveLevel_NoIcon_ReturnsRequested()
    {
        Assert.Equal(QRCodeErrorCorrectionLevel.Low, QRCodeIconEccHelper.GetEffectiveLevel(QRCodeErrorCorrectionLevel.Low, null));
        Assert.Equal(QRCodeErrorCorrectionLevel.High, QRCodeIconEccHelper.GetEffectiveLevel(QRCodeErrorCorrectionLevel.High, null));
    }

    [Theory]
    [InlineData(15, QRCodeErrorCorrectionLevel.Low, QRCodeErrorCorrectionLevel.Medium)]
    [InlineData(15, QRCodeErrorCorrectionLevel.Medium, QRCodeErrorCorrectionLevel.Medium)]
    [InlineData(20, QRCodeErrorCorrectionLevel.Medium, QRCodeErrorCorrectionLevel.Quartile)]
    [InlineData(20, QRCodeErrorCorrectionLevel.Quartile, QRCodeErrorCorrectionLevel.Quartile)]
    [InlineData(20, QRCodeErrorCorrectionLevel.High, QRCodeErrorCorrectionLevel.High)]
    [InlineData(25, QRCodeErrorCorrectionLevel.Medium, QRCodeErrorCorrectionLevel.High)]
    public void GetEffectiveLevel_WithIcon_RaisesLevelWhenNeeded(int iconPct, QRCodeErrorCorrectionLevel requested, QRCodeErrorCorrectionLevel expected)
    {
        var icon = new QRCodeIconOptions { IconBytes = [0], IconSizePercent = iconPct };
        Assert.Equal(expected, QRCodeIconEccHelper.GetEffectiveLevel(requested, icon));
    }
}
