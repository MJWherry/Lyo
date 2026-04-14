namespace Lyo.Encryption.Tests;

public class EncryptionHeaderVersionTests
{
    [Fact]
    public void Unknown_HasValueZero() => Assert.Equal((byte)0, (byte)EncryptionHeaderVersion.Unknown);

    [Fact]
    public void V1_HasValueOne() => Assert.Equal((byte)1, (byte)EncryptionHeaderVersion.V1);

    [Fact]
    public void Enum_CanBeCastToByte()
    {
        var unknownByte = (byte)EncryptionHeaderVersion.Unknown;
        var v1Byte = (byte)EncryptionHeaderVersion.V1;
        Assert.Equal(0, unknownByte);
        Assert.Equal(1, v1Byte);
    }

    [Fact]
    public void Byte_CanBeCastToEnum()
    {
        var unknown = (EncryptionHeaderVersion)0;
        var v1 = (EncryptionHeaderVersion)1;
        Assert.Equal(EncryptionHeaderVersion.Unknown, unknown);
        Assert.Equal(EncryptionHeaderVersion.V1, v1);
    }
}