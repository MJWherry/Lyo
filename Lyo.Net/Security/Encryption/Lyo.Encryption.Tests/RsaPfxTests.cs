using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Lyo.Encryption.Rsa;
using Lyo.IO.Temp.Models;

namespace Lyo.Encryption.Tests;

public class RsaPfxTests : IDisposable, IAsyncDisposable
{
    private readonly IIOTempSession _tempSession = new IOTempSession(new());

    public async ValueTask DisposeAsync() => await _tempSession.DisposeAsync().ConfigureAwait(false);

    public void Dispose() => _tempSession.Dispose();

    [Fact]
    public void LoadFromPfx_Works_WithGeneratedCert()
    {
        // create a temporary self-signed cert using RSA for testing
        using var rsa = RSA.Create(2048);
        var req = new CertificateRequest("cn=unittest", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var cert = req.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(365));
        var pfx = cert.Export(X509ContentType.Pkcs12, "pwd");
        var pfxFile = _tempSession.CreateFile(pfx);
        using var loadedRsa = RsaKeyLoader.LoadFromPfx(pfxFile, "pwd");
        Assert.NotNull(loadedRsa);
    }

    [Fact]
    public void LoadFromPemFiles_PublicOnly_AllowsPublicEncryptionButNotDecryption()
    {
        var (pub, priv) = GeneratePemFiles();
        // Load only public key
        var rsaPub = RsaKeyLoader.LoadFromPemFiles(pub);
        Assert.NotNull(rsaPub);
        var data = new byte[] { 1, 2, 3 };
        var enc = rsaPub.Encrypt(data, RSAEncryptionPadding.OaepSHA256);
        // attempt to decrypt with public-only RSA should fail if used; but LoadFromPemFiles with null private path will return rsa with only public key loaded
        var threw = false;
        try {
            rsaPub.Decrypt(enc, RSAEncryptionPadding.OaepSHA256);
        }
        catch {
            threw = true;
        }

        Assert.True(threw, "Expected decryption with public-only key to throw");

        (string pubPath, string privPath) GeneratePemFiles()
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
}