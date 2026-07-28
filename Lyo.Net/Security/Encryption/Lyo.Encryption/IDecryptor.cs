using System.Text;
using Lyo.Encryption.Exceptions;
using Lyo.Exceptions.Models;

namespace Lyo.Encryption;

/// <summary>
/// Decrypt-only half of the encryption contract. Implemented directly by decrypt-only services (such as <see cref="Lyo.Encryption.Rsa.RsaDecryptor" />) and composed into
/// <see cref="IEncryptionService" /> for services that support both directions.
/// </summary>
/// <remarks>
/// <para>
/// Pass <c>keyId</c> to resolve key material from <see cref="Lyo.Keystore.IKeyStore" /> when the implementation is constructed with a store; pass <c>key</c> for inline
/// symmetric keys.
/// </para>
/// </remarks>
public interface IDecryptor
{
    /// <summary>Gets the encoding used when decrypting to strings (defaults to UTF-8).</summary>
    Encoding GetDecryptionEncoding();

    /// <summary>Sets the encoding used when decrypting to strings.</summary>
    /// <param name="encoding">The encoding to use for subsequent string decryption operations.</param>
    void SetDecryptionEncoding(Encoding encoding);

    /// <summary> Decrypts the provided encrypted byte array. </summary>
    /// <param name="encryptedBytes">The encrypted data to decrypt</param>
    /// <param name="keyId">The key identifier to use from the KeyStore. If null, uses the provided key directly.</param>
    /// <param name="key">Optional decryption key. If null and keyId is provided, uses the key from KeyStore.</param>
    /// <param name="associatedData">
    /// Optional associated data that was authenticated with the ciphertext at encryption time. Must match the encrypt-time value exactly (null when none was
    /// used) or decryption fails authentication. Only AEAD implementations support it.
    /// </param>
    /// <returns>Decrypted data</returns>
    /// <exception cref="ArgumentOutsideRangeException">Thrown when encryptedBytes is empty (length is less than 1) or too small (below minimum required size)</exception>
    /// <exception cref="InvalidDataException">Thrown when encrypted data format is invalid, unsupported format version, or corrupted</exception>
    /// <exception cref="InvalidOperationException">Thrown when no decryption key is available (neither keyId nor key provided, or keyId not found in KeyStore)</exception>
    /// <exception cref="NotSupportedException">Thrown when associatedData is provided but the implementation does not support associated data</exception>
    /// <exception cref="DecryptionFailedException">Thrown when decryption fails due to wrong key, corrupted data, authentication failure, mismatched associated data, or tampered data</exception>
    byte[] Decrypt(byte[] encryptedBytes, string? keyId = null, byte[]? key = null, byte[]? associatedData = null);

    /// <summary>Decrypts a contiguous region of <paramref name="buffer" />. Implementations may override to decrypt without copying the slice to a new array.</summary>
    /// <param name="buffer">Buffer containing encrypted data.</param>
    /// <param name="offset">Start index of the encrypted slice in <paramref name="buffer" />.</param>
    /// <param name="count">Length of the encrypted slice in bytes.</param>
    /// <param name="keyId">The key identifier to use from the KeyStore. If null, uses the provided key directly.</param>
    /// <param name="key">Optional decryption key. If null and keyId is provided, uses the key from KeyStore.</param>
    /// <param name="associatedData">Optional associated data that was authenticated with the ciphertext; see <see cref="Decrypt(byte[], string, byte[], byte[])" />.</param>
    /// <returns>Decrypted data.</returns>
    /// <exception cref="ArgumentOutsideRangeException">Thrown when the slice is empty or too small (below minimum required size), or exceeds maximum allowed size.</exception>
    /// <exception cref="InvalidDataException">Thrown when encrypted data format is invalid, unsupported format version, or corrupted.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no decryption key is available (neither keyId nor key provided, or keyId not found in KeyStore).</exception>
    /// <exception cref="NotSupportedException">Thrown when associatedData is provided but the implementation does not support associated data.</exception>
    /// <exception cref="DecryptionFailedException">Thrown when decryption fails due to wrong key, corrupted data, authentication failure, mismatched associated data, or tampered data.</exception>
    byte[] Decrypt(byte[] buffer, int offset, int count, string? keyId = null, byte[]? key = null, byte[]? associatedData = null);

