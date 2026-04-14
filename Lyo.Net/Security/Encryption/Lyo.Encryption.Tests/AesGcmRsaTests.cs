using System.Security.Cryptography;
using System.Text;
using Lyo.Encryption.AesGcmRsa;
using Lyo.Encryption.Exceptions;
using Lyo.Encryption.Rsa;
using Lyo.IO.Temp.Models;

namespace Lyo.Encryption.Tests;

public class AesGcmRsaTests : IDisposable, IAsyncDisposable
{
    private readonly IIOTempSession _tempSession = new IOTempSession(new());

    public async ValueTask DisposeAsync() => await _tempSession.DisposeAsync().ConfigureAwait(false);

    public void Dispose() => _tempSession.Dispose();

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

    [Fact]
    public void RsaKeyLoader_LoadFromPemFiles_Works()
    {
        var (pub, priv) = GeneratePemFiles();
        using var rsa = RsaKeyLoader.LoadFromPemFiles(pub, priv);
        Assert.NotNull(rsa);
    }

    [Fact]
    public void Hybrid_EncryptDecrypt_EmbeddedKey()
    {
        var (pub, priv) = GeneratePemFiles();
        using var svc = new AesGcmRsaEncryptionService(pub, priv, padding: RSAEncryptionPadding.OaepSHA256);
        var plaintext = Encoding.UTF8.GetBytes("secret message");
        var enc = svc.Encrypt(plaintext);
        var dec = svc.Decrypt(enc);
        Assert.Equal("secret message", Encoding.UTF8.GetString(dec));
    }

    [Fact]
    public void Hybrid_EncryptDecrypt_ExternalKey()
    {
        var (pub, priv) = GeneratePemFiles();
        using var svc = new AesGcmRsaEncryptionService(pub, priv, padding: RSAEncryptionPadding.OaepSHA256);
        var aesKey = RandomNumberGenerator.GetBytes(32);
        var plaintext = Encoding.UTF8.GetBytes("external key message");
        var enc = svc.Encrypt(plaintext, key: aesKey);
        var dec = svc.Decrypt(enc, key: aesKey);
        Assert.Equal("external key message", Encoding.UTF8.GetString(dec));
    }

    [Fact]
    public void Hybrid_Decrypt_WithWrongExternalKey_Throws()
    {
        var (pub, priv) = GeneratePemFiles();
        using var svc = new AesGcmRsaEncryptionService(pub, priv, padding: RSAEncryptionPadding.OaepSHA256);
        var aesKey = RandomNumberGenerator.GetBytes(32);
        var plaintext = "payload"u8.ToArray();
        var enc = svc.Encrypt(plaintext, key: aesKey);
        var wrongKey = RandomNumberGenerator.GetBytes(32);
        Assert.ThrowsAny<DecryptionFailedException>(() => svc.Decrypt(enc, key: wrongKey));
    }

    [Fact]
    public void Hybrid_Decrypt_TamperedCiphertext_Throws()
    {
        var (pub, priv) = GeneratePemFiles();
        using var svc = new AesGcmRsaEncryptionService(pub, priv, padding: RSAEncryptionPadding.OaepSHA256);
        var plaintext = "payload2"u8.ToArray();
        var enc = svc.Encrypt(plaintext);
        // flip a byte in the ciphertext region
        enc[^1] ^= 0xFF;
        Assert.ThrowsAny<DecryptionFailedException>(() => svc.Decrypt(enc));
    }

    [Fact]
    public void Constructor_NoKeys_Throws()
        => Assert.Throws<InvalidOperationException>(() => {
            var _ = new AesGcmRsaEncryptionService();
        });
}