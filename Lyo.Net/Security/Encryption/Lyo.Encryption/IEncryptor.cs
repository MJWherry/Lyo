using System.Text;
using Lyo.Exceptions.Models;

namespace Lyo.Encryption;

/// <summary>
/// Encrypt side of the contract. Implemented on its own by encrypt-only types (for example <see cref="Lyo.Encryption.Rsa.RsaEncryptor" />) and composed into
/// <see cref="IEncryptionService" /> when a service handles both directions.
/// </summary>
/// <remarks>
/// <para>
/// Supply <c>keyId</c> to load material from <see cref="Lyo.KeyStore.IKeyStore" /> when the service was built with a store; supply <c>key</c> for inline
/// symmetric keys. Streaming helpers emit a short versioned header then length-prefixed ciphertext chunks.
/// </para>
/// </remarks>
public interface IEncryptor
{
    /// <summary>Extension written on encrypted files (examples: ".ag", ".rsa", ".chacha").</summary>
    string FileExtension { get; }

    /// <summary>Encoding used when encrypting strings (UTF-8 unless changed).</summary>
    Encoding GetEncryptionEncoding();

    /// <summary>Changes the encoding used when encrypting strings.</summary>
    /// <param name="encoding">Encoding applied to later string encrypt calls.</param>
    void SetEncryptionEncoding(Encoding encoding);

    /// <summary>Encrypts a byte array.</summary>
    /// <param name="bytes">Plaintext. Must not be empty.</param>
    /// <param name="keyId">KeyStore id. Null means use <paramref name="key" /> as-is.</param>
    /// <param name="key">Optional raw key. When null and <paramref name="keyId" /> is set, the KeyStore supplies the key.</param>
    /// <param name="associatedData">
    /// Optional AAD authenticated with the ciphertext (not encrypted, not stored). The same value must be passed at decrypt time or
    /// authentication fails. Only AEAD implementations accept it; others throw <see cref="NotSupportedException" /> if the value is non-null.
    /// </param>
    /// <returns>Ciphertext</returns>
    /// <exception cref="ArgumentOutsideRangeException">
    /// Thrown when bytes is empty (below MinInputSize), exceeds MaxInputSize, or the key
    /// length is invalid
    /// </exception>
    /// <exception cref="InvalidOperationException">Thrown when no key is available (neither keyId nor key, or keyId missing from the KeyStore)</exception>
    /// <exception cref="NotSupportedException">Thrown when associatedData is set but this implementation has no AAD support</exception>
    byte[] Encrypt(byte[] bytes, string? keyId = null, byte[]? key = null, byte[]? associatedData = null);

    /// <summary>Encrypts a span. Implementations may override to encrypt without copying into a new array.</summary>
    /// <param name="plaintext">Plaintext. Must not be empty.</param>
    /// <param name="keyId">KeyStore id. Null means use <paramref name="key" /> as-is.</param>
    /// <param name="key">Optional raw key. When null and <paramref name="keyId" /> is set, the KeyStore supplies the key.</param>
    /// <param name="associatedData">Optional AAD authenticated with the ciphertext (not encrypted or stored); see <see cref="Encrypt(byte[], string, byte[], byte[])" />.</param>
    /// <returns>Ciphertext.</returns>
    /// <exception cref="ArgumentOutsideRangeException">
    /// Thrown when plaintext is empty (below MinInputSize), exceeds MaxInputSize, or the key
    /// length is invalid.
    /// </exception>
    /// <exception cref="InvalidOperationException">Thrown when no key is available (neither keyId nor key, or keyId missing from the KeyStore).</exception>
    /// <exception cref="NotSupportedException">Thrown when associatedData is set but this implementation has no AAD support.</exception>
    byte[] Encrypt(ReadOnlySpan<byte> plaintext, string? keyId = null, byte[]? key = null, byte[]? associatedData = null);

