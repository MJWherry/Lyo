namespace Lyo.Pdf.Models;

/// <summary>One parsed key/value column from key/value extraction.</summary>
public record KvColumnResult(int ColumnIndex, IReadOnlyDictionary<string, string?> Values)
{
    /// <summary>Merges multiple column results into a single dictionary. Later columns override earlier for duplicate keys.</summary>
    public static IReadOnlyDictionary<string, string?> Merge(IEnumerable<KvColumnResult> columns)
    {
        var merged = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var col in columns.OrderBy(c => c.ColumnIndex))
        foreach (var kv in col.Values) {
            if (kv.Value != null || !merged.ContainsKey(kv.Key))
                merged[kv.Key] = kv.Value;
        }

        return merged;
    }
}