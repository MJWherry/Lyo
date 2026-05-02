using System.Collections.Concurrent;
using System.Text;

namespace Lyo.IO.Temp.Storage;

/// <summary>
/// In-memory <see cref="IIOTempStorageProvider"/> backed by a <see cref="ConcurrentDictionary{TKey,TValue}"/>.
/// Suitable for Blazor WASM, unit tests, or any environment without a writable filesystem.
/// All data lives for the lifetime of this provider instance.
/// </summary>
// ReSharper disable once InconsistentNaming
public sealed class InMemoryIOTempStorageProvider : IIOTempStorageProvider
{
    private sealed class Entry
    {
        public bool IsDirectory { get; init; }
        public byte[]? Content { get; set; }
        public DateTimeOffset CreatedAt { get; } = DateTimeOffset.UtcNow;
    }

    private readonly ConcurrentDictionary<string, Entry> _store = new(StringComparer.Ordinal);

    public InMemoryIOTempStorageProvider()
    {
        RootPath = $"/mem/lyo-{Guid.NewGuid():N}";
        _store[Normalize(RootPath)] = new Entry { IsDirectory = true };
    }

    public string RootPath { get; }
    
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

    public bool DirectoryExists(string path)
    {
        var n = Normalize(path);
        return _store.TryGetValue(n, out var e) && e.IsDirectory;
    }

    public void CreateDirectory(string path)
    {
        var n = Normalize(path);
        // Ensure all ancestor directories exist first
        var segments = n.Split('/');
        var current = string.Empty;
        foreach (var seg in segments) {
            if (seg.Length == 0) { current = "/"; continue; }
            current = current == "/" ? "/" + seg : current + "/" + seg;
            _store.TryAdd(current, new Entry { IsDirectory = true });
        }
    }

    public void DeleteDirectory(string path)
    {
        var n = Normalize(path) + "/";
        foreach (var key in _store.Keys.Where(k => k == n.TrimEnd('/') || k.StartsWith(n, StringComparison.Ordinal)).ToList())
            _store.TryRemove(key, out _);
    }

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
            yield return new ProviderEntryInfo(
                kvp.Key,
                kvp.Value.IsDirectory,
                kvp.Value.Content?.Length ?? 0,
                kvp.Value.CreatedAt);
        }
    }

    public void EnsureDirectoryAccessible(string path) { /* always accessible in-memory */ }
    
    public bool FileExists(string path)
    {
        var n = Normalize(path);
        return _store.TryGetValue(n, out var e) && !e.IsDirectory;
    }

    public void TouchFile(string path)
    {
        var n = Normalize(path);
        EnsureParentDirectory(n);
        _store[n] = new Entry { IsDirectory = false, Content = [] };
    }

    public void WriteAllBytes(string path, byte[] data)
    {
        var n = Normalize(path);
        EnsureParentDirectory(n);
        var entry = _store.GetOrAdd(n, _ => new Entry { IsDirectory = false });
        entry.Content = data;
    }

    public void WriteAllText(string path, string text, Encoding encoding)
        => WriteAllBytes(path, encoding.GetBytes(text));

    public void AppendAllText(string path, string text, Encoding encoding)
    {
        var n = Normalize(path);
        EnsureParentDirectory(n);
        var appended = encoding.GetBytes(text);
        _store.AddOrUpdate(
            n,
            _ => new Entry { IsDirectory = false, Content = appended },
            (_, existing) => {
                var old = existing.Content ?? [];
                var combined = new byte[old.Length + appended.Length];
                Buffer.BlockCopy(old, 0, combined, 0, old.Length);
                Buffer.BlockCopy(appended, 0, combined, old.Length, appended.Length);
                existing.Content = combined;
                return existing;
            });
    }

    public void DeleteFile(string path)
    {
        var n = Normalize(path);
        if (_store.TryGetValue(n, out var e) && !e.IsDirectory)
            _store.TryRemove(n, out _);
    }

    public void MoveFile(string source, string dest)
    {
        var srcN = Normalize(source);
        var dstN = Normalize(dest);
        if (!_store.TryRemove(srcN, out var entry) || entry.IsDirectory)
            throw new FileNotFoundException($"Source file not found: {source}", source);
        EnsureParentDirectory(dstN);
        _store[dstN] = entry;
    }

    public void CopyFile(string source, string dest)
    {
        var srcN = Normalize(source);
        var dstN = Normalize(dest);
        if (!_store.TryGetValue(srcN, out var entry) || entry.IsDirectory)
            throw new FileNotFoundException($"Source file not found: {source}", source);
        EnsureParentDirectory(dstN);
        _store[dstN] = new Entry { IsDirectory = false, Content = entry.Content?.ToArray() };
    }

    public Stream OpenRead(string path)
    {
        var n = Normalize(path);
        if (!_store.TryGetValue(n, out var entry) || entry.IsDirectory)
            throw new FileNotFoundException($"File not found: {path}", path);
        return new MemoryStream(entry.Content ?? [], writable: false);
    }

    public Stream OpenCreate(string path)
    {
        var n = Normalize(path);
        EnsureParentDirectory(n);
        var entry = new Entry { IsDirectory = false, Content = [] };
        _store[n] = entry;
        return new CommitOnCloseStream(bytes => entry.Content = bytes);
    }

    public Stream OpenAppend(string path)
    {
        var n = Normalize(path);
        EnsureParentDirectory(n);
        var entry = _store.GetOrAdd(n, _ => new Entry { IsDirectory = false, Content = [] });
        var existing = entry.Content ?? [];
        return new CommitOnCloseStream(bytes => {
            var combined = new byte[existing.Length + bytes.Length];
            Buffer.BlockCopy(existing, 0, combined, 0, existing.Length);
            Buffer.BlockCopy(bytes, 0, combined, existing.Length, bytes.Length);
            entry.Content = combined;
        });
    }

    public long GetFileLength(string path)
    {
        var n = Normalize(path);
        return _store.TryGetValue(n, out var e) ? e.Content?.Length ?? 0 : 0;
    }

    public DateTimeOffset GetFileCreationTimeUtc(string path)
    {
        var n = Normalize(path);
        return _store.TryGetValue(n, out var e) ? e.CreatedAt : DateTimeOffset.MinValue;
    }
    
    public Task WriteAllBytesAsync(string path, byte[] data, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        WriteAllBytes(path, data);
        return Task.CompletedTask;
    }

    public Task WriteAllTextAsync(string path, string text, Encoding encoding, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        WriteAllText(path, text, encoding);
        return Task.CompletedTask;
    }

    public Task AppendAllTextAsync(string path, string text, Encoding encoding, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        AppendAllText(path, text, encoding);
        return Task.CompletedTask;
    }

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

    public Task CopyFileAsync(string source, string dest, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        CopyFile(source, dest);
        return Task.CompletedTask;
    }

    private void EnsureParentDirectory(string normalizedPath)
    {
        var parent = ParentOf(normalizedPath);
        if (parent != null)
            CreateDirectory(parent);
    }

    /// <summary>A <see cref="MemoryStream"/> that writes its buffered bytes back to the store when closed/disposed.</summary>
    private sealed class CommitOnCloseStream : MemoryStream
    {
        private readonly Action<byte[]> _commit;
        private bool _committed;

        public CommitOnCloseStream(Action<byte[]> commit)
        {
            _commit = commit;
        }

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
