using Lyo.Encryption.AesGcm;
using Lyo.Encryption.ChaCha20Poly1305;
using Lyo.Encryption.Symmetric.Aes.AesSiv;
using Lyo.Encryption.Symmetric.ChaCha.XChaCha20Poly1305;
using Lyo.Keystore;

namespace Lyo.Encryption.Tests;

public class SymmetricKeyMaterialSizeTests
{
    [Fact]
    public void ChaCha20Poly1305_RequiredKeyBytes_Is32()
    {
        var ks = new LocalKeyStore();
        ISymmetricKeyMaterialSize svc = new ChaCha20Poly1305EncryptionService(ks);
        Assert.Equal(32, svc.RequiredKeyBytes);
    }

    [Theory]
    [InlineData(AesGcmKeySizeBits.Bits128, 16)]
    [InlineData(AesGcmKeySizeBits.Bits192, 24)]
    [InlineData(AesGcmKeySizeBits.Bits256, 32)]
    public void AesGcm_RequiredKeyBytes_MatchesKeySize(AesGcmKeySizeBits bits, int expected)
    {
        var ks = new LocalKeyStore();
        ISymmetricKeyMaterialSize svc = new AesGcmEncryptionService(ks, bits);
        Assert.Equal(expected, svc.RequiredKeyBytes);
    }

    [Fact]
    public void XChaCha_RequiredKeyBytes_Is32()
    {
        var ks = new LocalKeyStore();
        ISymmetricKeyMaterialSize svc = new XChaCha20Poly1305EncryptionService(ks);
        Assert.Equal(32, svc.RequiredKeyBytes);
    }

    [Theory]
    [InlineData(AesSivKeySizeBits.Bits256, 32)]
    [InlineData(AesSivKeySizeBits.Bits384, 48)]
    [InlineData(AesSivKeySizeBits.Bits512, 64)]
    public void AesSiv_RequiredKeyBytes_Matches(AesSivKeySizeBits bits, int expected)
    {
        var ks = new LocalKeyStore();
        ISymmetricKeyMaterialSize svc = new AesSivEncryptionService(ks, bits);
        Assert.Equal(expected, svc.RequiredKeyBytes);
    }
}
