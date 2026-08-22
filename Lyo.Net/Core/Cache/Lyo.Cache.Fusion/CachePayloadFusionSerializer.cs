using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using Lyo.Cache.Fusion.Internal;
using Lyo.Exceptions;
using ZiggyCreatures.Caching.Fusion.Internals.Distributed;
using ZiggyCreatures.Caching.Fusion.Serialization;

namespace Lyo.Cache.Fusion;

/// <summary>
/// Fusion L2 serializer: codec-framed value bytes plus a binary metadata header. Already-framed payload byte arrays are stored as-is. CLR values are
/// serialized once with <see cref="ICachePayloadSerializer" /> then framed by <see cref="ICachePayloadCodec" />.
/// </summary>
internal sealed class CachePayloadFusionSerializer : IFusionCacheSerializer
{
    private static readonly ConcurrentDictionary<Type, MethodInfo> PackEntryMethods = new();
    private static readonly ConcurrentDictionary<Type, MethodInfo> UnpackEntryMethods = new();
    private static readonly MethodInfo PackEntryDefinition = typeof(CachePayloadFusionSerializer)
        .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
        .Single(static m => m.Name == nameof(PackEntry) && m.IsGenericMethodDefinition);
    private static readonly MethodInfo UnpackEntryDefinition = typeof(CachePayloadFusionSerializer)
        .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
        .Single(static m => m.Name == nameof(UnpackEntry) && m.IsGenericMethodDefinition);

    private readonly ICachePayloadCodec _codec;
    private readonly ICachePayloadSerializer _payloadSerializer;

    public CachePayloadFusionSerializer(ICachePayloadCodec codec, ICachePayloadSerializer payloadSerializer)
    {
        _codec = ArgumentHelpers.ThrowIfNullReturn(codec);
        _payloadSerializer = ArgumentHelpers.ThrowIfNullReturn(payloadSerializer);
    }

    /// <inheritdoc />
    public byte[] Serialize<T>(T? obj)
    {
        if (obj is null)
            return [];

        if (obj is FusionCacheDistributedEntry<byte[]> bytesEntry)
            return PackEntry(bytesEntry);

        var type = typeof(T);
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(FusionCacheDistributedEntry<>)) {
            var pack = PackEntryMethods.GetOrAdd(type.GetGenericArguments()[0], static valueType => PackEntryDefinition.MakeGenericMethod(valueType));
            return (byte[])pack.Invoke(this, [obj])!;
        }

        return FusionL2Frame.Create(0, 0, null, null, EncodeClrValue(obj));
    }

    /// <inheritdoc />
    public T? Deserialize<T>(byte[] data)
    {
        ArgumentHelpers.ThrowIfNull(data);
        if (data.Length == 0)
            return default;

        if (FusionL2Frame.TryParse(data, out var header, out var valueBytes)) {
            var type = typeof(T);
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(FusionCacheDistributedEntry<>)) {
                if (type.GetGenericArguments()[0] == typeof(byte[]))
                    return (T)(object)UnpackEntry<byte[]>(header, valueBytes);

                var unpack = UnpackEntryMethods.GetOrAdd(type.GetGenericArguments()[0], static valueType => UnpackEntryDefinition.MakeGenericMethod(valueType));
                return (T?)unpack.Invoke(this, [header, valueBytes]);
            }

            return DecodeClrValue<T>(valueBytes);
        }

        if (data[0] is (byte)'{' or (byte)'[')
            return JsonSerializer.Deserialize<T>(data);

        throw new InvalidDataException("Fusion L2 blob is neither a LYO2 frame nor JSON.");
    }

    /// <inheritdoc />
    public ValueTask<byte[]> SerializeAsync<T>(T? obj, CancellationToken token = default) => new(Serialize(obj));

    /// <inheritdoc />
    public ValueTask<T?> DeserializeAsync<T>(byte[] data, CancellationToken token = default) => new(Deserialize<T>(data));

    private byte[] PackEntry<TValue>(FusionCacheDistributedEntry<TValue> entry)
        => FusionL2Frame.Create(entry.Timestamp, entry.LogicalExpirationTimestamp, entry.Tags, entry.Metadata, EncodeValue(entry.Value));

    private FusionCacheDistributedEntry<TValue> UnpackEntry<TValue>(FusionL2Frame.Header header, byte[] valueBytes)
        => new(DecodeValue<TValue>(valueBytes), header.Timestamp, header.LogicalExpirationTimestamp, header.Tags, header.Metadata);

    private byte[] EncodeValue<TValue>(TValue? value)
    {
        if (typeof(TValue) == typeof(byte[]) || value is byte[])
            return EncodeBytes(value as byte[]);

        return EncodeClrValue(value);
    }

    private TValue DecodeValue<TValue>(byte[] valueBytes)
    {
        if (typeof(TValue) == typeof(byte[]))
            return (TValue)(object)valueBytes;

        return DecodeClrValue<TValue>(valueBytes)!;
    }

    private byte[] EncodeBytes(byte[]? bytes)
    {
        if (bytes is null || bytes.Length == 0)
            return _codec.Encode([]);

        return _codec.IsFramed(bytes) ? bytes : _codec.Encode(bytes);
    }

    private byte[] EncodeClrValue<TValue>(TValue? value)
    {
        var plain = _payloadSerializer.Serialize(value);
        return _codec.Encode(plain ?? []);
    }

    private TValue? DecodeClrValue<TValue>(byte[] valueBytes)
    {
        var envelope = _codec.IsFramed(valueBytes) ? _codec.Decode(valueBytes) : new CacheEntryEnvelope(valueBytes);
        return _payloadSerializer.Deserialize<TValue>(envelope.Payload);
    }
}
