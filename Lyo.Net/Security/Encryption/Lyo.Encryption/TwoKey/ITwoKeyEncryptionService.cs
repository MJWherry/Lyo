using System.Text;
using Lyo.Encryption.Exceptions;
using Lyo.Exceptions.Models;

namespace Lyo.Encryption.TwoKey;

/// <summary>
/// Envelope encryption: a Key Encryption Key (KEK) wraps a fresh Data Encryption Key (DEK) for each encrypt, so every operation
/// uses a unique DEK that is itself encrypted under the KEK.
/// </summary>
/// <remarks>
/// <para>
/// Types such as <see cref="TwoKeyEncryptionService{TKeyEncryptionService, TDataEncryptionService}" /> compose two <see cref="IEncryptionService" /> instances (KEK
/// and DEK roles). Treat <c>keyId</c> / <c>kek</c> like the single-key APIs: keystore workflows pass ids; tests or sealed systems may pass raw
/// KEK bytes.
/// </para>
/// </remarks>
public interface ITwoKeyEncryptionService
{
    string FileExtension { get; }

    /// <summary>Algorithm used for Data Encryption Key (DEK) work.</summary>
    EncryptionAlgorithm? DekAlgorithm { get; }

    /// <summary>Algorithm used for Key Encryption Key (KEK) work.</summary>
    EncryptionAlgorithm? KekAlgorithm { get; }

    /// <summary>Encoding used when encrypting strings (UTF-8 unless changed).</summary>
    Encoding GetEncryptionEncoding();

    /// <summary>Changes the encoding used when encrypting strings.</summary>
    /// <param name="encoding">Encoding applied to later string encrypt calls.</param>
    void SetEncryptionEncoding(Encoding encoding);

    /// <summary>Encoding used when decrypting to strings (UTF-8 unless changed).</summary>
    Encoding GetDecryptionEncoding();

    /// <summary>Changes the encoding used when decrypting to strings.</summary>
    /// <param name="encoding">Encoding applied to later string decrypt calls.</param>
    void SetDecryptionEncoding(Encoding encoding);

    /// <summary>Current key version for a given key id.</summary>
    /// <param name="keyId">Key identifier</param>
    /// <returns>Current version, or null when this id has no keys</returns>
    string? GetKeyVersion(string keyId);

    /// <summary>Derivation salt for a given key id and version.</summary>
    /// <param name="keyId">Key identifier</param>
    /// <param name="version">Key version</param>
    /// <returns>Derivation salt, or null when none is stored</returns>
    byte[]? GetSaltForVersion(string keyId, string version);

    /// <summary>Encrypts with a random DEK. The DEK is wrapped by the KEK and returned on the result.</summary>
    /// <param name="bytes">Plaintext. Must not be empty.</param>
    /// <param name="keyId">KeyStore id. Null means use <paramref name="kek" /> as-is.</param>
    /// <param name="kek">Optional KEK. When null and <paramref name="keyId" /> is set, the KeyStore supplies the key.</param>
    /// <returns>Result holding ciphertext and the wrapped DEK</returns>
    /// <exception cref="ArgumentOutsideRangeException">Thrown when bytes is empty (length &lt; 1) or exceeds the maximum size</exception>
    /// <exception cref="InvalidOperationException">Thrown when neither keyId nor kek is supplied, or keyId is missing from the KeyStore</exception>
    TwoKeyEncryptionResult Encrypt(byte[] bytes, string? keyId = null, byte[]? kek = null);

    /// <summary>Decrypts using the wrapped DEK.</summary>
    /// <param name="encryptedData">Ciphertext</param>
    /// <param name="encryptedDataEncryptionKey">Wrapped Data Encryption Key (DEK)</param>
    /// <param name="keyId">KeyStore id. Null means use <paramref name="kek" /> as-is.</param>
    /// <param name="kek">Optional KEK. When null and <paramref name="keyId" /> is set, the KeyStore supplies the key.</param>
    /// <param name="keyVersion">Optional version. When set and a store is configured, that version's key is used.</param>
    /// <param name="salt">Optional salt used to derive the KEK. When set, this salt is used instead of the one in keystore metadata.</param>
    /// <returns>Plaintext</returns>
    /// <exception cref="ArgumentOutsideRangeException">Thrown when encryptedData or encryptedDataEncryptionKey is empty (length &lt; 1)</exception>
    /// <exception cref="InvalidOperationException">Thrown when neither keyId nor kek is supplied</exception>
    /// <exception cref="Lyo.KeyStore.Exceptions.KeyNotFoundException">Thrown when keyId/keyVersion is missing from the KeyStore</exception>
    /// <exception cref="DecryptionFailedException">Thrown on wrong KEK, damaged data, auth failure, or tampering</exception>
    byte[] Decrypt(byte[] encryptedData, byte[] encryptedDataEncryptionKey, string? keyId = null, byte[]? kek = null, string? keyVersion = null, byte[]? salt = null);

