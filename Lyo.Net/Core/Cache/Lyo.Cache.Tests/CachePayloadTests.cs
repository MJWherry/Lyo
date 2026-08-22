using System.Text;
using Lyo.Cache.Fusion;
using Lyo.Compression;
using Lyo.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Cache.Tests;

public class CachePayloadTests
{
    public static IEnumerable<object[]> CacheImplementations => [["Local"], ["Fusion"]];

    private static ICacheService CreateCache(string implementation, Action<CacheOptions>? configure = null)
    {
        var services = new ServiceCollection();
        if (implementation == "Local")
            services.AddLocalCache(configure);
        else
            services.AddFusionCache(configure);

        return services.BuildServiceProvider().GetRequiredService<ICacheService>();
    }

    [Theory]
    [MemberData(nameof(CacheImplementations))]
    public async Task GetOrSetPayloadAsync_roundtrips_bytes(string mode)
    {
        var cache = CreateCache(
            mode, o => {
                o.Enabled = true;
                o.Payload.AutoCompress = false;
            });

        var key = $"payload-plain-{mode}-{Guid.NewGuid():N}";
        var expected = new byte[] { 1, 2, 3, 4, 5 };
        var ct = TestContext.Current.CancellationToken;
        var env1 = await cache.GetOrSetPayloadAsync(key, _ => Task.FromResult<byte[]?>(expected), token: ct);
        env1.ShouldNotBeNull();
        env1.Compression.ShouldBeNull();
        env1.Payload.ToArray().ShouldBe(expected);
        var calls = 0;
        var env2 = await cache.GetOrSetPayloadAsync(
            key, _ => {
                calls++;
                return Task.FromResult<byte[]?>("\t\t"u8.ToArray());
            }, token: ct);

        calls.ShouldBe(0);
        env2.ShouldNotBeNull();
        env2.Payload.ToArray().ShouldBe(expected);
    }

    [Theory]
    [MemberData(nameof(CacheImplementations))]
    public async Task GetOrSetPayloadAsync_compresses_when_over_threshold(string mode)
    {
        var cache = CreateCache(
            mode, o => {
                o.Enabled = true;
                o.Payload.AutoCompress = true;
                o.Payload.AutoCompressMinSizeBytes = 64;
            });

        var key = $"payload-comp-{mode}-{Guid.NewGuid():N}";
        // Highly compressible so the codec chooses compressed form (smaller than raw).
        var data = new byte[200];
        var ct = TestContext.Current.CancellationToken;
        var env = await cache.GetOrSetPayloadAsync(key, _ => Task.FromResult<byte[]?>(data), token: ct);
        env.ShouldNotBeNull();
        env.Compression.ShouldNotBeNull();
        env.Compression.IsSuccess.ShouldBeTrue();
        env.Payload.ToArray().ShouldBe(data);
    }

    [Theory]
    [MemberData(nameof(CacheImplementations))]
    public async Task GetOrSetPayloadAsync_skips_compress_below_threshold(string mode)
    {
        var cache = CreateCache(
            mode, o => {
                o.Enabled = true;
                o.Payload.AutoCompress = true;
                o.Payload.AutoCompressMinSizeBytes = 10_000;
            });

        var key = $"payload-nocomp-{mode}-{Guid.NewGuid():N}";
        var data = new byte[50];
        Random.Shared.NextBytes(data);
        var ct = TestContext.Current.CancellationToken;
        var env = await cache.GetOrSetPayloadAsync(key, _ => Task.FromResult<byte[]?>(data), token: ct);
        env.ShouldNotBeNull();
        env.Compression.ShouldBeNull();
        env.Payload.ToArray().ShouldBe(data);
    }

    [Theory]
    [MemberData(nameof(CacheImplementations))]
    public void TryGetPayload_returns_false_for_corrupt_frame(string mode)
    {
        var cache = CreateCache(mode, o => o.Enabled = true);
        var key = $"payload-bad-{mode}-{Guid.NewGuid():N}";
        cache.Set(key, new byte[] { 0xFF, 0xFF });
        cache.TryGetPayload(key, out var env).ShouldBeFalse();
        env.ShouldBeNull();
    }

    [Theory]
    [MemberData(nameof(CacheImplementations))]
    public void SetPayload_WithSlidingExpiration_TryGetPayloadExtendsLifetime(string mode)
    {
        var cache = CreateCache(
            mode, o => {
                o.Enabled = true;
                o.Payload.AutoCompress = false;
            });

        var key = $"payload-sliding-{mode}-{Guid.NewGuid():N}";
        var bytes = new byte[] { 9, 8, 7 };
        cache.SetPayload(key, bytes, o => o.SetSlidingExpiration(TimeSpan.FromMilliseconds(500)));
        Thread.Sleep(150);
        cache.TryGetPayload(key, out var first).ShouldBeTrue();
        first.ShouldNotBeNull();
        first.Payload.ToArray().ShouldBe(bytes);
        Thread.Sleep(400);
        cache.TryGetPayload(key, out var still).ShouldBeTrue();
        still.ShouldNotBeNull();
        still.Payload.ToArray().ShouldBe(bytes);
    }

    [Fact]
    public void SetPayload_and_TryGetPayload_roundtrip_sync()
    {
        var cache = CreateCache(
            "Local", o => {
                o.Enabled = true;
                o.Payload.AutoCompress = false;
            });

        var key = $"payload-set-{Guid.NewGuid():N}";
        var bytes = "hello-bytes"u8.ToArray();
        cache.SetPayload(key, bytes);
        cache.TryGetPayload(key, out var env).ShouldBeTrue();
        env.ShouldNotBeNull();
        Encoding.UTF8.GetString(env.Payload.ToArray()).ShouldBe("hello-bytes");
    }

