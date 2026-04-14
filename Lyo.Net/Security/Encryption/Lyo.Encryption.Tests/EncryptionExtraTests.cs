using System.Security.Cryptography;
using System.Text;
using Lyo.Encryption.AesGcm;
using Lyo.Encryption.AesGcmRsa;
using Lyo.Exceptions.Models;
using Lyo.IO.Temp.Models;
using Lyo.Keystore;
using Lyo.Keystore.KeyDerivation;

namespace Lyo.Encryption.Tests;

public class EncryptionExtraTests : IDisposable, IAsyncDisposable
{
    private static readonly IKeyDerivationService KeyDerivationService = new Pbkdf2KeyDerivationService();

    private readonly IIOTempSession _tempSession = new IOTempSession(new());

    public async ValueTask DisposeAsync() => await _tempSession.DisposeAsync().ConfigureAwait(false);

    public void Dispose() => _tempSession.Dispose();

    private static byte[] DeriveKey(string password) => KeyDerivationService.DeriveKey(password);

    [Fact]
    public void AesGcmHelper_Constants_AreExpected()
    {
        Assert.Equal(12, AesGcmHelper.NonceSize);
        Assert.Equal(16, AesGcmHelper.TagSize);
    }

    [Fact]
    public void AesGcmHelper_EncryptDecrypt_EmptyPlaintext()
    {
        var key = DeriveKey("k");
        var nonce = RandomNumberGenerator.GetBytes(AesGcmHelper.NonceSize);
        var (cipher, tag) = AesGcmHelper.Encrypt([], key, nonce);
        var pt = AesGcmHelper.Decrypt(cipher, tag, key, nonce);
        Assert.Empty(pt);
    }

    [Fact]
    public void AesGcmEncryptionService_EmptyPayload_Throws()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        keyStore.UpdateKeyFromString(keyId, "opt");
        var svc = new AesGcmEncryptionService(keyStore);
        Assert.Throws<ArgumentOutsideRangeException>(() => svc.Encrypt([], keyId));
    }

    [Fact]
    public async Task Stream_EncryptDecrypt_WithExternalKey()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        await keyStore.UpdateKeyFromStringAsync(keyId, "stream-key", TestContext.Current.CancellationToken).ConfigureAwait(false);
        var svc = new AesGcmEncryptionService(keyStore);
        var aesKey = DeriveKey("external");
        var input = new MemoryStream(Encoding.UTF8.GetBytes("stream external key"));
        var encStream = new MemoryStream();
        await svc.EncryptToStreamAsync(input, encStream, key: aesKey, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        encStream.Position = 0;
        var outStream = new MemoryStream();
        await svc.DecryptToStreamAsync(encStream, outStream, key: aesKey, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        var res = Encoding.UTF8.GetString(outStream.ToArray());
        Assert.Equal("stream external key", res);
    }

    [Fact]
    public void AesGcmRsaEncryptionService_RandomKey_Workflow()
    {
        // Service should generate random AES key for each encryption and encrypt it with RSA
        var (pub, priv) = GeneratePemFiles();
        using var svc = new AesGcmRsaEncryptionService(pub, priv, padding: RSAEncryptionPadding.OaepSHA256);
        var plaintext = Encoding.UTF8.GetBytes("using random key");
        // Encrypt without key -> generates random key and encrypts it with RSA
        var enc = svc.Encrypt(plaintext);
        // Decrypt without key -> decrypts the embedded key using RSA private key
        var dec = svc.Decrypt(enc);
        Assert.Equal("using random key", Encoding.UTF8.GetString(dec));

        // Verify that each encryption uses a different key (different ciphertext)
        var enc2 = svc.Encrypt(plaintext);
        Assert.NotEqual(enc, enc2); // Should be different due to different random keys
    }

    [Fact]
    public void AesGcmRsaEncryptionService_Dispose_DisposesRSA()
    {
        var (pub, priv) = GeneratePemFiles();
        var svc = new AesGcmRsaEncryptionService(pub, priv, padding: RSAEncryptionPadding.OaepSHA256);
        svc.Dispose();
        // after Dispose further operations should throw some exception when using RSA; attempt encrypt
        var threw = false;
        try {
            svc.Encrypt([1, 2, 3]);
        }
        catch {
            threw = true;
        }

        Assert.True(threw, "Expected calling Encrypt after Dispose to throw");
    }

    private (string pubPath, string privPath) GeneratePemFiles()
    {
        using var rsa = RSA.Create(2048);
        var pub = rsa.ExportSubjectPublicKeyInfo();
        var priv = rsa.ExportPkcs8PrivateKey();
        var pubPem = "-----BEGIN PUBLIC KEY-----\n" + Convert.ToBase64String(pub) + "\n-----END PUBLIC KEY-----";
        var privPem = "-----BEGIN PRIVATE KEY-----\n" + Convert.ToBase64String(priv) + "\n-----END PRIVATE KEY-----";
        var pubPath = _tempSession.CreateFile(pubPem);
        var privPath = _tempSession.CreateFile(privPem);
        return (pubPath, privPath);
    }
}