    /// <summary>Encrypts a string and returns the full result (including the wrapped DEK). Decrypt with DecryptString using the result fields.</summary>
    /// <param name="text">Plaintext. Must not be empty.</param>
    /// <param name="keyId">KeyStore id. Null means use <paramref name="kek" /> as-is.</param>
    /// <param name="kek">Optional KEK. When null and <paramref name="keyId" /> is set, the KeyStore supplies the key.</param>
    /// <param name="encoding">Optional encoding. Null uses the encrypt encoding (see <see cref="GetEncryptionEncoding" />).</param>
    /// <returns>Result holding ciphertext and the wrapped DEK</returns>
    /// <exception cref="ArgumentOutsideRangeException">Thrown when text is empty, encoded bytes are empty (length &lt; 1), or encoded bytes exceed the maximum size</exception>
    /// <exception cref="InvalidOperationException">Thrown when neither keyId nor kek is supplied, or keyId is missing from the KeyStore</exception>
    TwoKeyEncryptionResult EncryptString(string text, string? keyId = null, byte[]? kek = null, Encoding? encoding = null);

    /// <summary>Decrypts ciphertext and returns a string.</summary>
    /// <param name="encryptedData">Ciphertext</param>
    /// <param name="encryptedDataEncryptionKey">Wrapped Data Encryption Key (DEK)</param>
    /// <param name="keyId">KeyStore id. Null means use <paramref name="kek" /> as-is.</param>
    /// <param name="encoding">Optional encoding. Null uses the decrypt encoding (see <see cref="GetDecryptionEncoding" />).</param>
    /// <param name="kek">Optional KEK. When null and <paramref name="keyId" /> is set, the KeyStore supplies the key.</param>
    /// <param name="keyVersion">Optional version. When set and a store is configured, that version's key is used.</param>
    /// <param name="salt">Optional salt used to derive the KEK. When set, this salt is used instead of the one in keystore metadata.</param>
    /// <returns>Plaintext string</returns>
    /// <exception cref="ArgumentOutsideRangeException">Thrown when encryptedData or encryptedDataEncryptionKey is empty (length &lt; 1)</exception>
    /// <exception cref="InvalidOperationException">Thrown when neither keyId nor kek is supplied</exception>
    /// <exception cref="Lyo.KeyStore.Exceptions.KeyNotFoundException">Thrown when keyId/keyVersion is missing from the KeyStore</exception>
    /// <exception cref="DecryptionFailedException">Thrown on wrong KEK, damaged data, auth failure, or tampering</exception>
    string DecryptString(
        byte[] encryptedData,
        byte[] encryptedDataEncryptionKey,
        string? keyId = null,
        Encoding? encoding = null,
        byte[]? kek = null,
        string? keyVersion = null,
        byte[]? salt = null);

    /// <summary>Encrypts a stream and returns the encryption result.</summary>
    /// <param name="input">Stream of plaintext</param>
    /// <param name="keyId">KeyStore id. Null means use <paramref name="kek" /> as-is.</param>
    /// <param name="kek">Optional KEK. When null and <paramref name="keyId" /> is set, the KeyStore supplies the key.</param>
    /// <param name="chunkSize">Read/encrypt chunk size. Default is 1MB.</param>
    /// <returns>Result holding ciphertext and the wrapped DEK</returns>
    /// <exception cref="ArgumentException">Thrown when chunkSize is invalid</exception>
    /// <exception cref="InvalidOperationException">Thrown when neither keyId nor kek is supplied, or keyId is missing from the KeyStore</exception>
    Task<TwoKeyEncryptionResult> EncryptStreamAsync(Stream input, string? keyId = null, byte[]? kek = null, int chunkSize = 1024 * 1024);

    /// <summary>Decrypts an encryption result onto an output stream.</summary>
    /// <param name="result">Result holding ciphertext and the wrapped DEK</param>
    /// <param name="output">Stream that receives plaintext</param>
    /// <param name="keyId">KeyStore id. Null uses <paramref name="kek" /> or the keyId on result.</param>
    /// <param name="kek">Optional KEK. When null and <paramref name="keyId" /> is set, the KeyStore supplies the key.</param>
    /// <exception cref="InvalidDataException">Thrown when the ciphertext layout is invalid, a chunk length is illegal, or the data is corrupted</exception>
    /// <exception cref="InvalidOperationException">Thrown when neither keyId nor kek is supplied and result.KeyId is empty</exception>
    /// <exception cref="Lyo.KeyStore.Exceptions.KeyNotFoundException">Thrown when keyId/keyVersion is missing from the KeyStore</exception>
    /// <exception cref="DecryptionFailedException">Thrown on wrong KEK, damaged data, auth failure, or tampering</exception>
    Task DecryptStreamAsync(TwoKeyEncryptionResult result, Stream output, string? keyId = null, byte[]? kek = null);

