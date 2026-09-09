using Lyo.Encryption.AesCcm;
using Lyo.Encryption.AesGcm;
using Lyo.Encryption.AesSiv;
using Lyo.Encryption.ChaCha20Poly1305;
using Lyo.Encryption.Exceptions;
using Lyo.Encryption.TwoKey;
using Lyo.Encryption.XChaCha20Poly1305;
using Lyo.KeyStore;
using Lyo.KeyStore.KeyDerivation;
using Lyo.Testing;

namespace Lyo.Encryption.Tests;

/// <summary>
/// Covers the compact stream chunk layout (<c>[lengthAndFinalFlag:4][ciphertext][tag:16]</c>, nonces derived from the header prefix) used by the
/// low-allocation streaming AEAD path: round-trips across chunk edges, tamper detection, unique per-chunk nonces, and decrypt across instances/algorithms.
/// </summary>
public class StreamingChunkFormatTests
{
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private static readonly IKeyDerivationService KeyDerivationService = new Pbkdf2KeyDerivationService();

    private static byte[] DeriveKey(string password) => KeyDerivationService.DeriveKey(password);

    private static IEncryptionService CreateService(string algorithm, IKeyStore keyStore)
        => algorithm switch {
            "aesgcm" => new AesGcmEncryptionService(keyStore),
            "chacha" => new ChaCha20Poly1305EncryptionService(keyStore),
            "aesccm" => new AesCcmEncryptionService(keyStore),
            "aessiv" => new AesSivEncryptionService(keyStore),
            "xchacha" => new XChaCha20Poly1305EncryptionService(keyStore),
            var _ => throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, null)
        };

    private static async Task<byte[]> EncryptAsync(IEncryptionService svc, byte[] plaintext, byte[]? key, string? keyId, int chunkSize, CancellationToken ct)
    {
        using var input = new MemoryStream(plaintext);
        using var output = new MemoryStream();
        await svc.EncryptToStreamAsync(input, output, keyId, key, chunkSize, ct: ct);
        return output.ToArray();
    }

    private static async Task<byte[]> DecryptAsync(IEncryptionService svc, byte[] encrypted, byte[]? key, string? keyId, CancellationToken ct)
    {
        using var input = new MemoryStream(encrypted);
        using var output = new MemoryStream();
        await svc.DecryptToStreamAsync(input, output, keyId, key, ct: ct);
        return output.ToArray();
    }

    // lengths relative to chunkSize 16: empty, shorter than a chunk, exact one chunk, exact multi-chunk edge, multi-chunk plus remainder.
    public static TheoryData<string, int> ServiceAndSize()
    {
        var data = new TheoryData<string, int>();
        foreach (var algorithm in new[] { "aesgcm", "chacha", "aesccm", "aessiv", "xchacha" })
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
        var plaintext = TestData.Create(size);
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
        var plaintext = TestData.Create(size);
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
        var plaintext = TestData.Create(5000);
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
        var plaintext = TestData.Create(200);
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
        var plaintext = TestData.Create(200);
        var encrypted = await EncryptAsync(svc, plaintext, key, null, 64, ct);

        // invert a bit in the last tag byte (after header and length prefix) so auth must fail.
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

        // 64 full chunks of size 16; lookahead marks the last data chunk final (no empty trailer on exact multiples).
        var plaintext = TestData.Create(16 * 64);
        var encrypted = await EncryptAsync(svc, plaintext, key, null, 16, ct);
        var nonces = ExtractChunkNonces(encrypted, out var finalFlagCount);
        Assert.Equal(64, nonces.Count);
        Assert.Equal(1, finalFlagCount);
        var distinct = new HashSet<string>(nonces.Select(Convert.ToHexString));
        Assert.Equal(nonces.Count, distinct.Count);

        // a second stream must pick a different random nonce prefix so nonces stay unique across streams.
        var encryptedAgain = await EncryptAsync(svc, plaintext, key, null, 16, ct);
        var noncesAgain = ExtractChunkNonces(encryptedAgain, out var _);
        distinct.UnionWith(noncesAgain.Select(Convert.ToHexString));
        Assert.Equal(nonces.Count + noncesAgain.Count, distinct.Count);
    }

    [Theory]
    [InlineData("aesgcm")]
    [InlineData("chacha")]
    public async Task Stream_Decrypt_ReorderedChunks_Throws(string algorithm)
    {
        var ct = TestContext.Current.CancellationToken;
        var svc = CreateService(algorithm, new LocalKeyStore());
        var key = DeriveKey("reorder-key");

        // 3 chunks of size 16; swap the first two frames. Nonces come from a local counter, so chunk 1 ciphertext under counter 0 must fail auth.
        var encrypted = await EncryptAsync(svc, TestData.Create(48), key, null, 16, ct);
        var frames = ParseChunkFrames(encrypted, out var headerLength);
        Assert.True(frames.Count >= 3);
        var tampered = ReassembleStream(encrypted, headerLength, [frames[1], frames[0], .. frames.Skip(2)]);
        await Assert.ThrowsAnyAsync<DecryptionFailedException>(() => DecryptAsync(svc, tampered, key, null, ct));
    }

    [Theory]
    [InlineData("aesgcm")]
    [InlineData("chacha")]
    public async Task Stream_Decrypt_DuplicatedChunk_Throws(string algorithm)
    {
        var ct = TestContext.Current.CancellationToken;
        var svc = CreateService(algorithm, new LocalKeyStore());
        var key = DeriveKey("replay-key");

        // replay frame 0 as frame 1: the copy is checked against counter 1 and must fail.
        var encrypted = await EncryptAsync(svc, TestData.Create(48), key, null, 16, ct);
        var frames = ParseChunkFrames(encrypted, out var headerLength);
        Assert.True(frames.Count >= 3);
        var tampered = ReassembleStream(encrypted, headerLength, [frames[0], frames[0], .. frames.Skip(2)]);
        await Assert.ThrowsAnyAsync<DecryptionFailedException>(() => DecryptAsync(svc, tampered, key, null, ct));
    }

    [Theory]
    [InlineData("aesgcm")]
    [InlineData("chacha")]
    public async Task Stream_Decrypt_TruncatedAtChunkBoundary_Throws(string algorithm)
    {
        var ct = TestContext.Current.CancellationToken;
        var svc = CreateService(algorithm, new LocalKeyStore());
        var key = DeriveKey("truncate-key");

        // drop the final-flagged frame. Remaining frames still auth, so only the missing final marker exposes truncation.
        var encrypted = await EncryptAsync(svc, TestData.Create(48), key, null, 16, ct);
        var frames = ParseChunkFrames(encrypted, out var headerLength);
        var truncated = ReassembleStream(encrypted, headerLength, frames.Take(frames.Count - 1).ToList());
        var ex = await Assert.ThrowsAnyAsync<InvalidDataException>(() => DecryptAsync(svc, truncated, key, null, ct));
        Assert.Contains("truncated", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("aesgcm")]
    [InlineData("chacha")]
    public async Task Stream_Decrypt_DataAfterFinalChunk_Throws(string algorithm)
    {
        var ct = TestContext.Current.CancellationToken;
        var svc = CreateService(algorithm, new LocalKeyStore());
        var key = DeriveKey("extend-key");
        var encrypted = await EncryptAsync(svc, TestData.Create(48), key, null, 16, ct);
        var frames = ParseChunkFrames(encrypted, out var headerLength);
        var extended = ReassembleStream(encrypted, headerLength, [.. frames, frames[0]]);
        var ex = await Assert.ThrowsAnyAsync<InvalidDataException>(() => DecryptAsync(svc, extended, key, null, ct));
        Assert.Contains("after the final chunk", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("aesgcm")]
    [InlineData("chacha")]
    public async Task Stream_Decrypt_TamperedHeaderNoncePrefix_Throws(string algorithm)
    {
        var ct = TestContext.Current.CancellationToken;
        var svc = CreateService(algorithm, new LocalKeyStore());
        var key = DeriveKey("header-tamper-key");
        var encrypted = await EncryptAsync(svc, TestData.Create(48), key, null, 16, ct);

        // invert a bit in the header nonce prefix: derived chunk nonces and header-as-AAD change, so every chunk fails auth.
        ParseChunkFrames(encrypted, out var headerLength);
        encrypted[headerLength - 1] ^= 0x01;
        await Assert.ThrowsAnyAsync<DecryptionFailedException>(() => DecryptAsync(svc, encrypted, key, null, ct));
    }

    [Theory]
    [InlineData("aesgcm")]
    [InlineData("chacha")]
    [InlineData("aesccm")]
    [InlineData("aessiv")]
    [InlineData("xchacha")]
    public async Task Stream_AssociatedData_RoundtripsAndBindsToStream(string algorithm)
    {
        var ct = TestContext.Current.CancellationToken;
        var svc = CreateService(algorithm, new LocalKeyStore());
        var key = DeriveKey("stream-aad-key");
        var plaintext = TestData.Create(100);
        var aad = "tenant-42/file-7"u8.ToArray();
        byte[] encrypted;
        using (var input = new MemoryStream(plaintext)) {
            using (var output = new MemoryStream()) {
                await svc.EncryptToStreamAsync(input, output, null, key, 32, aad, ct);
                encrypted = output.ToArray();
            }
        }

        // matching AAD decrypts; a different AAD (or none) must fail chunk auth.
        using (var input = new MemoryStream(encrypted)) {
            using (var output = new MemoryStream()) {
                await svc.DecryptToStreamAsync(input, output, null, key, aad, ct);
                Assert.Equal(plaintext, output.ToArray());
            }
        }

        using (var wrongInput = new MemoryStream(encrypted)) {
            using (var wrongOutput = new MemoryStream())
                await Assert.ThrowsAnyAsync<DecryptionFailedException>(() => svc.DecryptToStreamAsync(wrongInput, wrongOutput, null, key, "tenant-43/file-7"u8.ToArray(), ct));
        }

        using (var missingInput = new MemoryStream(encrypted)) {
            using (var missingOutput = new MemoryStream())
                await Assert.ThrowsAnyAsync<DecryptionFailedException>(() => svc.DecryptToStreamAsync(missingInput, missingOutput, null, key, ct: ct));
        }
    }

    [Theory]
    [InlineData("aesgcm")]
    [InlineData("chacha")]
    [InlineData("aesccm")]
    [InlineData("aessiv")]
    [InlineData("xchacha")]
    public void SingleShot_AssociatedData_RoundtripsAndBindsToCiphertext(string algorithm)
    {
        var svc = CreateService(algorithm, new LocalKeyStore());
        var key = DeriveKey("single-shot-aad-key");
        var plaintext = TestData.Create(64);
        var aad = "context-binding"u8.ToArray();
        var encrypted = svc.Encrypt(plaintext, null, key, aad);
        Assert.Equal(plaintext, svc.Decrypt(encrypted, null, key, aad));
        Assert.ThrowsAny<DecryptionFailedException>(() => svc.Decrypt(encrypted, null, key, "different-context"u8.ToArray()));
        Assert.ThrowsAny<DecryptionFailedException>(() => svc.Decrypt(encrypted, null, key));
    }

    // AES-SIV is deterministic: counter-only nonce (no random prefix) plus the same key, plaintext, and chunk size yields identical stream bytes.
    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    [InlineData(64)]
    [InlineData(500)]
    public async Task AesSiv_Stream_IsDeterministic_ForSameKeyAndChunkSize(int size)
    {
        var ct = TestContext.Current.CancellationToken;
        var key = DeriveKey("siv-determinism-key");
        var plaintext = TestData.Create(size);
        var first = await EncryptAsync(new AesSivEncryptionService(new LocalKeyStore()), plaintext, key, null, 16, ct);
        var second = await EncryptAsync(new AesSivEncryptionService(new LocalKeyStore()), plaintext, key, null, 16, ct);
        Assert.Equal(first, second);
        Assert.Equal(plaintext, await DecryptAsync(new AesSivEncryptionService(new LocalKeyStore()), first, key, null, ct));
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
        var plaintext = TestData.Create(size);
        byte[] encrypted;
        using (var input = new MemoryStream(plaintext)) {
            using (var output = new MemoryStream()) {
                await svc.EncryptToStreamAsync(input, output, keyId, chunkSize: 16, ct: ct);
                encrypted = output.ToArray();
            }
        }

        byte[] decrypted;
        using (var input = new MemoryStream(encrypted)) {
            using (var output = new MemoryStream()) {
                await svc.DecryptToStreamAsync(input, output, keyId, ct: ct);
                decrypted = output.ToArray();
            }
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
        var plaintext = TestData.Create(300);
        byte[] encrypted;
        using (var input = new MemoryStream(plaintext)) {
            using (var output = new MemoryStream()) {
                await svc.EncryptToStreamAsync(input, output, keyId, chunkSize: 64, ct: ct);
                encrypted = output.ToArray();
            }
        }

        encrypted[^1] ^= 0xFF;
        using var tamperedInput = new MemoryStream(encrypted);
        using var tamperedOutput = new MemoryStream();
        await Assert.ThrowsAnyAsync<DecryptionFailedException>(() => svc.DecryptToStreamAsync(tamperedInput, tamperedOutput, keyId, ct: ct));
    }

    /// <summary>
    /// Breaks a ciphertext stream into raw frames (<c>[lengthAndFinalFlag:4][ciphertext][tag:16]</c>) so tests can reorder, replay, or drop whole frames.
    /// Expects a 12-byte-nonce algorithm and a raw-key stream (empty keyId/keyVersion in the header).
    /// </summary>
    private static List<byte[]> ParseChunkFrames(byte[] encrypted, out int headerLength)
    {
        const int counterSize = 4;
        const uint finalChunkFlag = 0x8000_0000;
        using var ms = new MemoryStream(encrypted);
        using var br = new BinaryReader(ms);
        br.ReadByte(); // format version
        br.ReadByte(); // algorithm id
        br.ReadBytes(br.ReadInt32()); // keyId
        br.ReadBytes(br.ReadInt32()); // keyVersion
        br.ReadBytes(NonceSize - counterSize); // nonce prefix
        headerLength = (int)ms.Position;
        var frames = new List<byte[]>();
        while (ms.Position < ms.Length) {
            var frameStart = ms.Position;
            var lengthAndFlag = br.ReadUInt32();
            var ciphertextLength = (int)(lengthAndFlag & ~finalChunkFlag);
            br.ReadBytes(ciphertextLength + TagSize);
            frames.Add(encrypted.AsSpan((int)frameStart, (int)(ms.Position - frameStart)).ToArray());
        }

        return frames;
    }

    /// <summary>Rebuilds a stream from the original header plus any sequence of chunk frames.</summary>
    private static byte[] ReassembleStream(byte[] original, int headerLength, IReadOnlyList<byte[]> frames)
    {
        using var ms = new MemoryStream();
        ms.Write(original, 0, headerLength);
        foreach (var frame in frames)
            ms.Write(frame, 0, frame.Length);

        return ms.ToArray();
    }

    /// <summary>
    /// Reads the stream header (per-stream nonce prefix) then walks compact frames (<c>[lengthAndFinalFlag:4][ciphertext][tag]</c>), deriving
    /// each chunk nonce as <c>prefix || counter</c> the same way the codec does — the wire stores no nonces.
    /// </summary>
    private static List<byte[]> ExtractChunkNonces(byte[] encrypted, out int finalFlagCount)
    {
        const int counterSize = 4;
        const uint finalChunkFlag = 0x8000_0000;
        using var ms = new MemoryStream(encrypted);
        using var br = new BinaryReader(ms);
        br.ReadByte(); // format version
        br.ReadByte(); // algorithm id
        var keyIdLength = br.ReadInt32();
        br.ReadBytes(keyIdLength);
        var keyVersionLength = br.ReadInt32();
        br.ReadBytes(keyVersionLength);
        var noncePrefix = br.ReadBytes(NonceSize - counterSize);
        var nonces = new List<byte[]>();
        var chunkIndex = 0u;
        finalFlagCount = 0;
        while (ms.Position < ms.Length) {
            var lengthAndFlag = br.ReadUInt32();
            if ((lengthAndFlag & finalChunkFlag) != 0)
                finalFlagCount++;

            var ciphertextLength = (int)(lengthAndFlag & ~finalChunkFlag);
            br.ReadBytes(ciphertextLength + TagSize);
            var nonce = new byte[NonceSize];
            noncePrefix.CopyTo(nonce, 0);
            BitConverter.GetBytes(chunkIndex).CopyTo(nonce, NonceSize - counterSize);
            nonces.Add(nonce);
            chunkIndex++;
        }

        return nonces;
    }
}