using System.Collections.Concurrent;
using System.Text;

namespace Lyo.IO.Temp.Storage;

/// <summary>
/// In-memory <see cref="IIOTempStorageProvider" /> backed by a <see cref="ConcurrentDictionary{TKey,TValue}" />. Suitable for Blazor WASM, unit tests, or any environment
/// without a writable filesystem. All data lives for the lifetime of this provider instance.
/// </summary>
// ReSharper disable once InconsistentNaming
public sealed class InMemoryIOTempStorageProvider : IIOTempStorageProvider
{
    private readonly ConcurrentDictionary<string, Entry> _store = new(StringComparer.Ordinal);

    /// <summary>Initializes an in-memory store with a unique synthetic <see cref="RootPath" />.</summary>
    public InMemoryIOTempStorageProvider()
    {
        RootPath = $"/mem/lyo-{Guid.NewGuid():N}";
        _store[Normalize(RootPath)] = new() { IsDirectory = true };
    }

    /// <inheritdoc />
    public string RootPath { get; }

    /// <inheritdoc />
    public bool DirectoryExists(string path)
    {
        var n = Normalize(path);
        return _store.TryGetValue(n, out var e) && e.IsDirectory;
    }

    /// <inheritdoc />
    public void CreateDirectory(string path)
    {
        var n = Normalize(path);
        // Ensure all ancestor directories exist first
        var segments = n.Split('/');
        var current = string.Empty;
        foreach (var seg in segments) {
            if (seg.Length == 0) {
                current = "/";
                continue;
            }

            current = current == "/" ? "/" + seg : current + "/" + seg;
            _store.TryAdd(current, new() { IsDirectory = true });
        }
    }

    /// <inheritdoc />
    public void DeleteDirectory(string path)
    {
        var n = Normalize(path) + "/";
        foreach (var key in _store.Keys.Where(k => k == n.TrimEnd('/') || k.StartsWith(n, StringComparison.Ordinal)).ToList())
            _store.TryRemove(key, out var _);
    }

    /// <inheritdoc />
    public IEnumerable<ProviderEntryInfo> EnumerateEntries(string path)
    {
        var n = Normalize(path);
        var prefix = n + "/";
        foreach (var kvp in _store) {
            if (!kvp.Key.StartsWith(prefix, StringComparison.Ordinal))
                continue;

            var rel = kvp.Key[prefix.Length..];
            if (rel.Length == 0 || rel.Contains('/'))
                continue; // skip self and deeper descendants

            yield return new(kvp.Key, kvp.Value.IsDirectory, kvp.Value.Content?.Length ?? 0, kvp.Value.CreatedAt);
        }
    }

    /// <inheritdoc />
    public void EnsureDirectoryAccessible(string path)
    { /* always accessible in-memory */
    }

    /// <inheritdoc />
    public bool FileExists(string path)
    {
        var n = Normalize(path);
        return _store.TryGetValue(n, out var e) && !e.IsDirectory;
    }

    /// <inheritdoc />
    public void TouchFile(string path)
    {
        var n = Normalize(path);
        EnsureParentDirectory(n);
        _store[n] = new() { IsDirectory = false, Content = [] };
    }

    /// <inheritdoc />
    public void WriteAllBytes(string path, byte[] data)
    {
        var n = Normalize(path);
        EnsureParentDirectory(n);
        var entry = _store.GetOrAdd(n, _ => new() { IsDirectory = false });
        entry.Content = data;
    }

    /// <inheritdoc />
    public void WriteAllText(string path, string text, Encoding encoding) => WriteAllBytes(path, encoding.GetBytes(text));

    /// <inheritdoc />
    public void AppendAllText(string path, string text, Encoding encoding)
    {
        var n = Normalize(path);
        EnsureParentDirectory(n);
        var appended = encoding.GetBytes(text);
        _store.AddOrUpdate(
            n, _ => new() { IsDirectory = false, Content = appended }, (_, existing) => {
                var old = existing.Content ?? [];
                var combined = new byte[old.Length + appended.Length];
                Buffer.BlockCopy(old, 0, combined, 0, old.Length);
                Buffer.BlockCopy(appended, 0, combined, old.Length, appended.Length);
                existing.Content = combined;
                return existing;
            });
    }

    /// <inheritdoc />
    public void DeleteFile(string path)
    {
        var n = Normalize(path);
        if (_store.TryGetValue(n, out var e) && !e.IsDirectory)
            _store.TryRemove(n, out var _);
    }

    /// <inheritdoc />
    public void MoveFile(string source, string dest)
    {
        var srcN = Normalize(source);
        var dstN = Normalize(dest);
        if (!_store.TryRemove(srcN, out var entry) || entry.IsDirectory)
            throw new FileNotFoundException($"Source file not found: {source}", source);

        EnsureParentDirectory(dstN);
        _store[dstN] = entry;
    }

