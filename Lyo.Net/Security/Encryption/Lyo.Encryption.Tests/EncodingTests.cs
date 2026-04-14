using System.Security.Cryptography;
using System.Text;
using Lyo.Encryption.AesGcm;
using Lyo.Encryption.ChaCha20Poly1305;
using Lyo.Encryption.Rsa;
using Lyo.Encryption.TwoKey;
using Lyo.IO.Temp.Models;
using Lyo.Keystore;

namespace Lyo.Encryption.Tests;

public class EncodingTests : IDisposable, IAsyncDisposable
{
    // Test strings with various Unicode characters
    private static readonly string[] TestStrings = [
        "Hello World", // ASCII
        "Привет мир", // Cyrillic
        "你好世界", // Chinese
        "こんにちは世界", // Japanese
        "مرحبا بالعالم", // Arabic
        "🌍🌎🌏", // Emoji
        "Test with émojis 🎉 and spéciál chàracters", // Mixed
        "Line1\nLine2\tTabbed\r\nWindows" // Control characters
    ];

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

    [Theory]
    [InlineData("UTF-8")]
    [InlineData("UTF-16")]
    [InlineData("UTF-32")]
    [InlineData("ASCII")]
    public void AesGcm_EncryptString_DecryptString_WithDifferentEncodings(string encodingName)
    {
        var encoding = Encoding.GetEncoding(encodingName);
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        keyStore.UpdateKeyFromString(keyId, "test-key");
        var svc = new AesGcmEncryptionService(keyStore);
        foreach (var testString in TestStrings) {
            // Skip strings that can't be encoded in ASCII
            if (encodingName == "ASCII" && !IsAsciiCompatible(testString))
                continue;

            var enc = svc.EncryptString(testString, keyId, encoding: encoding);
            var dec = svc.DecryptString(enc, keyId, encoding: encoding);
            Assert.Equal(testString, dec);
        }
    }

    [Theory]
    [InlineData("UTF-8")]
    [InlineData("UTF-16")]
    [InlineData("UTF-32")]
    [InlineData("ASCII")]
    public void ChaCha20Poly1305_EncryptString_DecryptString_WithDifferentEncodings(string encodingName)
    {
        var encoding = Encoding.GetEncoding(encodingName);
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        keyStore.UpdateKeyFromString(keyId, "test-key");
        var svc = new ChaCha20Poly1305EncryptionService(keyStore);
        foreach (var testString in TestStrings) {
            if (encodingName == "ASCII" && !IsAsciiCompatible(testString))
                continue;

            var enc = svc.EncryptString(testString, keyId, encoding: encoding);
            var dec = svc.DecryptString(enc, keyId, encoding: encoding);
            Assert.Equal(testString, dec);
        }
    }

    [Theory]
    [InlineData("UTF-8")]
    [InlineData("UTF-16")]
    [InlineData("UTF-32")]
    [InlineData("ASCII")]
    public void Rsa_EncryptString_DecryptString_WithDifferentEncodings(string encodingName)
    {
        var encoding = Encoding.GetEncoding(encodingName);
        var (pub, priv) = GeneratePemFiles();
        using var svc = new RsaEncryptionService(pub, priv, padding: RSAEncryptionPadding.OaepSHA256);
        foreach (var testString in TestStrings) {
            if (encodingName == "ASCII" && !IsAsciiCompatible(testString))
                continue;

            var enc = svc.EncryptString(testString, encoding: encoding);
            var dec = svc.DecryptString(enc, encoding: encoding);
            Assert.Equal(testString, dec);
        }
    }

