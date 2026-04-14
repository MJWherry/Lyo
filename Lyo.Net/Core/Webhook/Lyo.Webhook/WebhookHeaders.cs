namespace Lyo.Webhook;

/// <summary>Case-insensitive header lookup for webhook verification.</summary>
public static class WebhookHeaders
{
    /// <summary>Finds a header by name using ordinal case-insensitive comparison.</summary>
    public static bool TryGet(IReadOnlyDictionary<string, string> headers, string name, out string value)
    {
        foreach (var kv in headers) {
            if (string.Equals(kv.Key, name, StringComparison.OrdinalIgnoreCase)) {
                value = kv.Value;
                return true;
            }
        }

        value = null!;
        return false;
    }
}