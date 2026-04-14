using Lyo.Encryption;

namespace Lyo.Encryption.Tests;

public class EncryptionHeaderDekKeyMaterialTests
{
    [Theory]
    [InlineData((byte)16)]
    [InlineData((byte)24)]
    [InlineData((byte)32)]
    public void WriteThenRead_PreservesDekKeyMaterialBytes(byte dekMaterialLen)
    {
        var original = new EncryptionHeader {
            DekAlgorithmId = (byte)EncryptionAlgorithm.AesGcm,
            KekAlgorithmId = (byte)EncryptionAlgorithm.AesGcm,
            DekKeyMaterialBytes = dekMaterialLen,
            KeyId = "kid",
            KeyVersion = "v1",
            EncryptedDataEncryptionKey = [1, 2, 3]
        };

        using var ms = new MemoryStream();
        original.Write(ms);
        ms.Position = 0;
        var read = EncryptionHeader.Read(ms);
        Assert.Equal((byte)StreamFormatVersion.V1, read.FormatVersion);
        Assert.Equal(dekMaterialLen, read.DekKeyMaterialBytes);
        Assert.Equal(original.DekAlgorithmId, read.DekAlgorithmId);
        Assert.Equal(original.KekAlgorithmId, read.KekAlgorithmId);
        Assert.Equal(original.KeyId, read.KeyId);
        Assert.Equal(original.KeyVersion, read.KeyVersion);
        Assert.Equal(original.EncryptedDataEncryptionKey, read.EncryptedDataEncryptionKey);
    }
}
