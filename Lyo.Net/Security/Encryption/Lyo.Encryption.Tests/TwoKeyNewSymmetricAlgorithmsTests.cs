using System.Text;
using Lyo.Encryption.AesGcm;
using Lyo.Encryption.ChaCha20Poly1305;
using Lyo.Encryption.Symmetric.Aes.AesCcm;
using Lyo.Encryption.Symmetric.Aes.AesSiv;
using Lyo.Encryption.Symmetric.ChaCha.XChaCha20Poly1305;
using Lyo.Encryption.TwoKey;
using Lyo.Keystore;

namespace Lyo.Encryption.Tests;

public class TwoKeyNewSymmetricAlgorithmsTests
{
    private static LocalKeyStore Store()
    {
        var ks = new LocalKeyStore();
        ks.UpdateKeyFromString("kid", "kek-material-test-32bytes!!");
        return ks;
    }

    [Fact]
    public void AesCcmDek_AesGcmKek_Roundtrip()
    {
        var ks = Store();
        var dek = new AesCcmEncryptionService(ks);
        var kek = new AesGcmEncryptionService(ks);
        using var svc = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(dek, kek, ks);
        var pt = Encoding.UTF8.GetBytes("two-key ccm");
        var r = svc.Encrypt(pt, "kid");
        var dec = svc.Decrypt(r.EncryptedData, r.EncryptedDataEncryptionKey, "kid");
        Assert.Equal(pt, dec);
    }

    [Fact]
    public void XChaChaDek_ChaChaKek_Roundtrip()
    {
        var ks = Store();
        var dek = new XChaCha20Poly1305EncryptionService(ks);
        var kek = new ChaCha20Poly1305EncryptionService(ks);
        using var svc = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(dek, kek, ks);
        var pt = Encoding.UTF8.GetBytes("two-key xchacha");
        var r = svc.Encrypt(pt, "kid");
        var dec = svc.Decrypt(r.EncryptedData, r.EncryptedDataEncryptionKey, "kid");
        Assert.Equal(pt, dec);
    }

    [Fact]
    public void AesSivDek_AesGcmKek_Roundtrip()
    {
        var ks = Store();
        var dek = new AesSivEncryptionService(ks);
        var kek = new AesGcmEncryptionService(ks);
        using var svc = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(dek, kek, ks);
        var pt = Encoding.UTF8.GetBytes("two-key siv");
        var r = svc.Encrypt(pt, "kid");
        var dec = svc.Decrypt(r.EncryptedData, r.EncryptedDataEncryptionKey, "kid");
        Assert.Equal(pt, dec);
    }
}
