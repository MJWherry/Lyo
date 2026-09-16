using Lyo.Exceptions;
using Lyo.Rfid;

namespace Lyo.Rfid.Tonie;

/// <summary>
/// Directory and file name a Toniebox looks up from an 8-byte SLIX-L UID.
/// The file name is the first four UID bytes reversed. The directory name is the last four reversed.
/// </summary>
public readonly record struct TonieContentPath(string FileName, string DirectoryName)
{
    /// <summary>
    /// Maps <paramref name="uid"/>. The UID must be 8 bytes and start with <c>E0 04 03</c>.
    /// Example <c>E0 04 03 50 12 34 56 78</c> is file <c>500304E0</c> and directory <c>78563412</c>.
    /// </summary>
    public static TonieContentPath FromUid(ReadOnlySpan<byte> uid)
    {
        ArgumentHelpers.ThrowIfNotEqual(uid.Length, 8);
        if (uid[0] != 0xE0 || uid[1] != 0x04 || uid[2] != 0x03)
            throw new RfidException("UID is not an ICODE SLIX-L Tonie tag (expected E0 04 03).");

        return new TonieContentPath(ReverseHex(uid[..4]), ReverseHex(uid[4..]));
    }

    private static string ReverseHex(ReadOnlySpan<byte> bytes)
    {
        var chars = new char[8];
        var order = new[] { bytes[3], bytes[2], bytes[1], bytes[0] };
        for (var i = 0; i < 4; i++) {
            var b = order[i];
            chars[i * 2] = HexDigit(b >> 4);
            chars[(i * 2) + 1] = HexDigit(b & 0x0F);
        }

        return new string(chars);
    }

    private static char HexDigit(int value) => (char)(value < 10 ? '0' + value : 'A' + (value - 10));
}
