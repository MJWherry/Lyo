using System.Text;

namespace Lyo.Encryption.TwoKey;

/// <summary>
/// Two-key encryption service that uses a Key Encryption Key (KEK) to encrypt Data Encryption Keys (DEK). This enables envelope encryption where each encryption operation
/// uses a unique DEK that is encrypted with the KEK.
/// </summary>
public interface ITwoKeyEncryptionService
{
    string FileExtension { get; }

    Encoding DefaultEncoding { get; set; }

    /// <summary>Gets the encryption algorithm used for Data Encryption Key (DEK) operations.</summary>
    EncryptionAlgorithm? DekAlgorithm { get; }

    /// <summary>Gets the encryption algorithm used for Key Encryption Key (KEK) operations.</summary>
    EncryptionAlgorithm? KekAlgorithm { get; }

    /// <summary>Gets the current key version for a specific key ID.</summary>
    /// <param name="keyId">The key identifier</param>
    /// <returns>The current key version, or null if no keys exist for this key ID</returns>
    string? GetKeyVersion(string keyId);

    /// <summary>Gets the salt used for key derivation for a specific key ID and version.</summary>
    /// <param name="keyId">The key identifier</param>
    /// <param name="version">The key version</param>
    /// <returns>The salt used for key derivation, or null if not available</returns>
    byte[]? GetSaltForVersion(string keyId, string version);

    /// <summary>Encrypts data using a randomly generated Data Encryption Key (DEK). The DEK is encrypted with the Key Encryption Key (KEK) and returned in the result.</summary>
    /// <param name="bytes">Data to encrypt. Must not be empty.</param>
    /// <param name="keyId">The key identifier to use from the KeyStore. If null, uses the provided kek directly.</param>
    /// <param name="kek">Optional Key Encryption Key. If null and keyId is provided, uses the key from KeyStore.</param>
    /// <returns>Encryption result containing encrypted data and encrypted DEK</returns>
    /// <exception cref="ArgumentOutsideRangeException">Thrown when bytes is empty (length is less than 1) or exceeds maximum allowed size</exception>
    /// <exception cref="InvalidOperationException">Thrown when neither keyId nor kek is provided, or when keyId is not found in KeyStore</exception>
    TwoKeyEncryptionResult Encrypt(byte[] bytes, string? keyId = null, byte[]? kek = null);

    /// <summary> Decrypts data using the encrypted DEK. </summary>
    /// <param name="encryptedData">The encrypted data</param>
    /// <param name="encryptedDataEncryptionKey">The encrypted Data Encryption Key (DEK)</param>
    /// <param name="keyId">The key identifier to use from the KeyStore. If null, uses the provided kek directly.</param>
    /// <param name="kek">Optional Key Encryption Key. If null and keyId is provided, uses the key from KeyStore.</param>
    /// <param name="keyVersion">Optional key version. If provided and a key store is configured, uses the key for that version.</param>
    /// <param name="salt">Optional salt used to derive the KEK. If provided, the KEK will be derived using this salt instead of the salt stored in keystore metadata.</param>
    /// <returns>Decrypted data</returns>
    /// <exception cref="ArgumentOutsideRangeException">Thrown when encryptedData or encryptedDataEncryptionKey is empty (length is less than 1)</exception>
    /// <exception cref="InvalidOperationException">Thrown when neither keyId nor kek is provided, or when keyId/keyVersion is not found in KeyStore</exception>
    /// <exception cref="DecryptionFailedException">Thrown when decryption fails due to wrong KEK, corrupted data, authentication failure, or tampered data</exception>
    byte[] Decrypt(byte[] encryptedData, byte[] encryptedDataEncryptionKey, string? keyId = null, byte[]? kek = null, string? keyVersion = null, byte[]? salt = null);

