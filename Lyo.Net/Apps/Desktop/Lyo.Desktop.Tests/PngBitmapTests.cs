namespace Lyo.Desktop.Tests;

public class PngBitmapTests
{
    [Fact]
    public void ToDib_RgbPng_IsBottomUpBgra()
    {
        byte[] png = [
            137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0, 13, 73, 72, 68, 82, 0, 0, 0, 2, 0, 0, 0, 1, 8, 2, 0, 0, 0, 123, 64, 232, 221, 0, 0, 0, 13, 73, 68, 65, 84, 120, 156, 99,
            248, 207, 0, 4, 255, 1, 7, 0, 1, 255, 226, 35, 158, 89, 0, 0, 0, 0, 73, 69, 78, 68, 174, 66, 96, 130
        ];

        var dib = PngBitmap.ToDib(png);

        Assert.Equal(48, dib.Length);
        Assert.Equal(2, BitConverter.ToInt32(dib, 4));
        Assert.Equal(1, BitConverter.ToInt32(dib, 8));
        Assert.Equal(32, BitConverter.ToInt16(dib, 14));
        Assert.Equal(new byte[] { 0, 0, 255, 255, 255, 0, 0, 255 }, dib[40..]);
    }
}
