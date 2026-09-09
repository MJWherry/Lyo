using System.Text;
using Lyo.Encryption.Exceptions;
using Lyo.Exceptions.Models;

namespace Lyo.Encryption;

/// <summary>
/// Decrypt side of the contract. Implemented on its own by decrypt-only types (for example <see cref="Lyo.Encryption.Rsa.RsaDecryptor" />) and composed into
/// <see cref="IEncryptionService" /> when a service handles both directions.
/// </summary>
/// <remarks>
/// <para>
/// Supply <c>keyId</c> to load material from <see cref="Lyo.KeyStore.IKeyStore" /> when the service was built with a store; supply <c>key</c> for inline
/// symmetric keys.
/// </para>
/// </remarks>
public interface IDecryptor
{
    /// <summary>Encoding used when decrypting to strings (UTF-8 unless changed).</summary>
    Encoding GetDecryptionEncoding();

    /// <summary>Changes the encoding used when decrypting to strings.</summary>
    /// <param name="encoding">Encoding applied to later string decrypt calls.</param>
    void SetDecryptionEncoding(Encoding encoding);

    /// <summary>Decrypts a ciphertext byte array.</summary>
    /// <param name="encryptedBytes">Ciphertext</param>
    /// <param name="keyId">KeyStore id. Null means use <paramref name="key" /> as-is.</param>
    /// <param name="key">Optional raw key. When null and <paramref name="keyId" /> is set, the KeyStore supplies the key.</param>
    /// <param name="associatedData">
    /// Optional AAD that was authenticated with the ciphertext at encrypt time. Must equal the encrypt-time value exactly (null if none was
    /// used) or authentication fails. Only AEAD implementations accept it.
    /// </param>
    /// <returns>Plaintext</returns>
    /// <exception cref="ArgumentOutsideRangeException">Thrown when encryptedBytes is empty (length &lt; 1) or shorter than the minimum size</exception>
    /// <exception cref="InvalidDataException">Thrown when the ciphertext layout is invalid, the format version is unsupported, or the data is corrupted</exception>
    /// <exception cref="InvalidOperationException">Thrown when no key is available (neither keyId nor key, or keyId missing from the KeyStore)</exception>
    /// <exception cref="NotSupportedException">Thrown when associatedData is set but this implementation has no AAD support</exception>
    /// <exception cref="DecryptionFailedException">Thrown on wrong key, damaged data, auth failure, mismatched AAD, or tampering</exception>
    byte[] Decrypt(byte[] encryptedBytes, string? keyId = null, byte[]? key = null, byte[]? associatedData = null);

    /// <summary>Decrypts a contiguous slice of <paramref name="buffer" />. Implementations may override to decrypt without copying the slice.</summary>
    /// <param name="buffer">Buffer holding ciphertext.</param>
    /// <param name="offset">Start of the ciphertext slice in <paramref name="buffer" />.</param>
    /// <param name="count">Ciphertext slice length in bytes.</param>
    /// <param name="keyId">KeyStore id. Null means use <paramref name="key" /> as-is.</param>
    /// <param name="key">Optional raw key. When null and <paramref name="keyId" /> is set, the KeyStore supplies the key.</param>
    /// <param name="associatedData">Optional AAD authenticated with the ciphertext; see <see cref="Decrypt(byte[], string, byte[], byte[])" />.</param>
    /// <returns>Plaintext.</returns>
    /// <exception cref="ArgumentOutsideRangeException">Thrown when the slice is empty, shorter than the minimum, or exceeds the maximum size.</exception>
    /// <exception cref="InvalidDataException">Thrown when the ciphertext layout is invalid, the format version is unsupported, or the data is corrupted.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no key is available (neither keyId nor key, or keyId missing from the KeyStore).</exception>
    /// <exception cref="NotSupportedException">Thrown when associatedData is set but this implementation has no AAD support.</exception>
    /// <exception cref="DecryptionFailedException">Thrown on wrong key, damaged data, auth failure, mismatched AAD, or tampering.</exception>
    byte[] Decrypt(byte[] buffer, int offset, int count, string? keyId = null, byte[]? key = null, byte[]? associatedData = null);