    [Theory]
    [MemberData(nameof(CacheImplementations))]
    public void SetPayload_TracksCompressedSizeAndNotEncrypted(string mode)
    {
        var cache = CreateCache(
            mode, o => {
                o.Enabled = true;
                o.Payload.AutoCompress = true;
                o.Payload.AutoCompressMinSizeBytes = 64;
                o.Payload.AutoEncrypt = false;
            });

        var key = $"payload-meta-{mode}-{Guid.NewGuid():N}";
        var data = new byte[200];
        cache.SetPayload(key, data);
        var item = cache.Items.Single(i => i.Type == CacheItemTypeEnum.Key && string.Equals(i.Name, key, StringComparison.OrdinalIgnoreCase));
        item.Encrypted.ShouldBe(false);
        item.Compressed.ShouldBe(true);
        item.SizeBytes.ShouldNotBeNull();
        item.SizeBytes!.Value.ShouldBeGreaterThan(0);
        item.SizeBytes.Value.ShouldBeLessThan(data.Length + 9);
    }

    [Theory]
    [MemberData(nameof(CacheImplementations))]
    public void Set_ObjectValue_TracksUnframedKey(string mode)
    {
        var cache = CreateCache(mode, o => o.Enabled = true);
        var key = $"obj-meta-{mode}-{Guid.NewGuid():N}";
        var tag = $"obj-meta-tag-{mode}-{Guid.NewGuid():N}";
        cache.Set(key, "value", [tag]);
        var keyItem = cache.Items.Single(i => i.Type == CacheItemTypeEnum.Key && string.Equals(i.Name, key, StringComparison.OrdinalIgnoreCase));
        keyItem.Encrypted.ShouldBe(false);
        keyItem.Compressed.ShouldBe(false);
        keyItem.SizeBytes.ShouldBeNull();
        keyItem.Expires.ShouldNotBeNull();
        keyItem.Tags.ShouldNotBeNull();
        keyItem.Tags!.ShouldContain(tag.ToLowerInvariant());
        foreach (var tagItem in cache.Items.Where(i => i.Type == CacheItemTypeEnum.Tag)) {
            tagItem.Encrypted.ShouldBeNull();
            tagItem.Compressed.ShouldBeNull();
            tagItem.SizeBytes.ShouldBeNull();
            tagItem.Expires.ShouldBeNull();
            tagItem.Tags.ShouldBeNull();
        }
    }

    [Theory]
    [MemberData(nameof(CacheImplementations))]
    public void Set_TracksExpiresFromDuration(string mode)
    {
        var cache = CreateCache(mode, o => o.Enabled = true);
        var key = $"expires-{mode}-{Guid.NewGuid():N}";
        var ttl = TimeSpan.FromMinutes(5);
        var before = DateTime.UtcNow;
        cache.Set(key, "value", ttl);
        var after = DateTime.UtcNow;
        var item = cache.Items.Single(i => i.Type == CacheItemTypeEnum.Key && string.Equals(i.Name, key, StringComparison.OrdinalIgnoreCase));
        item.Expires.ShouldNotBeNull();
        item.Expires!.Value.ShouldBeGreaterThanOrEqualTo(before.Add(ttl).AddSeconds(-1));
        item.Expires.Value.ShouldBeLessThanOrEqualTo(after.Add(ttl).AddSeconds(1));
    }

    [Theory]
    [MemberData(nameof(CacheImplementations))]
    public void Set_WithSlidingExpiration_HitExtendsTrackedExpires(string mode)
    {
        var cache = CreateCache(mode, o => o.Enabled = true);
        var key = $"expires-sliding-{mode}-{Guid.NewGuid():N}";
        var ttl = TimeSpan.FromMinutes(5);
        cache.Set(key, "value", o => o.SetSlidingExpiration(ttl));
        var first = cache.Items.Single(i => i.Type == CacheItemTypeEnum.Key && string.Equals(i.Name, key, StringComparison.OrdinalIgnoreCase)).Expires;
        first.ShouldNotBeNull();
        Thread.Sleep(250);
        cache.TryGetValue<string>(key, out var hit).ShouldBeTrue();
        hit.ShouldBe("value");
        var second = cache.Items.Single(i => i.Type == CacheItemTypeEnum.Key && string.Equals(i.Name, key, StringComparison.OrdinalIgnoreCase)).Expires;
        second.ShouldNotBeNull();
        (second!.Value - first!.Value).ShouldBeGreaterThan(TimeSpan.FromMilliseconds(100));
    }

#if NET10_0_OR_GREATER
    [Fact]
    public void CachePayloadCodec_encode_throws_when_auto_encrypt_without_encryption_service()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new CacheOptions { Payload = new() { AutoEncrypt = true, EncryptionKeyId = "k" } });
        services.AddCompressionService();
        services.AddDefaultCompressionService<CompressionService>();
        services.AddSingleton<ICachePayloadCodec>(sp => new CachePayloadCodec(sp.GetRequiredService<CacheOptions>(), sp.GetRequiredService<ICompressionService>()));
        var codec = services.BuildServiceProvider().GetRequiredService<ICachePayloadCodec>();
        Assert.Throws<InvalidOperationException>(() => codec.Encode([1, 2, 3]));
    }
#endif
}