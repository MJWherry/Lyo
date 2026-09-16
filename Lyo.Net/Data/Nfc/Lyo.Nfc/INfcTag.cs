namespace Lyo.Nfc;

/// <summary>
/// One Type 2 tag (NTAG21x and similar). Pages are 4 bytes. Dispose releases the reader.
/// </summary>
public interface INfcTag : IAsyncDisposable
{
    /// <summary>UID as uppercase hex with no separators.</summary>
    string UidHex { get; }

    /// <summary>Reads <paramref name="pageCount"/> pages starting at <paramref name="startPage"/>. The buffer length is <paramref name="pageCount"/> × 4.</summary>
    Task<byte[]> ReadPagesAsync(int startPage, int pageCount, CancellationToken cancellationToken);

    /// <summary>Writes one 4-byte page. Any other length throws <see cref="ArgumentException"/>.</summary>
    Task WritePageAsync(int page, ReadOnlyMemory<byte> fourBytes, CancellationToken cancellationToken);
}
