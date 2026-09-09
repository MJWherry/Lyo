using System.Text;
using Lyo.Encryption;
using Lyo.Encryption.TwoKey;

namespace Lyo.FileStorage.Tests.Support;

/// <summary>
/// Delegates every operation to <paramref name="inner" /> except <see cref="EncryptToStreamAsync" />, which writes a prefix of the real ciphertext and then throws. Lets a test
/// drive a save or DEK rewrite that fails after bytes have already been written to the storage output stream.
/// </summary>
public sealed class FailingEncryptToStreamTwoKeyEncryptionService(ITwoKeyEncryptionService inner) : ITwoKeyEncryptionService
{
    /// <summary>Text carried by the exception thrown from <see cref="EncryptToStreamAsync" />.</summary>
    public const string FailureMessage = "Injected encrypt-to-stream failure.";

    public string FileExtension => inner.FileExtension;

    public EncryptionAlgorithm? DekAlgorithm => inner.DekAlgorithm;

    public EncryptionAlgorithm? KekAlgorithm => inner.KekAlgorithm;

    public Encoding GetEncryptionEncoding() => inner.GetEncryptionEncoding();

    public void SetEncryptionEncoding(Encoding encoding) => inner.SetEncryptionEncoding(encoding);

    public Encoding GetDecryptionEncoding() => inner.GetDecryptionEncoding();

    public void SetDecryptionEncoding(Encoding encoding) => inner.SetDecryptionEncoding(encoding);

    public string? GetKeyVersion(string keyId) => inner.GetKeyVersion(keyId);

    public byte[]? GetSaltForVersion(string keyId, string version) => inner.GetSaltForVersion(keyId, version);

    public TwoKeyEncryptionResult Encrypt(byte[] bytes, string? keyId = null, byte[]? kek = null) => inner.Encrypt(bytes, keyId, kek);

    public byte[] Decrypt(byte[] encryptedData, byte[] encryptedDataEncryptionKey, string? keyId = null, byte[]? kek = null, string? keyVersion = null, byte[]? salt = null)
        => inner.Decrypt(encryptedData, encryptedDataEncryptionKey, keyId, kek, keyVersion, salt);

    public TwoKeyEncryptionResult EncryptString(string text, string? keyId = null, byte[]? kek = null, Encoding? encoding = null)
        => inner.EncryptString(text, keyId, kek, encoding);

    public string DecryptString(
        byte[] encryptedData,
        byte[] encryptedDataEncryptionKey,
        string? keyId = null,
        Encoding? encoding = null,
        byte[]? kek = null,
        string? keyVersion = null,
        byte[]? salt = null)
        => inner.DecryptString(encryptedData, encryptedDataEncryptionKey, keyId, encoding, kek, keyVersion, salt);

    public Task<TwoKeyEncryptionResult> EncryptStreamAsync(Stream input, string? keyId = null, byte[]? kek = null, int chunkSize = 1024 * 1024)
        => inner.EncryptStreamAsync(input, keyId, kek, chunkSize);

    public Task DecryptStreamAsync(TwoKeyEncryptionResult result, Stream output, string? keyId = null, byte[]? kek = null) => inner.DecryptStreamAsync(result, output, keyId, kek);

    /// <summary>Encrypts into a buffer, writes the first half to <paramref name="output" />, then throws without a flush.</summary>
    public async Task EncryptToStreamAsync(
        Stream input,
        Stream output,
        string? keyId = null,
        byte[]? kek = null,
        int chunkSize = 1024 * 1024,
        CancellationToken ct = default)
    {
        using var buffer = new MemoryStream();
        await inner.EncryptToStreamAsync(input, buffer, keyId, kek, chunkSize, ct).ConfigureAwait(false);
        var partial = buffer.ToArray();
        await output.WriteAsync(partial, 0, partial.Length / 2, ct).ConfigureAwait(false);
        throw new InvalidOperationException(FailureMessage);
    }

    public Task DecryptToStreamAsync(Stream input, Stream output, string? keyId = null, byte[]? kek = null, CancellationToken ct = default)
        => inner.DecryptToStreamAsync(input, output, keyId, kek, ct);

    public Task EncryptToFileAsync(byte[] data, string outputPath, string? keyId = null, byte[]? kek = null, CancellationToken ct = default)
        => inner.EncryptToFileAsync(data, outputPath, keyId, kek, ct);

    public Task EncryptToFileAsync(Stream input, string outputPath, string? keyId = null, byte[]? kek = null, int chunkSize = 1024 * 1024, CancellationToken ct = default)
        => inner.EncryptToFileAsync(input, outputPath, keyId, kek, chunkSize, ct);

    public Task<byte[]> DecryptFromFileAsync(string inputPath, string? keyId = null, byte[]? kek = null, CancellationToken ct = default)
        => inner.DecryptFromFileAsync(inputPath, keyId, kek, ct);

    public byte[] ReEncryptDek(byte[] encryptedDek, string sourceKeyId, string sourceKeyVersion, string? targetKeyId = null, string? targetKeyVersion = null)
        => inner.ReEncryptDek(encryptedDek, sourceKeyId, sourceKeyVersion, targetKeyId, targetKeyVersion);

    public Task<byte[]> ReEncryptDekAsync(
        byte[] encryptedDek,
        string sourceKeyId,
        string sourceKeyVersion,
        string? targetKeyId = null,
        string? targetKeyVersion = null,
        CancellationToken ct = default)
        => inner.ReEncryptDekAsync(encryptedDek, sourceKeyId, sourceKeyVersion, targetKeyId, targetKeyVersion, ct);
}
