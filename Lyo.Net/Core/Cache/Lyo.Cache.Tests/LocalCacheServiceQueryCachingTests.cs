using Lyo.Testing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Cache.Tests;

/// <summary>
/// <see cref="LocalCacheService"/> keeps a bidirectional tag index on top of <see cref="IMemoryCache"/>.
/// Production often uses <see cref="Fusion.FusionCacheService"/> with Redis; these tests lock in tagging
/// behavior for local/in-memory scenarios (e.g. manual tag work, dev, tests).
/// </summary>
public sealed class LocalCacheServiceQueryCachingTests : IDisposable
{
    private readonly MemoryCache _memoryCache;
    private readonly LocalCacheService _cache;

    public LocalCacheServiceQueryCachingTests()
    {
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        var options = new CacheOptions { Enabled = true, DefaultExpiration = TimeSpan.FromMinutes(5) };
        _cache = new LocalCacheService(_memoryCache, NullLogger<LocalCacheService>.Instance, options);
    }

    public void Dispose() => _memoryCache.Dispose();

    [Fact]
    public async Task InvalidateAllCachedQueriesAsync_RemovesEntriesTaggedQueries()
    {
        const string key1 = "query:person:page1";
        const string key2 = "query:person:page2";
        _cache.Set(key1, "v1", ["queries", "entity:person"]);
        _cache.Set(key2, "v2", ["queries", "entity:person"]);

        await _cache.InvalidateAllCachedQueriesAsync().ConfigureAwait(false);

        var calls = 0;
        (await _cache.GetOrSetAsync<string>(
            key1,
            _ => {
                calls++;
                return Task.FromResult("miss")!;
            },
            token: TestContext.Current.CancellationToken).ConfigureAwait(false)).ShouldBe("miss");
        (await _cache.GetOrSetAsync<string>(
            key2,
            _ => {
                calls++;
                return Task.FromResult("miss")!;
            },
            token: TestContext.Current.CancellationToken).ConfigureAwait(false)).ShouldBe("miss");
        calls.ShouldBe(2);
    }

    [Fact]
    public async Task InvalidateCacheItemByTag_EntityInstanceTag_RemovesOnlyKeysWithThatTag()
    {
        var id1 = Guid.NewGuid().ToString("N");
        var id2 = Guid.NewGuid().ToString("N");
        var keyA = $"query:person:includes:{id1}";
        var keyB = $"query:person:includes:{id2}";
        _cache.Set(keyA, "a", ["queries", $"entity:person:{id1}", "entity:address:aaa"]);
        _cache.Set(keyB, "b", ["queries", $"entity:person:{id2}"]);

        await _cache.InvalidateCacheItemByTag($"entity:person:{id1}").ConfigureAwait(false);

        (await _cache.GetOrSetAsync<string>(
            keyA,
            _ => Task.FromResult("miss-a")!,
            token: TestContext.Current.CancellationToken).ConfigureAwait(false)).ShouldBe("miss-a");
        (await _cache.GetOrSetAsync<string>(
            keyB,
            _ => Task.FromResult("miss-b")!,
            token: TestContext.Current.CancellationToken).ConfigureAwait(false)).ShouldBe("b");
    }

    [Fact]
    public async Task InvalidateAllCachedQueriesAsync_DoesNotRemoveSingleEntityGetTaggedEntitiesOnly()
    {
        const string listKey = "query:person:list";
        const string getKey = "entity:person:keys=abc";
        _cache.Set(listKey, "list-payload", ["queries", "entity:person", "entity:person:p1"]);
        _cache.Set(getKey, "get-payload", ["entities", "entity:person", "entity:person:p1"]);

        await _cache.InvalidateAllCachedQueriesAsync().ConfigureAwait(false);

        (await _cache.GetOrSetAsync<string>(
            listKey,
            _ => Task.FromResult("list-miss")!,
            token: TestContext.Current.CancellationToken).ConfigureAwait(false)).ShouldBe("list-miss");
        (await _cache.GetOrSetAsync<string>(
            getKey,
            _ => Task.FromResult("get-miss")!,
            token: TestContext.Current.CancellationToken).ConfigureAwait(false)).ShouldBe("get-payload");
    }

    [Fact]
    public async Task GetOrSetAsync_WithFactoryTags_MergesTagsForInvalidation()
    {
        const string key = "query:widget:q1";
        await _cache.GetOrSetAsync<string>(
            key,
            async ct => {
                await Task.Yield();
                return ("payload", ["queries", "entity:widget", "entity:widget:w1"]);
            },
            extraTags: ["queryproject"],
            token: TestContext.Current.CancellationToken).ConfigureAwait(false);

        await _cache.InvalidateCacheItemByTag("entity:widget:w1").ConfigureAwait(false);

        (await _cache.GetOrSetAsync<string>(
            key,
            _ => Task.FromResult("miss")!,
            token: TestContext.Current.CancellationToken).ConfigureAwait(false)).ShouldBe("miss");
    }

    [Fact]
    public async Task InvalidateCacheItemByTag_NormalizesTagCase()
    {
        const string key = "k1";
        _cache.Set(key, "v", ["Queries", "ENTITY:Person"]);

        await _cache.InvalidateCacheItemByTag("queries").ConfigureAwait(false);
        (await _cache.GetOrSetAsync<string>(
            key,
            _ => Task.FromResult("miss")!,
            token: TestContext.Current.CancellationToken).ConfigureAwait(false)).ShouldBe("miss");
    }

    [Fact]
    public async Task InvalidateQueryCacheAsync_RemovesKeysWithEntityTypeTag()
    {
        const string typeTag = "entity:persontagteststub";
        var key1 = "q1";
        var key2 = "q2";
        _cache.Set(key1, "a", [typeTag]);
        _cache.Set(key2, "b", [typeTag]);

        await _cache.InvalidateQueryCacheAsync<PersonTagTestStub>().ConfigureAwait(false);

        (await _cache.GetOrSetAsync<string>(
            key1,
            _ => Task.FromResult("m1")!,
            token: TestContext.Current.CancellationToken).ConfigureAwait(false)).ShouldBe("m1");
        (await _cache.GetOrSetAsync<string>(
            key2,
            _ => Task.FromResult("m2")!,
            token: TestContext.Current.CancellationToken).ConfigureAwait(false)).ShouldBe("m2");
    }

    [Fact]
    public async Task Set_SameKey_ReplacesTagIndex_InvalidatingOldTagDoesNotRemoveKey()
    {
        const string key = "mutable";
        _cache.Set(key, "first", ["queries"]);
        _cache.Set(key, "second", ["entities"]);

        await _cache.InvalidateCacheItemByTag("queries").ConfigureAwait(false);

        (await _cache.GetOrSetAsync<string>(
            key,
            _ => Task.FromResult("miss")!,
            token: TestContext.Current.CancellationToken).ConfigureAwait(false)).ShouldBe("second");
    }

    private sealed class PersonTagTestStub;
}
