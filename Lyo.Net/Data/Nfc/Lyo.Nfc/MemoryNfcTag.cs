using Lyo.Exceptions;

namespace Lyo.Nfc;

/// <summary>
/// Type 2 tag kept in a byte buffer. Use it for tests and for dump files. <see cref="UidHex"/> is the value passed to the constructor, uppercased, with spaces removed.
/// </summary>
public sealed class MemoryNfcTag : INfcTag
{
    private readonly NtagLayout _layout;
    private readonly byte[] _pages;

    /// <param name="layout">Chip size. The buffer covers every page, including lock and config pages.</param>
    /// <param name="uidHex">UID hex. Spaces are stripped.</param>
    public MemoryNfcTag(NtagLayout layout, string uidHex)
    {
        ArgumentHelpers.ThrowIfNull(layout);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(uidHex);
        _layout = layout;
        _pages = new byte[layout.TotalPages * 4];
        UidHex = uidHex.Replace(" ", "").ToUpperInvariant();
    }

    /// <inheritdoc />
    public string UidHex { get; }

    /// <inheritdoc />
    public async Task<byte[]> ReadPagesAsync(int startPage, int pageCount, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentHelpers.ThrowIfNotInRange(startPage, 0, _layout.TotalPages - 1);
        ArgumentHelpers.ThrowIfNotInRange(pageCount, 1, _layout.TotalPages - startPage);
        var bytes = new byte[pageCount * 4];
        Buffer.BlockCopy(_pages, startPage * 4, bytes, 0, bytes.Length);
        await Task.CompletedTask.ConfigureAwait(false);
        return bytes;
    }

    /// <inheritdoc />
    public async Task WritePageAsync(int page, ReadOnlyMemory<byte> fourBytes, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentHelpers.ThrowIfNotEqual(fourBytes.Length, 4);
        ArgumentHelpers.ThrowIfNotInRange(page, 0, _layout.TotalPages - 1);
        fourBytes.Span.CopyTo(_pages.AsSpan(page * 4, 4));
        await Task.CompletedTask.ConfigureAwait(false);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => default;
}
