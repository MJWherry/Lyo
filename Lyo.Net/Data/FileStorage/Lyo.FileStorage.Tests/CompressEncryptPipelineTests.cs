using System.Security.Cryptography;
using Lyo.Compression;
using Lyo.Compression.Compressors;
using Lyo.Encryption.AesGcm;
using Lyo.Keystore;

namespace Lyo.FileStorage.Tests;

/// <summary>
/// Exercises <see cref="CompressEncryptPipeline" />: roundtrips, and — critically — that a failure in the pipe-<em>reading</em> stage (decompression bomb guard, invalid key)
/// propagates as an exception instead of deadlocking the writing stage on pipe backpressure.
/// </summary>
public sealed class CompressEncryptPipelineTests
{
    private static readonly TimeSpan NoHangTimeout = TimeSpan.FromSeconds(30);

    private static CompressionService CreateCompressionService(long? maxInputSize = null)
        => new(
            [new GZipCompressorFactory(), new DeflateCompressorFactory(), new BrotliCompressorFactory(), new ZLibCompressorFactory()],
            options: maxInputSize == null ? new() : new() { MaxInputSize = maxInputSize.Value });

    private static AesGcmEncryptionService CreateEncryptionService() => new(new LocalKeyStore());

    [Fact]
    public async Task Roundtrip_CompressEncrypt_DecryptDecompress_RestoresPlaintext()
    {
        var ct = TestContext.Current.CancellationToken;
        var compression = CreateCompressionService();
        var encryption = CreateEncryptionService();
        var key = RandomNumberGenerator.GetBytes(32);
        var plaintext = new byte[300_000];
        RandomNumberGenerator.Fill(plaintext.AsSpan(0, 1000)); // mostly zeros so compression actually shrinks it

        using var encrypted = new MemoryStream();
        using (var input = new MemoryStream(plaintext))
            await CompressEncryptPipeline.CompressThenEncryptAsync(input, encrypted, compression, encryption, key: key, ct: ct);

        Assert.True(encrypted.Length < plaintext.Length);
        encrypted.Position = 0;
        using var restored = new MemoryStream();
        await CompressEncryptPipeline.DecryptThenDecompressAsync(encrypted, restored, compression, encryption, key: key, ct: ct);
        Assert.Equal(plaintext, restored.ToArray());
    }

    [Fact]
    public async Task DecryptThenDecompress_BombGuardTrips_Throws_InsteadOfDeadlocking()
    {
        var ct = TestContext.Current.CancellationToken;
        var encryption = CreateEncryptionService();
        var key = RandomNumberGenerator.GetBytes(32);

        // Incompressible plaintext (~1 MB compressed) so that when the bomb guard trips, the decrypt stage still has far
        // more than the pipe's ~64 KB pause threshold left to write — the exact condition that used to deadlock.
        var plaintext = RandomNumberGenerator.GetBytes(1024 * 1024);
        using var encrypted = new MemoryStream();
        using (var input = new MemoryStream(plaintext))
            await CompressEncryptPipeline.CompressThenEncryptAsync(input, encrypted, CreateCompressionService(), encryption, key: key, ct: ct);

        // Decompress side is capped at 1 KB (the options minimum), so the guard fires almost immediately.
        var bombGuarded = CreateCompressionService(maxInputSize: 1024);
        encrypted.Position = 0;
        using var output = new MemoryStream();
        await Assert.ThrowsAsync<InvalidDataException>(
            () => CompressEncryptPipeline.DecryptThenDecompressAsync(encrypted, output, bombGuarded, encryption, key: key, ct: ct).WaitAsync(NoHangTimeout, ct));
    }

    [Fact]
    public async Task CompressThenEncrypt_EncryptStageFails_Throws_InsteadOfDeadlocking()
    {
        var ct = TestContext.Current.CancellationToken;

        // Invalid AES key: the encrypt stage faults before consuming the pipe, while the compress stage wants to write ~1 MB.
        var invalidKey = new byte[5];
        var plaintext = RandomNumberGenerator.GetBytes(1024 * 1024);
        using var input = new MemoryStream(plaintext);
        using var output = new MemoryStream();
        var ex = await Assert.ThrowsAnyAsync<Exception>(
            () => CompressEncryptPipeline.CompressThenEncryptAsync(input, output, CreateCompressionService(), CreateEncryptionService(), key: invalidKey, ct: ct)
                .WaitAsync(NoHangTimeout, ct));

        Assert.IsNotType<TimeoutException>(ex);
    }
}
