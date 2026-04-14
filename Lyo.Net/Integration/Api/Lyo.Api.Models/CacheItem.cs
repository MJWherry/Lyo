using System.Diagnostics;
using Lyo.Api.Models.Enums;

namespace Lyo.Api.Models;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record CacheItem(CacheItemTypeEnum Type, string Name, DateTime Created)
{
    public static CacheItem Key(string name, DateTime? created = null) => new(CacheItemTypeEnum.Key, name, created ?? DateTime.UtcNow);

    public static CacheItem Tag(string name, DateTime? created = null) => new(CacheItemTypeEnum.Tag, name, created ?? DateTime.UtcNow);

    public override int GetHashCode()
    {
        unchecked {
            var hashCode = Name.GetHashCode();
            hashCode = (hashCode * 397) ^ (int)Type;
            return hashCode;
        }
    }

    public override string ToString() => $"{Type.ToString()}: {Name} {Created:g}";
}