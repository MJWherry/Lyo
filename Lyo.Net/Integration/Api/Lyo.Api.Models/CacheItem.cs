using System.Diagnostics;
using Lyo.Api.Models.Enums;

namespace Lyo.Api.Models;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record CacheItem(
    CacheItemTypeEnum Type,
    string Name,
    DateTime Created,
    bool? Encrypted = null,
    bool? Compressed = null,
    long? SizeBytes = null,
    DateTime? Expires = null)
{
    public static CacheItem Key(
        string name,
        DateTime? created = null,
        bool? encrypted = null,
        bool? compressed = null,
        long? sizeBytes = null,
        DateTime? expires = null)
        => new(CacheItemTypeEnum.Key, name, created ?? DateTime.UtcNow, encrypted, compressed, sizeBytes, expires);

    public static CacheItem Tag(string name, DateTime? created = null) => new(CacheItemTypeEnum.Tag, name, created ?? DateTime.UtcNow);

    public override int GetHashCode()
    {
        unchecked {
            var hashCode = Name.GetHashCode();
            hashCode = (hashCode * 397) ^ (int)Type;
            return hashCode;
        }
    }

    public override string ToString()
    {
        var exp = Expires is { } e ? $" exp {e:g}" : "";
        return $"{Type.ToString()}: {Name}{exp} {Created:g}";
    }
}
