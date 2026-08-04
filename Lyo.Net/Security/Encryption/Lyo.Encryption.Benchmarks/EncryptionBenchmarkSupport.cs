using System.Security.Cryptography;
using Lyo.IO.Temp.Models;
using Lyo.Keystore;

namespace Lyo.Encryption.Benchmarks;

internal static class EncryptionBenchmarkSupport
{
    internal const string KeyId = "benchmark-key";
    internal const string KeyMaterial = "benchmark-test-key-32-bytes-long!";

    internal static LocalKeyStore CreateKeyStore()
    {
        var keyStore = new LocalKeyStore();
        keyStore.UpdateKeyFromString(KeyId, KeyMaterial);
        return keyStore;
    }

    /// <summary>32-byte key material for benchmarks that pass an explicit key (e.g. XChaCha20-Poly1305).</summary>
    internal static byte[] GetSymmetricKey(LocalKeyStore keyStore)
        => keyStore.GetCurrentKey(KeyId) ?? throw new InvalidOperationException($"Benchmark key store missing key ID {KeyId}.");

    /// <summary>Writes RSA PEM key pair files under <paramref name="temp" /> and returns their paths.</summary>
    internal static (string PublicPath, string PrivatePath) CreateRsaPemFiles(IIOTempSession temp)
    {
        using var rsa = RSA.Create(2048);
        var pubPem = "-----BEGIN PUBLIC KEY-----\n" + Convert.ToBase64String(rsa.ExportSubjectPublicKeyInfo()) + "\n-----END PUBLIC KEY-----";
        var privPem = "-----BEGIN PRIVATE KEY-----\n" + Convert.ToBase64String(rsa.ExportPkcs8PrivateKey()) + "\n-----END PRIVATE KEY-----";
        var pubPath = temp.CreateFile(pubPem);
        var privPath = temp.CreateFile(privPem);
        return (pubPath, privPath);
    }
}
