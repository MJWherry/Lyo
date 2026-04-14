using Lyo.Api.Client;
using Lyo.Keystore;

namespace Lyo.Gateway.Services;

public sealed class TestApiKeyStore : IKeyStore, IKeyInventoryStore
{
    private readonly IApiClient _apiClient;
    private readonly string _routePrefix;

    public TestApiKeyStore(IApiClient apiClient, string routePrefix)
    {
        _apiClient = apiClient;
        _routePrefix = routePrefix.Trim('/');
    }

    public async Task<IReadOnlyList<string>> GetAvailableKeyIdsAsync(CancellationToken ct = default)
        => await _apiClient.GetAsAsync<List<string>>(BuildUri("keys/available"), ct: ct).ConfigureAwait(false) ?? [];

    public async Task<IReadOnlyList<string>> GetAvailableVersionsAsync(string keyId, CancellationToken ct = default)
        => await _apiClient.GetAsAsync<List<string>>(BuildUri($"keys/{Uri.EscapeDataString(keyId)}/versions"), ct: ct).ConfigureAwait(false) ?? [];

    public byte[]? GetKey(string keyId, string? version = null) => GetKeyAsync(keyId, version).GetAwaiter().GetResult();

    public Task<byte[]?> GetCurrentKeyAsync(string keyId, CancellationToken ct = default) => GetKeyAsync(keyId, null, ct);

    public byte[]? GetCurrentKey(string keyId) => GetCurrentKeyAsync(keyId).GetAwaiter().GetResult();

    public async Task<byte[]?> GetKeyAsync(string keyId, string? version = null, CancellationToken ct = default)
        => await _apiClient.GetAsAsync<object, byte[]>(BuildUri($"keys/{Uri.EscapeDataString(keyId)}/raw"), new { version }, ct: ct).ConfigureAwait(false);

    public string? GetCurrentVersion(string keyId) => GetCurrentVersionAsync(keyId).GetAwaiter().GetResult();

    public async Task<string?> GetCurrentVersionAsync(string keyId, CancellationToken ct = default)
        => await _apiClient.GetAsAsync<string>(BuildUri($"keys/{Uri.EscapeDataString(keyId)}/current-version"), ct: ct).ConfigureAwait(false);

    public void AddKey(string keyId, string version, byte[] key) => AddKeyAsync(keyId, version, key).GetAwaiter().GetResult();

    public async Task AddKeyAsync(string keyId, string version, byte[] key, CancellationToken ct = default)
        => _ = await _apiClient.PostAsAsync<KeyStoreAddKeyRequest, bool>(BuildUri("keys/add"), new(keyId, version, key), ct: ct).ConfigureAwait(false);

    public void AddKeyFromString(string keyId, string version, string keyString) => AddKeyFromStringAsync(keyId, version, keyString).GetAwaiter().GetResult();

    public async Task AddKeyFromStringAsync(string keyId, string version, string keyString, CancellationToken ct = default)
        => _ = await _apiClient.PostAsAsync<KeyStoreAddKeyStringRequest, bool>(BuildUri("keys/add-string"), new(keyId, version, keyString), ct: ct).ConfigureAwait(false);

    public void SetCurrentVersion(string keyId, string version) => SetCurrentVersionAsync(keyId, version).GetAwaiter().GetResult();

    public async Task SetCurrentVersionAsync(string keyId, string version, CancellationToken ct = default)
        => _ = await _apiClient.PostAsAsync<KeyStoreSetCurrentVersionRequest, bool>(BuildUri("keys/set-current"), new(keyId, version), ct: ct).ConfigureAwait(false);

    public bool HasKey(string keyId, string? version = null) => HasKeyAsync(keyId, version).GetAwaiter().GetResult();

    public async Task<bool> HasKeyAsync(string keyId, string? version = null, CancellationToken ct = default)
        => await _apiClient.GetAsAsync<object, bool>(BuildUri($"keys/{Uri.EscapeDataString(keyId)}/exists"), new { version }, ct: ct).ConfigureAwait(false);

    public KeyMetadata? GetKeyMetadata(string keyId, string version) => GetKeyMetadataAsync(keyId, version).GetAwaiter().GetResult();

    public async Task<KeyMetadata?> GetKeyMetadataAsync(string keyId, string version, CancellationToken ct = default)
        => await _apiClient.GetAsAsync<KeyMetadata>(BuildUri($"keys/{Uri.EscapeDataString(keyId)}/metadata/{Uri.EscapeDataString(version)}"), ct: ct).ConfigureAwait(false);

    public void SetKeyMetadata(string keyId, string version, KeyMetadata metadata) => SetKeyMetadataAsync(keyId, version, metadata).GetAwaiter().GetResult();

    public async Task SetKeyMetadataAsync(string keyId, string version, KeyMetadata metadata, CancellationToken ct = default)
        => _ = await _apiClient.PutAsAsync<KeyMetadata, bool>(BuildUri($"keys/{Uri.EscapeDataString(keyId)}/metadata/{Uri.EscapeDataString(version)}"), metadata, ct: ct)
            .ConfigureAwait(false);

    public byte[]? GetSaltForVersion(string keyId, string version) => GetSaltForVersionAsync(keyId, version).GetAwaiter().GetResult();

    public async Task<byte[]?> GetSaltForVersionAsync(string keyId, string version, CancellationToken ct = default)
        => await _apiClient.GetAsAsync<byte[]>(BuildUri($"keys/{Uri.EscapeDataString(keyId)}/salt/{Uri.EscapeDataString(version)}"), ct: ct).ConfigureAwait(false);

    public string UpdateKey(string keyId, byte[] key) => UpdateKeyAsync(keyId, key).GetAwaiter().GetResult();

    public async Task<string> UpdateKeyAsync(string keyId, byte[] key, CancellationToken ct = default)
        => await _apiClient.PostAsAsync<KeyStoreUpdateKeyRequest, string>(BuildUri("keys/update"), new(keyId, key), ct: ct).ConfigureAwait(false);

    public string UpdateKeyFromString(string keyId, string keyString) => UpdateKeyFromStringAsync(keyId, keyString).GetAwaiter().GetResult();

    public async Task<string> UpdateKeyFromStringAsync(string keyId, string keyString, CancellationToken ct = default)
        => await _apiClient.PostAsAsync<KeyStoreUpdateKeyStringRequest, string>(BuildUri("keys/update-string"), new(keyId, keyString), ct: ct).ConfigureAwait(false);

    private string BuildUri(string relativePath) => $"{_routePrefix}/{relativePath}";
}