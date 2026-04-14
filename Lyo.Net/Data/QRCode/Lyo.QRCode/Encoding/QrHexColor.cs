namespace Lyo.QRCode.Encoding;

internal static class QrHexColor
{
    /// <summary>Parses #RGB or #RRGGBB (optional leading #) into RGBA (A=255). Throws <see cref="FormatException" /> if invalid.</summary>
    public static byte[] ToRgba(string hex)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hex);
        var s = hex.Trim();
        if (s.StartsWith('#'))
            s = s[1..];

        if (s.Length == 3) {
            var r = (byte)(FromHexNibble(s[0]) * 17);
            var g = (byte)(FromHexNibble(s[1]) * 17);
            var b = (byte)(FromHexNibble(s[2]) * 17);
            return [r, g, b, 255];
        }

        if (s.Length == 6)
            return [FromHexByte(s[0], s[1]), FromHexByte(s[2], s[3]), FromHexByte(s[4], s[5]), 255];

        throw new FormatException("Expected #RGB or #RRGGBB.");
    }

    private static byte FromHexByte(char a, char b) => (byte)((FromHexNibble(a) << 4) | FromHexNibble(b));

    private static int FromHexNibble(char c)
        => c switch {
            >= '0' and <= '9' => c - '0',
            >= 'A' and <= 'F' => c - 'A' + 10,
            >= 'a' and <= 'f' => c - 'a' + 10,
            var _ => throw new FormatException("Invalid hex digit.")
        };
}