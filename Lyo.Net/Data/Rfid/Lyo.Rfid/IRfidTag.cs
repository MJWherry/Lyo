namespace Lyo.Rfid;

/// <summary>One ISO 15693 tag. <see cref="BlockSize" /> comes from the tag (4 bytes on ICODE SLIX). Dispose does not close the transceiver. The caller owns that.</summary>
public interface IRfidTag : IAsyncDisposable
{
    /// <summary>UID as uppercase hex with no separators.</summary>
    string UidHex { get; }

    /// <summary>Bytes in one block.</summary>
    int BlockSize { get; }

    /// <summary>Number of blocks on the tag.</summary>
    int BlockCount { get; }

    /// <summary>Reads <paramref name="blockCount" /> blocks starting at <paramref name="startBlock" />. The buffer length is <paramref name="blockCount" /> × <see cref="BlockSize" />.</summary>
    Task<byte[]> ReadBlocksAsync(int startBlock, int blockCount, CancellationToken cancellationToken);

    /// <summary>Writes one block. Any length other than <see cref="BlockSize" /> throws <see cref="ArgumentException" />.</summary>
    Task WriteBlockAsync(int block, ReadOnlyMemory<byte> blockBytes, CancellationToken cancellationToken);
}