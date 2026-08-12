using Lyo.Compression;
using Lyo.Compression.Compressors;
using Lyo.Compression.Models;
using Lyo.Encryption;
using Lyo.Encryption.AesGcm;
using Lyo.Encryption.TwoKey;
using Lyo.KeyStore;

namespace Lyo.FileStorage.Benchmarks;

/// <summary>Shared helpers for FileStorage benchmark suites (compression, two-key encryption, key material).</summary>
internal static class FileStorageBenchmarkSupport
{
    internal const string KeyId = "benchmark-key";
    private const string KeyMaterial = "benchmark-test-key-32-bytes-long!";

    internal static LocalKeyStore CreateKeyStore()
    {
        var keyStore = new LocalKeyStore();
        keyStore.UpdateKeyFromString(KeyId, KeyMaterial);
        return keyStore;
    }

    internal static CompressionService CreateCompressionService()
        => new([new GZipCompressorFactory(), new DeflateCompressorFactory()], options: new() { DefaultAlgorithm = CompressionAlgorithm.GZip, EnableMetrics = false });

    internal static ITwoKeyEncryptionService CreateTwoKeyEncryptionService(LocalKeyStore keyStore)
    {
        var aesGcm = new AesGcmEncryptionService(keyStore);
        return new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(aesGcm, keyStore);
    }
}