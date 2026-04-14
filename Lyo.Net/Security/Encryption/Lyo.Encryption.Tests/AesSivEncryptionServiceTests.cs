using System.Text;
using Lyo.Common.Records;
using Lyo.Encryption.Extensions;
using Lyo.Encryption.Symmetric.Aes.AesSiv;
using Lyo.Keystore;
using Lyo.Keystore.KeyDerivation;

namespace Lyo.Encryption.Tests;

public class AesSivEncryptionServiceTests
{
    private static readonly IKeyDerivationService Kdf = new Pbkdf2KeyDerivationService();

    private static byte[] Key32(string s) => Kdf.DeriveKey(s, keySizeBytes: 32);

    [Fact]
    public void Service_Roundtrip_WithKey()
    {
        var ks = new LocalKeyStore();
        ks.UpdateKeyFromString("k", "pw");
        var svc = new AesSivEncryptionService(ks, AesSivKeySizeBits.Bits256);
        var key = Key32("siv");
        var pt = "hello-siv"u8.ToArray();
        var enc = svc.Encrypt(pt, key: key);
        var dec = svc.Decrypt(enc, key: key);
        Assert.Equal("hello-siv", Encoding.UTF8.GetString(dec));
    }

    [Fact]
    public void FileExtension_IsSiv()
    {
        var ks = new LocalKeyStore();
        ks.UpdateKeyFromString("k", "pw");
        var svc = new AesSivEncryptionService(ks);
        Assert.Equal(FileTypeInfo.LyoAesSiv.DefaultExtension, svc.FileExtension);
    }

    [Fact]
    public void DetermineAlgorithm_ReturnsAesSiv()
    {
        var ks = new LocalKeyStore();
        ks.UpdateKeyFromString("k", "pw");
        var svc = new AesSivEncryptionService(ks);
        Assert.Equal(EncryptionAlgorithm.AesSiv, EncryptionServiceExtensions.DetermineAlgorithm(svc));
    }
}