    /// <summary>Encrypts a string and returns the full encryption result (including encrypted DEK). Use DecryptString with the returned result's properties to decrypt.</summary>
    /// <param name="text">String to encrypt. Must not be empty.</param>
    /// <param name="keyId">The key identifier to use from the KeyStore. If null, uses the provided kek directly.</param>
    /// <param name="kek">Optional Key Encryption Key. If null and keyId is provided, uses the key from KeyStore.</param>
    /// <param name="encoding">Optional encoding. If null, uses DefaultEncoding.</param>
    /// <returns>Encryption result containing encrypted data and encrypted DEK</returns>
    /// <exception cref="ArgumentOutsideRangeException">Thrown when text is empty, the encoded bytes are empty (length is less than 1), or encoded bytes exceed maximum allowed size</exception>
    /// <exception cref="InvalidOperationException">Thrown when neither keyId nor kek is provided, or when keyId is not found in KeyStore</exception>
    TwoKeyEncryptionResult EncryptString(string text, string? keyId = null, byte[]? kek = null, Encoding? encoding = null);

    /// <summary> Decrypts encrypted data and returns the decrypted string. </summary>
    /// <param name="encryptedData">The encrypted data</param>
    /// <param name="encryptedDataEncryptionKey">The encrypted Data Encryption Key (DEK)</param>
    /// <param name="keyId">The key identifier to use from the KeyStore. If null, uses the provided kek directly.</param>
    /// <param name="encoding">Optional encoding. If null, uses DefaultEncoding.</param>
    /// <param name="kek">Optional Key Encryption Key. If null and keyId is provided, uses the key from KeyStore.</param>
    /// <param name="keyVersion">Optional key version. If provided and a key store is configured, uses the key for that version.</param>
    /// <param name="salt">Optional salt used to derive the KEK. If provided, the KEK will be derived using this salt instead of the salt stored in keystore metadata.</param>
    /// <returns>Decrypted string</returns>
    /// <exception cref="ArgumentOutsideRangeException">Thrown when encryptedData or encryptedDataEncryptionKey is empty (length is less than 1)</exception>
    /// <exception cref="InvalidOperationException">Thrown when neither keyId nor kek is provided, or when keyId/keyVersion is not found in KeyStore</exception>
    /// <exception cref="DecryptionFailedException">Thrown when decryption fails due to wrong KEK, corrupted data, authentication failure, or tampered data</exception>
    string DecryptString(
        byte[] encryptedData,
        byte[] encryptedDataEncryptionKey,
        string? keyId = null,
        Encoding? encoding = null,
        byte[]? kek = null,
        string? keyVersion = null,
        byte[]? salt = null);

    /// <summary>Encrypts data from a stream and returns the encryption result.</summary>
    /// <param name="input">The input stream containing data to encrypt</param>
    /// <param name="keyId">The key identifier to use from the KeyStore. If null, uses the provided kek directly.</param>
    /// <param name="kek">Optional Key Encryption Key. If null and keyId is provided, uses the key from KeyStore.</param>
    /// <param name="chunkSize">Size of chunks to read and encrypt. Default is 1MB.</param>
    /// <returns>Encryption result containing encrypted data and encrypted DEK</returns>
    /// <exception cref="ArgumentException">Thrown when chunkSize is invalid</exception>
    /// <exception cref="InvalidOperationException">Thrown when neither keyId nor kek is provided, or when keyId is not found in KeyStore</exception>
    Task<TwoKeyEncryptionResult> EncryptStreamAsync(Stream input, string? keyId = null, byte[]? kek = null, int chunkSize = 1024 * 1024);

    /// <summary>Decrypts data from an encryption result and writes it to an output stream.</summary>
    /// <param name="result">The encryption result containing encrypted data and encrypted DEK</param>
    /// <param name="output">The output stream to write decrypted data to</param>
    /// <param name="keyId">The key identifier to use from the KeyStore. If null, uses the provided kek directly or the keyId from result.</param>
    /// <param name="kek">Optional Key Encryption Key. If null and keyId is provided, uses the key from KeyStore.</param>
    /// <exception cref="InvalidDataException">Thrown when encrypted data format is invalid, invalid chunk length, or corrupted</exception>
    /// <exception cref="InvalidOperationException">Thrown when neither keyId nor kek is provided and result.KeyId is empty, or when keyId/keyVersion is not found in KeyStore</exception>
    /// <exception cref="DecryptionFailedException">Thrown when decryption fails due to wrong KEK, corrupted data, authentication failure, or tampered data</exception>
    Task DecryptStreamAsync(TwoKeyEncryptionResult result, Stream output, string? keyId = null, byte[]? kek = null);

