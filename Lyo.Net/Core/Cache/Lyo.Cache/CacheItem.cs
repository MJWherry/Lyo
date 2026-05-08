using System.Diagnostics;

namespace Lyo.Cache;

/// <summary>Lightweight snapshot entry for observability: either a logical cache key or a tag marker listed in <see cref="ICacheService.Items" />.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record CacheItem(string Name, CacheItemTypeEnum Type, DateTime Created)
{
    /// <summary>Case-insensitive equality for <see cref="Name" /> with matching <see cref="Type" />.</summary>
    public bool Equals(CacheItem? other) => other is not null && Type == other.Type && string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase);

    /// <summary>Creates an item representing a stored cache key.</summary>
    public static CacheItem Key(string name, DateTime? created = null) => new(name, CacheItemTypeEnum.Key, created ?? DateTime.UtcNow);

    /// <summary>Creates an item representing a tag used in the tag index.</summary>
    public static CacheItem Tag(string name, DateTime? created = null) => new(name, CacheItemTypeEnum.Tag, created ?? DateTime.UtcNow);

    public override string ToString() => $"{Type.ToString()}: {Name} {Created:g}";

    public override int GetHashCode()
    {
        unchecked {
            var hashCode = StringComparer.OrdinalIgnoreCase.GetHashCode(Name);
            hashCode = (hashCode * 397) ^ (int)Type;
            return hashCode;
        }
    }
}