    /// <summary> Decrypts encrypted bytes and returns the decrypted string. </summary>
    /// <param name="encryptedBytes">The encrypted data to decrypt</param>
    /// <param name="keyId">The key identifier to use from the KeyStore. If null, uses the provided key directly.</param>
    /// <param name="key">Optional decryption key. If null and keyId is provided, uses the key from KeyStore.</param>
    /// <param name="encoding">Optional encoding. If null, uses the decryption encoding (see <see cref="GetDecryptionEncoding" />).</param>
    /// <returns>Decrypted string</returns>
    /// <exception cref="ArgumentOutsideRangeException">Thrown when encryptedBytes is empty (length is less than 1) or too small (below minimum required size)</exception>
    /// <exception cref="InvalidDataException">Thrown when encrypted data format is invalid, unsupported format version, or corrupted</exception>
    /// <exception cref="InvalidOperationException">Thrown when no decryption key is available (neither keyId nor key provided, or keyId not found in KeyStore)</exception>
    /// <exception cref="DecryptionFailedException">Thrown when decryption fails due to wrong key, corrupted data, authentication failure, or tampered data</exception>
    string DecryptString(byte[] encryptedBytes, string? keyId = null, byte[]? key = null, Encoding? encoding = null);

    /// <summary> Decrypts data from an input stream and writes it to an output stream. </summary>
    /// <param name="input">The input stream containing encrypted data</param>
    /// <param name="output">The output stream to write decrypted data to</param>
    /// <param name="keyId">The key identifier to use from the KeyStore. If null, uses the provided key directly.</param>
    /// <param name="key">Optional decryption key. If null and keyId is provided, uses the key from KeyStore.</param>
    /// <param name="associatedData">
    /// Optional associated data that was authenticated with the stream at encryption time. Must match the encrypt-time value exactly (null when none was
    /// used) or chunk authentication fails. Only supported for V2 streams produced by AEAD implementations.
    /// </param>
    /// <param name="ct">Cancellation token</param>
    /// <exception cref="InvalidDataException">Thrown when encrypted stream format is invalid, unsupported format version, invalid chunk length, or corrupted</exception>
    /// <exception cref="EndOfStreamException">Thrown when the stream ends unexpectedly while reading encrypted data</exception>
    /// <exception cref="InvalidOperationException">Thrown when no decryption key is available (neither keyId nor key provided, or keyId not found in KeyStore)</exception>
    /// <exception cref="NotSupportedException">Thrown when associatedData is provided but the stream format or implementation does not support associated data</exception>
    /// <exception cref="DecryptionFailedException">Thrown when decryption fails due to wrong key, corrupted data, authentication failure, mismatched associated data, or tampered data</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via ct</exception>
    Task DecryptToStreamAsync(Stream input, Stream output, string? keyId = null, byte[]? key = null, byte[]? associatedData = null, CancellationToken ct = default);

    /// <summary> Decrypts data from a file and returns the decrypted bytes. </summary>
    /// <param name="inputPath">The path to the encrypted file</param>
    /// <param name="keyId">The key identifier to use from the KeyStore. If null, uses the provided key directly.</param>
    /// <param name="key">Optional decryption key. If null and keyId is provided, uses the key from KeyStore.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Decrypted data</returns>
    /// <exception cref="ArgumentException">Thrown when inputPath is null or empty</exception>
    /// <exception cref="FileNotFoundException">Thrown when the input file does not exist</exception>
    /// <exception cref="InvalidDataException">Thrown when encrypted file format is invalid, unsupported format version, invalid chunk length, or corrupted</exception>
    /// <exception cref="EndOfStreamException">Thrown when the file stream ends unexpectedly while reading encrypted data</exception>
    /// <exception cref="InvalidOperationException">Thrown when no decryption key is available (neither keyId nor key provided, or keyId not found in KeyStore)</exception>
    /// <exception cref="DecryptionFailedException">Thrown when decryption fails due to wrong key, corrupted data, authentication failure, or tampered data</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via ct</exception>
    Task<byte[]> DecryptFromFileAsync(string inputPath, string? keyId = null, byte[]? key = null, CancellationToken ct = default);
}