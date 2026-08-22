using System.Diagnostics;
using Lyo.Cache.Internal;
using Lyo.Exceptions;

namespace Lyo.Cache;

/// <summary>
/// Lightweight L1 snapshot entry for observability: either a logical cache key or a tag marker listed in <see cref="ICacheService.Items" />.
/// </summary>
/// <remarks>
/// <see cref="ICacheService.Items" /> is this process's in-memory (L1) list. Other processes can write Redis (L2) keys that never appear here until this process loads them.
/// <see cref="Encrypted" />, <see cref="Compressed" />, <see cref="SizeBytes" />, <see cref="Expires" />, and <see cref="Tags" /> apply to keys; tag-index rows leave them null.
/// <see cref="Expires" /> is the UTC instant this process expects the entry to drop (absolute TTL from last write, or sliding TTL from last successful access).
/// </remarks>
/// <param name="Name">Normalized cache key or tag-index name.</param>
/// <param name="Type">Whether this row is a cache key or a tag marker.</param>
/// <param name="Created">When this process first tracked the entry.</param>
/// <param name="Encrypted">Encrypt flag from the stored payload frame. Null for tags; false for object-cache keys that are not framed.</param>
/// <param name="Compressed">Compress flag from the stored payload frame. Null for tags; false for object-cache keys that are not framed.</param>
/// <param name="SizeBytes">Stored byte length for this process's copy. Set for byte payloads; null for object-cache keys and tags.</param>
/// <param name="Expires">UTC instant when this L1 key is expected to expire. Null for tags and when TTL is unknown.</param>
/// <param name="Tags">Normalized tags attached to this key. Null for tag-index rows and keys with no tags.</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record CacheItem(
    string Name,
    CacheItemTypeEnum Type,
    DateTime Created,
    bool? Encrypted = null,
    bool? Compressed = null,
    long? SizeBytes = null,
    DateTime? Expires = null,
    IReadOnlyList<string>? Tags = null)
{
    /// <summary>Case-insensitive equality for <see cref="Name" /> with matching <see cref="Type" />. Storage flags and timestamps are ignored so the item can be used as a dictionary key.</summary>
    public bool Equals(CacheItem? other) => other is not null && Type == other.Type && string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase);

    /// <summary>Creates an item representing a stored cache key.</summary>
    /// <param name="name">Normalized cache key.</param>
    /// <param name="created">When the process first tracked this key; defaults to UTC now.</param>
    /// <param name="encrypted">Encrypt flag from the stored payload frame, or false for object-cache keys.</param>
    /// <param name="compressed">Compress flag from the stored payload frame, or false for object-cache keys.</param>
    /// <param name="sizeBytes">Stored byte length when known.</param>
    /// <param name="expires">UTC instant when this key is expected to expire.</param>
    /// <param name="tags">Normalized tags attached to this key.</param>
    public static CacheItem Key(
        string name,
        DateTime? created = null,
        bool? encrypted = null,
        bool? compressed = null,
        long? sizeBytes = null,
        DateTime? expires = null,
        IReadOnlyList<string>? tags = null)
        => new(name, CacheItemTypeEnum.Key, created ?? DateTime.UtcNow, encrypted, compressed, sizeBytes, expires, tags);

    /// <summary>Creates an item representing a tag used in the tag index. Storage flags, <see cref="Expires" />, and <see cref="Tags" /> stay null.</summary>
    /// <param name="name">Normalized tag-index name.</param>
    /// <param name="created">When the process first tracked this tag; defaults to UTC now.</param>
    public static CacheItem Tag(string name, DateTime? created = null) => new(name, CacheItemTypeEnum.Tag, created ?? DateTime.UtcNow);

    /// <summary>
    /// Builds a key item from the bytes this process stored. Framed <c>LYO1</c> payloads set <see cref="Encrypted" /> / <see cref="Compressed" /> from flags;
    /// other byte arrays report both as false. <see cref="SizeBytes" /> is always <paramref name="stored" />.Length.
    /// </summary>
    public static CacheItem FromStoredBytes(string name, byte[] stored, DateTime? created = null, DateTime? expires = null, IReadOnlyList<string>? tags = null)
    {
        ArgumentHelpers.ThrowIfNull(stored);
        var encrypted = false;
        var compressed = false;

        if (!CachePayloadFrame.TryInspect(stored, out var flags, out _))
            return Key(name, created, encrypted, compressed, stored.Length, expires, tags);

        encrypted = (flags & CachePayloadFrame.FlagEncrypted) != 0;
        compressed = (flags & CachePayloadFrame.FlagCompressed) != 0;
        return Key(name, created, encrypted, compressed, stored.Length, expires, tags);
    }

    public override string ToString()
    {
        if (Type != CacheItemTypeEnum.Key)
            return $"{Type}: {Name} {Created:g}";

        var enc = Encrypted == true ? " enc" : "";
        var zip = Compressed == true ? " zip" : "";
        var size = SizeBytes is { } b ? $" {b}B" : "";
        var exp = Expires is { } e ? $" exp {e:g}" : "";
        return $"{Type}: {Name}{enc}{zip}{size}{exp} {Created:g}";
    }

    public override int GetHashCode()
    {
        unchecked {
            var hashCode = StringComparer.OrdinalIgnoreCase.GetHashCode(Name);
            hashCode = (hashCode * 397) ^ (int)Type;
            return hashCode;
        }
    }
}
