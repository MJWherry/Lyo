namespace Lyo.Encryption;

/// <summary>
/// Surfaces the <see cref="EncryptionAlgorithm" /> a service implements so callers can name it without referencing concrete algorithm packages or assuming a particular
/// base type. <see cref="EncryptionServiceBase" /> implements this; so do standalone services (hybrid AES-GCM-RSA) that do not inherit from it.
/// </summary>
public interface IEncryptionAlgorithmProvider
{
    /// <summary>Algorithm this service implements; same value as the stream-format algorithm byte.</summary>
    EncryptionAlgorithm AlgorithmKind { get; }
}