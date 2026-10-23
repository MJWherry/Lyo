using Lyo.Common.Core.Conversion;
using Lyo.Exceptions;

namespace Lyo.Rfid;

/// <summary>
/// One ISO 15693 tag on an <see cref="IIso15693Transceiver" />. <see cref="OpenAsync" /> runs a one-slot inventory, then GET SYSTEM INFORMATION, and keeps the block size
/// from that response. A Type 2 NFC reader cannot carry these frames. Place one tag in the field; a second tag collides on the one-slot inventory.
/// </summary>
public sealed class Iso15693Tag : IRfidTag
{
    private readonly IIso15693Transceiver _transceiver;
    private readonly byte[] _uid;

    private Iso15693Tag(IIso15693Transceiver transceiver, byte[] uid, int blockSize, int blockCount)
    {
        _transceiver = transceiver;
        _uid = uid;
        UidHex = ByteConvert.ToHex(uid);
        BlockSize = blockSize;
        BlockCount = blockCount;
    }

    /// <inheritdoc />
    public string UidHex { get; }

    /// <inheritdoc />
    public int BlockSize { get; }

    /// <inheritdoc />
    public int BlockCount { get; }

    /// <inheritdoc />
    public async Task<byte[]> ReadBlocksAsync(int startBlock, int blockCount, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentHelpers.ThrowIfNotInRange(startBlock, 0, BlockCount - 1);
        ArgumentHelpers.ThrowIfNotInRange(blockCount, 1, Math.Min(256, BlockCount - startBlock));
        var payload = await ExchangeAsync(_transceiver, Iso15693Frames.ReadMultipleBlocks(_uid, (byte)startBlock, blockCount), cancellationToken).ConfigureAwait(false);
        var expected = blockCount * BlockSize;
        if (payload.Length < expected)
            throw new RfidException("ISO 15693 read response was short.");

        return [.. payload.AsSpan(0, expected)];
    }

    /// <inheritdoc />
    public async Task WriteBlockAsync(int block, ReadOnlyMemory<byte> blockBytes, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentHelpers.ThrowIfNotEqual(blockBytes.Length, BlockSize);
        ArgumentHelpers.ThrowIfNotInRange(block, 0, BlockCount - 1);
        _ = await ExchangeAsync(_transceiver, Iso15693Frames.WriteSingleBlock(_uid, (byte)block, blockBytes.Span), cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => default;

    /// <summary>Inventories the tag in the field and reads its block size. The transceiver must already speak ISO 15693. This call does not take ownership of it.</summary>
    public static async Task<Iso15693Tag> OpenAsync(IIso15693Transceiver transceiver, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentHelpers.ThrowIfNull(transceiver);
        var inventory = await ExchangeAsync(transceiver, Iso15693Frames.Inventory(), cancellationToken).ConfigureAwait(false);
        if (inventory.Length != 9)
            throw new RfidException("ISO 15693 inventory response did not include a UID.");

        var uid = new byte[8];
        for (var i = 0; i < 8; i++)
            uid[i] = inventory[8 - i];

        var info = await ExchangeAsync(transceiver, Iso15693Frames.GetSystemInformation(uid), cancellationToken).ConfigureAwait(false);
        var (blockCount, blockSize) = MemorySize(info);
        return new(transceiver, uid, blockSize, blockCount);
    }

    private static (int BlockCount, int BlockSize) MemorySize(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 9)
            throw new RfidException("GET SYSTEM INFORMATION response was short.");

        var info = payload[0];
        if ((info & 0x04) == 0)
            throw new RfidException("GET SYSTEM INFORMATION did not include a memory size.");

        var offset = 9;
        if ((info & 0x01) != 0)
            offset++;

        if ((info & 0x02) != 0)
            offset++;

        if (payload.Length < offset + 2)
            throw new RfidException("GET SYSTEM INFORMATION response was short.");

        return (payload[offset] + 1, (payload[offset + 1] & 0x1F) + 1);
    }

    private static async Task<byte[]> ExchangeAsync(IIso15693Transceiver transceiver, byte[] request, CancellationToken cancellationToken)
    {
        var response = await transceiver.TransceiveAsync(request, cancellationToken).ConfigureAwait(false);
        return Iso15693Response.Payload(response.Span);
    }
}