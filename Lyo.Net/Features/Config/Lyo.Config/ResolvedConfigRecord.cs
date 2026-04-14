using Lyo.Common;
using Lyo.Exceptions;

namespace Lyo.Config;

/// <summary>Represents the resolved configuration for a specific entity.</summary>
public sealed class ResolvedConfigRecord
{
    /// <summary>Gets or sets the target entity type.</summary>
    public string ForEntityType { get; set; } = string.Empty;

    /// <summary>Gets or sets the target entity id.</summary>
    public string ForEntityId { get; set; } = string.Empty;

    /// <summary>Gets or sets the resolved config entries.</summary>
    public IReadOnlyList<ResolvedConfigItemRecord> Items { get; set; } = [];

    /// <summary>Gets the referenced entity.</summary>
    public EntityRef ForEntity => new(ForEntityType, ForEntityId);

    /// <summary>Gets the resolved value for the given key if present.</summary>
    public bool TryGetValue(string key, out ConfigValue? value)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key, nameof(key));
        var item = Items.FirstOrDefault(i => string.Equals(i.Definition.Key, key, StringComparison.Ordinal));
        value = item?.Value;
        return value != null;
    }

    /// <summary>Gets the resolved value for the given key deserialized as T, or the fallback value when missing.</summary>
    public T? GetValue<T>(string key, T? fallback = default) => TryGetValue(key, out var value) ? value!.GetValue<T>(ConfigJsonSerializerOptions.Default) : fallback;

    /// <summary>
    /// Throws <see cref="InvalidOperationException" /> when any definition with <see cref="ConfigDefinitionRecord.IsRequired" /> has no resolved value (no binding and no
    /// default).
    /// </summary>
    public void ValidateRequired()
    {
        foreach (var item in Items) {
            if (!item.Definition.IsRequired)
                continue;

            if (item.Value != null)
                continue;

            OperationHelpers.ThrowIf(true, $"Required config key '{item.Definition.Key}' for entity type '{ForEntityType}' (id '{ForEntityId}') has no binding and no default.");
        }
    }

    /// <summary>Returns the resolved config values indexed by key.</summary>
    public IReadOnlyDictionary<string, ConfigValue?> AsDictionary() => Items.ToDictionary(i => i.Definition.Key, i => i.Value, StringComparer.Ordinal);
}