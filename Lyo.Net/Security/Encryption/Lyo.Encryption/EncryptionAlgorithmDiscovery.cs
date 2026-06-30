namespace Lyo.Encryption;

/// <summary>Resolves <see cref="EncryptionAlgorithm" /> from an <see cref="IEncryptionService" /> without referencing concrete algorithm assemblies.</summary>
public static class EncryptionAlgorithmDiscovery
{
    /// <summary>Returns the algorithm for services that expose <see cref="IEncryptionAlgorithmProvider" />; otherwise null.</summary>
    public static EncryptionAlgorithm? FromEncryptionService(IEncryptionService? encryptionService)
        => encryptionService is IEncryptionAlgorithmProvider provider ? provider.AlgorithmKind : null;
}