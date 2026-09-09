using System.Collections;
using System.Globalization;
using System.Reflection;
using Lyo.Exceptions;

namespace Lyo.Web.Components.LyoType;

/// <summary>
/// Constructs a nested placeholder dictionary from a CLR type, so template autocomplete can offer the paths a DTO will expose before any real instance exists.
/// </summary>
/// <remarks>
/// A job definition that has never run has no <c>JobRunRes</c> to walk, and a catalog built from types alone is not an option: the formatter context catalog walks
/// <em>values</em>, so a null property yields no children. Filling every property with a placeholder of the right shape keeps the paths discoverable, and the
/// preview column simply displays placeholder text until a real run exists.
/// </remarks>
public static class LyoSampleShape
{
    /// <summary>
    /// Nesting levels to expand, counting the root object's own properties as level 1. Matches the depth the formatter context catalog walks, so this never produces
    /// paths deeper than the dropdown will show.
    /// </summary>
    public const int MaxDepth = 3;

    private static readonly Uri SampleUri = new("https://example.com");

    /// <summary>Placeholder graph for <paramref name="type" />'s public readable properties, expanded <see cref="MaxDepth" /> levels deep.</summary>
    public static Dictionary<string, object?> FromType(Type type) => FromType(type, MaxDepth);

    /// <summary>Placeholder graph for <paramref name="type" />, expanded <paramref name="maxDepth" /> levels deep. Depths below 1 are treated as 1.</summary>
    public static Dictionary<string, object?> FromType(Type type, int maxDepth)
    {
        ArgumentHelpers.ThrowIfNull(type);
        return BuildObject(type, Math.Max(1, maxDepth), new HashSet<Type>());
    }

    private static Dictionary<string, object?> BuildObject(Type type, int remainingDepth, HashSet<Type> ancestors)
    {
        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
            if (!prop.CanRead || prop.GetIndexParameters().Length > 0)
                continue;

            result[prop.Name] = SampleFor(prop.PropertyType, prop.Name, remainingDepth, ancestors);
        }

        return result;
    }

    private static object? SampleFor(Type type, string propertyName, int remainingDepth, HashSet<Type> ancestors)
    {
        var target = Nullable.GetUnderlyingType(type) ?? type;
        if (target == typeof(string))
            return propertyName.ToLower(CultureInfo.InvariantCulture);
        if (target == typeof(bool))
            return false;
        if (target == typeof(Guid))
            return Guid.Empty;
        if (target == typeof(DateTime))
            return DateTime.UtcNow;
        if (target == typeof(DateTimeOffset))
            return DateTimeOffset.UtcNow;
        if (target == typeof(DateOnly))
            return DateOnly.FromDateTime(DateTime.UtcNow);
        if (target == typeof(TimeOnly))
            return TimeOnly.FromDateTime(DateTime.UtcNow);
        if (target == typeof(TimeSpan))
            return TimeSpan.Zero;
        if (target == typeof(decimal))
            return 0m;
        if (target == typeof(Uri))
            return SampleUri;
        if (target == typeof(byte[]))
            return Array.Empty<byte>();
        if (target.IsEnum)
            return Enum.GetNames(target).FirstOrDefault() ?? "";
        if (target.IsPrimitive)
            return Activator.CreateInstance(target);

        // Dictionaries and collections stay empty: a placeholder element would invent paths that a real payload may not have.
        if (typeof(IDictionary).IsAssignableFrom(target))
            return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        if (typeof(IEnumerable).IsAssignableFrom(target))
            return new List<object?>();

        // Deeper than the dropdown displays, or a cycle back to a type already on this path: the type name reads better than an empty object.
        if (remainingDepth <= 1 || !ancestors.Add(target))
            return target.Name;

        try {
            return BuildObject(target, remainingDepth - 1, ancestors);
        }
        finally {
            ancestors.Remove(target);
        }
    }
}
