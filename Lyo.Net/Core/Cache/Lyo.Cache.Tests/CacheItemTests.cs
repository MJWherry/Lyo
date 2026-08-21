namespace Lyo.Cache.Tests;

public class CacheItemTests
{
    [Fact]
    public void CacheItem_WithSameNameAndTypeButDifferentCreated_AreEqual()
    {
        var item1 = CacheItem.Tag("__tag:entity:personentity", new DateTime(2026, 3, 8, 21, 36, 0, DateTimeKind.Utc));
        var item2 = CacheItem.Tag("__tag:entity:personentity", new DateTime(2026, 3, 8, 21, 42, 0, DateTimeKind.Utc));
        Assert.True(item1.Equals(item2));
        Assert.Equal(item1.GetHashCode(), item2.GetHashCode());
    }

    [Fact]
    public void CacheItem_NameComparison_IsCaseInsensitive()
    {
        var item1 = CacheItem.Tag("__tag:entity:personentity");
        var item2 = CacheItem.Tag("__TAG:ENTITY:PERSONENTITY");
        Assert.True(item1.Equals(item2));
        Assert.Equal(item1.GetHashCode(), item2.GetHashCode());
    }

    [Fact]
    public void CacheItem_Equality_IgnoresStorageFlags()
    {
        var a = CacheItem.Key("k", encrypted: true, compressed: false, sizeBytes: 10);
        var b = CacheItem.Key("K", encrypted: false, compressed: true, sizeBytes: 99);
        Assert.True(a.Equals(b));
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void CacheItem_Equality_IgnoresExpires()
    {
        var a = CacheItem.Key("k", expires: new DateTime(2026, 8, 21, 12, 0, 0, DateTimeKind.Utc));
        var b = CacheItem.Key("K", expires: new DateTime(2026, 8, 21, 13, 0, 0, DateTimeKind.Utc));
        Assert.True(a.Equals(b));
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void FromStoredBytes_RawArray_ReportsSizeWithoutFlags()
    {
        var stored = new byte[] { 1, 2, 3, 4 };
        var item = CacheItem.FromStoredBytes("raw-key", stored);
        Assert.Equal(CacheItemTypeEnum.Key, item.Type);
        Assert.Equal("raw-key", item.Name);
        Assert.False(item.Encrypted);
        Assert.False(item.Compressed);
        Assert.Equal(4, item.SizeBytes);
    }

    [Fact]
    public void FromStoredBytes_FramedPayload_ReadsEncryptAndCompressFlags()
    {
        var payload = new byte[] { 9, 8, 7 };
        var framed = new byte[9 + payload.Length];
        "LYO1"u8.CopyTo(framed);
        framed[4] = 0x03;
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(framed.AsSpan(5, 4), (uint)payload.Length);
        payload.CopyTo(framed.AsSpan(9));

        var item = CacheItem.FromStoredBytes("framed-key", framed);
        Assert.True(item.Encrypted);
        Assert.True(item.Compressed);
        Assert.Equal(framed.Length, item.SizeBytes);
    }

    [Fact]
    public void FromStoredBytes_WithExpires_SetsExpiration()
    {
        var expires = new DateTime(2026, 8, 21, 18, 0, 0, DateTimeKind.Utc);
        var item = CacheItem.FromStoredBytes("raw-key", [1, 2, 3], expires: expires);
        Assert.Equal(expires, item.Expires);
        Assert.Equal(3, item.SizeBytes);
    }

    [Fact]
    public void Tag_LeavesStorageFlagsNull()
    {
        var item = CacheItem.Tag("queries");
        Assert.Null(item.Encrypted);
        Assert.Null(item.Compressed);
        Assert.Null(item.SizeBytes);
        Assert.Null(item.Expires);
    }
}