using System.Collections.Concurrent;
using Lyo.Exceptions;

namespace Lyo.FileTags;

/// <summary>Thread-safe Name + Id lookup for <see cref="FileTagKey" />.</summary>
internal static class FileTagCatalog
{
    public static T Register<T>(ConcurrentDictionary<string, T> byName, ConcurrentDictionary<string, T> byId, T instance, string name, string id)
        where T : class
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(name);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(id);
        byName.TryAdd(name, instance);
        byId.TryAdd(NormalizeId(id), instance);
        return instance;
    }

    public static T? TryFromId<T>(ConcurrentDictionary<string, T> byId, string? id)
        where T : class
        => !string.IsNullOrEmpty(id) && byId.TryGetValue(NormalizeId(id), out var found) ? found : null;

    public static T? TryFromName<T>(ConcurrentDictionary<string, T> byName, string? name)
        where T : class
        => !string.IsNullOrEmpty(name) && byName.TryGetValue(name, out var found) ? found : null;

    public static T GetOrCustom<T>(ConcurrentDictionary<string, T> byId, ConcurrentDictionary<string, T> byName, string id, Func<string, T> factory)
        where T : class
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(id);
        var key = NormalizeId(id);
        var stored = byId.GetOrAdd(key, _ => factory(key));
        byName.AddOrUpdate(key, stored, (_, _) => stored);
        return stored;
    }

    public static string NormalizeId(string id) => id.Trim().TrimStart('.').ToLowerInvariant();
}
