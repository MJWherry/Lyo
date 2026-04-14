using System.Security.Cryptography;
using Lyo.Encryption.Rsa;
using Lyo.IO.Temp.Models;

namespace Lyo.Encryption.Tests;

public class RsaKeyLoaderTests : IDisposable, IAsyncDisposable
{
    private readonly IIOTempSession _tempSession = new IOTempSession(new());

    public async ValueTask DisposeAsync() => await _tempSession.DisposeAsync().ConfigureAwait(false);

    public void Dispose() => _tempSession.Dispose();

    [Fact]
    public void ReadPem_Invalid_Throws()
    {
        var path = _tempSession.CreateFile("no pem here");
        Assert.Throws<FormatException>(() => RsaKeyLoader.LoadFromPemFiles(path));
    }

    [Fact]
    public void LoadFromPemFiles_WithBothKeys_CanEncryptAndDecrypt()
    {
        var (pub, priv) = GeneratePemFiles();
        using var rsa = RsaKeyLoader.LoadFromPemFiles(pub, priv);
        Assert.NotNull(rsa);
        var data = new byte[] { 10, 20, 30, 40 };
        var enc = rsa.Encrypt(data, RSAEncryptionPadding.OaepSHA256);
        var dec = rsa.Decrypt(enc, RSAEncryptionPadding.OaepSHA256);
        Assert.Equal(data, dec);
    }

    [Fact]
    public void LoadFromPemFiles_PublicOnly_ReturnsRsaButDecryptFails()
    {
        var (pub, priv) = GeneratePemFiles();
        using var rsaPub = RsaKeyLoader.LoadFromPemFiles(pub);
        Assert.NotNull(rsaPub);
        var data = new byte[] { 5, 6, 7 };
        var enc = rsaPub.Encrypt(data, RSAEncryptionPadding.OaepSHA256);
        var threw = false;
        try {
            rsaPub.Decrypt(enc, RSAEncryptionPadding.OaepSHA256);
        }
        catch {
            threw = true;
        }

        Assert.True(threw);
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