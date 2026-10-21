using System.Text.Json;
using Lyo.Testing;

namespace Lyo.KeyStore.Aws.Tests;

public class AwsKeyStoreRoundTripTests
{
    private const string Prefix = "lyo/kek";

    [Fact]
    public async Task AddKey_Version1_GetKey_ReturnsTheSameBytes()
    {
        var (client, _) = MemorySecretsManager.Create();
        var store = new AwsKeyStore(client);
        var key = TestData.Create(32);
        await store.AddKeyAsync("app", "1", key, TestCt);
        Assert.Equal(key, await store.GetKeyAsync("app", "1", TestCt));
    }

    [Fact]
    public async Task SetCurrentVersion_ShortVersion_GetCurrentVersion_ReturnsThatString()
    {
        var (client, _) = MemorySecretsManager.Create();
        var store = new AwsKeyStore(client);
        await store.AddKeyAsync("app", "1", TestData.Create(32), TestCt);
        await store.SetCurrentVersionAsync("app", "1", TestCt);
        Assert.Equal("1", await store.GetCurrentVersionAsync("app", TestCt));
    }

    [Fact]
    public async Task GetCurrentVersion_DoesNotWriteAwsVersionId()
    {
        var (client, state) = MemorySecretsManager.Create();
        state.Put(Prefix, JsonSerializer.Serialize(new Dictionary<string, string> { ["app"] = "legacy-passphrase" }));
        var before = state.Names.Count;
        var store = new AwsKeyStore(client);
        var version = await store.GetCurrentVersionAsync("app", TestCt);
        Assert.Null(version);
        Assert.Equal(before, state.Names.Count);
    }

    [Fact]
    public async Task AddKeyFromString_GetKey_MatchesLocalKeyStore()
    {
        const string passphrase = "same-passphrase";
        var local = new LocalKeyStore();
        local.AddKeyFromString("app", "1", passphrase);
        var (client, _) = MemorySecretsManager.Create();
        var aws = new AwsKeyStore(client);
        await aws.AddKeyFromStringAsync("app", "1", passphrase, TestCt);
        Assert.Equal(local.GetKey("app", "1"), await aws.GetKeyAsync("app", "1", TestCt));
    }

    [Fact]
    public async Task GetKey_SecretWithoutRawPrefix_StillDerives()
    {
        const string passphrase = "legacy-passphrase";
        var local = new LocalKeyStore();
        local.AddKeyFromString("app", "1", passphrase);
        var (client, state) = MemorySecretsManager.Create();
        state.Put(Prefix, JsonSerializer.Serialize(new Dictionary<string, string> { ["app"] = passphrase }));
        var aws = new AwsKeyStore(client);
        Assert.Equal(local.GetKey("app", "1"), await aws.GetKeyAsync("app", null, TestCt));
    }

    [Fact]
    public async Task AddKey_TwoKeyIds_EachVersionStillReads()
    {
        var (client, _) = MemorySecretsManager.Create();
        var store = new AwsKeyStore(client);
        var keyA = TestData.Create(32);
        var keyB = TestData.Create(32, TestData.Seed + 1);
        await store.AddKeyAsync("alpha", "1", keyA, TestCt);
        await store.AddKeyAsync("beta", "1", keyB, TestCt);
        Assert.Equal(keyA, await store.GetKeyAsync("alpha", "1", TestCt));
        Assert.Equal(keyB, await store.GetKeyAsync("beta", "1", TestCt));
    }
}