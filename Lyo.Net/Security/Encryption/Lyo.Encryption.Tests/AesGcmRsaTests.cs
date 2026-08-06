using System.Security.Cryptography;
using System.Text;
using Lyo.Encryption.AesGcmRsa;
using Lyo.Encryption.Exceptions;
using Lyo.Encryption.Rsa;
using Lyo.Exceptions.Models;
using Lyo.IO.Temp.Models;
using Lyo.Testing;

namespace Lyo.Encryption.Tests;

public class AesGcmRsaTests : IDisposable, IAsyncDisposable
{
    private readonly IIOTempSession _tempSession = new IOTempSession(new());

    public async ValueTask DisposeAsync() => await _tempSession.DisposeAsync();

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
        var aesKey = TestData.Create(32);
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
        var aesKey = TestData.Create(32);
        var plaintext = "payload"u8.ToArray();
        var enc = svc.Encrypt(plaintext, key: aesKey);
        var wrongKey = TestData.Create(32, TestData.Seed ^ 1);
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
        => Assert.Throws<ConfigurationException>(() => {
            var _ = new AesGcmRsaEncryptionService();
        });

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    [InlineData(5000)]
    public async Task Hybrid_Stream_Roundtrip_AcrossChunkBoundaries(int size)
    {
        var ct = TestContext.Current.CancellationToken;
        var (pub, priv) = GeneratePemFiles();
        using var svc = new AesGcmRsaEncryptionService(pub, priv, padding: RSAEncryptionPadding.OaepSHA256);
        var plaintext = TestData.Create(size);
        byte[] encrypted;
        using (var input = new MemoryStream(plaintext)) {
            using (var output = new MemoryStream()) {
                await svc.EncryptToStreamAsync(input, output, chunkSize: 64, ct: ct);
                encrypted = output.ToArray();
            }
        }

        byte[] decrypted;
        using (var input = new MemoryStream(encrypted)) {
            using (var output = new MemoryStream()) {
                await svc.DecryptToStreamAsync(input, output, ct: ct);
                decrypted = output.ToArray();
            }
        }

        Assert.Equal(plaintext, decrypted);
    }

    [Fact]
    public void Hybrid_IsDiscoverable_AsAesGcmRsa()
    {
        var (pub, priv) = GeneratePemFiles();
        using var svc = new AesGcmRsaEncryptionService(pub, priv, padding: RSAEncryptionPadding.OaepSHA256);
        Assert.Equal(EncryptionAlgorithm.AesGcmRsa, EncryptionAlgorithmDiscovery.FromEncryptionService(svc));
    }
}