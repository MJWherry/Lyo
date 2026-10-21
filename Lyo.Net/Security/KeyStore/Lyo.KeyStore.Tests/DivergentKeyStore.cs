namespace Lyo.KeyStore.Tests;

/// <summary>Stores keys, then returns different bytes from <see cref="GetKey" /> so a copy cannot verify.</summary>
internal sealed class DivergentKeyStore : IKeyStore, IKeyInventoryStore
{
    private readonly LocalKeyStore _inner = new();

    public bool CurrentVersionWasSet { get; private set; }

    public Task<IReadOnlyList<string>> GetAvailableKeyIdsAsync(CancellationToken ct = default) => _inner.GetAvailableKeyIdsAsync(ct);

    public Task<IReadOnlyList<string>> GetAvailableVersionsAsync(string keyId, CancellationToken ct = default) => _inner.GetAvailableVersionsAsync(keyId, ct);

    public byte[]? GetKey(string keyId, string? version = null)
    {
        var key = _inner.GetKey(keyId, version);
        if (key is null)
            return null;

        var copy = (byte[])key.Clone();
        copy[0] ^= 0xFF;
        return copy;
    }

    public Task<byte[]?> GetKeyAsync(string keyId, string? version = null, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(GetKey(keyId, version));
    }

    public byte[]? GetCurrentKey(string keyId) => GetKey(keyId);

    public Task<byte[]?> GetCurrentKeyAsync(string keyId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(GetCurrentKey(keyId));
    }

    public string? GetCurrentVersion(string keyId) => _inner.GetCurrentVersion(keyId);

    public Task<string?> GetCurrentVersionAsync(string keyId, CancellationToken ct = default) => _inner.GetCurrentVersionAsync(keyId, ct);

    public void AddKey(string keyId, string version, byte[] key) => _inner.AddKey(keyId, version, key);

    public Task AddKeyAsync(string keyId, string version, byte[] key, CancellationToken ct = default) => _inner.AddKeyAsync(keyId, version, key, ct);

    public void AddKeyFromString(string keyId, string version, string keyString) => _inner.AddKeyFromString(keyId, version, keyString);

    public Task AddKeyFromStringAsync(string keyId, string version, string keyString, CancellationToken ct = default)
        => _inner.AddKeyFromStringAsync(keyId, version, keyString, ct);

    public void SetCurrentVersion(string keyId, string version)
    {
        CurrentVersionWasSet = true;
        _inner.SetCurrentVersion(keyId, version);
    }

    public Task SetCurrentVersionAsync(string keyId, string version, CancellationToken ct = default)
    {
        CurrentVersionWasSet = true;
        return _inner.SetCurrentVersionAsync(keyId, version, ct);
    }

    public bool HasKey(string keyId, string? version = null) => _inner.HasKey(keyId, version);

    public Task<bool> HasKeyAsync(string keyId, string? version = null, CancellationToken ct = default) => _inner.HasKeyAsync(keyId, version, ct);

    public KeyMetadata? GetKeyMetadata(string keyId, string version) => _inner.GetKeyMetadata(keyId, version);

    public Task<KeyMetadata?> GetKeyMetadataAsync(string keyId, string version, CancellationToken ct = default) => _inner.GetKeyMetadataAsync(keyId, version, ct);

    public void SetKeyMetadata(string keyId, string version, KeyMetadata metadata) => _inner.SetKeyMetadata(keyId, version, metadata);

    public Task SetKeyMetadataAsync(string keyId, string version, KeyMetadata metadata, CancellationToken ct = default) => _inner.SetKeyMetadataAsync(keyId, version, metadata, ct);

    public byte[]? GetSaltForVersion(string keyId, string version) => _inner.GetSaltForVersion(keyId, version);

    public Task<byte[]?> GetSaltForVersionAsync(string keyId, string version, CancellationToken ct = default) => _inner.GetSaltForVersionAsync(keyId, version, ct);

    public string UpdateKey(string keyId, byte[] key) => _inner.UpdateKey(keyId, key);

    public Task<string> UpdateKeyAsync(string keyId, byte[] key, CancellationToken ct = default) => _inner.UpdateKeyAsync(keyId, key, ct);

    public string UpdateKeyFromString(string keyId, string keyString) => _inner.UpdateKeyFromString(keyId, keyString);

    public Task<string> UpdateKeyFromStringAsync(string keyId, string keyString, CancellationToken ct = default) => _inner.UpdateKeyFromStringAsync(keyId, keyString, ct);
}