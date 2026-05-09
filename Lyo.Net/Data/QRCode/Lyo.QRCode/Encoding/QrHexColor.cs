using Lyo.Exceptions;
using Lyo.Exceptions.Models;

namespace Lyo.QRCode.Encoding;

internal static class QrHexColor
{
    /// <summary>Parses #RGB or #RRGGBB (optional leading #) into RGBA (A=255). Throws <see cref="InvalidFormatException" /> if invalid.</summary>
    public static byte[] ToRgba(string hex)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(hex);
        var s = hex.Trim();
        if (s.StartsWith('#'))
            s = s[1..];

        if (s.Length == 3) {
            var r = (byte)(FromHexNibble(s[0], hex, nameof(hex)) * 17);
            var g = (byte)(FromHexNibble(s[1], hex, nameof(hex)) * 17);
            var b = (byte)(FromHexNibble(s[2], hex, nameof(hex)) * 17);
            return [r, g, b, 255];
        }

        if (s.Length == 6)
            return [
                FromHexByte(s[0], s[1], hex, nameof(hex)),
                FromHexByte(s[2], s[3], hex, nameof(hex)),
                FromHexByte(s[4], s[5], hex, nameof(hex)),
                255
            ];

        throw new InvalidFormatException("Expected 3 or 6 hex digits after optional '#'.", nameof(hex), hex, "#RGB", "#RRGGBB");
    }

    private static byte FromHexByte(char a, char b, string originalHex, string paramName)
        => (byte)((FromHexNibble(a, originalHex, paramName) << 4) | FromHexNibble(b, originalHex, paramName));

    private static int FromHexNibble(char c, string originalHex, string paramName)
        => c switch {
            >= '0' and <= '9' => c - '0',
            >= 'A' and <= 'F' => c - 'A' + 10,
            >= 'a' and <= 'f' => c - 'a' + 10,
            var _ => throw new InvalidFormatException("Invalid hex digit.", paramName, originalHex, "0-9, A-F, a-f")
        };
}