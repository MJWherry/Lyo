using Lyo.Keystore;

namespace Lyo.Encryption.Tests;

public class KeyMetadataTests
{
    [Fact]
    public void KeyMetadata_Default_CreatedAtIsSet()
    {
        var metadata = new KeyMetadata();
        Assert.True(metadata.CreatedAt <= DateTime.UtcNow);
        Assert.True(metadata.CreatedAt >= DateTime.UtcNow.AddSeconds(-1));
    }

    [Fact]
    public void KeyMetadata_WithExpiration_IsExpiredWorks()
    {
        var expiredMetadata = new KeyMetadata { ExpiresAt = DateTime.UtcNow.AddDays(-1) };
        var validMetadata = new KeyMetadata { ExpiresAt = DateTime.UtcNow.AddDays(1) };
        Assert.True(expiredMetadata.IsExpired);
        Assert.False(validMetadata.IsExpired);
    }

    [Fact]
    public void KeyMetadata_NoExpiration_IsValid()
    {
        var metadata = new KeyMetadata { ExpiresAt = null };
        Assert.False(metadata.IsExpired);
        Assert.True(metadata.IsValid);
    }

    [Fact]
    public void KeyMetadata_WithAdditionalData_StoresData()
    {
        var metadata = new KeyMetadata { AdditionalData = new() { { "Environment", "Production" }, { "Region", "US-East" } } };
        Assert.NotNull(metadata.AdditionalData);
        Assert.Equal("Production", metadata.AdditionalData["Environment"]);
        Assert.Equal("US-East", metadata.AdditionalData["Region"]);
    }

    [Fact]
    public void KeyStore_SetKeyMetadata_StoresMetadata()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        var key = Enumerable.Range(1, 32).Select(x => (byte)x).ToArray();
        keyStore.AddKey(keyId, "1", key);
        var metadata = new KeyMetadata { Algorithm = "AES-256", ExpiresAt = DateTime.UtcNow.AddDays(30), AdditionalData = new() { { "Source", "Manual" } } };
        keyStore.SetKeyMetadata(keyId, "1", metadata);
        var retrieved = keyStore.GetKeyMetadata(keyId, "1");
        Assert.NotNull(retrieved);
        Assert.Equal("AES-256", retrieved.Algorithm);
        Assert.NotNull(retrieved.ExpiresAt);
        Assert.True(retrieved.ExpiresAt!.Value > DateTime.UtcNow);
        Assert.Equal("Manual", retrieved.AdditionalData!["Source"]);
    }

    [Fact]
    public void KeyStore_GetKeyMetadata_NonExistent_ReturnsNull()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        var metadata = keyStore.GetKeyMetadata(keyId, "999");
        Assert.Null(metadata);
    }

    [Fact]
    public void KeyStore_GetKeyMetadata_NoMetadataSet_ReturnsDefaultMetadata()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        var key = Enumerable.Range(1, 32).Select(x => (byte)x).ToArray();
        keyStore.AddKey(keyId, "1", key);
        var metadata = keyStore.GetKeyMetadata(keyId, "1");
        Assert.NotNull(metadata);
        Assert.True(metadata.CreatedAt <= DateTime.UtcNow);
        Assert.Null(metadata.ExpiresAt);
    }

    [Fact]
    public void KeyStore_SetKeyMetadata_NonExistentKey_Throws()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        var metadata = new KeyMetadata();
        Assert.Throws<InvalidOperationException>(() => keyStore.SetKeyMetadata(keyId, "999", metadata));
    }

    [Fact]
    public void KeyStore_SetKeyMetadata_NullMetadata_Throws()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        var key = Enumerable.Range(1, 32).Select(x => (byte)x).ToArray();
        keyStore.AddKey(keyId, "1", key);
        Assert.Throws<ArgumentNullException>(() => keyStore.SetKeyMetadata(keyId, "1", null!));
    }

    [Fact]
    public async Task KeyStore_GetKeyMetadataAsync_ReturnsMetadata()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        var key = Enumerable.Range(1, 32).Select(x => (byte)x).ToArray();
        await keyStore.AddKeyAsync(keyId, "1", key, TestContext.Current.CancellationToken).ConfigureAwait(false);
        var metadata = new KeyMetadata { Algorithm = "AES-256" };
        await keyStore.SetKeyMetadataAsync(keyId, "1", metadata, TestContext.Current.CancellationToken).ConfigureAwait(false);
        var retrieved = await keyStore.GetKeyMetadataAsync(keyId, "1", TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.NotNull(retrieved);
        Assert.Equal("AES-256", retrieved.Algorithm);
    }

    [Fact]
    public async Task KeyStore_SetKeyMetadataAsync_StoresMetadata()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        var key = Enumerable.Range(1, 32).Select(x => (byte)x).ToArray();
        await keyStore.AddKeyAsync(keyId, "1", key, TestContext.Current.CancellationToken).ConfigureAwait(false);
        var metadata = new KeyMetadata { Algorithm = "ChaCha20-Poly1305" };
        await keyStore.SetKeyMetadataAsync(keyId, "1", metadata, TestContext.Current.CancellationToken).ConfigureAwait(false);
        var retrieved = await keyStore.GetKeyMetadataAsync(keyId, "1", TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal("ChaCha20-Poly1305", retrieved!.Algorithm);
    }

    [Fact]
    public void KeyStore_RemoveKey_RemovesMetadata()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        var key1 = Enumerable.Range(1, 32).Select(x => (byte)x).ToArray();
        var key2 = Enumerable.Range(32, 64).Select(x => (byte)x).ToArray();
        keyStore.AddKey(keyId, "1", key1);
        keyStore.AddKey(keyId, "2", key2);
        keyStore.SetCurrentVersion(keyId, "1");
        keyStore.SetKeyMetadata(keyId, "2", new() { Algorithm = "Test" });
        keyStore.RemoveKey(keyId, "2");
        Assert.Null(keyStore.GetKeyMetadata(keyId, "2"));
    }
}