    /// <summary>Encrypts data from an input stream and writes it to an output stream. The encrypted DEK is written first, followed by the encrypted data chunks.</summary>
    /// <param name="input">The input stream containing data to encrypt</param>
    /// <param name="output">The output stream to write encrypted data to</param>
    /// <param name="keyId">The key identifier to use from the KeyStore. If null, uses the provided kek directly.</param>
    /// <param name="kek">Optional Key Encryption Key. If null and keyId is provided, uses the key from KeyStore.</param>
    /// <param name="chunkSize">Size of chunks to read and encrypt. Default is 1MB.</param>
    /// <param name="ct">Cancellation token</param>
    /// <exception cref="ArgumentException">Thrown when chunkSize is invalid</exception>
    /// <exception cref="InvalidOperationException">Thrown when neither keyId nor kek is provided, or when keyId is not found in KeyStore</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via ct</exception>
    Task EncryptToStreamAsync(Stream input, Stream output, string? keyId = null, byte[]? kek = null, int chunkSize = 1024 * 1024, CancellationToken ct = default);

    /// <summary>Decrypts data from an input stream and writes it to an output stream. Reads the encrypted DEK first, then decrypts the encrypted data chunks.</summary>
    /// <param name="input">The input stream containing encrypted data (encrypted DEK + encrypted data)</param>
    /// <param name="output">The output stream to write decrypted data to</param>
    /// <param name="keyId">The key identifier to use from the KeyStore. If null, uses the provided kek directly.</param>
    /// <param name="kek">Optional Key Encryption Key. If null and keyId is provided, uses the key from KeyStore.</param>
    /// <param name="ct">Cancellation token</param>
    /// <exception cref="InvalidDataException">Thrown when encrypted stream format is invalid, unsupported format version, invalid chunk length, or corrupted</exception>
    /// <exception cref="EndOfStreamException">Thrown when the stream ends unexpectedly while reading encrypted data</exception>
    /// <exception cref="NotSupportedException">Thrown when the stream format version is not supported</exception>
    /// <exception cref="InvalidOperationException">Thrown when neither keyId nor kek is provided, or when keyId/keyVersion is not found in KeyStore</exception>
    /// <exception cref="DecryptionFailedException">Thrown when decryption fails due to wrong KEK, corrupted data, authentication failure, or tampered data</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via ct</exception>
    Task DecryptToStreamAsync(Stream input, Stream output, string? keyId = null, byte[]? kek = null, CancellationToken ct = default);

    /// <summary> Encrypts data and writes it to a file. </summary>
    /// <param name="data">The data to encrypt</param>
    /// <param name="outputPath">The path to write the encrypted file to</param>
    /// <param name="keyId">The key identifier to use from the KeyStore. If null, uses the provided kek directly.</param>
    /// <param name="kek">Optional Key Encryption Key. If null and keyId is provided, uses the key from KeyStore.</param>
    /// <param name="ct">Cancellation token</param>
    /// <exception cref="ArgumentException">Thrown when outputPath is null or empty</exception>
    /// <exception cref="ArgumentOutsideRangeException">Thrown when data is empty (length is less than 1) or exceeds maximum allowed size</exception>
    /// <exception cref="InvalidOperationException">Thrown when neither keyId nor kek is provided, or when keyId is not found in KeyStore</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via ct</exception>
    Task EncryptToFileAsync(byte[] data, string outputPath, string? keyId = null, byte[]? kek = null, CancellationToken ct = default);

    /// <summary> Encrypts data from a stream and writes it to a file. </summary>
    /// <param name="input">The input stream containing data to encrypt</param>
    /// <param name="outputPath">The path to write the encrypted file to</param>
    /// <param name="keyId">The key identifier to use from the KeyStore. If null, uses the provided kek directly.</param>
    /// <param name="kek">Optional Key Encryption Key. If null and keyId is provided, uses the key from KeyStore.</param>
    /// <param name="chunkSize">Size of chunks to read and encrypt. Default is 1MB.</param>
    /// <param name="ct">Cancellation token</param>
    /// <exception cref="ArgumentException">Thrown when outputPath is null or empty, or chunkSize is invalid</exception>
    /// <exception cref="InvalidOperationException">Thrown when neither keyId nor kek is provided, or when keyId is not found in KeyStore</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via ct</exception>
    Task EncryptToFileAsync(Stream input, string outputPath, string? keyId = null, byte[]? kek = null, int chunkSize = 1024 * 1024, CancellationToken ct = default);