    /// <summary>Encrypts a string and returns ciphertext bytes.</summary>
    /// <param name="text">Plaintext. Must not be empty.</param>
    /// <param name="keyId">KeyStore id. Null means use <paramref name="key" /> as-is.</param>
    /// <param name="key">Optional raw key. When null and <paramref name="keyId" /> is set, the KeyStore supplies the key.</param>
    /// <param name="encoding">Optional encoding. Null uses the encrypt encoding (see <see cref="GetEncryptionEncoding" />).</param>
    /// <returns>Ciphertext</returns>
    /// <exception cref="ArgumentOutsideRangeException">
    /// Thrown when text is empty, the encoded bytes are empty (below MinInputSize), the encoded bytes exceed
    /// MaxInputSize, or the key length is invalid
    /// </exception>
    /// <exception cref="InvalidOperationException">Thrown when no key is available (neither keyId nor key, or keyId missing from the KeyStore)</exception>
    byte[] EncryptString(string text, string? keyId = null, byte[]? key = null, Encoding? encoding = null);

    /// <summary>Encrypts an input stream and writes ciphertext to an output stream.</summary>
    /// <param name="input">Stream of plaintext</param>
    /// <param name="output">Stream that receives ciphertext</param>
    /// <param name="keyId">KeyStore id. Null means use <paramref name="key" /> as-is.</param>
    /// <param name="key">Optional raw key. When null and <paramref name="keyId" /> is set, the KeyStore supplies the key.</param>
    /// <param name="chunkSize">Read/encrypt chunk size. Default is 1MB.</param>
    /// <param name="associatedData">
    /// Optional AAD authenticated with every stream chunk (not encrypted, not stored). The same value must be passed to
    /// <see cref="IDecryptor.DecryptToStreamAsync" /> or authentication fails. Only AEAD implementations accept it.
    /// </param>
    /// <param name="ct">Cancellation token</param>
    /// <exception cref="ArgumentException">Thrown when chunkSize is invalid</exception>
    /// <exception cref="InvalidOperationException">Thrown when no key is available (neither keyId nor key, or keyId missing from the KeyStore)</exception>
    /// <exception cref="NotSupportedException">Thrown when associatedData is set but this implementation has no AAD support</exception>
    /// <exception cref="OperationCanceledException">Thrown when <paramref name="ct" /> cancels the call</exception>
    Task EncryptToStreamAsync(
        Stream input,
        Stream output,
        string? keyId = null,
        byte[]? key = null,
        int chunkSize = 1024 * 1024,
        byte[]? associatedData = null,
        CancellationToken ct = default);

    /// <summary>Encrypts data and writes the ciphertext to a file.</summary>
    /// <param name="data">Plaintext</param>
    /// <param name="outputPath">Destination path for the ciphertext file</param>
    /// <param name="keyId">KeyStore id. Null means use <paramref name="key" /> as-is.</param>
    /// <param name="key">Optional raw key. When null and <paramref name="keyId" /> is set, the KeyStore supplies the key.</param>
    /// <param name="ct">Cancellation token</param>
    /// <exception cref="ArgumentException">Thrown when outputPath is null or empty</exception>
    /// <exception cref="ArgumentOutsideRangeException">Thrown when data is empty (below MinInputSize), exceeds MaxInputSize, or the key length is invalid</exception>
    /// <exception cref="InvalidOperationException">Thrown when no key is available (neither keyId nor key, or keyId missing from the KeyStore)</exception>
    /// <exception cref="OperationCanceledException">Thrown when <paramref name="ct" /> cancels the call</exception>
    Task EncryptToFileAsync(byte[] data, string outputPath, string? keyId = null, byte[]? key = null, CancellationToken ct = default);

    /// <summary>Encrypts a stream and writes the ciphertext to a file.</summary>
    /// <param name="input">Stream of plaintext</param>
    /// <param name="outputPath">Destination path for the ciphertext file</param>
    /// <param name="keyId">KeyStore id. Null means use <paramref name="key" /> as-is.</param>
    /// <param name="key">Optional raw key. When null and <paramref name="keyId" /> is set, the KeyStore supplies the key.</param>
    /// <param name="chunkSize">Read/encrypt chunk size. Default is 1MB.</param>
    /// <param name="ct">Cancellation token</param>
    /// <exception cref="ArgumentException">Thrown when outputPath is null or empty, or chunkSize is invalid</exception>
    /// <exception cref="InvalidOperationException">Thrown when no key is available (neither keyId nor key, or keyId missing from the KeyStore)</exception>
    /// <exception cref="OperationCanceledException">Thrown when <paramref name="ct" /> cancels the call</exception>
    Task EncryptToFileAsync(Stream input, string outputPath, string? keyId = null, byte[]? key = null, int chunkSize = 1024 * 1024, CancellationToken ct = default);
}