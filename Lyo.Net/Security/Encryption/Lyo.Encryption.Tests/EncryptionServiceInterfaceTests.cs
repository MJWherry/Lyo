using System.Security.Cryptography;
using System.Text;
using Lyo.Encryption.AesGcm;
using Lyo.Encryption.ChaCha20Poly1305;
using Lyo.Encryption.Rsa;
using Lyo.IO.Temp.Models;
using Lyo.Keystore;

namespace Lyo.Encryption.Tests;

public class EncryptionServiceInterfaceTests : IDisposable, IAsyncDisposable
{
    private readonly IIOTempSession _tempSession = new IOTempSession(new());

    public async ValueTask DisposeAsync() => await _tempSession.DisposeAsync().ConfigureAwait(false);

    public void Dispose() => _tempSession.Dispose();

    [Fact]
    public void EncryptString_DecryptString_AesService()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        keyStore.UpdateKeyFromString(keyId, "s3cr3t");
        IEncryptionService svc = new AesGcmEncryptionService(keyStore);
        var enc = svc.EncryptString("hello", keyId);
        var dec = svc.DecryptString(enc, keyId);
        Assert.Equal("hello", dec);
    }

    [Fact]
    public void EncryptString_DecryptString_ChaCha20Poly1305Service()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        keyStore.UpdateKeyFromString(keyId, "s3cr3t");
        IEncryptionService svc = new ChaCha20Poly1305EncryptionService(keyStore);
        var enc = svc.EncryptString("hello", keyId);
        var dec = svc.DecryptString(enc, keyId);
        Assert.Equal("hello", dec);
    }

    [Fact]
    public void EncryptString_DecryptString_RsaService()
    {
        var (pub, priv) = GeneratePemFiles();
        using var svc = new RsaEncryptionService(pub, priv, padding: RSAEncryptionPadding.OaepSHA256);
        IEncryptionService service = svc;
        var enc = service.EncryptString("hello");
        var dec = service.DecryptString(enc);
        Assert.Equal("hello", dec);
    }

    [Fact]
    public async Task EncryptStreamAsync_DecryptStreamAsync_Roundtrip()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        await keyStore.UpdateKeyFromStringAsync(keyId, "stream-key", TestContext.Current.CancellationToken).ConfigureAwait(false);
        IEncryptionService svc = new AesGcmEncryptionService(keyStore);
        var input = new MemoryStream(Encoding.UTF8.GetBytes("This is a long stream content to encrypt"));
        var encStream = new MemoryStream();
        await svc.EncryptToStreamAsync(input, encStream, keyId, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        encStream.Position = 0;
        var outStream = new MemoryStream();
        await svc.DecryptToStreamAsync(encStream, outStream, keyId, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        var result = Encoding.UTF8.GetString(outStream.ToArray());
        Assert.Equal("This is a long stream content to encrypt", result);
    }

    [Fact]
    public async Task EncryptStreamAsync_DecryptStreamAsync_ChaCha20Poly1305()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        await keyStore.UpdateKeyFromStringAsync(keyId, "stream-key", TestContext.Current.CancellationToken).ConfigureAwait(false);
        IEncryptionService svc = new ChaCha20Poly1305EncryptionService(keyStore);
        var input = new MemoryStream(Encoding.UTF8.GetBytes("This is a long stream content to encrypt"));
        var encStream = new MemoryStream();
        await svc.EncryptToStreamAsync(input, encStream, keyId, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        encStream.Position = 0;
        var outStream = new MemoryStream();
        await svc.DecryptToStreamAsync(encStream, outStream, keyId, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        var result = Encoding.UTF8.GetString(outStream.ToArray());
        Assert.Equal("This is a long stream content to encrypt", result);
    }

    [Fact]
    public async Task EncryptStreamAsync_DecryptStreamAsync_Rsa()
    {
        var (pub, priv) = GeneratePemFiles();
        await using var svc = new RsaEncryptionService(pub, priv, padding: RSAEncryptionPadding.OaepSHA256);
        IEncryptionService service = svc;
        var input = new MemoryStream(Encoding.UTF8.GetBytes("This is a long stream content to encrypt"));
        var encStream = new MemoryStream();
        await service.EncryptToStreamAsync(input, encStream, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        encStream.Position = 0;
        var outStream = new MemoryStream();
        await service.DecryptToStreamAsync(encStream, outStream, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        var result = Encoding.UTF8.GetString(outStream.ToArray());
        Assert.Equal("This is a long stream content to encrypt", result);
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