    /// <summary>Decrypts ciphertext bytes and returns a string.</summary>
    /// <param name="encryptedBytes">Ciphertext</param>
    /// <param name="keyId">KeyStore id. Null means use <paramref name="key" /> as-is.</param>
    /// <param name="key">Optional raw key. When null and <paramref name="keyId" /> is set, the KeyStore supplies the key.</param>
    /// <param name="encoding">Optional encoding. Null uses the decrypt encoding (see <see cref="GetDecryptionEncoding" />).</param>
    /// <returns>Plaintext string</returns>
    /// <exception cref="ArgumentOutsideRangeException">Thrown when encryptedBytes is empty (length &lt; 1) or shorter than the minimum size</exception>
    /// <exception cref="InvalidDataException">Thrown when the ciphertext layout is invalid, the format version is unsupported, or the data is corrupted</exception>
    /// <exception cref="InvalidOperationException">Thrown when no key is available (neither keyId nor key, or keyId missing from the KeyStore)</exception>
    /// <exception cref="DecryptionFailedException">Thrown on wrong key, damaged data, auth failure, or tampering</exception>
    string DecryptString(byte[] encryptedBytes, string? keyId = null, byte[]? key = null, Encoding? encoding = null);

    /// <summary>Decrypts an input stream and writes plaintext to an output stream.</summary>
    /// <param name="input">Stream of ciphertext</param>
    /// <param name="output">Stream that receives plaintext</param>
    /// <param name="keyId">KeyStore id. Null means use <paramref name="key" /> as-is.</param>
    /// <param name="key">Optional raw key. When null and <paramref name="keyId" /> is set, the KeyStore supplies the key.</param>
    /// <param name="associatedData">
    /// Optional AAD authenticated with the stream at encrypt time. Must equal the encrypt-time value exactly (null if none was
    /// used) or chunk authentication fails. Only V2 streams from AEAD implementations support it.
    /// </param>
    /// <param name="ct">Cancellation token</param>
    /// <exception cref="InvalidDataException">Thrown when stream layout is invalid, format version unsupported, chunk length illegal, or data corrupted</exception>
    /// <exception cref="EndOfStreamException">Thrown when the stream ends while ciphertext is still being read</exception>
    /// <exception cref="InvalidOperationException">Thrown when no key is available (neither keyId nor key, or keyId missing from the KeyStore)</exception>
    /// <exception cref="NotSupportedException">Thrown when associatedData is set but the stream format or implementation has no AAD support</exception>
    /// <exception cref="DecryptionFailedException">Thrown on wrong key, damaged data, auth failure, mismatched AAD, or tampering</exception>
    /// <exception cref="OperationCanceledException">Thrown when <paramref name="ct" /> cancels the call</exception>
    Task DecryptToStreamAsync(Stream input, Stream output, string? keyId = null, byte[]? key = null, byte[]? associatedData = null, CancellationToken ct = default);

    /// <summary>Decrypts a file and returns the plaintext bytes.</summary>
    /// <param name="inputPath">Path of the ciphertext file</param>
    /// <param name="keyId">KeyStore id. Null means use <paramref name="key" /> as-is.</param>
    /// <param name="key">Optional raw key. When null and <paramref name="keyId" /> is set, the KeyStore supplies the key.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Plaintext</returns>
    /// <exception cref="ArgumentException">Thrown when inputPath is null or empty</exception>
    /// <exception cref="FileNotFoundException">Thrown when the input file does not exist</exception>
    /// <exception cref="InvalidDataException">Thrown when file layout is invalid, format version unsupported, chunk length illegal, or data corrupted</exception>
    /// <exception cref="EndOfStreamException">Thrown when the file stream ends while ciphertext is still being read</exception>
    /// <exception cref="InvalidOperationException">Thrown when no key is available (neither keyId nor key, or keyId missing from the KeyStore)</exception>
    /// <exception cref="DecryptionFailedException">Thrown on wrong key, damaged data, auth failure, or tampering</exception>
    /// <exception cref="OperationCanceledException">Thrown when <paramref name="ct" /> cancels the call</exception>
    Task<byte[]> DecryptFromFileAsync(string inputPath, string? keyId = null, byte[]? key = null, CancellationToken ct = default);
}