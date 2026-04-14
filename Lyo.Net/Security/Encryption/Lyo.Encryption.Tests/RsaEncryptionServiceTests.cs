using System.Security.Cryptography;
using System.Text;
using Lyo.Encryption.Exceptions;
using Lyo.Encryption.Rsa;
using Lyo.Exceptions.Models;
using Lyo.IO.Temp.Models;

namespace Lyo.Encryption.Tests;

public class RsaEncryptionServiceTests : IDisposable, IAsyncDisposable
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
    public void EncryptDecrypt_SmallData_Roundtrip()
    {
        var (pub, priv) = GeneratePemFiles();
        using var svc = new RsaEncryptionService(pub, priv, padding: RSAEncryptionPadding.OaepSHA256);
        var plaintext = "small message"u8.ToArray();
        var enc = svc.Encrypt(plaintext);
        var dec = svc.Decrypt(enc);
        Assert.Equal("small message", Encoding.UTF8.GetString(dec));
    }

    [Fact]
    public void EncryptDecrypt_LargeData_Chunked()
    {
        var (pub, priv) = GeneratePemFiles();
        using var svc = new RsaEncryptionService(pub, priv, padding: RSAEncryptionPadding.OaepSHA256);

        // Create data larger than RSA can encrypt in one chunk (~190 bytes for 2048-bit key with OAEP-SHA256)
        var plaintext = new byte[500];
        RandomNumberGenerator.Fill(plaintext);
        var enc = svc.Encrypt(plaintext);
        var dec = svc.Decrypt(enc);
        Assert.Equal(plaintext, dec);
    }

    [Fact]
    public void EncryptDecrypt_EmptyData_Throws()
    {
        var (pub, priv) = GeneratePemFiles();
        using var svc = new RsaEncryptionService(pub, priv, padding: RSAEncryptionPadding.OaepSHA256);
        var plaintext = Array.Empty<byte>();
        Assert.Throws<ArgumentOutsideRangeException>(() => svc.Encrypt(plaintext));
    }

    [Fact]
    public void EncryptString_DecryptString_Roundtrip()
    {
        var (pub, priv) = GeneratePemFiles();
        using var svc = new RsaEncryptionService(pub, priv, padding: RSAEncryptionPadding.OaepSHA256);

        // Use interface to access default interface methods
        var enc = svc.EncryptString("test string");
        var dec = svc.DecryptString(enc);
        Assert.Equal("test string", dec);
    }

    [Fact]
    public void EncryptString_DecryptString_WithCustomEncoding()
    {
        var (pub, priv) = GeneratePemFiles();
        using var svc = new RsaEncryptionService(pub, priv, padding: RSAEncryptionPadding.OaepSHA256);
        // Test with UTF-32 encoding
        var enc = svc.EncryptString("test string", encoding: Encoding.UTF32);
        var dec = svc.DecryptString(enc, encoding: Encoding.UTF32);
        Assert.Equal("test string", dec);
    }

    [Fact]
    public void Constructor_NoKeys_Throws()
        => Assert.Throws<InvalidOperationException>(() => {
            _ = new RsaEncryptionService();
        });

    [Fact]
    public void Decrypt_TamperedData_Throws()
    {
        var (pub, priv) = GeneratePemFiles();
        using var svc = new RsaEncryptionService(pub, priv, padding: RSAEncryptionPadding.OaepSHA256);
        var plaintext = Encoding.UTF8.GetBytes("message");
        var enc = svc.Encrypt(plaintext);
        // Tamper the encrypted data
        enc[10] ^= 0xFF;
        Assert.ThrowsAny<DecryptionFailedException>(() => svc.Decrypt(enc));
    }

    [Fact]
    public void Dispose_DisposesRSA()
    {
        var (pub, priv) = GeneratePemFiles();
        var svc = new RsaEncryptionService(pub, priv, padding: RSAEncryptionPadding.OaepSHA256);
        svc.Dispose();

        // After Dispose, operations should fail
        var threw = false;
        try {
            svc.Encrypt([1, 2, 3]);
        }
        catch {
            threw = true;
        }

        Assert.True(threw, "Expected calling Encrypt after Dispose to throw");
    }
}