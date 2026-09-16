using Lyo.Exceptions;

namespace Lyo.Rfid;

/// <summary>
/// ISO 15693 tag kept in a byte buffer. Use it for tests and for dump files.
/// <see cref="UidHex"/> is the value passed to the constructor, uppercased, with spaces removed.
/// </summary>
public sealed class MemoryRfidTag : IRfidTag
{
    readonly byte[] _memory;

    /// <param name="blockSize">Bytes per block. ICODE SLIX uses 4.</param>
    /// <param name="blockCount">Number of blocks. At most 256, which is the 8-bit GET SYSTEM INFORMATION field.</param>
    /// <param name="uidHex">UID hex. Spaces are stripped.</param>
    public MemoryRfidTag(int blockSize, int blockCount, string uidHex)
    {
        ArgumentHelpers.ThrowIfNotInRange(blockSize, 1, 32);
        ArgumentHelpers.ThrowIfNotInRange(blockCount, 1, 256);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(uidHex);
        BlockSize = blockSize;
        BlockCount = blockCount;
        _memory = new byte[blockSize * blockCount];
        UidHex = uidHex.Replace(" ", "").ToUpperInvariant();
    }

    /// <inheritdoc />
    public string UidHex { get; }

    /// <inheritdoc />
    public int BlockSize { get; }

    /// <inheritdoc />
    public int BlockCount { get; }

    /// <inheritdoc />
    public Task<byte[]> ReadBlocksAsync(int startBlock, int blockCount, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentHelpers.ThrowIfNotInRange(startBlock, 0, BlockCount - 1);
        ArgumentHelpers.ThrowIfNotInRange(blockCount, 1, BlockCount - startBlock);
        var bytes = new byte[blockCount * BlockSize];
        Buffer.BlockCopy(_memory, startBlock * BlockSize, bytes, 0, bytes.Length);
        return Task.FromResult(bytes);
    }

    /// <inheritdoc />
    public Task WriteBlockAsync(int block, ReadOnlyMemory<byte> blockBytes, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentHelpers.ThrowIfNotEqual(blockBytes.Length, BlockSize);
        ArgumentHelpers.ThrowIfNotInRange(block, 0, BlockCount - 1);
        blockBytes.Span.CopyTo(_memory.AsSpan(block * BlockSize, BlockSize));
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => default;
}
