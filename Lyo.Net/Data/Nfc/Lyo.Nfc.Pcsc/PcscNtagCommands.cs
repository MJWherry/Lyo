namespace Lyo.Nfc.Pcsc;

/// <summary>ACR122U-style APDUs for a Type 2 tag. Tests lock these bytes. The reader transmits them unchanged.</summary>
static class PcscNtagCommands
{
    /// <summary>GET UID: <c>FF CA 00 00 00</c>.</summary>
    public static byte[] GetUid() => [0xFF, 0xCA, 0x00, 0x00, 0x00];

    /// <summary>READ BINARY of four pages (16 bytes) starting at <paramref name="startPage"/>.</summary>
    public static byte[] ReadPages(byte startPage) => [0xFF, 0xB0, 0x00, startPage, 0x10];

    /// <summary>UPDATE BINARY of one 4-byte page.</summary>
    public static byte[] WritePage(byte page, ReadOnlySpan<byte> fourBytes)
    {
        if (fourBytes.Length != 4)
            throw new ArgumentException("A Type 2 page is 4 bytes.", nameof(fourBytes));

        return [0xFF, 0xD6, 0x00, page, 0x04, fourBytes[0], fourBytes[1], fourBytes[2], fourBytes[3]];
    }
}
