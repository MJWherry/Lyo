using Lyo.Exceptions;
using Lyo.Rfid;

namespace Lyo.Rfid.Tonie;

/// <summary>
/// Addressed SLIX-L frames, CRC included. The UID argument is display order (<c>E0 04 03 …</c>). On the wire it is sent least-significant byte first.
/// Flags are <c>0x22</c> (addressed, high data rate). Inventory uses <c>0x26</c> (one slot).
/// </summary>
public static class SlixFrames
{
    /// <summary>Addressed mode and high data rate.</summary>
    public const byte AddressedHighDataRate = 0x22;

    /// <summary>Inventory, high data rate, one slot.</summary>
    public const byte InventoryOneSlot = 0x26;

    /// <summary>NXP manufacturer code.</summary>
    public const byte NxpManufacturer = 0x04;

    /// <summary>GET RANDOM NUMBER for the privacy password.</summary>
    public static byte[] GetRandom(ReadOnlySpan<byte> uid) => Custom(0xB2, uid, [ToniePrivacy.PrivacyIdentifier]);

    /// <summary>SET PASSWORD with <paramref name="xorPassword"/> (already XORed with the tag random).</summary>
    public static byte[] SetPassword(ReadOnlySpan<byte> uid, ReadOnlySpan<byte> xorPassword)
    {
        ArgumentHelpers.ThrowIfNotEqual(xorPassword.Length, 4);
        Span<byte> tail = stackalloc byte[5];
        tail[0] = ToniePrivacy.PrivacyIdentifier;
        xorPassword.CopyTo(tail[1..]);
        return Custom(0xB3, uid, tail);
    }

    /// <summary>ENABLE PRIVACY. <paramref name="xorPassword"/> is the session password from the latest GET RANDOM NUMBER.</summary>
    public static byte[] EnablePrivacy(ReadOnlySpan<byte> uid, ReadOnlySpan<byte> xorPassword)
    {
        ArgumentHelpers.ThrowIfNotEqual(xorPassword.Length, 4);
        return Custom(0xBA, uid, xorPassword);
    }

    /// <summary>ISO 15693 READ SINGLE BLOCK.</summary>
    public static byte[] ReadSingleBlock(ReadOnlySpan<byte> uid, byte block) => Iso15693Frames.ReadSingleBlock(uid, block);

    /// <summary>
    /// ISO 15693 READ MULTIPLE BLOCKS. <paramref name="blockCount"/> is the number of 4-byte blocks.
    /// The on-air count byte is <paramref name="blockCount"/> minus one.
    /// </summary>
    public static byte[] ReadMultipleBlocks(ReadOnlySpan<byte> uid, byte firstBlock, int blockCount) =>
        Iso15693Frames.ReadMultipleBlocks(uid, firstBlock, blockCount);

    /// <summary>ISO 15693 WRITE SINGLE BLOCK. <paramref name="blockData"/> is 4 bytes.</summary>
    public static byte[] WriteSingleBlock(ReadOnlySpan<byte> uid, byte block, ReadOnlySpan<byte> blockData)
    {
        ArgumentHelpers.ThrowIfNotEqual(blockData.Length, 4);
        return Iso15693Frames.WriteSingleBlock(uid, block, blockData);
    }

    /// <summary>One-slot inventory. The response UID is least-significant byte first.</summary>
    public static byte[] Inventory() => Iso15693Frames.Inventory();

    private static byte[] Custom(byte command, ReadOnlySpan<byte> uid, ReadOnlySpan<byte> tail)
    {
        RequireUid(uid);
        var body = new byte[3 + 8 + tail.Length];
        body[0] = AddressedHighDataRate;
        body[1] = command;
        body[2] = NxpManufacturer;
        CopyUidLsb(uid, body.AsSpan(3, 8));
        tail.CopyTo(body.AsSpan(11));
        return Iso15693Crc.Append(body);
    }

    private static void RequireUid(ReadOnlySpan<byte> uid)
    {
        if (uid.Length != 8)
            throw new RfidException("SLIX-L UID must be 8 bytes.");
    }

    private static void CopyUidLsb(ReadOnlySpan<byte> uid, Span<byte> destination)
    {
        for (var i = 0; i < 8; i++)
            destination[i] = uid[7 - i];
    }
}
