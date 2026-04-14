using System.Text;
using Lyo.Common.Records;
using Lyo.Encryption.Extensions;
using Lyo.Encryption.Symmetric.ChaCha.XChaCha20Poly1305;
using Lyo.Keystore;
using Lyo.Keystore.KeyDerivation;

namespace Lyo.Encryption.Tests;

public class XChaCha20Poly1305EncryptionServiceTests
{
    private static readonly IKeyDerivationService Kdf = new Pbkdf2KeyDerivationService();

    private static byte[] Key32(string s) => Kdf.DeriveKey(s, keySizeBytes: 32);

    [Fact]
    public void Service_Roundtrip_WithKey()
    {
        var ks = new LocalKeyStore();
        ks.UpdateKeyFromString("k", "pw");
        var svc = new XChaCha20Poly1305EncryptionService(ks);
        var key = Key32("x");
        var pt = "hello-xchacha"u8.ToArray();
        var enc = svc.Encrypt(pt, key: key);
        var dec = svc.Decrypt(enc, key: key);
        Assert.Equal("hello-xchacha", Encoding.UTF8.GetString(dec));
    }

    [Fact]
    public void FileExtension_IsXChaCha()
    {
        var ks = new LocalKeyStore();
        ks.UpdateKeyFromString("k", "pw");
        var svc = new XChaCha20Poly1305EncryptionService(ks);
        Assert.Equal(FileTypeInfo.LyoXChaCha20Poly1305.DefaultExtension, svc.FileExtension);
    }

    [Fact]
    public void DetermineAlgorithm_ReturnsXChaCha()
    {
        var ks = new LocalKeyStore();
        ks.UpdateKeyFromString("k", "pw");
        var svc = new XChaCha20Poly1305EncryptionService(ks);
        Assert.Equal(EncryptionAlgorithm.XChaCha20Poly1305, EncryptionServiceExtensions.DetermineAlgorithm(svc));
    }
}
