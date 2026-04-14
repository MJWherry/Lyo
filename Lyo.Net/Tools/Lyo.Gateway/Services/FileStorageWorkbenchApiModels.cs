using Lyo.Keystore;

namespace Lyo.Gateway.Services;

public sealed record FileStorageSaveRequest(
    byte[] Data,
    string? OriginalFileName = null,
    bool Compress = false,
    bool Encrypt = false,
    string? KeyId = null,
    string? PathPrefix = null,
    int? ChunkSize = null,
    string? ContentType = null,
    string? TenantId = null);

public sealed record PresignedReadResponse(string Url);

public sealed record FileStorageMigrateDeksRequest(
    string SourceKeyId,
    string? SourceKeyVersion = null,
    string? TargetKeyId = null,
    string? TargetKeyVersion = null,
    int BatchSize = 100);

public sealed record FileStorageRotateDeksRequest(IReadOnlyList<Guid> FileIds, string? TargetKeyId = null, string? TargetKeyVersion = null, int BatchSize = 100);

public sealed record KeyStoreAddKeyRequest(string KeyId, string Version, byte[] Key);

public sealed record KeyStoreAddKeyStringRequest(string KeyId, string Version, string KeyString);

public sealed record KeyStoreUpdateKeyRequest(string KeyId, byte[] Key);

public sealed record KeyStoreUpdateKeyStringRequest(string KeyId, string KeyString);

public sealed record KeyStoreSetCurrentVersionRequest(string KeyId, string Version);

public sealed record FileStorageWorkbenchApiKeyRecord(string KeyId, string Version, bool IsCurrent, KeyMetadata? Metadata, int FileCount);