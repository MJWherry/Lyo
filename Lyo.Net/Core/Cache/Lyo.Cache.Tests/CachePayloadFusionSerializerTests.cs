using System.Text.Json;
using Lyo.Cache.Fusion;
using Lyo.Cache.Fusion.Internal;
using Lyo.Compression;
using Lyo.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ZiggyCreatures.Caching.Fusion.Internals;
using ZiggyCreatures.Caching.Fusion.Internals.Distributed;

namespace Lyo.Cache.Tests;

public class CachePayloadFusionSerializerTests
{
    [Fact]
    public void Serialize_FramedByteEntry_PassesValueThroughWithoutJson()
    {
        var (serializer, codec) = CreateSerializer(autoCompress: true, minSize: 64);
        var plaintext = new byte[200];
        var framed = codec.Encode(plaintext);
        codec.IsFramed(framed).ShouldBeTrue();
        var entry = new FusionCacheDistributedEntry<byte[]>(framed, 11, 22, ["tag-a"], null);
        var blob = serializer.Serialize(entry);
        FusionL2Frame.IsFramed(blob).ShouldBeTrue();
        FusionL2Frame.TryParse(blob, out var header, out var valueBytes).ShouldBeTrue();
        valueBytes.ShouldBe(framed);
        header.Timestamp.ShouldBe(11);
        header.LogicalExpirationTimestamp.ShouldBe(22);
        header.Tags.ShouldBe(["tag-a"]);
        var roundtrip = serializer.Deserialize<FusionCacheDistributedEntry<byte[]>>(blob);
        roundtrip.ShouldNotBeNull();
        roundtrip.Value.ShouldBe(framed);
        roundtrip.Tags.ShouldBe(["tag-a"]);
    }

    [Fact]
    public void Serialize_StringEntry_FramesCompressedJson()
    {
        var (serializer, codec) = CreateSerializer(autoCompress: true, minSize: 64);
        var entry = new FusionCacheDistributedEntry<string>(new string('z', 500), 1, 2, null, new FusionCacheEntryMetadata(true, 9, "etag", 8, 7, 2));
        var blob = serializer.Serialize(entry);
        FusionL2Frame.TryParse(blob, out var header, out var valueBytes).ShouldBeTrue();
        codec.IsFramed(valueBytes).ShouldBeTrue();
        var env = codec.Decode(valueBytes);
        env.Compression.ShouldNotBeNull();
        header.Metadata.ShouldNotBeNull();
        header.Metadata!.IsStale.ShouldBeTrue();
        header.Metadata.ETag.ShouldBe("etag");
        var roundtrip = serializer.Deserialize<FusionCacheDistributedEntry<string>>(blob);
        roundtrip.ShouldNotBeNull();
        roundtrip.Value.ShouldBe(entry.Value);
        roundtrip.Metadata.ShouldNotBeNull();
        roundtrip.Metadata!.ETag.ShouldBe("etag");
        roundtrip.Metadata.IsStale.ShouldBeTrue();
    }

    [Fact]
    public void Deserialize_LegacyJsonEntry_RoundtripsValue()
    {
        var (serializer, _) = CreateSerializer(autoCompress: false, minSize: 1024);
        var entry = new FusionCacheDistributedEntry<string>("hello-legacy", 3, 4, ["legacy-tag"], null);
        var json = JsonSerializer.SerializeToUtf8Bytes(entry);
        json[0].ShouldBe((byte)'{');
        var roundtrip = serializer.Deserialize<FusionCacheDistributedEntry<string>>(json);
        roundtrip.ShouldNotBeNull();
        roundtrip.Value.ShouldBe("hello-legacy");
        roundtrip.Timestamp.ShouldBe(3);
        roundtrip.LogicalExpirationTimestamp.ShouldBe(4);
        roundtrip.Tags.ShouldBe(["legacy-tag"]);
    }

    private static (CachePayloadFusionSerializer Serializer, ICachePayloadCodec Codec) CreateSerializer(bool autoCompress, int minSize)
    {
        var services = new ServiceCollection();
        services.AddSingleton(new CacheOptions { Payload = new() { AutoCompress = autoCompress, AutoCompressMinSizeBytes = minSize } });
        services.AddCompressionService();
        services.AddDefaultCompressionService<CompressionService>();
        services.TryAddSingleton(CachePayloadSerializerRegistration.Create);
        services.AddSingleton<ICachePayloadCodec>(sp => new CachePayloadCodec(
            sp.GetRequiredService<CacheOptions>(), sp.GetRequiredService<ICompressionService>()));
        var sp = services.BuildServiceProvider();
        var codec = sp.GetRequiredService<ICachePayloadCodec>();
        var payloadSerializer = sp.GetRequiredService<ICachePayloadSerializer>();
        return (new CachePayloadFusionSerializer(codec, payloadSerializer), codec);
    }
}
