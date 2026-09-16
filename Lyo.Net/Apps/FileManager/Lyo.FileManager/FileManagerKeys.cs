namespace Lyo.FileManager;

/// <summary>Key ids a local server can use on upload.</summary>
public static class FileManagerKeys
{
    /// <summary>Live inventory ids plus passphrase and saved keys. Order is live, then the passphrase id, then saved ids.</summary>
    public static IReadOnlyList<string> Ids(FileManagerServer? server, IEnumerable<string>? live)
    {
        var ids = new List<string>();
        if (live != null)
            foreach (var id in live)
                Add(ids, id);
        if (!string.IsNullOrWhiteSpace(server?.Passphrase))
            Add(ids, FileManagerLimits.LocalKeyId);
        if (server?.Keys != null)
            foreach (var key in server.Keys)
                Add(ids, key.Id);
        return ids;
    }

    /// <summary>Live inventory ids plus the passphrase and saved keys on <paramref name="store" />. Order is live, then the passphrase id, then saved ids.</summary>
    public static IReadOnlyList<string> Ids(FileManagerKeyStore? store, IEnumerable<string>? live)
    {
        var ids = new List<string>();
        if (live != null)
            foreach (var id in live)
                Add(ids, id);
        if (!string.IsNullOrWhiteSpace(store?.Passphrase))
            Add(ids, FileManagerLimits.LocalKeyId);
        if (store?.Keys != null)
            foreach (var key in store.Keys)
                Add(ids, key.Id);
        return ids;
    }

    /// <summary>
    /// Key id to send. A typed choice wins. When encryption is on and the choice is blank, the only configured key is used.
    /// Returns null when encryption is on and there is not exactly one key to choose.
    /// </summary>
    public static string? Resolve(bool encrypt, string? chosen, IReadOnlyList<string> ids)
    {
        if (!string.IsNullOrWhiteSpace(chosen))
            return chosen.Trim();
        if (!encrypt)
            return null;
        return ids.Count == 1 ? ids[0] : null;
    }

    private static void Add(List<string> ids, string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return;
        var trimmed = id.Trim();
        if (ids.Exists(item => string.Equals(item, trimmed, StringComparison.Ordinal)))
            return;
        ids.Add(trimmed);
    }
}
