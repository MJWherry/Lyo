using System.Collections.Concurrent;
using System.Security.Cryptography;
using Lyo.Encryption.AesGcm;
using Lyo.Encryption.ChaCha20Poly1305;
using Lyo.Keystore;

namespace Lyo.Encryption.Tests;

/// <summary>
/// Verifies that encrypt is thread-safe after switching to stateless random_base nonces: many concurrent single-shot and streaming encrypts on a single shared KeyStore key
/// must never reuse a nonce and must always round-trip. Covers both a shared singleton service instance and transient instances sharing the KeyStore (the scenario the old, racy
/// KeyStore-backed nonce counter could corrupt).
/// </summary>
public class EncryptionConcurrencyTests
{
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private const int Concurrency = 256;

    private static IEncryptionService CreateService(string algorithm, IKeyStore keyStore)
        => algorithm switch {
            "aesgcm" => new AesGcmEncryptionService(keyStore),
            "chacha" => new ChaCha20Poly1305EncryptionService(keyStore),
            var _ => throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, null)
        };

    [Theory]
    [InlineData("aesgcm")]
    [InlineData("chacha")]
    public async Task SingleShot_ParallelEncrypts_SingletonService_ProduceUniqueNoncesAndRoundtrip(string algorithm)
    {
        var ct = TestContext.Current.CancellationToken;
        const string keyId = "concurrent-single";
        var keyStore = new LocalKeyStore();
        keyStore.UpdateKeyFromString(keyId, "concurrent-single-password");
        var svc = CreateService(algorithm, keyStore);
        var plaintexts = Enumerable.Range(0, Concurrency).Select(_ => RandomNumberGenerator.GetBytes(64)).ToArray();
        var encrypted = new byte[Concurrency][];
        await Parallel.ForEachAsync(
            Enumerable.Range(0, Concurrency), ct, (i, _) => {
                encrypted[i] = svc.Encrypt(plaintexts[i], keyId);
                return ValueTask.CompletedTask;
            });

        AssertAllUnique(encrypted.Select(ExtractSingleShotNonce));
        for (var i = 0; i < Concurrency; i++)
            Assert.Equal(plaintexts[i], svc.Decrypt(encrypted[i], keyId));
    }

    [Theory]
    [InlineData("aesgcm")]
    [InlineData("chacha")]
    public async Task SingleShot_ParallelEncrypts_TransientServices_ProduceUniqueNoncesAndRoundtrip(string algorithm)
    {
        var ct = TestContext.Current.CancellationToken;
        const string keyId = "concurrent-transient";
        var keyStore = new LocalKeyStore();
        keyStore.UpdateKeyFromString(keyId, "concurrent-transient-password");
        var plaintexts = Enumerable.Range(0, Concurrency).Select(_ => RandomNumberGenerator.GetBytes(64)).ToArray();
        var encrypted = new byte[Concurrency][];

        // Each task builds its own service instance over the shared KeyStore.
        await Parallel.ForEachAsync(
            Enumerable.Range(0, Concurrency), ct, (i, _) => {
                encrypted[i] = CreateService(algorithm, keyStore).Encrypt(plaintexts[i], keyId);
                return ValueTask.CompletedTask;
            });

        AssertAllUnique(encrypted.Select(ExtractSingleShotNonce));
        var decryptService = CreateService(algorithm, keyStore);
        for (var i = 0; i < Concurrency; i++)
            Assert.Equal(plaintexts[i], decryptService.Decrypt(encrypted[i], keyId));
    }

    [Theory]
    [InlineData("aesgcm")]
    [InlineData("chacha")]
    public async Task Streaming_ParallelEncrypts_SingletonService_ProduceGloballyUniqueNoncesAndRoundtrip(string algorithm)
    {
        var ct = TestContext.Current.CancellationToken;
        const string keyId = "concurrent-stream";
        var keyStore = new LocalKeyStore();
        keyStore.UpdateKeyFromString(keyId, "concurrent-stream-password");
        var svc = CreateService(algorithm, keyStore);

        // Each payload spans many chunks (chunkSize 16) so every stream emits a counter sequence under its own random prefix.
        var plaintexts = Enumerable.Range(0, Concurrency).Select(_ => RandomNumberGenerator.GetBytes(16 * 8)).ToArray();
        var encrypted = new byte[Concurrency][];
        await Parallel.ForEachAsync(
            Enumerable.Range(0, Concurrency), ct, async (i, token) => {
                using var input = new MemoryStream(plaintexts[i]);
                using var output = new MemoryStream();
                await svc.EncryptToStreamAsync(input, output, keyId, null, 16, token);
                encrypted[i] = output.ToArray();
            });

        // Nonces must be unique both within each stream (counter) and across all concurrent streams (random prefix).
        AssertAllUnique(encrypted.SelectMany(ExtractStreamNonces));
        for (var i = 0; i < Concurrency; i++) {
            using var input = new MemoryStream(encrypted[i]);
            using var output = new MemoryStream();
            await svc.DecryptToStreamAsync(input, output, keyId, ct: ct);
            Assert.Equal(plaintexts[i], output.ToArray());
        }
    }

    [Theory]
    [InlineData("aesgcm")]
    [InlineData("chacha")]
    public async Task Mixed_ParallelSingleShotAndStreaming_ProduceGloballyUniqueNonces(string algorithm)
    {
        var ct = TestContext.Current.CancellationToken;
        const string keyId = "concurrent-mixed";
        var keyStore = new LocalKeyStore();
        keyStore.UpdateKeyFromString(keyId, "concurrent-mixed-password");
        var svc = CreateService(algorithm, keyStore);
        var nonces = new ConcurrentBag<byte[]>();
        await Parallel.ForEachAsync(
            Enumerable.Range(0, Concurrency), ct, async (i, token) => {
                if (i % 2 == 0) {
                    var plaintext = RandomNumberGenerator.GetBytes(64);
                    var encrypted = svc.Encrypt(plaintext, keyId);
                    nonces.Add(ExtractSingleShotNonce(encrypted));
                    Assert.Equal(plaintext, svc.Decrypt(encrypted, keyId));
                }
                else {
                    var plaintext = RandomNumberGenerator.GetBytes(16 * 8);
                    using var input = new MemoryStream(plaintext);
                    using var output = new MemoryStream();
                    await svc.EncryptToStreamAsync(input, output, keyId, null, 16, token);
                    foreach (var nonce in ExtractStreamNonces(output.ToArray()))
                        nonces.Add(nonce);
                }
            });

        AssertAllUnique(nonces);
    }

    private static void AssertAllUnique(IEnumerable<byte[]> nonces)
    {
        var list = nonces.ToList();
        var distinct = new HashSet<string>(list.Select(Convert.ToHexString));
        Assert.Equal(list.Count, distinct.Count);
    }

    /// <summary>Parses the single-shot format header (<c>[version][keyIdLen:4][keyId][keyVersion:string][nonceLen:4][nonce]...</c>) and returns the nonce.</summary>
    private static byte[] ExtractSingleShotNonce(byte[] encrypted)
    {
        using var ms = new MemoryStream(encrypted);
        using var br = new BinaryReader(ms);
        br.ReadByte(); // format version
        var keyIdLength = br.ReadInt32();
        br.ReadBytes(keyIdLength);
        br.ReadString(); // key version (length-prefixed BinaryWriter string)
        var nonceLength = br.ReadInt32();
        return br.ReadBytes(nonceLength);
    }

    /// <summary>Parses the streaming format header then walks each compact chunk frame, returning the nonce from each chunk.</summary>
    private static List<byte[]> ExtractStreamNonces(byte[] encrypted)
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