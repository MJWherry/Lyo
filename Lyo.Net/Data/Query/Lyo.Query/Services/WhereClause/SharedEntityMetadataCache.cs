using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Lyo.Query.Models.Attributes;
using static Lyo.Cache.Constants;

namespace Lyo.Query.Services.WhereClause;

/// <summary>Static in-memory caches for entity metadata, shared by BaseWhereClauseService and ProjectionService.</summary>
public static class SharedEntityMetadataCache
{
    private const BindingFlags PropertySearchFlags = BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance;
    private static readonly Type IEnumerableGenericType = typeof(IEnumerable<>);

    private static readonly ConcurrentDictionary<string, PropertyInfo?> PropertyCache = new();
    private static readonly ConcurrentDictionary<string, string> NormalizedPathCache = new();
    private static readonly ConcurrentDictionary<string, PropertyPathMetadata> PropertyPathCache = new();
    private static readonly ConcurrentDictionary<Type, Type> CollectionElementTypeCache = new();
    private static readonly ConcurrentDictionary<string, ComparisonMetadata> ComparisonMetadataCache = new();
    private static readonly ConcurrentDictionary<string, MethodInfo> ReflectedMethodCache = new();
    private static readonly ConcurrentDictionary<Type, CollectionMetadata> CollectionAdjustmentCache = new();
    private static readonly ConcurrentDictionary<string, MethodInfo> OrderMethodCache = new();

    /// <summary>Resolves a property by name. Shared by query and projection services.</summary>
    public static PropertyInfo? ResolveProperty(Type type, string name)
        => PropertyCache.GetOrAdd($"{EntityMetadata.PropertyPrefix}{type.FullName}:{name}", _ => ResolvePropertyCore(type, name));

    /// <summary>Normalizes a field path. Shared by projection service.</summary>
    public static string NormalizeFieldPath(Type rootType, string path)
        => NormalizedPathCache.GetOrAdd($"{EntityMetadata.NormalizedPathPrefix}{rootType.FullName}:{path}", _ => NormalizeFieldPathCore(rootType, path));

    /// <summary>
    /// Attempts to normalize a field path without throwing. Failures are not cached (only successful normalizations use <see cref="NormalizedPathCache" />).
    /// </summary>
    public static bool TryNormalizeFieldPath(Type rootType, string path, [NotNullWhen(true)] out string? normalized, [NotNullWhen(false)] out string? errorMessage)
    {
        try {
            normalized = NormalizeFieldPath(rootType, path);
            errorMessage = null;
            return true;
        }
        catch (ArgumentException ex) {
            normalized = null;
            errorMessage = ex.Message;
            return false;
        }
    }

    internal static PropertyPathMetadata GetOrAddPropertyPath<T>(string propertyName, Func<PropertyPathMetadata> factory)
        => PropertyPathCache.GetOrAdd($"{EntityMetadata.PropertyPathPrefix}{typeof(T).FullName}:{propertyName}", _ => factory());

    public static Type GetCollectionElementType(Type type) => CollectionElementTypeCache.GetOrAdd(type, GetCollectionElementTypeCore);

    internal static ComparisonMetadata GetOrAddComparisonMetadata(string key, Func<ComparisonMetadata> factory) => ComparisonMetadataCache.GetOrAdd(key, _ => factory());

    public static MethodInfo GetOrAddReflectedMethod(string key, Func<MethodInfo> factory) => ReflectedMethodCache.GetOrAdd(key, _ => factory());

    internal static CollectionMetadata GetOrAddCollectionAdjustment(Type type, Func<CollectionMetadata> factory) => CollectionAdjustmentCache.GetOrAdd(type, _ => factory());

    public static MethodInfo GetOrAddOrderMethod(string key, Func<MethodInfo> factory) => OrderMethodCache.GetOrAdd(key, _ => factory());

    /// <summary>Gets cached AsQueryable method for projection count expressions. Keyed by element type.</summary>
    public static MethodInfo GetProjectionAsQueryableMethod(Type elementType)
        => ReflectedMethodCache.GetOrAdd(
            $"{EntityMetadata.ReflectedMethodPrefix}ProjectionAsQueryable:{elementType.FullName}",
            _ => typeof(Queryable).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .First(m => m.Name == nameof(Queryable.AsQueryable) && m.GetParameters().Length == 1 && m.IsGenericMethod)
                .MakeGenericMethod(elementType));

    /// <summary>Gets cached Queryable.Count method for projection count expressions. Keyed by element type.</summary>
    public static MethodInfo GetProjectionQueryableCountMethod(Type elementType)
        => ReflectedMethodCache.GetOrAdd(
            $"{EntityMetadata.ReflectedMethodPrefix}ProjectionQueryableCount:{elementType.FullName}",
            _ => typeof(Queryable).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .First(m => m.Name == nameof(Queryable.Count) && m.GetParameters().Length == 1)
                .MakeGenericMethod(elementType));

    private static PropertyInfo? ResolvePropertyCore(Type type, string name)
    {
        var prop = type.GetProperty(name, PropertySearchFlags);
        if (prop != null)
            return prop;

        return type.GetProperties(PropertySearchFlags)
            .FirstOrDefault(p => {
                var attr = p.GetCustomAttribute<QueryPropertyNameAttribute>();
                return attr != null && string.Equals(attr.PropertyName, name, StringComparison.OrdinalIgnoreCase);
            });
    }

    private static string NormalizeFieldPathCore(Type rootType, string path)
    {
        var currentType = rootType;
        var parts = path.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            throw new ArgumentException($"Invalid selected field path '{path}'.");

        var normalizedParts = new List<string>(parts.Length);
        var lastWasCollection = false;
        for (var i = 0; i < parts.Length; i++) {
            if (IsCollectionType(currentType))
                currentType = GetCollectionElementType(currentType);

            var part = parts[i];
            if (part == "*") {
                if (i != parts.Length - 1)
                    throw new ArgumentException($"Selected field '{path}' contains '*' in an invalid position.");

                normalizedParts.Add(part);
                break;
            }

            if (part.Equals("count", StringComparison.OrdinalIgnoreCase) && lastWasCollection) {
                normalizedParts.Add("count");
                break;
            }

            var property = ResolvePropertyCore(currentType, part);
            if (property == null)
                throw new ArgumentException($"Selected field '{path}' not found on type '{currentType.Name}'.");

            normalizedParts.Add(property.Name);
            currentType = property.PropertyType;
            lastWasCollection = IsCollectionType(currentType);
            if (lastWasCollection)
                currentType = GetCollectionElementType(currentType);
        }

        return string.Join(".", normalizedParts);
    }

    private static Type GetCollectionElementTypeCore(Type type)
    {
        if (type.IsArray)
            return type.GetElementType()!;

        if (type.IsGenericType) {
            var genericArgs = type.GetGenericArguments();
            if (genericArgs.Length == 1)
                return genericArgs[0];
        }

        var interfaces = type.GetInterfaces();
        foreach (var iface in interfaces) {
            if (iface.IsGenericType && iface.GetGenericTypeDefinition() == IEnumerableGenericType)
                return iface.GetGenericArguments()[0];
        }

        return typeof(object);
    }

    /// <summary>Shared by query and projection services. Excludes string and byte[].</summary>
    public static bool IsCollectionType(Type type) => type != typeof(string) && type != typeof(byte[]) && typeof(IEnumerable).IsAssignableFrom(type);
}