using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Lyo.Common.Records;

namespace Lyo.Common;

public static class GenericComparer
{
    private static ConcurrentDictionary<Type, IList<PropertyInfo>> PropertyDefinitions { get; } = new();

    /// <summary>Gets property differences between two objects as a dictionary.</summary>
    [return: NotNull]
    public static Dictionary<string, Tuple<object?, object?>> GetDifferences<T1, T2>(in T1? obj1, in T2? obj2)
        where T1 : class where T2 : class
        => GetDifferences(obj1, obj2, null);

    /// <summary>Gets property differences between two objects, optionally excluding properties.</summary>
    /// <param name="excludeProperty">Optional predicate - properties for which this returns true are skipped.</param>
    [return: NotNull]
    public static Dictionary<string, Tuple<object?, object?>> GetDifferences<T1, T2>(in T1? obj1, in T2? obj2, Func<string, bool>? excludeProperty)
        where T1 : class where T2 : class
    {
        var dict = GetDifferencesInternal(obj1, obj2, excludeProperty);
        return dict;
    }

    /// <summary>Gets property differences as a list of PropertyDifference records.</summary>
    [return: NotNull]
    public static IReadOnlyList<PropertyDifference> GetPropertyDifferences<T1, T2>(in T1? obj1, in T2? obj2)
        where T1 : class where T2 : class
        => GetPropertyDifferences(obj1, obj2, null);

    /// <summary>Gets property differences as a list of PropertyDifference records, optionally excluding properties.</summary>
    [return: NotNull]
    public static IReadOnlyList<PropertyDifference> GetPropertyDifferences<T1, T2>(in T1? obj1, in T2? obj2, Func<string, bool>? excludeProperty)
        where T1 : class where T2 : class
    {
        var dict = GetDifferencesInternal(obj1, obj2, excludeProperty);
        return dict.Select(kvp => new PropertyDifference(kvp.Key, kvp.Value.Item1, kvp.Value.Item2)).ToList();
    }

    private static Dictionary<string, Tuple<object?, object?>> GetDifferencesInternal<T1, T2>(in T1? obj1, in T2? obj2, Func<string, bool>? excludeProperty)
        where T1 : class where T2 : class
    {
        var t1 = typeof(T1);
        var t2 = typeof(T2);
        if (!PropertyDefinitions.ContainsKey(t1))
            PropertyDefinitions.TryAdd(t1, t1.GetProperties(BindingFlags.Public | BindingFlags.Instance));

        if (!PropertyDefinitions.ContainsKey(t2))
            PropertyDefinitions.TryAdd(t2, t2.GetProperties(BindingFlags.Public | BindingFlags.Instance));

        var differences = new Dictionary<string, Tuple<object?, object?>>(StringComparer.OrdinalIgnoreCase);
        if (obj1 == null && obj2 == null)
            return differences;

        if (obj1 == null) {
            foreach (var prop2 in PropertyDefinitions[t2]) {
                if (excludeProperty?.Invoke(prop2.Name) == true)
                    continue;

                var value2 = prop2.GetValue(obj2);
                differences[prop2.Name] = Tuple.Create<object?, object?>(null, value2);
            }

            return differences;
        }

        if (obj2 == null) {
            foreach (var prop1 in PropertyDefinitions[t1]) {
                if (excludeProperty?.Invoke(prop1.Name) == true)
                    continue;

                var value1 = prop1.GetValue(obj1);
                differences[prop1.Name] = Tuple.Create<object?, object?>(value1, null);
            }

            return differences;
        }

        var type1Props = PropertyDefinitions[t1];
        var type2PropsDict = PropertyDefinitions[t2].ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);
        foreach (var prop1 in type1Props) {
            if (excludeProperty?.Invoke(prop1.Name) == true)
                continue;

            if (!type2PropsDict.TryGetValue(prop1.Name, out var prop2))
                continue;

            var value1 = IsEnumOrNullableEnum(prop1.PropertyType) ? prop1.GetValue(obj1)?.ToString() : prop1.GetValue(obj1);
            var value2 = IsEnumOrNullableEnum(prop2.PropertyType) ? prop2.GetValue(obj2)?.ToString() : prop2.GetValue(obj2);
            if (!AreEqual(value1, value2))
                differences[prop1.Name] = Tuple.Create(value1, value2);
        }

        return differences;
    }

    private static bool IsEnumOrNullableEnum(Type type) => type.IsEnum || (Nullable.GetUnderlyingType(type)?.IsEnum ?? false);

    private static bool AreEqual(object? value1, object? value2)
    {
        if (value1 == null && value2 == null)
            return true;

        if (value1 == null || value2 == null)
            return false;

        return value1.Equals(value2);
    }
}