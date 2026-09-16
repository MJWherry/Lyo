using Lyo.Common.Core.Conversion;
using Lyo.Exceptions;
using PCSC;

namespace Lyo.Nfc.Pcsc;

public sealed class PcscNfcTag : INfcTag
{
    private readonly ICardReader _reader;
    private bool _disposed;

    public PcscNfcTag(ICardReader reader)
    {
        _reader = reader;
        var uid = Transmit(PcscNtagCommands.GetUid());
        if (uid.Length == 0)
            throw new NfcException("PC/SC reader returned an empty UID.");

        UidHex = ByteConvert.ToHex(uid);
    }

    public string UidHex { get; }

    public Task<byte[]> ReadPagesAsync(int startPage, int pageCount, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        if (startPage is < 0 or > 255)
            throw new NfcException($"Page {startPage} is outside a single-byte Type 2 address.");

        if (pageCount < 1)
            throw new NfcException("Page count must be at least 1.");

        var output = new byte[pageCount * 4];
        var filled = 0;
        var page = startPage;
        while (filled < pageCount) {
            if (page > 255)
                throw new NfcException("Read walked past the last Type 2 page address.");

            var chunk = Transmit(PcscNtagCommands.ReadPages((byte)page));
            if (chunk.Length < 16)
                throw new NfcException($"PC/SC read of page {page} returned {chunk.Length} bytes.");

            var takePages = Math.Min(4, pageCount - filled);
            Buffer.BlockCopy(chunk, 0, output, filled * 4, takePages * 4);
            filled += takePages;
            page += 4;
        }

        return Task.FromResult(output);
    }

    public Task WritePageAsync(int page, ReadOnlyMemory<byte> fourBytes, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        if (fourBytes.Length != 4)
            throw new ArgumentException("A Type 2 page is 4 bytes.", nameof(fourBytes));

        if (page is < 0 or > 255)
            throw new NfcException($"Page {page} is outside a single-byte Type 2 address.");

        _ = Transmit(PcscNtagCommands.WritePage((byte)page, fourBytes.Span));
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        if (_disposed)
            return default;

        _reader.Dispose();
        _disposed = true;
        return default;
    }

    private byte[] Transmit(byte[] command)
    {
        var sendPci = SCardPCI.GetPci(_reader.Protocol);
        var buffer = new byte[256];
        var received = _reader.Transmit(sendPci, command, buffer);
        if (received < 2)
            throw new NfcException("PC/SC response was too short.");

        if (buffer[received - 2] != 0x90 || buffer[received - 1] != 0x00)
            throw new NfcException($"PC/SC status {buffer[received - 2]:X2}{buffer[received - 1]:X2}.");

        var data = new byte[received - 2];
        Buffer.BlockCopy(buffer, 0, data, 0, data.Length);
        return data;
    }

    private void ThrowIfDisposed() => OperationHelpers.ThrowIfDisposed(_disposed, nameof(PcscNfcTag));
}
