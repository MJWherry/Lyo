namespace Lyo.Pdf.Models;

/// <summary>Key/value pairs from one extracted column.</summary>
public record KvColumnResult(int ColumnIndex, IReadOnlyDictionary<string, string?> Values)
{
    /// <summary>Combines column results into one dictionary. Later columns win on duplicate keys.</summary>
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