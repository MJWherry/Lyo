namespace Lyo.Encryption;

/// <summary>Maps an <see cref="IEncryptionService" /> to <see cref="EncryptionAlgorithm" /> without taking a dependency on concrete algorithm packages.</summary>
public static class EncryptionAlgorithmDiscovery
{
    /// <summary>Algorithm for types implementing <see cref="IEncryptionAlgorithmProvider" />; otherwise null.</summary>
    public static EncryptionAlgorithm? FromEncryptionService(IEncryptionService? encryptionService)
        => encryptionService is IEncryptionAlgorithmProvider provider ? provider.AlgorithmKind : null;
}