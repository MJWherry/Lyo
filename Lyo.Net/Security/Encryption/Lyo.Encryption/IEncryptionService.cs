using System.Text;

namespace Lyo.Encryption;

/// <summary>
/// Core interface for encryption services. Defines the essential contract that all encryption services must implement. For additional helper methods (string, file
/// operations), see EncryptionServiceBase.
/// </summary>
public interface IEncryptionService
{
    /// <summary> The file extension used for encrypted files (e.g., ".ag", ".rsa", ".chacha"). </summary>
    string FileExtension { get; }

    /// <summary>The default encoding used for string operations.</summary>
    Encoding DefaultEncoding { get; set; }

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
    /// Thrown when plaintext is empty (length is less than MinInputSize) or exceeds maximum allowed size (MaxInputSize), or key size is invalid.
    /// </exception>
    /// <exception cref="InvalidOperationException">Thrown when no encryption key is available (neither keyId nor key provided, or keyId not found in KeyStore).</exception>
    byte[] Encrypt(ReadOnlySpan<byte> plaintext, string? keyId = null, byte[]? key = null) => Encrypt(plaintext.ToArray(), keyId, key);

    /// <summary> Decrypts the provided encrypted byte array. </summary>
    /// <param name="encryptedBytes">The encrypted data to decrypt</param>
    /// <param name="keyId">The key identifier to use from the KeyStore. If null, uses the provided key directly.</param>
    /// <param name="key">Optional decryption key. If null and keyId is provided, uses the key from KeyStore.</param>
    /// <returns>Decrypted data</returns>
    /// <exception cref="ArgumentOutsideRangeException">Thrown when encryptedBytes is empty (length is less than 1) or too small (below minimum required size)</exception>
    /// <exception cref="InvalidDataException">Thrown when encrypted data format is invalid, unsupported format version, or corrupted</exception>
    /// <exception cref="InvalidOperationException">Thrown when no decryption key is available (neither keyId nor key provided, or keyId not found in KeyStore)</exception>
    /// <exception cref="DecryptionFailedException">Thrown when decryption fails due to wrong key, corrupted data, authentication failure, or tampered data</exception>
    byte[] Decrypt(byte[] encryptedBytes, string? keyId = null, byte[]? key = null);

    /// <summary>Decrypts a contiguous region of <paramref name="buffer"/>. Implementations may override to decrypt without copying the slice to a new array.</summary>
    /// <param name="buffer">Buffer containing encrypted data.</param>
    /// <param name="offset">Start index of the encrypted slice in <paramref name="buffer"/>.</param>
    /// <param name="count">Length of the encrypted slice in bytes.</param>
    /// <param name="keyId">The key identifier to use from the KeyStore. If null, uses the provided key directly.</param>
    /// <param name="key">Optional decryption key. If null and keyId is provided, uses the key from KeyStore.</param>
    /// <returns>Decrypted data.</returns>
    /// <exception cref="ArgumentOutsideRangeException">Thrown when the slice is empty or too small (below minimum required size), or exceeds maximum allowed size.</exception>
    /// <exception cref="InvalidDataException">Thrown when encrypted data format is invalid, unsupported format version, or corrupted.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no decryption key is available (neither keyId nor key provided, or keyId not found in KeyStore).</exception>
    /// <exception cref="DecryptionFailedException">Thrown when decryption fails due to wrong key, corrupted data, authentication failure, or tampered data.</exception>
    byte[] Decrypt(byte[] buffer, int offset, int count, string? keyId = null, byte[]? key = null)
    {
        var chunk = new byte[count];
        Array.Copy(buffer, offset, chunk, 0, count);
        return Decrypt(chunk, keyId, key);
    }

    /// <summary> Encrypts a string and returns the encrypted data as bytes.</summary>
    /// <param name="text">The text to encrypt. Must not be empty.</param>
    /// <param name="keyId">The key identifier to use from the KeyStore. If null, uses the provided key directly.</param>
    /// <param name="key">Optional encryption key. If null and keyId is provided, uses the key from KeyStore.</param>
    /// <param name="encoding">Optional encoding. If null, uses DefaultEncoding.</param>
    /// <returns>Encrypted data</returns>
    /// <exception cref="ArgumentOutsideRangeException">
    /// Thrown when text is empty, the encoded bytes are empty (length is less than MinInputSize), or encoded bytes exceed maximum allowed
    /// size (MaxInputSize), or key size is invalid
    /// </exception>
    /// <exception cref="InvalidOperationException">Thrown when no encryption key is available (neither keyId nor key provided, or keyId not found in KeyStore)</exception>
    byte[] EncryptString(string text, string? keyId = null, byte[]? key = null, Encoding? encoding = null);

    /// <summary> Decrypts encrypted bytes and returns the decrypted string. </summary>
    /// <param name="encryptedBytes">The encrypted data to decrypt</param>
    /// <param name="keyId">The key identifier to use from the KeyStore. If null, uses the provided key directly.</param>
    /// <param name="key">Optional decryption key. If null and keyId is provided, uses the key from KeyStore.</param>
    /// <param name="encoding">Optional encoding. If null, uses DefaultEncoding.</param>
    /// <returns>Decrypted string</returns>
    /// <exception cref="ArgumentOutsideRangeException">Thrown when encryptedBytes is empty (length is less than 1) or too small (below minimum required size)</exception>
    /// <exception cref="InvalidDataException">Thrown when encrypted data format is invalid, unsupported format version, or corrupted</exception>
    /// <exception cref="InvalidOperationException">Thrown when no decryption key is available (neither keyId nor key provided, or keyId not found in KeyStore)</exception>
    /// <exception cref="DecryptionFailedException">Thrown when decryption fails due to wrong key, corrupted data, authentication failure, or tampered data</exception>
    string DecryptString(byte[] encryptedBytes, string? keyId = null, byte[]? key = null, Encoding? encoding = null);

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

    /// <summary> Decrypts data from an input stream and writes it to an output stream. </summary>
    /// <param name="input">The input stream containing encrypted data</param>
    /// <param name="output">The output stream to write decrypted data to</param>
    /// <param name="keyId">The key identifier to use from the KeyStore. If null, uses the provided key directly.</param>
    /// <param name="key">Optional decryption key. If null and keyId is provided, uses the key from KeyStore.</param>
    /// <param name="ct">Cancellation token</param>
    /// <exception cref="InvalidDataException">Thrown when encrypted stream format is invalid, unsupported format version, invalid chunk length, or corrupted</exception>
    /// <exception cref="EndOfStreamException">Thrown when the stream ends unexpectedly while reading encrypted data</exception>
    /// <exception cref="InvalidOperationException">Thrown when no decryption key is available (neither keyId nor key provided, or keyId not found in KeyStore)</exception>
    /// <exception cref="DecryptionFailedException">Thrown when decryption fails due to wrong key, corrupted data, authentication failure, or tampered data</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via ct</exception>
    Task DecryptToStreamAsync(Stream input, Stream output, string? keyId = null, byte[]? key = null, CancellationToken ct = default);

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