namespace Lyo.Encryption;

/// <summary>
/// Core interface for encryption services. Composes <see cref="IEncryptor" /> and <see cref="IDecryptor" /> into the full bidirectional contract that all encryption services
/// must implement. For additional helper methods (string, file operations), see EncryptionServiceBase.
/// </summary>
/// <remarks>
/// <para>
/// Pass <c>keyId</c> to resolve key material from <see cref="Lyo.Keystore.IKeyStore" /> when the implementation is constructed with a store; pass <c>key</c> for inline
/// symmetric keys. Streaming helpers write a small versioned header then length-prefixed ciphertext chunks — see <see cref="EncryptionServiceBase" /> source for the exact frame
/// layout.
/// </para>
/// </remarks>
public interface IEncryptionService : IEncryptor, IDecryptor;