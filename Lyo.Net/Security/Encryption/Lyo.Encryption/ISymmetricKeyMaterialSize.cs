namespace Lyo.Encryption;

/// <summary>Symmetric encryption services that use a fixed DEK/plaintext key material length (e.g. AES-GCM, ChaCha20-Poly1305).</summary>
public interface ISymmetricKeyMaterialSize
{
    /// <summary>Length in bytes of the raw symmetric key material (DEK) for this service configuration.</summary>
    int RequiredKeyBytes { get; }
}
