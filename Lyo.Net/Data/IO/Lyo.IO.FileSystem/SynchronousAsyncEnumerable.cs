using System.Runtime.CompilerServices;

namespace Lyo.IO.FileSystem;

/// <summary>Adapts a synchronous sequence to <see cref="IAsyncEnumerable{T}" />.</summary>
internal static class SynchronousAsyncEnumerable
{
    /// <summary>
    /// Yields <paramref name="source" /> as an async sequence, checking <paramref name="ct" /> before each element. Used by backends whose enumeration is genuinely
    /// synchronous (local disk, in-memory) so they can satisfy <see cref="IFileSystem.EnumerateDirectoryAsync" /> without pretending to do I/O.
    /// </summary>
    public static async IAsyncEnumerable<T> Create<T>(IEnumerable<T> source, [EnumeratorCancellation] CancellationToken ct = default)
    {
        foreach (var item in source) {
            ct.ThrowIfCancellationRequested();
            yield return item;
        }

        // An async iterator must contain an await. This is the one place that cost is paid instead of once per synchronous backend.
        await Task.CompletedTask.ConfigureAwait(false);
    }
}
