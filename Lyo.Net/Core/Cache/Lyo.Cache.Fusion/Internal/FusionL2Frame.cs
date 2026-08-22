using System.Buffers.Binary;
using System.Text;
using ZiggyCreatures.Caching.Fusion.Internals;

namespace Lyo.Cache.Fusion.Internal;

/// <summary>Binary Fusion L2 envelope: magic LYO2, metadata header, then raw value bytes (already codec-framed when applicable).</summary>
internal static class FusionL2Frame
{
    internal const byte Version = 1;
    internal const byte FlagHasMetadata = 0x01;
    internal const byte MetaIsStale = 0x01;
    internal const byte MetaHasEagerExpiration = 0x02;
    internal const byte MetaHasETag = 0x04;
    internal const byte MetaHasLastModified = 0x08;
    internal const byte MetaHasSize = 0x10;
    internal const byte MetaHasPriority = 0x20;
    internal const int FixedPrefixLength = 24;

    internal readonly record struct Header(
        long Timestamp,
        long LogicalExpirationTimestamp,
        string[]? Tags,
        FusionCacheEntryMetadata? Metadata);

    private static ReadOnlySpan<byte> Magic => "LYO2"u8;

    internal static bool IsFramed(ReadOnlySpan<byte> data) => data.Length >= FixedPrefixLength && data.StartsWith(Magic);