    /// <summary>Encrypts an input stream onto an output stream. The wrapped DEK is written first, then the ciphertext chunks.</summary>
    /// <param name="input">Stream of plaintext</param>
    /// <param name="output">Stream that receives ciphertext</param>
    /// <param name="keyId">KeyStore id. Null means use <paramref name="kek" /> as-is.</param>
    /// <param name="kek">Optional KEK. When null and <paramref name="keyId" /> is set, the KeyStore supplies the key.</param>
    /// <param name="chunkSize">Read/encrypt chunk size. Default is 1MB.</param>
    /// <param name="ct">Cancellation token</param>
    /// <exception cref="ArgumentException">Thrown when chunkSize is invalid</exception>
    /// <exception cref="InvalidOperationException">Thrown when neither keyId nor kek is supplied, or keyId is missing from the KeyStore</exception>
    /// <exception cref="OperationCanceledException">Thrown when <paramref name="ct" /> cancels the call</exception>
    Task EncryptToStreamAsync(Stream input, Stream output, string? keyId = null, byte[]? kek = null, int chunkSize = 1024 * 1024, CancellationToken ct = default);

    /// <summary>Decrypts an input stream onto an output stream. Reads the wrapped DEK first, then the ciphertext chunks.</summary>
    /// <param name="input">Ciphertext stream (wrapped DEK plus encrypted data)</param>
    /// <param name="output">Stream that receives plaintext</param>
    /// <param name="keyId">KeyStore id. Null means use <paramref name="kek" /> as-is.</param>
    /// <param name="kek">Optional KEK. When null and <paramref name="keyId" /> is set, the KeyStore supplies the key.</param>
    /// <param name="ct">Cancellation token</param>
    /// <exception cref="InvalidDataException">Thrown when stream layout is invalid, format version unsupported, chunk length illegal, or data corrupted</exception>
    /// <exception cref="EndOfStreamException">Thrown when the stream ends while ciphertext is still being read</exception>
    /// <exception cref="NotSupportedException">Thrown when the stream format version is not supported</exception>
    /// <exception cref="InvalidOperationException">Thrown when neither keyId nor kek is supplied</exception>
    /// <exception cref="Lyo.KeyStore.Exceptions.KeyNotFoundException">Thrown when keyId/keyVersion is missing from the KeyStore</exception>
    /// <exception cref="DecryptionFailedException">Thrown on wrong KEK, damaged data, auth failure, or tampering</exception>
    /// <exception cref="OperationCanceledException">Thrown when <paramref name="ct" /> cancels the call</exception>
    Task DecryptToStreamAsync(Stream input, Stream output, string? keyId = null, byte[]? kek = null, CancellationToken ct = default);

    /// <summary>Encrypts data and writes the ciphertext to a file.</summary>
    /// <param name="data">Plaintext</param>
    /// <param name="outputPath">Destination path for the ciphertext file</param>
    /// <param name="keyId">KeyStore id. Null means use <paramref name="kek" /> as-is.</param>
    /// <param name="kek">Optional KEK. When null and <paramref name="keyId" /> is set, the KeyStore supplies the key.</param>
    /// <param name="ct">Cancellation token</param>
    /// <exception cref="ArgumentException">Thrown when outputPath is null or empty</exception>
    /// <exception cref="ArgumentOutsideRangeException">Thrown when data is empty (length &lt; 1) or exceeds the maximum size</exception>
    /// <exception cref="InvalidOperationException">Thrown when neither keyId nor kek is supplied, or keyId is missing from the KeyStore</exception>
    /// <exception cref="OperationCanceledException">Thrown when <paramref name="ct" /> cancels the call</exception>
    Task EncryptToFileAsync(byte[] data, string outputPath, string? keyId = null, byte[]? kek = null, CancellationToken ct = default);

    /// <summary>Encrypts a stream and writes the ciphertext to a file.</summary>
    /// <param name="input">Stream of plaintext</param>
    /// <param name="outputPath">Destination path for the ciphertext file</param>
    /// <param name="keyId">KeyStore id. Null means use <paramref name="kek" /> as-is.</param>
    /// <param name="kek">Optional KEK. When null and <paramref name="keyId" /> is set, the KeyStore supplies the key.</param>
    /// <param name="chunkSize">Read/encrypt chunk size. Default is 1MB.</param>
    /// <param name="ct">Cancellation token</param>
    /// <exception cref="ArgumentException">Thrown when outputPath is null or empty, or chunkSize is invalid</exception>
    /// <exception cref="InvalidOperationException">Thrown when neither keyId nor kek is supplied, or keyId is missing from the KeyStore</exception>
    /// <exception cref="OperationCanceledException">Thrown when <paramref name="ct" /> cancels the call</exception>
    Task EncryptToFileAsync(Stream input, string outputPath, string? keyId = null, byte[]? kek = null, int chunkSize = 1024 * 1024, CancellationToken ct = default);

