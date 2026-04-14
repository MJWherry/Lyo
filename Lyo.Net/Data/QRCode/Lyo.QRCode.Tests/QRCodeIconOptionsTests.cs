using Lyo.QRCode.Models;

namespace Lyo.QRCode.Tests;

public class QRCodeIconOptionsTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(30, 30)]
    [InlineData(31, 30)]
    [InlineData(50, 30)]
    public void ClampIconSizePercent_ClampToSupportedRange(int input, int expected)
        => Assert.Equal(expected, QRCodeIconOptions.ClampIconSizePercent(input));
}
