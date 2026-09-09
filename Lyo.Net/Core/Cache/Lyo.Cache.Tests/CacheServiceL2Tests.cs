using System.Collections.Concurrent;
using System.Text;
using Lyo.Cache.Fusion;
using Lyo.Cache.Fusion.Internal;
using Lyo.Testing;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using ZiggyCreatures.Caching.Fusion;

namespace Lyo.Cache.Tests;

public class CacheServiceL2Tests
{
    [Fact]
    public void SetPayload_WithL2_WritesLyo2WrappingFramedBytesNotJson()
    {
        var capture = new CapturingDistributedCache();
        using var host = CreateL2Host(
            capture, o => {
                o.Payload.AutoCompress = true;
                o.Payload.AutoCompressMinSizeBytes = 64;
            });
        var data = new byte[400];
        host.Cache.SetPayload("l2-payload-zip", data);
        var blob = AssertSingleL2Blob(capture);
        FusionL2Frame.IsFramed(blob).ShouldBeTrue();
        blob[0].ShouldBe((byte)'L');
        FusionL2Frame.TryParse(blob, out _, out var valueBytes).ShouldBeTrue();
        host.Codec.IsFramed(valueBytes).ShouldBeTrue();
        valueBytes[0].ShouldNotBe((byte)'{');
        valueBytes[0].ShouldNotBe((byte)'[');
        var env = host.Codec.Decode(valueBytes);
        env.Compression.ShouldNotBeNull();
        env.Payload.ToArray().ShouldBe(data);
        valueBytes.Length.ShouldBeLessThan(data.Length + 9);
    }

    [Fact]
    public void GetOrSet_WithL2_CompressesClrValue()
    {
        var capture = new CapturingDistributedCache();
        using var host = CreateL2Host(
            capture, o => {
                o.Payload.AutoCompress = true;
                o.Payload.AutoCompressMinSizeBytes = 64;
            });
        var value = new string('a', 2000);
        host.Cache.Set("l2-string-zip", value);
        var blob = AssertSingleL2Blob(capture);
        FusionL2Frame.TryParse(blob, out _, out var valueBytes).ShouldBeTrue();
        host.Codec.IsFramed(valueBytes).ShouldBeTrue();
        var env = host.Codec.Decode(valueBytes);
        env.Compression.ShouldNotBeNull();
        env.Payload.Length.ShouldBeGreaterThan(value.Length);
        valueBytes.Length.ShouldBeLessThan(env.Payload.Length);
        host.Cache.TryGetValue<string>("l2-string-zip", out var hit).ShouldBeTrue();
        hit.ShouldBe(value);
    }

    [Fact]
    public void SetPayload_WithL2_SecondProcessReadsFramedPayload()
    {
        var capture = new CapturingDistributedCache();
        var data = Encoding.UTF8.GetBytes(new string('x', 300));
        using (var writer = CreateL2Host(
                   capture, o => {
                       o.Payload.AutoCompress = true;
                       o.Payload.AutoCompressMinSizeBytes = 64;
                   }))
            writer.Cache.SetPayload("l2-payload-share", data);

        using var reader = CreateL2Host(
            capture, o => {
                o.Payload.AutoCompress = true;
                o.Payload.AutoCompressMinSizeBytes = 64;
            });
        reader.Cache.TryGetPayload("l2-payload-share", out var env).ShouldBeTrue();
        env.ShouldNotBeNull();
        env.Payload.ToArray().ShouldBe(data);
    }

    [Fact]
    public void GetOrSet_WithL2_SecondProcessReadsCompressedString()
    {
        var capture = new CapturingDistributedCache();
        var value = new string('b', 1500);
        using (var writer = CreateL2Host(
                   capture, o => {
                       o.Payload.AutoCompress = true;
                       o.Payload.AutoCompressMinSizeBytes = 64;
                   }))
            writer.Cache.Set("l2-string-share", value);

        using var reader = CreateL2Host(
            capture, o => {
                o.Payload.AutoCompress = true;
                o.Payload.AutoCompressMinSizeBytes = 64;
            });
        reader.Cache.TryGetValue<string>("l2-string-share", out var hit).ShouldBeTrue();
        hit.ShouldBe(value);
    }

    private static byte[] AssertSingleL2Blob(CapturingDistributedCache capture)
    {
        capture.Store.ShouldNotBeEmpty();
        var blobs = capture.Store.Values.Where(static v => v is { Length: > 0 }).ToArray();
        blobs.Length.ShouldBeGreaterThan(0);
        return blobs.First(static b => FusionL2Frame.IsFramed(b) || b[0] is (byte)'{' or (byte)'[' or (byte)'L');
    }

    private static L2Host CreateL2Host(CapturingDistributedCache capture, Action<CacheOptions>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IDistributedCache>(capture);
        services.AddFusionCache(o => {
            o.Enabled = true;
            configure?.Invoke(o);
        });
        var sp = services.BuildServiceProvider();
        var fusion = sp.GetRequiredService<IFusionCache>();
        fusion.DefaultEntryOptions.AllowBackgroundDistributedCacheOperations = false;
        fusion.HasDistributedCache.ShouldBeTrue();
        return new(sp, sp.GetRequiredService<ICacheService>(), sp.GetRequiredService<ICachePayloadCodec>());
    }

    private sealed class L2Host(IServiceProvider services, ICacheService cache, ICachePayloadCodec codec) : IDisposable
    {
        public ICacheService Cache { get; } = cache;
        public ICachePayloadCodec Codec { get; } = codec;

        public void Dispose() => (services as IDisposable)?.Dispose();
    }

    internal sealed class CapturingDistributedCache : IDistributedCache
    {
        public ConcurrentDictionary<string, byte[]> Store { get; } = new(StringComparer.Ordinal);

        public byte[]? Get(string key) => Store.TryGetValue(key, out var value) ? value : null;

        public Task<byte[]?> GetAsync(string key, CancellationToken token = default) => Task.FromResult(Get(key));

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options) => Store[key] = value;

        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
        {
            Set(key, value, options);
            return Task.CompletedTask;
        }

        public void Refresh(string key)
        {
        }

        public Task RefreshAsync(string key, CancellationToken token = default) => Task.CompletedTask;

        public void Remove(string key) => Store.TryRemove(key, out _);

        public Task RemoveAsync(string key, CancellationToken token = default)
        {
            Remove(key);
            return Task.CompletedTask;
        }
    }
}
