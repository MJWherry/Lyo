using Lyo.Keystore;

namespace Lyo.Encryption.Tests;

public class KeyStoreTests
{
    private static readonly string[] expected = ["1", "2", "3"];

    [Fact]
    public void AddKey_StoresKey()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        var key = Enumerable.Range(1, 32).Select(x => (byte)x).ToArray();
        keyStore.AddKey(keyId, "1", key);
        Assert.True(keyStore.HasKey(keyId, "1"));
        var retrieved = keyStore.GetKey(keyId, "1");
        Assert.NotNull(retrieved);
        Assert.Equal(key, retrieved);
    }

    [Fact]
    public void AddKey_NullKey_Throws()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        Assert.Throws<ArgumentNullException>(() => keyStore.AddKey(keyId, "1", null!));
    }

    [Fact]
    public void AddKey_EmptyKey_Throws()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        Assert.Throws<ArgumentException>(() => keyStore.AddKey(keyId, "1", []));
    }

    [Fact]
    public void AddKey_CreatesCopy()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        var key = Enumerable.Range(1, 32).Select(x => (byte)x).ToArray();
        keyStore.AddKey(keyId, "1", key);
        key[0] = 99; // Modify original
        var retrieved = keyStore.GetKey(keyId, "1");
        Assert.NotNull(retrieved);
        Assert.Equal(1, retrieved[0]); // Should still be original value
    }

    [Fact]
    public void AddKeyFromString_DerivesKey()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        keyStore.AddKeyFromString(keyId, "1", "test-key");
        Assert.True(keyStore.HasKey(keyId, "1"));
        var retrieved = keyStore.GetKey(keyId, "1");
        Assert.NotNull(retrieved);
        Assert.Equal(32, retrieved.Length); // SHA256 produces 32 bytes
    }

    [Fact]
    public void AddKeyFromString_NullString_Throws()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        Assert.Throws<ArgumentNullException>(() => keyStore.AddKeyFromString(keyId, "1", null!));
    }

    [Fact]
    public void AddKeyFromString_EmptyString_Throws()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        Assert.Throws<ArgumentException>(() => keyStore.AddKeyFromString(keyId, "1", ""));
    }

    [Fact]
    public void GetKey_NonExistent_ReturnsNull()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        var result = keyStore.GetKey(keyId, "999");
        Assert.Null(result);
    }

    [Fact]
    public void GetCurrentKey_ReturnsCurrentVersionKey()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        var key1 = Enumerable.Range(1, 32).Select(x => (byte)x).ToArray();
        var key2 = Enumerable.Range(1, 64).Select(x => (byte)x).ToArray();
        keyStore.AddKey(keyId, "1", key1);
        keyStore.AddKey(keyId, "2", key2);
        keyStore.SetCurrentVersion(keyId, "1");
        var current = keyStore.GetCurrentKey(keyId);
        Assert.Equal(key1, current);
        keyStore.SetCurrentVersion(keyId, "2");
        current = keyStore.GetCurrentKey(keyId);
        Assert.Equal(key2, current);
    }

    [Fact]
    public void GetCurrentKey_NoKeys_ReturnsNull()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        var result = keyStore.GetCurrentKey(keyId);
        Assert.Null(result);
    }

    [Fact]
    public void GetCurrentVersion_Default_ReturnsOne()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        keyStore.AddKeyFromString(keyId, "1", "test");
        Assert.Equal("1", keyStore.GetCurrentVersion(keyId));
    }

    [Fact]
    public void SetCurrentVersion_UpdatesCurrent()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        var key1 = Enumerable.Range(1, 32).Select(x => (byte)x).ToArray();
        var key2 = Enumerable.Range(32, 64).Select(x => (byte)x).ToArray();
        keyStore.AddKey(keyId, "1", key1);
        keyStore.AddKey(keyId, "2", key2);
        keyStore.SetCurrentVersion(keyId, "2");
        Assert.Equal("2", keyStore.GetCurrentVersion(keyId));
        Assert.Equal(key2, keyStore.GetCurrentKey(keyId));
    }

    [Fact]
    public void SetCurrentVersion_NonExistent_Throws()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        Assert.Throws<InvalidOperationException>(() => keyStore.SetCurrentVersion(keyId, "999"));
    }

    [Fact]
    public void HasKey_ExistingKey_ReturnsTrue()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        var key = Enumerable.Range(1, 32).Select(x => (byte)x).ToArray();
        keyStore.AddKey(keyId, "1", key);
        Assert.True(keyStore.HasKey(keyId, "1"));
    }

    [Fact]
    public void HasKey_NonExistent_ReturnsFalse()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        Assert.False(keyStore.HasKey(keyId, "999"));
    }

    [Fact]
    public void GetAvailableVersions_ReturnsAllVersions()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        var key = Enumerable.Range(1, 32).Select(x => (byte)x).ToArray();
        keyStore.AddKey(keyId, "1", key);
        keyStore.AddKey(keyId, "3", key);
        keyStore.AddKey(keyId, "2", key);
        var versions = keyStore.GetAvailableVersions(keyId).ToList();
        Assert.Equal(3, versions.Count);
        Assert.Equal(expected, versions);
    }

    [Fact]
    public void RemoveKey_RemovesKey()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        var key = Enumerable.Range(1, 32).Select(x => (byte)x).ToArray();
        keyStore.AddKey(keyId, "1", key);
        keyStore.AddKey(keyId, "2", key);
        keyStore.SetCurrentVersion(keyId, "1");
        var removed = keyStore.RemoveKey(keyId, "2");
        Assert.True(removed);
        Assert.False(keyStore.HasKey(keyId, "2"));
        Assert.True(keyStore.HasKey(keyId, "1"));
    }

    [Fact]
    public void RemoveKey_CurrentVersion_Throws()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        var key = Enumerable.Range(1, 32).Select(x => (byte)x).ToArray();
        keyStore.AddKey(keyId, "1", key);
        keyStore.SetCurrentVersion(keyId, "1");
        Assert.Throws<InvalidOperationException>(() => keyStore.RemoveKey(keyId, "1"));
    }

    [Fact]
    public void RemoveKey_NonExistent_ReturnsFalse()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        var removed = keyStore.RemoveKey(keyId, "999");
        Assert.False(removed);
    }

    [Fact]
    public async Task GetKeyAsync_ReturnsKey()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        var key = Enumerable.Range(1, 32).Select(x => (byte)x).ToArray();
        await keyStore.AddKeyAsync(keyId, "1", key, TestContext.Current.CancellationToken).ConfigureAwait(false);
        var result = await keyStore.GetKeyAsync(keyId, "1", TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.NotNull(result);
        Assert.Equal(key, result);
    }

    [Fact]
    public async Task GetCurrentKeyAsync_ReturnsCurrentKey()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        var key = Enumerable.Range(1, 32).Select(x => (byte)x).ToArray();
        await keyStore.AddKeyAsync(keyId, "1", key, TestContext.Current.CancellationToken).ConfigureAwait(false);
        await keyStore.SetCurrentVersionAsync(keyId, "1", TestContext.Current.CancellationToken).ConfigureAwait(false);
        var result = await keyStore.GetCurrentKeyAsync(keyId, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.NotNull(result);
        Assert.Equal(key, result);
    }

    [Fact]
    public async Task AddKeyAsync_StoresKey()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        var key = Enumerable.Range(1, 32).Select(x => (byte)x).ToArray();
        await keyStore.AddKeyAsync(keyId, "1", key, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(await keyStore.HasKeyAsync(keyId, "1", TestContext.Current.CancellationToken).ConfigureAwait(false));
    }

    [Fact]
    public async Task AddKeyFromStringAsync_DerivesKey()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        await keyStore.AddKeyFromStringAsync(keyId, "1", "test-key", TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(await keyStore.HasKeyAsync(keyId, "1", TestContext.Current.CancellationToken).ConfigureAwait(false));
    }

    [Fact]
    public async Task SetCurrentVersionAsync_UpdatesCurrent()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        var key = Enumerable.Range(1, 32).Select(x => (byte)x).ToArray();
        await keyStore.AddKeyAsync(keyId, "1", key, TestContext.Current.CancellationToken).ConfigureAwait(false);
        await keyStore.AddKeyAsync(keyId, "2", key, TestContext.Current.CancellationToken).ConfigureAwait(false);
        await keyStore.SetCurrentVersionAsync(keyId, "2", TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal("2", await keyStore.GetCurrentVersionAsync(keyId, TestContext.Current.CancellationToken).ConfigureAwait(false));
    }

    [Fact]
    public async Task HasKeyAsync_ReturnsCorrectValue()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        var key = Enumerable.Range(1, 32).Select(x => (byte)x).ToArray();
        await keyStore.AddKeyAsync(keyId, "1", key, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(await keyStore.HasKeyAsync(keyId, "1", TestContext.Current.CancellationToken).ConfigureAwait(false));
        Assert.False(await keyStore.HasKeyAsync(keyId, "999", TestContext.Current.CancellationToken).ConfigureAwait(false));
    }

    [Fact]
    public void AddKey_OverwritesExisting()
    {
        const string keyId = "test-key";
        var keyStore = new LocalKeyStore();
        var key1 = Enumerable.Range(1, 32).Select(x => (byte)x).ToArray();
        var key2 = Enumerable.Range(32, 64).Select(x => (byte)x).ToArray();
        keyStore.AddKey(keyId, "1", key1);
        keyStore.AddKey(keyId, "1", key2);
        var retrieved = keyStore.GetKey(keyId, "1");
        Assert.Equal(key2, retrieved);
    }
}