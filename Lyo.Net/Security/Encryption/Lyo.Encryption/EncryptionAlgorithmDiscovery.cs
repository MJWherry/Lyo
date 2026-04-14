namespace Lyo.Encryption;

/// <summary>Resolves <see cref="EncryptionAlgorithm"/> from an <see cref="IEncryptionService"/> without referencing concrete algorithm assemblies.</summary>
public static class EncryptionAlgorithmDiscovery
{
    /// <summary>Returns the algorithm for services that derive from <see cref="EncryptionServiceBase"/>; otherwise null.</summary>
    public static EncryptionAlgorithm? FromEncryptionService(IEncryptionService? encryptionService) => encryptionService is EncryptionServiceBase b ? b.AlgorithmKind : null;
}
