using System.Text;
using Lyo.Exceptions.Models;

namespace Lyo.Encryption;

/// <summary>
/// Encrypt-only half of the encryption contract. Implemented directly by encrypt-only services (such as <see cref="Lyo.Encryption.Rsa.RsaEncryptor" />) and composed into
/// <see cref="IEncryptionService" /> for services that support both directions.
/// </summary>
/// <remarks>
/// <para>
/// Pass <c>keyId</c> to resolve key material from <see cref="Lyo.Keystore.IKeyStore" /> when the implementation is constructed with a store; pass <c>key</c> for inline
/// symmetric keys. Streaming helpers write a small versioned header then length-prefixed ciphertext chunks.
/// </para>
/// </remarks>
public interface IEncryptor
{
    /// <summary> The file extension used for encrypted files (e.g., ".ag", ".rsa", ".chacha"). </summary>
    string FileExtension { get; }

    /// <summary>Gets the encoding used when encrypting strings (defaults to UTF-8).</summary>
    Encoding GetEncryptionEncoding();

    /// <summary>Sets the encoding used when encrypting strings.</summary>
    /// <param name="encoding">The encoding to use for subsequent string encryption operations.</param>
    void SetEncryptionEncoding(Encoding encoding);

    /// <summary> Encrypts the provided byte array. </summary>
    /// <param name="bytes">The data to encrypt. Must not be empty.</param>
    /// <param name="keyId">The key identifier to use from the KeyStore. If null, uses the provided key directly.</param>
    /// <param name="key">Optional encryption key. If null and keyId is provided, uses the key from KeyStore.</param>
    /// <returns>Encrypted data</returns>
    /// <exception cref="ArgumentOutsideRangeException">
    /// Thrown when bytes is empty (length is less than MinInputSize) or exceeds maximum allowed size (MaxInputSize), or key size is
    /// invalid
    /// </exception>
    /// <exception cref="InvalidOperationException">Thrown when no encryption key is available (neither keyId nor key provided, or keyId not found in KeyStore)</exception>
    byte[] Encrypt(byte[] bytes, string? keyId = null, byte[]? key = null);

    /// <summary>Encrypts the provided span. Implementations may override to avoid copying to a new array when they support span-based encryption.</summary>
    /// <param name="plaintext">The data to encrypt. Must not be empty.</param>
    /// <param name="keyId">The key identifier to use from the KeyStore. If null, uses the provided key directly.</param>
    /// <param name="key">Optional encryption key. If null and keyId is provided, uses the key from KeyStore.</param>
    /// <returns>Encrypted data.</returns>
    /// <exception cref="ArgumentOutsideRangeException">
    /// Thrown when plaintext is empty (length is less than MinInputSize) or exceeds maximum allowed size (MaxInputSize), or key size is
    /// invalid.
    /// </exception>
    /// <exception cref="InvalidOperationException">Thrown when no encryption key is available (neither keyId nor key provided, or keyId not found in KeyStore).</exception>
    byte[] Encrypt(ReadOnlySpan<byte> plaintext, string? keyId = null, byte[]? key = null);

    /// <summary> Encrypts a string and returns the encrypted data as bytes.</summary>
    /// <param name="text">The text to encrypt. Must not be empty.</param>
    /// <param name="keyId">The key identifier to use from the KeyStore. If null, uses the provided key directly.</param>
    /// <param name="key">Optional encryption key. If null and keyId is provided, uses the key from KeyStore.</param>
    /// <param name="encoding">Optional encoding. If null, uses the encryption encoding (see <see cref="GetEncryptionEncoding" />).</param>
    /// <returns>Encrypted data</returns>
    /// <exception cref="ArgumentOutsideRangeException">
    /// Thrown when text is empty, the encoded bytes are empty (length is less than MinInputSize), or encoded bytes exceed maximum allowed
    /// size (MaxInputSize), or key size is invalid
    /// </exception>
    /// <exception cref="InvalidOperationException">Thrown when no encryption key is available (neither keyId nor key provided, or keyId not found in KeyStore)</exception>
    byte[] EncryptString(string text, string? keyId = null, byte[]? key = null, Encoding? encoding = null);

    /// <summary> Encrypts data from an input stream and writes it to an output stream. </summary>
    /// <param name="input">The input stream containing data to encrypt</param>
    /// <param name="output">The output stream to write encrypted data to</param>
    /// <param name="keyId">The key identifier to use from the KeyStore. If null, uses the provided key directly.</param>
    /// <param name="key">Optional encryption key. If null and keyId is provided, uses the key from KeyStore.</param>
    /// <param name="chunkSize">Size of chunks to read and encrypt. Default is 1MB.</param>
    /// <param name="ct">Cancellation token</param>
    /// <exception cref="ArgumentException">Thrown when chunkSize is invalid</exception>
    /// <exception cref="InvalidOperationException">Thrown when no encryption key is available (neither keyId nor key provided, or keyId not found in KeyStore)</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via ct</exception>
    Task EncryptToStreamAsync(Stream input, Stream output, string? keyId = null, byte[]? key = null, int chunkSize = 1024 * 1024, CancellationToken ct = default);

    /// <summary> Encrypts data and writes it to a file. </summary>
    /// <param name="data">The data to encrypt</param>
    /// <param name="outputPath">The path to write the encrypted file to</param>
    /// <param name="keyId">The key identifier to use from the KeyStore. If null, uses the provided key directly.</param>
    /// <param name="key">Optional encryption key. If null and keyId is provided, uses the key from KeyStore.</param>
    /// <param name="ct">Cancellation token</param>
    /// <exception cref="ArgumentException">Thrown when outputPath is null or empty</exception>
    /// <exception cref="ArgumentOutsideRangeException">Thrown when data is empty (length is less than MinInputSize) or exceeds maximum allowed size (MaxInputSize), or key size is invalid</exception>
    /// <exception cref="InvalidOperationException">Thrown when no encryption key is available (neither keyId nor key provided, or keyId not found in KeyStore)</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via ct</exception>
    Task EncryptToFileAsync(byte[] data, string outputPath, string? keyId = null, byte[]? key = null, CancellationToken ct = default);

    /// <summary> Encrypts data from a stream and writes it to a file. </summary>
    /// <param name="input">The input stream containing data to encrypt</param>
    /// <param name="outputPath">The path to write the encrypted file to</param>
    /// <param name="keyId">The key identifier to use from the KeyStore. If null, uses the provided key directly.</param>
    /// <param name="key">Optional encryption key. If null and keyId is provided, uses the key from KeyStore.</param>
    /// <param name="chunkSize">Size of chunks to read and encrypt. Default is 1MB.</param>
    /// <param name="ct">Cancellation token</param>
    /// <exception cref="ArgumentException">Thrown when outputPath is null or empty, or chunkSize is invalid</exception>
    /// <exception cref="InvalidOperationException">Thrown when no encryption key is available (neither keyId nor key provided, or keyId not found in KeyStore)</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via ct</exception>
    Task EncryptToFileAsync(Stream input, string outputPath, string? keyId = null, byte[]? key = null, int chunkSize = 1024 * 1024, CancellationToken ct = default);
}