    [Theory]
    [InlineData("UTF-8")]
    [InlineData("UTF-16")]
    [InlineData("UTF-32")]
    [InlineData("ASCII")]
    public async Task AesGcm_EncryptStreamAsync_DecryptStreamAsync_WithDifferentEncodings(string encodingName)
    {
        var encoding = Encoding.GetEncoding(encodingName);
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        await keyStore.UpdateKeyFromStringAsync(keyId, "test-key", TestContext.Current.CancellationToken).ConfigureAwait(false);
        var svc = new AesGcmEncryptionService(keyStore);
        foreach (var testString in TestStrings) {
            if (encodingName == "ASCII" && !IsAsciiCompatible(testString))
                continue;

            var bytes = encoding.GetBytes(testString);
            var input = new MemoryStream(bytes);
            var encStream = new MemoryStream();
            await svc.EncryptToStreamAsync(input, encStream, keyId, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
            encStream.Position = 0;
            var outStream = new MemoryStream();
            await svc.DecryptToStreamAsync(encStream, outStream, keyId, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
            var result = encoding.GetString(outStream.ToArray());
            Assert.Equal(testString, result);
        }
    }

    [Theory]
    [InlineData("UTF-8")]
    [InlineData("UTF-16")]
    [InlineData("UTF-32")]
    [InlineData("ASCII")]
    public async Task ChaCha20Poly1305_EncryptStreamAsync_DecryptStreamAsync_WithDifferentEncodings(string encodingName)
    {
        var encoding = Encoding.GetEncoding(encodingName);
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        await keyStore.UpdateKeyFromStringAsync(keyId, "test-key", TestContext.Current.CancellationToken).ConfigureAwait(false);
        var svc = new ChaCha20Poly1305EncryptionService(keyStore);
        foreach (var testString in TestStrings) {
            if (encodingName == "ASCII" && !IsAsciiCompatible(testString))
                continue;

            var bytes = encoding.GetBytes(testString);
            var input = new MemoryStream(bytes);
            var encStream = new MemoryStream();
            await svc.EncryptToStreamAsync(input, encStream, keyId, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
            encStream.Position = 0;
            var outStream = new MemoryStream();
            await svc.DecryptToStreamAsync(encStream, outStream, keyId, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
            var result = encoding.GetString(outStream.ToArray());
            Assert.Equal(testString, result);
        }
    }

    [Theory]
    [InlineData("UTF-8")]
    [InlineData("UTF-16")]
    [InlineData("UTF-32")]
    [InlineData("ASCII")]
    public async Task Rsa_EncryptStreamAsync_DecryptStreamAsync_WithDifferentEncodings(string encodingName)
    {
        var encoding = Encoding.GetEncoding(encodingName);
        var (pub, priv) = GeneratePemFiles();
        await using var svc = new RsaEncryptionService(pub, priv, padding: RSAEncryptionPadding.OaepSHA256);
        foreach (var testString in TestStrings) {
            if (encodingName == "ASCII" && !IsAsciiCompatible(testString))
                continue;

            var bytes = encoding.GetBytes(testString);
            var input = new MemoryStream(bytes);
            var encStream = new MemoryStream();
            await svc.EncryptToStreamAsync(input, encStream, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
            encStream.Position = 0;
            var outStream = new MemoryStream();
            await svc.DecryptToStreamAsync(encStream, outStream, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
            var result = encoding.GetString(outStream.ToArray());
            Assert.Equal(testString, result);
        }
    }

    [Fact]
    public void AesGcm_DefaultEncoding_IsUtf8()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        keyStore.UpdateKeyFromString(keyId, "test-key");
        var svc = new AesGcmEncryptionService(keyStore);
        Assert.Equal(Encoding.UTF8, svc.DefaultEncoding);
    }

    [Fact]
    public void ChaCha20Poly1305_DefaultEncoding_IsUtf8()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        keyStore.UpdateKeyFromString(keyId, "test-key");
        var svc = new ChaCha20Poly1305EncryptionService(keyStore);
        Assert.Equal(Encoding.UTF8, svc.DefaultEncoding);
    }

    [Fact]
    public void Rsa_DefaultEncoding_IsUtf8()
    {
        var (pub, priv) = GeneratePemFiles();
        using var svc = new RsaEncryptionService(pub, priv, padding: RSAEncryptionPadding.OaepSHA256);
        Assert.Equal(Encoding.UTF8, svc.DefaultEncoding);
    }

    [Fact]
    public void AesGcm_EncryptString_UsesDefaultEncoding_WhenNotSpecified()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        keyStore.UpdateKeyFromString(keyId, "test-key");
        var svc = new AesGcmEncryptionService(keyStore);
        var testString = "Hello 世界";
        var enc = svc.EncryptString(testString, keyId);
        var dec = svc.DecryptString(enc, keyId);
        Assert.Equal(testString, dec);
    }

    [Fact]
    public void ChaCha20Poly1305_EncryptString_UsesDefaultEncoding_WhenNotSpecified()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        keyStore.UpdateKeyFromString(keyId, "test-key");
        var svc = new ChaCha20Poly1305EncryptionService(keyStore);
        var testString = "Hello 世界";
        var enc = svc.EncryptString(testString, keyId);
        var dec = svc.DecryptString(enc, keyId);
        Assert.Equal(testString, dec);
    }

    [Fact]
    public void Rsa_EncryptString_UsesDefaultEncoding_WhenNotSpecified()
    {
        var (pub, priv) = GeneratePemFiles();
        using var svc = new RsaEncryptionService(pub, priv, padding: RSAEncryptionPadding.OaepSHA256);
        var testString = "Hello 世界";
        var enc = svc.EncryptString(testString);
        var dec = svc.DecryptString(enc);
        Assert.Equal(testString, dec);
    }

    [Fact]
    public void TwoKeyAesGcm_EncryptString_DecryptString_WithUtf32()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        keyStore.UpdateKeyFromString(keyId, "kek-key");
        var aesGcmService = new AesGcmEncryptionService(keyStore);
        using var svc = new TwoKeyEncryptionService<AesGcmEncryptionService, AesGcmEncryptionService>(aesGcmService, keyStore);
        var testString = "Hello 世界";
        var result = svc.EncryptString(testString, keyId, encoding: Encoding.UTF32);
        var decrypted = svc.DecryptString(result.EncryptedData, result.EncryptedDataEncryptionKey, keyId, Encoding.UTF32);
        Assert.Equal(testString, decrypted);
    }

    [Fact]
    public void Encoding_Mismatch_ProducesIncorrectResult()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        keyStore.UpdateKeyFromString(keyId, "test-key");
        var svc = new AesGcmEncryptionService(keyStore);
        var testString = "Hello 世界";
        var enc = svc.EncryptString(testString, keyId, encoding: Encoding.UTF8);
        var dec = svc.DecryptString(enc, keyId, encoding: Encoding.UTF32);
        Assert.NotEqual(testString, dec);
    }

    private static bool IsAsciiCompatible(string str) => str.All(c => c <= 127);
}