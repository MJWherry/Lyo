using Lyo.EntityReference.Models;
using Lyo.Exceptions;

namespace Lyo.Config;

/// <summary>Model of the resolved configuration for a specific entity.</summary>
public sealed class ResolvedConfigRecord
{
    /// <summary>Value of the target entity type.</summary>
    public string SubjectEntityType { get; set; } = string.Empty;

    /// <summary>Value of the target entity id (string — may be a composite key such as <c>kind:id</c> for app-scoped config).</summary>
    public string SubjectEntityId { get; set; } = string.Empty;

    /// <summary>Holds the resolved config entries.</summary>
    public IReadOnlyList<ResolvedConfigItemRecord> Items { get; set; } = [];

    /// <summary>Returns the referenced entity.</summary>
    public EntityRef ForEntity => EntityRef.ForKey(SubjectEntityType, SubjectEntityId);

    /// <summary>Returns the resolved value for the given key if present.</summary>
    public bool TryGetValue(string key, out ConfigValue? value)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key);
        var item = Items.FirstOrDefault(i => string.Equals(i.Definition.Key, key, StringComparison.Ordinal));
        value = item?.Value;
        return value != null;
    }

    /// <summary>Returns the resolved value for the given key deserialized as T, or the fallback value when missing.</summary>
    public T? GetValue<T>(string key, T? fallback = default) => TryGetValue(key, out var value) ? value!.GetValue<T>(ConfigJsonSerializerOptions.Default) : fallback;

    /// <summary>
    /// Raises <see cref="InvalidOperationException" /> when any definition with <see cref="ConfigDefinitionRecord.IsRequired" /> has no resolved value (no binding and no
    /// fallback).
    /// </summary>
    public void ValidateRequired()
    {
        foreach (var item in Items) {
            if (!item.Definition.IsRequired)
                continue;

            if (item.Value != null)
                continue;

            OperationHelpers.ThrowIf(
                true, $"Required config key '{item.Definition.Key}' for entity type '{SubjectEntityType}' (id '{SubjectEntityId}') has no binding and no default.");
        }
    }

    /// <summary>Gives the resolved config values indexed by key.</summary>
    public IReadOnlyDictionary<string, ConfigValue?> AsDictionary() => Items.ToDictionary(i => i.Definition.Key, i => i.Value, StringComparer.Ordinal);
}