using Lyo.Encryption;

namespace Lyo.Encryption.Tests;

public class AesGcmKeySizeExtensionsTests
{
    [Theory]
    [InlineData(AesGcmKeySizeBits.Bits128, 16)]
    [InlineData(AesGcmKeySizeBits.Bits192, 24)]
    [InlineData(AesGcmKeySizeBits.Bits256, 32)]
    public void GetKeyLengthBytes_MapsToAesKeyMaterialLength(AesGcmKeySizeBits bits, int expectedBytes)
        => Assert.Equal(expectedBytes, bits.GetKeyLengthBytes());

    [Fact]
    public void GetKeyLengthBytes_InvalidValue_Throws()
        => Assert.Throws<ArgumentOutOfRangeException>(() => ((AesGcmKeySizeBits)99).GetKeyLengthBytes());
}
