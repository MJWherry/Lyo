using System.Text;
using Lyo.Cache.Fusion;
using Lyo.Compression;
using Lyo.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Cache.Tests;

public class CachePayloadTests
{
    private static ICacheService CreateCache(string implementation, Action<CacheOptions>? configure = null)
    {
        var services = new ServiceCollection();
        if (implementation == "Local")
            services.AddLocalCache(configure);
        else
            services.AddFusionCache(configure);

        return services.BuildServiceProvider().GetRequiredService<ICacheService>();
    }

    public static IEnumerable<object[]> CacheImplementations => [["Local"], ["Fusion"]];

    [Theory]
    [MemberData(nameof(CacheImplementations))]
    public async Task GetOrSetPayloadAsync_roundtrips_bytes(string mode)
    {
        var cache = CreateCache(mode, o => {
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
            key,
            _ => {
                calls++;
                return Task.FromResult<byte[]?>("\t\t"u8.ToArray());
            },
            token: ct);

        calls.ShouldBe(0);
        env2.ShouldNotBeNull();
        env2.Payload.ToArray().ShouldBe(expected);
    }

    [Theory]
    [MemberData(nameof(CacheImplementations))]
    public async Task GetOrSetPayloadAsync_compresses_when_over_threshold(string mode)
    {
        var cache = CreateCache(mode, o => {
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
        var cache = CreateCache(mode, o => {
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

    [Fact]
    public void SetPayload_and_TryGetPayload_roundtrip_sync()
    {
        var cache = CreateCache("Local", o => {
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

#if NET10_0_OR_GREATER
    [Fact]
    public void CachePayloadCodec_encode_throws_when_auto_encrypt_without_encryption_service()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new CacheOptions {
            Payload = new() {
                AutoEncrypt = true,
                EncryptionKeyId = "k"
            }
        });
        services.AddCompressionService();
        services.AddSingleton<ICachePayloadCodec>(sp =>
            new CachePayloadCodec(
                sp.GetRequiredService<CacheOptions>(),
                sp.GetRequiredService<ICompressionService>(),
                encryption: null));

        var codec = services.BuildServiceProvider().GetRequiredService<ICachePayloadCodec>();
        Assert.Throws<InvalidOperationException>(() => codec.Encode([1, 2, 3]));
    }
#endif
}
