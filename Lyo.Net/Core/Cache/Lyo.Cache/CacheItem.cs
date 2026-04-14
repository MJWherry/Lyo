using System.Diagnostics;

namespace Lyo.Cache;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record CacheItem(string Name, CacheItemTypeEnum Type, DateTime Created)
{
    public bool Equals(CacheItem? other) => other is not null && Type == other.Type && string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase);

    public static CacheItem Key(string name, DateTime? created = null) => new(name, CacheItemTypeEnum.Key, created ?? DateTime.UtcNow);

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