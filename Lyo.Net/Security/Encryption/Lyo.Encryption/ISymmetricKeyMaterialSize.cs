namespace Lyo.Encryption;

/// <summary>Symmetric services whose DEK / raw key material has a fixed width (AES-GCM, ChaCha20-Poly1305, and similar).</summary>
public interface ISymmetricKeyMaterialSize
{
    /// <summary>Byte length of the raw symmetric key (DEK) for this service's configured key size.</summary>
    int RequiredKeyBytes { get; }
}