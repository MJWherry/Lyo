namespace Lyo.Encryption;

/// <summary>
/// Exposes the <see cref="EncryptionAlgorithm" /> a service implements so callers can identify it without referencing concrete algorithm assemblies or assuming a particular
/// base class. Implemented by <see cref="EncryptionServiceBase" /> and by standalone services such as the hybrid AES-GCM-RSA service that do not derive from it.
/// </summary>
public interface IEncryptionAlgorithmProvider
{
    /// <summary>The algorithm this service implements; matches the stream format algorithm byte.</summary>
    EncryptionAlgorithm AlgorithmKind { get; }
}