using Lyo.Exceptions;

namespace Lyo.Rfid;

/// <summary>
/// ISO 15693 command frames, CRC included. Addressed commands take the UID in display order (<c>E0</c> first) and send it least-significant byte first.
/// Flags are <c>0x22</c> (addressed, high data rate). Inventory uses <c>0x26</c> (one slot).
/// </summary>
public static class Iso15693Frames
{
    /// <summary>Addressed mode and high data rate.</summary>
    public const byte AddressedHighDataRate = 0x22;

    /// <summary>Inventory, high data rate, one slot.</summary>
    public const byte InventoryOneSlot = 0x26;

    /// <summary>One-slot inventory. The response UID is least-significant byte first, after the DSFID.</summary>
    public static byte[] Inventory() => Iso15693Crc.Append([InventoryOneSlot, 0x01, 0x00]);

    /// <summary>Addressed GET SYSTEM INFORMATION. The memory-size field is how <see cref="Iso15693Tag"/> learns <see cref="IRfidTag.BlockSize"/>.</summary>
    public static byte[] GetSystemInformation(ReadOnlySpan<byte> uid) => Addressed(0x2B, uid, []);

    /// <summary>ISO 15693 READ SINGLE BLOCK.</summary>
    public static byte[] ReadSingleBlock(ReadOnlySpan<byte> uid, byte block) => Addressed(0x20, uid, [block]);

    /// <summary>
    /// ISO 15693 READ MULTIPLE BLOCKS. <paramref name="blockCount"/> is the number of blocks.
    /// The on-air count byte is <paramref name="blockCount"/> minus one.
    /// </summary>
    public static byte[] ReadMultipleBlocks(ReadOnlySpan<byte> uid, byte firstBlock, int blockCount)
    {
        ArgumentHelpers.ThrowIfNotInRange(blockCount, 1, 256);
        Span<byte> tail = stackalloc byte[2];
        tail[0] = firstBlock;
        tail[1] = (byte)(blockCount - 1);
        return Addressed(0x23, uid, tail);
    }

    /// <summary>ISO 15693 WRITE SINGLE BLOCK. <paramref name="blockData"/> is one block.</summary>
    public static byte[] WriteSingleBlock(ReadOnlySpan<byte> uid, byte block, ReadOnlySpan<byte> blockData)
    {
        var tail = new byte[1 + blockData.Length];
        tail[0] = block;
        blockData.CopyTo(tail.AsSpan(1));
        return Addressed(0x21, uid, tail);
    }

    private static byte[] Addressed(byte command, ReadOnlySpan<byte> uid, ReadOnlySpan<byte> tail)
    {
        if (uid.Length != 8)
            throw new RfidException("ISO 15693 UID must be 8 bytes.");

        var body = new byte[2 + 8 + tail.Length];
        body[0] = AddressedHighDataRate;
        body[1] = command;
        for (var i = 0; i < 8; i++)
            body[2 + i] = uid[7 - i];

        tail.CopyTo(body.AsSpan(10));
        return Iso15693Crc.Append(body);
    }
}