    internal static byte[] Create(long timestamp, long logicalExpirationTimestamp, string[]? tags, FusionCacheEntryMetadata? metadata, ReadOnlySpan<byte> value)
    {
        var tagBytes = EncodeTags(tags);
        var metaSize = MetadataSize(metadata);
        var flags = metadata is null ? (byte)0 : FlagHasMetadata;
        var tagCount = tags is { Length: > 0 } ? Math.Min(tags.Length, ushort.MaxValue) : 0;
        var length = FixedPrefixLength + tagBytes.Length + metaSize + 4 + value.Length;
        var buf = new byte[length];
        Magic.CopyTo(buf);
        buf[4] = Version;
        buf[5] = flags;
        BinaryPrimitives.WriteInt64LittleEndian(buf.AsSpan(6, 8), timestamp);
        BinaryPrimitives.WriteInt64LittleEndian(buf.AsSpan(14, 8), logicalExpirationTimestamp);
        BinaryPrimitives.WriteUInt16LittleEndian(buf.AsSpan(22, 2), (ushort)tagCount);
        var offset = FixedPrefixLength;
        tagBytes.CopyTo(buf.AsSpan(offset));
        offset += tagBytes.Length;
        if (metadata is not null)
            offset += WriteMetadata(buf.AsSpan(offset), metadata);

        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(offset, 4), (uint)value.Length);
        offset += 4;
        value.CopyTo(buf.AsSpan(offset));
        return buf;
    }

    internal static bool TryParse(ReadOnlySpan<byte> data, out Header header, out byte[] value)
    {
        header = default;
        value = [];
        if (!IsFramed(data) || data[4] != Version)
            return false;

        var flags = data[5];
        var timestamp = BinaryPrimitives.ReadInt64LittleEndian(data.Slice(6, 8));
        var logicalExpiration = BinaryPrimitives.ReadInt64LittleEndian(data.Slice(14, 8));
        var tagCount = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(22, 2));
        var offset = FixedPrefixLength;
        if (!TryReadTags(data, ref offset, tagCount, out var tags))
            return false;

        FusionCacheEntryMetadata? metadata = null;
        if ((flags & FlagHasMetadata) != 0) {
            if (!TryReadMetadata(data, ref offset, out metadata))
                return false;
        }

        if (offset + 4 > data.Length)
            return false;

        var valueLength = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(offset, 4));
        offset += 4;
        if (valueLength > int.MaxValue - offset || offset + (int)valueLength != data.Length)
            return false;

        value = data.Slice(offset, (int)valueLength).ToArray();
        header = new(timestamp, logicalExpiration, tags, metadata);
        return true;
    }

    private static byte[] EncodeTags(string[]? tags)
    {
        if (tags is not { Length: > 0 })
            return [];

        var count = Math.Min(tags.Length, ushort.MaxValue);
        var encoded = new byte[count][];
        var total = 0;
        for (var i = 0; i < count; i++) {
            encoded[i] = Encoding.UTF8.GetBytes(tags[i] ?? "");
            if (encoded[i].Length > ushort.MaxValue)
                encoded[i] = encoded[i].AsSpan(0, ushort.MaxValue).ToArray();

            total += 2 + encoded[i].Length;
        }

        var buf = new byte[total];
        var offset = 0;
        for (var i = 0; i < count; i++) {
            BinaryPrimitives.WriteUInt16LittleEndian(buf.AsSpan(offset, 2), (ushort)encoded[i].Length);
            offset += 2;
            encoded[i].CopyTo(buf.AsSpan(offset));
            offset += encoded[i].Length;
        }

        return buf;
    }

    private static bool TryReadTags(ReadOnlySpan<byte> data, ref int offset, int tagCount, out string[]? tags)
    {
        tags = null;
        if (tagCount == 0)
            return true;

        var list = new string[tagCount];
        for (var i = 0; i < tagCount; i++) {
            if (offset + 2 > data.Length)
                return false;

            var len = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(offset, 2));
            offset += 2;
            if (offset + len > data.Length)
                return false;

            list[i] = Encoding.UTF8.GetString(data.Slice(offset, len).ToArray());
            offset += len;
        }

        tags = list;
        return true;
    }

    private static int MetadataSize(FusionCacheEntryMetadata? metadata)
    {
        if (metadata is null)
            return 0;

        var size = 1;
        if (metadata.EagerExpirationTimestamp.HasValue)
            size += 8;
        if (!string.IsNullOrEmpty(metadata.ETag))
            size += 2 + Math.Min(Encoding.UTF8.GetByteCount(metadata.ETag), ushort.MaxValue);
        if (metadata.LastModifiedTimestamp.HasValue)
            size += 8;
        if (metadata.Size.HasValue)
            size += 8;
        if (metadata.Priority.HasValue)
            size += 1;

        return size;
    }

    private static int WriteMetadata(Span<byte> dest, FusionCacheEntryMetadata metadata)
    {
        byte metaFlags = 0;
        if (metadata.IsStale)
            metaFlags |= MetaIsStale;
        if (metadata.EagerExpirationTimestamp.HasValue)
            metaFlags |= MetaHasEagerExpiration;
        if (!string.IsNullOrEmpty(metadata.ETag))
            metaFlags |= MetaHasETag;
        if (metadata.LastModifiedTimestamp.HasValue)
            metaFlags |= MetaHasLastModified;
        if (metadata.Size.HasValue)
            metaFlags |= MetaHasSize;
        if (metadata.Priority.HasValue)
            metaFlags |= MetaHasPriority;

        dest[0] = metaFlags;
        var offset = 1;
        if (metadata.EagerExpirationTimestamp.HasValue) {
            BinaryPrimitives.WriteInt64LittleEndian(dest.Slice(offset, 8), metadata.EagerExpirationTimestamp.Value);
            offset += 8;
        }

        if (!string.IsNullOrEmpty(metadata.ETag)) {
            var etag = Encoding.UTF8.GetBytes(metadata.ETag);
            if (etag.Length > ushort.MaxValue)
                etag = etag.AsSpan(0, ushort.MaxValue).ToArray();

            BinaryPrimitives.WriteUInt16LittleEndian(dest.Slice(offset, 2), (ushort)etag.Length);
            offset += 2;
            etag.CopyTo(dest.Slice(offset));
            offset += etag.Length;
        }

        if (metadata.LastModifiedTimestamp.HasValue) {
            BinaryPrimitives.WriteInt64LittleEndian(dest.Slice(offset, 8), metadata.LastModifiedTimestamp.Value);
            offset += 8;
        }

        if (metadata.Size.HasValue) {
            BinaryPrimitives.WriteInt64LittleEndian(dest.Slice(offset, 8), metadata.Size.Value);
            offset += 8;
        }

        if (metadata.Priority.HasValue) {
            dest[offset] = metadata.Priority.Value;
            offset += 1;
        }

        return offset;
    }

    private static bool TryReadMetadata(ReadOnlySpan<byte> data, ref int offset, out FusionCacheEntryMetadata? metadata)
    {
        metadata = null;
        if (offset >= data.Length)
            return false;

        var metaFlags = data[offset];
        offset += 1;
        long? eager = null;
        string? etag = null;
        long? lastModified = null;
        long? size = null;
        byte? priority = null;
        if ((metaFlags & MetaHasEagerExpiration) != 0) {
            if (offset + 8 > data.Length)
                return false;

            eager = BinaryPrimitives.ReadInt64LittleEndian(data.Slice(offset, 8));
            offset += 8;
        }

        if ((metaFlags & MetaHasETag) != 0) {
            if (offset + 2 > data.Length)
                return false;

            var len = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(offset, 2));
            offset += 2;
            if (offset + len > data.Length)
                return false;

            etag = Encoding.UTF8.GetString(data.Slice(offset, len).ToArray());
            offset += len;
        }

        if ((metaFlags & MetaHasLastModified) != 0) {
            if (offset + 8 > data.Length)
                return false;

            lastModified = BinaryPrimitives.ReadInt64LittleEndian(data.Slice(offset, 8));
            offset += 8;
        }

        if ((metaFlags & MetaHasSize) != 0) {
            if (offset + 8 > data.Length)
                return false;

            size = BinaryPrimitives.ReadInt64LittleEndian(data.Slice(offset, 8));
            offset += 8;
        }

        if ((metaFlags & MetaHasPriority) != 0) {
            if (offset >= data.Length)
                return false;

            priority = data[offset];
            offset += 1;
        }

        metadata = new FusionCacheEntryMetadata((metaFlags & MetaIsStale) != 0, eager, etag, lastModified, size, priority);
        return true;
    }
}