    /// <summary>Decrypts a file and returns the plaintext bytes.</summary>
    /// <param name="inputPath">Path of the ciphertext file</param>
    /// <param name="keyId">KeyStore id. Null means use <paramref name="kek" /> as-is.</param>
    /// <param name="kek">Optional KEK. When null and <paramref name="keyId" /> is set, the KeyStore supplies the key.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Plaintext</returns>
    /// <exception cref="ArgumentException">Thrown when inputPath is null or empty</exception>
    /// <exception cref="FileNotFoundException">Thrown when the input file does not exist</exception>
    /// <exception cref="InvalidDataException">Thrown when file layout is invalid, format version unsupported, chunk length illegal, or data corrupted</exception>
    /// <exception cref="EndOfStreamException">Thrown when the file stream ends while ciphertext is still being read</exception>
    /// <exception cref="NotSupportedException">Thrown when the stream format version is not supported</exception>
    /// <exception cref="InvalidOperationException">Thrown when neither keyId nor kek is supplied</exception>
    /// <exception cref="Lyo.KeyStore.Exceptions.KeyNotFoundException">Thrown when keyId/keyVersion is missing from the KeyStore</exception>
    /// <exception cref="DecryptionFailedException">Thrown on wrong KEK, damaged data, auth failure, or tampering</exception>
    /// <exception cref="OperationCanceledException">Thrown when <paramref name="ct" /> cancels the call</exception>
    Task<byte[]> DecryptFromFileAsync(string inputPath, string? keyId = null, byte[]? kek = null, CancellationToken ct = default);

    /// <summary>
    /// Unwraps a DEK under one KEK and re-wraps it under another. Used for rotation (same keyId, new version)
    /// or migration (different keyId).
    /// </summary>
    /// <param name="encryptedDek">DEK wrapped by the source KEK</param>
    /// <param name="sourceKeyId">Key id that originally wrapped the DEK</param>
    /// <param name="sourceKeyVersion">KEK version that originally wrapped the DEK</param>
    /// <param name="targetKeyId">Key id for the new wrap. Null uses sourceKeyId (same key, new version).</param>
    /// <param name="targetKeyVersion">KEK version for the new wrap. Null uses the current version of targetKeyId.</param>
    /// <returns>DEK re-wrapped under the target KEK</returns>
    /// <exception cref="ArgumentException">Thrown when sourceKeyId or sourceKeyVersion is null or empty</exception>
    /// <exception cref="InvalidOperationException">Thrown when the source or target KEK is missing from the store, or source and target are the same</exception>
    /// <exception cref="DecryptionFailedException">Thrown when unwrapping the DEK fails (wrong source KEK or damaged data)</exception>
    byte[] ReEncryptDek(byte[] encryptedDek, string sourceKeyId, string sourceKeyVersion, string? targetKeyId = null, string? targetKeyVersion = null);

    /// <summary>
    /// Unwraps a DEK under one KEK and re-wraps it under another, asynchronously. Used for rotation (same keyId,
    /// new version) or migration (different keyId).
    /// </summary>
    /// <param name="encryptedDek">DEK wrapped by the source KEK</param>
    /// <param name="sourceKeyId">Key id that originally wrapped the DEK</param>
    /// <param name="sourceKeyVersion">KEK version that originally wrapped the DEK</param>
    /// <param name="targetKeyId">Key id for the new wrap. Null uses sourceKeyId (same key, new version).</param>
    /// <param name="targetKeyVersion">KEK version for the new wrap. Null uses the current version of targetKeyId.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>DEK re-wrapped under the target KEK</returns>
    /// <exception cref="ArgumentException">Thrown when sourceKeyId or sourceKeyVersion is null or empty</exception>
    /// <exception cref="InvalidOperationException">Thrown when the source or target KEK is missing from the store, or source and target are the same</exception>
    /// <exception cref="DecryptionFailedException">Thrown when unwrapping the DEK fails (wrong source KEK or damaged data)</exception>
    /// <exception cref="OperationCanceledException">Thrown when <paramref name="ct" /> cancels the call</exception>
    Task<byte[]> ReEncryptDekAsync(
        byte[] encryptedDek,
        string sourceKeyId,
        string sourceKeyVersion,
        string? targetKeyId = null,
        string? targetKeyVersion = null,
        CancellationToken ct = default);
}