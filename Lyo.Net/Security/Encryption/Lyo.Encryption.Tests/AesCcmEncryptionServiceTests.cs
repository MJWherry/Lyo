using System.Text;
using Lyo.Common.Records;
using Lyo.Encryption.Extensions;
using Lyo.Encryption.Symmetric.Aes.AesCcm;
using Lyo.Keystore;
using Lyo.Keystore.KeyDerivation;

namespace Lyo.Encryption.Tests;

public class AesCcmEncryptionServiceTests
{
    private static readonly IKeyDerivationService Kdf = new Pbkdf2KeyDerivationService();

    private static byte[] Key32(string salt) => Kdf.DeriveKey(salt, keySizeBytes: 32);

    [Fact]
    public void Service_Roundtrip_WithKey()
    {
        var ks = new LocalKeyStore();
        ks.UpdateKeyFromString("k", "pw");
        var svc = new AesCcmEncryptionService(ks, AesGcmKeySizeBits.Bits256);
        var key = Key32("raw");
        var pt = "hello-ccm"u8.ToArray();
        var enc = svc.Encrypt(pt, key: key);
        var dec = svc.Decrypt(enc, key: key);
        Assert.Equal("hello-ccm", Encoding.UTF8.GetString(dec));
    }

    [Fact]
    public void FileExtension_IsCcm()
    {
        var ks = new LocalKeyStore();
        ks.UpdateKeyFromString("k", "pw");
        var svc = new AesCcmEncryptionService(ks);
        Assert.Equal(FileTypeInfo.LyoAesCcm.DefaultExtension, svc.FileExtension);
    }

    [Fact]
    public void DetermineAlgorithm_ReturnsAesCcm()
    {
        var ks = new LocalKeyStore();
        ks.UpdateKeyFromString("k", "pw");
        var svc = new AesCcmEncryptionService(ks);
        Assert.Equal(EncryptionAlgorithm.AesCcm, EncryptionServiceExtensions.DetermineAlgorithm(svc));
    }
}
