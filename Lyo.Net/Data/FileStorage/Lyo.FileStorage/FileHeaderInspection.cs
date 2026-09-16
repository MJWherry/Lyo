using Lyo.Streams;

namespace Lyo.FileStorage;

/// <summary>Reads a short header and, for a non-seekable stream, puts those bytes back in front of the remainder.</summary>
internal static class FileHeaderInspection
{
    public const int PrefixLength = 32;

    public static async Task<byte[]> ReadPrefixAsync(Stream input, CancellationToken ct)
    {
        var buffer = new byte[PrefixLength];
        var read = 0;
        while (read < PrefixLength) {
            var n = await input.ReadAsync(buffer, read, PrefixLength - read, ct).ConfigureAwait(false);
            if (n == 0)
                break;

            read += n;
        }

        if (read == buffer.Length)
            return buffer;

        var exact = new byte[read];
        Buffer.BlockCopy(buffer, 0, exact, 0, read);
        return exact;
    }

    /// <summary>
    /// Reads the prefix. When <paramref name="input" /> can seek, rewinds it and returns that same stream. Otherwise returns a stream that yields the prefix and then
    /// the unread remainder. Dispose <see cref="HeaderPeek.Owned" /> after the save; it does not dispose <paramref name="input" />.
    /// </summary>
    public static async Task<HeaderPeek> PeekAsync(Stream input, CancellationToken ct)
    {
        if (input.CanSeek) {
            var position = input.Position;
            var header = await ReadPrefixAsync(input, ct).ConfigureAwait(false);
            input.Position = position;
            return new(header, input, null);
        }

        var prefix = await ReadPrefixAsync(input, ct).ConfigureAwait(false);
        var head = new MemoryStream(prefix, writable: false);
        var combined = new ConcatenatedStream([head, input]);
        return new(prefix, combined, new HeaderReplay(combined, head));
    }

    internal readonly struct HeaderPeek(byte[] header, Stream stream, IDisposable? owned)
    {
        public byte[] Header { get; } = header;

        public Stream Stream { get; } = stream;

        public IDisposable? Owned { get; } = owned;
    }

    private sealed class HeaderReplay(ConcatenatedStream combined, MemoryStream head) : IDisposable
    {
        public void Dispose()
        {
            combined.Dispose();
            head.Dispose();
        }
    }
}