    /// <summary> Decrypts data from a file and returns the decrypted bytes. </summary>
    /// <param name="inputPath">The path to the encrypted file</param>
    /// <param name="keyId">The key identifier to use from the KeyStore. If null, uses the provided kek directly.</param>
    /// <param name="kek">Optional Key Encryption Key. If null and keyId is provided, uses the key from KeyStore.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Decrypted data</returns>
    /// <exception cref="ArgumentException">Thrown when inputPath is null or empty</exception>
    /// <exception cref="FileNotFoundException">Thrown when the input file does not exist</exception>
    /// <exception cref="InvalidDataException">Thrown when encrypted file format is invalid, unsupported format version, invalid chunk length, or corrupted</exception>
    /// <exception cref="EndOfStreamException">Thrown when the file stream ends unexpectedly while reading encrypted data</exception>
    /// <exception cref="NotSupportedException">Thrown when the stream format version is not supported</exception>
    /// <exception cref="InvalidOperationException">Thrown when neither keyId nor kek is provided, or when keyId/keyVersion is not found in KeyStore</exception>
    /// <exception cref="DecryptionFailedException">Thrown when decryption fails due to wrong KEK, corrupted data, authentication failure, or tampered data</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via ct</exception>
    Task<byte[]> DecryptFromFileAsync(string inputPath, string? keyId = null, byte[]? kek = null, CancellationToken ct = default);

    /// <summary>
    /// Re-encrypts a Data Encryption Key (DEK) that was encrypted with one KEK using a different KEK. This is useful for key rotation scenarios (same keyId, different version)
    /// or key migration (different keyId).
    /// </summary>
    /// <param name="encryptedDek">The DEK encrypted with the source KEK</param>
    /// <param name="sourceKeyId">The key identifier that was used to encrypt the DEK</param>
    /// <param name="sourceKeyVersion">The KEK version that was used to encrypt the DEK</param>
    /// <param name="targetKeyId">The key identifier to use for re-encryption. If null, uses sourceKeyId (same key, different version).</param>
    /// <param name="targetKeyVersion">The KEK version to use for re-encryption. If null, uses the current version of targetKeyId.</param>
    /// <returns>The DEK re-encrypted with the target KEK</returns>
    /// <exception cref="ArgumentException">Thrown when sourceKeyId or sourceKeyVersion is null or empty</exception>
    /// <exception cref="InvalidOperationException">Thrown when the source or target KEK is not available in the keystore, or when source and target are the same</exception>
    /// <exception cref="DecryptionFailedException">Thrown when decryption of the DEK fails due to wrong source KEK or corrupted data</exception>
    byte[] ReEncryptDek(byte[] encryptedDek, string sourceKeyId, string sourceKeyVersion, string? targetKeyId = null, string? targetKeyVersion = null);

    /// <summary>
    /// Re-encrypts a Data Encryption Key (DEK) that was encrypted with one KEK using a different KEK asynchronously. This is useful for key rotation scenarios (same keyId,
    /// different version) or key migration (different keyId).
    /// </summary>
    /// <param name="encryptedDek">The DEK encrypted with the source KEK</param>
    /// <param name="sourceKeyId">The key identifier that was used to encrypt the DEK</param>
    /// <param name="sourceKeyVersion">The KEK version that was used to encrypt the DEK</param>
    /// <param name="targetKeyId">The key identifier to use for re-encryption. If null, uses sourceKeyId (same key, different version).</param>
    /// <param name="targetKeyVersion">The KEK version to use for re-encryption. If null, uses the current version of targetKeyId.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The DEK re-encrypted with the target KEK</returns>
    /// <exception cref="ArgumentException">Thrown when sourceKeyId or sourceKeyVersion is null or empty</exception>
    /// <exception cref="InvalidOperationException">Thrown when the source or target KEK is not available in the keystore, or when source and target are the same</exception>
    /// <exception cref="DecryptionFailedException">Thrown when decryption of the DEK fails due to wrong source KEK or corrupted data</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via ct</exception>
    Task<byte[]> ReEncryptDekAsync(
        byte[] encryptedDek,
        string sourceKeyId,
        string sourceKeyVersion,
        string? targetKeyId = null,
        string? targetKeyVersion = null,
        CancellationToken ct = default);
}