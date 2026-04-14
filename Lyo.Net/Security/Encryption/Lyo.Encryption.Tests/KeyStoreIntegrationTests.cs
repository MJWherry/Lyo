using Lyo.Keystore;

namespace Lyo.Encryption.Tests;

public class KeyStoreIntegrationTests
{
    [Fact]
    public void KeyStore_KeyRotation_Works()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        var key1 = SecureKeyGenerator.GenerateKey();
        var key2 = SecureKeyGenerator.GenerateKey();

        // Add version 1
        keyStore.AddKey(keyId, "1", key1);
        keyStore.SetCurrentVersion(keyId, "1");
        Assert.Equal(key1, keyStore.GetCurrentKey(keyId));

        // Add version 2
        keyStore.AddKey(keyId, "2", key2);
        keyStore.SetCurrentVersion(keyId, "2");
        Assert.Equal(key2, keyStore.GetCurrentKey(keyId));

        // Can still access version 1
        Assert.Equal(key1, keyStore.GetKey(keyId, "1"));
        Assert.Equal(key2, keyStore.GetKey(keyId, "2"));
    }

    [Fact]
    public void KeyStore_MultipleVersions_AllAccessible()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        var keys = new byte[5][];
        for (var i = 0; i < 5; i++) {
            keys[i] = SecureKeyGenerator.GenerateKey();
            keyStore.AddKey(keyId, (i + 1).ToString(), keys[i]);
        }

        // Verify all keys are accessible
        for (var i = 0; i < 5; i++) {
            var retrieved = keyStore.GetKey(keyId, (i + 1).ToString());
            Assert.Equal(keys[i], retrieved);
        }
    }

    [Fact]
    public void KeyStore_AddKeyFromString_ConsistentDerivation()
    {
        const string keyId = "test-key";
        var keyStore1 = new LocalKeyStore();
        var keyStore2 = new LocalKeyStore();
        keyStore1.UpdateKeyFromString(keyId, "test-password");
        keyStore2.UpdateKeyFromString(keyId, "test-password");
        var key1 = keyStore1.GetKey(keyId, keyStore1.GetCurrentVersion(keyId));
        var key2 = keyStore2.GetKey(keyId, keyStore2.GetCurrentVersion(keyId));

        // Should derive the same key from the same string
        Assert.Equal(key1, key2);
    }

    [Fact]
    public void KeyStore_MetadataWithKeyRotation_TracksVersions()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        var key1 = SecureKeyGenerator.GenerateKey();
        var key2 = SecureKeyGenerator.GenerateKey();
        keyStore.AddKey(keyId, "1", key1);
        keyStore.SetKeyMetadata(keyId, "1", new() { Algorithm = "AES-256", CreatedAt = DateTime.UtcNow.AddDays(-30) });
        keyStore.AddKey(keyId, "2", key2);
        keyStore.SetKeyMetadata(keyId, "2", new() { Algorithm = "AES-256", CreatedAt = DateTime.UtcNow });
        var metadata1 = keyStore.GetKeyMetadata(keyId, "1");
        var metadata2 = keyStore.GetKeyMetadata(keyId, "2");
        Assert.NotNull(metadata1);
        Assert.NotNull(metadata2);
        Assert.True(metadata1.CreatedAt < metadata2.CreatedAt);
    }

    [Fact]
    public void KeyStore_ExpiredKey_IsExpired()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        var key = SecureKeyGenerator.GenerateKey();
        keyStore.AddKey(keyId, "1", key);
        keyStore.SetKeyMetadata(
            keyId, "1", new() {
                ExpiresAt = DateTime.UtcNow.AddDays(-1) // Expired yesterday
            });

        var metadata = keyStore.GetKeyMetadata(keyId, "1");
        Assert.True(metadata!.IsExpired);
        Assert.False(metadata.IsValid);
    }

    [Fact]
    public void KeyStore_ValidKey_IsValid()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        var key = SecureKeyGenerator.GenerateKey();
        keyStore.AddKey(keyId, "1", key);
        keyStore.SetKeyMetadata(
            keyId, "1", new() {
                ExpiresAt = DateTime.UtcNow.AddDays(30) // Valid for 30 days
            });

        var metadata = keyStore.GetKeyMetadata(keyId, "1");
        Assert.False(metadata!.IsExpired);
        Assert.True(metadata.IsValid);
    }

    [Fact]
    public async Task KeyStore_AsyncMethods_WorkCorrectly()
    {
        var keyStore = new LocalKeyStore();
        const string keyId = "test-key";
        var key = SecureKeyGenerator.GenerateKey();
        await keyStore.AddKeyAsync(keyId, "1", key, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(await keyStore.HasKeyAsync(keyId, "1", TestContext.Current.CancellationToken).ConfigureAwait(false));
        var retrieved = await keyStore.GetKeyAsync(keyId, "1", TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(key, retrieved);
        await keyStore.SetCurrentVersionAsync(keyId, "1", TestContext.Current.CancellationToken).ConfigureAwait(false);
        var current = await keyStore.GetCurrentKeyAsync(keyId, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(key, current);
    }

    [Fact]
    public void KeyStore_RemoveKey_RemovesMetadata()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        var key1 = SecureKeyGenerator.GenerateKey();
        var key2 = SecureKeyGenerator.GenerateKey();
        keyStore.AddKey(keyId, "1", key1);
        keyStore.AddKey(keyId, "2", key2);
        keyStore.SetCurrentVersion(keyId, "1");
        keyStore.SetKeyMetadata(keyId, "2", new() { Algorithm = "Test" });
        keyStore.RemoveKey(keyId, "2");
        Assert.False(keyStore.HasKey(keyId, "2"));
        Assert.Null(keyStore.GetKeyMetadata(keyId, "2"));
    }

    [Fact]
    public void KeyStore_GetAvailableVersions_AfterRemoval_ExcludesRemoved()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        var key = SecureKeyGenerator.GenerateKey();
        keyStore.AddKey(keyId, "1", key);
        keyStore.AddKey(keyId, "2", key);
        keyStore.AddKey(keyId, "3", key);
        keyStore.SetCurrentVersion(keyId, "1");
        keyStore.RemoveKey(keyId, "2");
        var versions = keyStore.GetAvailableVersions(keyId).ToList();
        Assert.DoesNotContain("2", versions);
        Assert.Contains("1", versions);
        Assert.Contains("3", versions);
    }
}