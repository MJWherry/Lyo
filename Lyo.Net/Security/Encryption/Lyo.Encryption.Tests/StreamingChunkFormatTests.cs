using System.Security.Cryptography;
using Lyo.Encryption.AesGcm;
using Lyo.Encryption.ChaCha20Poly1305;
using Lyo.Encryption.Exceptions;
using Lyo.Encryption.TwoKey;
using Lyo.Keystore;
using Lyo.Keystore.KeyDerivation;

namespace Lyo.Encryption.Tests;

/// <summary>
/// Exercises the compact streaming chunk format (<c>[ciphertextLen:4][nonce:12][ciphertext][tag:16]</c>) used by the allocation-reduced streaming AEAD paths: round-trips across
/// chunk boundaries, tamper detection, per-chunk nonce uniqueness, and cross-instance/cross-algorithm decryption.
/// </summary>
public class StreamingChunkFormatTests
{
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private static readonly IKeyDerivationService KeyDerivationService = new Pbkdf2KeyDerivationService();

    private static byte[] DeriveKey(string password) => KeyDerivationService.DeriveKey(password);

    private static IEncryptionService CreateService(string algorithm, IKeyStore keyStore) => algorithm switch {
        "aesgcm" => new AesGcmEncryptionService(keyStore),
        "chacha" => new ChaCha20Poly1305EncryptionService(keyStore),
        _ => throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, null)
    };

    private static async Task<byte[]> EncryptAsync(IEncryptionService svc, byte[] plaintext, byte[]? key, string? keyId, int chunkSize, CancellationToken ct)
    {
        using var input = new MemoryStream(plaintext);
        using var output = new MemoryStream();
        await svc.EncryptToStreamAsync(input, output, keyId, key, chunkSize, ct);
        return output.ToArray();
    }

    private static async Task<byte[]> DecryptAsync(IEncryptionService svc, byte[] encrypted, byte[]? key, string? keyId, CancellationToken ct)
    {
        using var input = new MemoryStream(encrypted);
        using var output = new MemoryStream();
        await svc.DecryptToStreamAsync(input, output, keyId, key, ct);
        return output.ToArray();
    }

    // Sizes chosen relative to chunkSize 16: empty, sub-chunk, exact single chunk, exact multi-chunk boundary, multi-chunk with remainder.
    public static TheoryData<string, int> ServiceAndSize()
    {
        var data = new TheoryData<string, int>();
        foreach (var algorithm in new[] { "aesgcm", "chacha" })
            foreach (var size in new[] { 0, 1, 7, 16, 32, 48, 50, 1000 })
                data.Add(algorithm, size);

        return data;
    }

    [Theory]
    [MemberData(nameof(ServiceAndSize))]
    public async Task Stream_Roundtrip_WithRawKey_AcrossChunkBoundaries(string algorithm, int size)
    {
        var ct = TestContext.Current.CancellationToken;
        var svc = CreateService(algorithm, new LocalKeyStore());
        var key = DeriveKey("raw-key");
        var plaintext = RandomNumberGenerator.GetBytes(size);

        var encrypted = await EncryptAsync(svc, plaintext, key, null, 16, ct);
        var decrypted = await DecryptAsync(svc, encrypted, key, null, ct);

        Assert.Equal(plaintext, decrypted);
    }

    [Theory]
    [MemberData(nameof(ServiceAndSize))]
    public async Task Stream_Roundtrip_WithKeyId_AcrossChunkBoundaries(string algorithm, int size)
    {
        var ct = TestContext.Current.CancellationToken;
        const string keyId = "stream-key";
        var keyStore = new LocalKeyStore();
        keyStore.UpdateKeyFromString(keyId, "stream-password");
        var svc = CreateService(algorithm, keyStore);
        var plaintext = RandomNumberGenerator.GetBytes(size);

        var encrypted = await EncryptAsync(svc, plaintext, null, keyId, 16, ct);
        var decrypted = await DecryptAsync(svc, encrypted, null, keyId, ct);

        Assert.Equal(plaintext, decrypted);
    }

    [Theory]
    [InlineData("aesgcm")]
    [InlineData("chacha")]
    public async Task Stream_Decrypt_WithFreshServiceInstance_Succeeds(string algorithm)
    {
        var ct = TestContext.Current.CancellationToken;
        const string keyId = "shared-key";
        var keyStore = new LocalKeyStore();
        keyStore.UpdateKeyFromString(keyId, "shared-password");
        var encryptService = CreateService(algorithm, keyStore);
        var decryptService = CreateService(algorithm, keyStore);
        var plaintext = RandomNumberGenerator.GetBytes(5000);

        var encrypted = await EncryptAsync(encryptService, plaintext, null, keyId, 256, ct);
        var decrypted = await DecryptAsync(decryptService, encrypted, null, keyId, ct);

        Assert.Equal(plaintext, decrypted);
    }

    [Fact]
    public async Task Stream_Decrypt_WithMismatchedAlgorithm_Throws()
    {
        var ct = TestContext.Current.CancellationToken;
        const string keyId = "algo-key";
        var keyStore = new LocalKeyStore();
        keyStore.UpdateKeyFromString(keyId, "algo-password");
        var aesGcm = new AesGcmEncryptionService(keyStore);
        var chacha = new ChaCha20Poly1305EncryptionService(keyStore);
        var plaintext = RandomNumberGenerator.GetBytes(200);

        var encrypted = await EncryptAsync(aesGcm, plaintext, null, keyId, 64, ct);

        await Assert.ThrowsAnyAsync<InvalidDataException>(() => DecryptAsync(chacha, encrypted, null, keyId, ct));
    }

    [Theory]
    [InlineData("aesgcm")]
    [InlineData("chacha")]
    public async Task Stream_Decrypt_TamperedCiphertext_ThrowsDecryptionFailed(string algorithm)
    {
        var ct = TestContext.Current.CancellationToken;
        var svc = CreateService(algorithm, new LocalKeyStore());
        var key = DeriveKey("tamper-key");
        var plaintext = RandomNumberGenerator.GetBytes(200);

        var encrypted = await EncryptAsync(svc, plaintext, key, null, 64, ct);

        // Flip a bit in the final tag byte (well past the header / length prefix) so it surfaces as an auth failure.
        encrypted[^1] ^= 0xFF;

        await Assert.ThrowsAnyAsync<DecryptionFailedException>(() => DecryptAsync(svc, encrypted, key, null, ct));
    }

    [Theory]
    [InlineData("aesgcm")]
    [InlineData("chacha")]
    public async Task Stream_PerChunkNonces_AreUnique(string algorithm)
    {
        var ct = TestContext.Current.CancellationToken;
        var svc = CreateService(algorithm, new LocalKeyStore());
        var key = DeriveKey("nonce-key");
        var plaintext = RandomNumberGenerator.GetBytes(16 * 64); // 64 chunks at chunkSize 16

        var encrypted = await EncryptAsync(svc, plaintext, key, null, 16, ct);

        var nonces = ExtractChunkNonces(encrypted);
        Assert.Equal(64, nonces.Count);
        var distinct = new HashSet<string>(nonces.Select(Convert.ToHexString));
        Assert.Equal(nonces.Count, distinct.Count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    [InlineData(64)]
    [InlineData(500)]
    public async Task TwoKey_Stream_Roundtrip_AcrossChunkBoundaries(int size)
    {
        var ct = TestContext.Current.CancellationToken;
        const string keyId = "twokey-stream";
        var keyStore = new LocalKeyStore();
        keyStore.UpdateKeyFromString(keyId, "twokey-password");
        var aesGcm = new AesGcmEncryptionService(keyStore);
        using var svc = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(aesGcm, keyStore);
        var plaintext = RandomNumberGenerator.GetBytes(size);

        byte[] encrypted;
        using (var input = new MemoryStream(plaintext))
        using (var output = new MemoryStream()) {
            await svc.EncryptToStreamAsync(input, output, keyId, chunkSize: 16, ct: ct);
            encrypted = output.ToArray();
        }

        byte[] decrypted;
        using (var input = new MemoryStream(encrypted))
        using (var output = new MemoryStream()) {
            await svc.DecryptToStreamAsync(input, output, keyId, ct: ct);
            decrypted = output.ToArray();
        }

        Assert.Equal(plaintext, decrypted);
    }

    [Fact]
    public async Task TwoKey_Stream_TamperedCiphertext_ThrowsDecryptionFailed()
    {
        var ct = TestContext.Current.CancellationToken;
        const string keyId = "twokey-tamper";
        var keyStore = new LocalKeyStore();
        keyStore.UpdateKeyFromString(keyId, "twokey-password");
        var aesGcm = new AesGcmEncryptionService(keyStore);
        using var svc = new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(aesGcm, keyStore);
        var plaintext = RandomNumberGenerator.GetBytes(300);

        byte[] encrypted;
        using (var input = new MemoryStream(plaintext))
        using (var output = new MemoryStream()) {
            await svc.EncryptToStreamAsync(input, output, keyId, chunkSize: 64, ct: ct);
            encrypted = output.ToArray();
        }

        encrypted[^1] ^= 0xFF;

        using var tamperedInput = new MemoryStream(encrypted);
        using var tamperedOutput = new MemoryStream();
        await Assert.ThrowsAnyAsync<DecryptionFailedException>(() => svc.DecryptToStreamAsync(tamperedInput, tamperedOutput, keyId, ct: ct));
    }

    /// <summary>Parses the base/plain stream format header then walks each compact chunk frame, returning the nonce from each chunk.</summary>
    private static List<byte[]> ExtractChunkNonces(byte[] encrypted)
    {
        using var ms = new MemoryStream(encrypted);
        using var br = new BinaryReader(ms);
        br.ReadByte(); // format version
        br.ReadByte(); // algorithm id
        var keyIdLength = br.ReadInt32();
        br.ReadBytes(keyIdLength);
        var keyVersionLength = br.ReadInt32();
        br.ReadBytes(keyVersionLength);

        var nonces = new List<byte[]>();
        while (ms.Position < ms.Length) {
            var ciphertextLength = br.ReadInt32();
            nonces.Add(br.ReadBytes(NonceSize));
            br.ReadBytes(ciphertextLength + TagSize);
        }

        return nonces;
    }
}