    /// <inheritdoc />
    public void CopyFile(string source, string dest)
    {
        var srcN = Normalize(source);
        var dstN = Normalize(dest);
        if (!_store.TryGetValue(srcN, out var entry) || entry.IsDirectory)
            throw new FileNotFoundException($"Source file not found: {source}", source);

        EnsureParentDirectory(dstN);
        _store[dstN] = new() { IsDirectory = false, Content = entry.Content?.ToArray() };
    }

    /// <inheritdoc />
    public Stream OpenRead(string path)
    {
        var n = Normalize(path);
        if (!_store.TryGetValue(n, out var entry) || entry.IsDirectory)
            throw new FileNotFoundException($"File not found: {path}", path);

        return new MemoryStream(entry.Content ?? [], false);
    }

    /// <inheritdoc />
    public Stream OpenCreate(string path)
    {
        var n = Normalize(path);
        EnsureParentDirectory(n);
        var entry = new Entry { IsDirectory = false, Content = [] };
        _store[n] = entry;
        return new CommitOnCloseStream(bytes => entry.Content = bytes);
    }

    /// <inheritdoc />
    public Stream OpenAppend(string path)
    {
        var n = Normalize(path);
        EnsureParentDirectory(n);
        var entry = _store.GetOrAdd(n, _ => new() { IsDirectory = false, Content = [] });
        var existing = entry.Content ?? [];
        return new CommitOnCloseStream(bytes => {
            var combined = new byte[existing.Length + bytes.Length];
            Buffer.BlockCopy(existing, 0, combined, 0, existing.Length);
            Buffer.BlockCopy(bytes, 0, combined, existing.Length, bytes.Length);
            entry.Content = combined;
        });
    }

    /// <inheritdoc />
    public long GetFileLength(string path)
    {
        var n = Normalize(path);
        return _store.TryGetValue(n, out var e) ? e.Content?.Length ?? 0 : 0;
    }

    /// <inheritdoc />
    public DateTimeOffset GetFileCreationTimeUtc(string path)
    {
        var n = Normalize(path);
        return _store.TryGetValue(n, out var e) ? e.CreatedAt : DateTimeOffset.MinValue;
    }

    /// <inheritdoc />
    public Task WriteAllBytesAsync(string path, byte[] data, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        WriteAllBytes(path, data);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task WriteAllTextAsync(string path, string text, Encoding encoding, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        WriteAllText(path, text, encoding);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task AppendAllTextAsync(string path, string text, Encoding encoding, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        AppendAllText(path, text, encoding);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task CopyStreamToFileAsync(Stream source, string destPath, CancellationToken ct)
    {
        using var ms = new MemoryStream();
#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        await source.CopyToAsync(ms, ct);
#else
        await source.CopyToAsync(ms, 81920, ct).ConfigureAwait(false);
#endif
        WriteAllBytes(destPath, ms.ToArray());
    }

    /// <inheritdoc />
    public Task CopyFileAsync(string source, string dest, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        CopyFile(source, dest);
        return Task.CompletedTask;
    }

    private static string Normalize(string path)
    {
        // Collapse any ./ ../ and duplicate slashes so paths are canonical.
        var uri = new Uri("file://" + path.Replace('\\', '/'));
        return uri.AbsolutePath.TrimEnd('/');
    }

    private static string? ParentOf(string normalizedPath)
    {
        var idx = normalizedPath.LastIndexOf('/');
        return idx <= 0 ? null : normalizedPath[..idx];
    }

    private void EnsureParentDirectory(string normalizedPath)
    {
        var parent = ParentOf(normalizedPath);
        if (parent != null)
            CreateDirectory(parent);
    }

    private sealed class Entry
    {
        public bool IsDirectory { get; init; }

        public byte[]? Content { get; set; }

        public DateTimeOffset CreatedAt { get; } = DateTimeOffset.UtcNow;
    }

    /// <summary>A <see cref="MemoryStream" /> that writes its buffered bytes back to the store when closed/disposed.</summary>
    private sealed class CommitOnCloseStream : MemoryStream
    {
        private readonly Action<byte[]> _commit;
        private bool _committed;

        public CommitOnCloseStream(Action<byte[]> commit) => _commit = commit;

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_committed) {
                _committed = true;
                _commit(ToArray());
            }

            base.Dispose(disposing);
        }

#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        public override ValueTask DisposeAsync()
        {
            if (!_committed) {
                _committed = true;
                _commit(ToArray());
            }

            return base.DisposeAsync();
        }
#endif
    }
}