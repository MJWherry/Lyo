using System.Text;
using System.Text.RegularExpressions;

namespace Lyo.Images;

/// <summary>Decodes EXIF UserComment values, including base64 payloads (e.g. Snapchat, some Android cameras).</summary>
public static class ExifUserCommentDecoder
{
    /// <summary>EXIF UserComment uses an 8-byte character-code prefix (e.g. "ASCII\0\0\0", "UNICODE\0", or zeros).</summary>
    private const int ExifUserCommentPrefixLength = 8;

    /// <summary>Tries to decode a UserComment value. Handles base64-encoded UTF-8 (common with Snapchat/Android).</summary>
    /// <param name="value">Raw UserComment string (base64 or plain text).</param>
    /// <returns>Decoded text, or the original value when decoding fails.</returns>
    public static string Decode(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return value ?? string.Empty;

        // Prefer base64 decode first (Snapchat and some Android apps use this).
        if (TryBase64Decode(value, out var decoded))
            return decoded;

        return value;
    }

    /// <summary>Tries to decode a UserComment and strip the EXIF character-code prefix.</summary>
    /// <param name="value">Raw UserComment string.</param>
    /// <param name="stripExifPrefix">If true, drops the standard 8-byte EXIF character-code prefix when present.</param>
    /// <returns>Decoded text with the prefix stripped when requested.</returns>
    public static string Decode(string? value, bool stripExifPrefix)
    {
        var decoded = Decode(value);
        if (!stripExifPrefix || decoded.Length <= ExifUserCommentPrefixLength)
            return decoded;

        var bytes = Encoding.UTF8.GetBytes(decoded);
        if (bytes.Length <= ExifUserCommentPrefixLength)
            return decoded;

        // Common EXIF prefixes: ASCII\0\0\0, UNICODE\0, or all zeros.
        var prefix = bytes.AsSpan(0, ExifUserCommentPrefixLength);
        var isAscii = prefix[0] == (byte)'A' && prefix[1] == (byte)'S' && prefix[2] == (byte)'C' && prefix[3] == (byte)'I';
        var isUnicode = prefix[0] == (byte)'U' && prefix[1] == (byte)'N' && prefix[2] == (byte)'I' && prefix[3] == (byte)'C';
        var allZeros = prefix.IndexOfAnyExcept((byte)0) < 0;
        if (isAscii || isUnicode || allZeros)
            return Encoding.UTF8.GetString(bytes.AsSpan(ExifUserCommentPrefixLength));

        return decoded;
    }

    private static bool TryBase64Decode(string value, out string decoded)
    {
        decoded = string.Empty;
        if (string.IsNullOrWhiteSpace(value) || value.Length % 4 != 0)
            return false;

        // Base64 charset is A-Za-z0-9+/= only.
        if (!Regex.IsMatch(value, @"^[A-Za-z0-9+/]*=*$"))
            return false;

        try {
            var bytes = Convert.FromBase64String(value);
            decoded = Encoding.UTF8.GetString(bytes);
            return true;
        }
        catch {
            return false;
        }
    }

    /// <summary>Splits Snapchat-style UserComment: "LSnapchat/13.73.0.61 Beta (Pixel 7a; Android 16#...)" into app, device, and OS.</summary>
    /// <param name="decoded">Decoded UserComment text (e.g. from Decode).</param>
    /// <returns>A record with AppName, AppVersion, Device, OsInfo, and Raw when parsing succeeds.</returns>
    public static SnapchatUserCommentInfo? TryParseSnapchatFormat(string? decoded)
    {
        if (string.IsNullOrWhiteSpace(decoded))
            return null;

        // Shape: "N\nLSnapchat/13.73.0.61 Beta (Pixel 7a; Android 16#14339231#36; gzip) V/MUSHROOM"
        // Layout: L{app}/{version} ({device}; {os}) {suffix}
        // Need the paren group so greedy [^(]+ backtracks and leaves room for \s+ before (
        var match = Regex.Match(decoded, @"L(?<app>[^/]+)/(?<version>[^(]+)\s+\((?<device>[^;]+);\s*(?<os>[^)]+)\)\s*(?<suffix>.*)?", RegexOptions.Singleline);
        if (!match.Success) {
            // Fallback without a device block, e.g. "LSnapchat/13.73.0.61 Beta V/MUSHROOM"
            match = Regex.Match(decoded, @"L(?<app>[^/]+)/(?<version>.+?)\s+(?<suffix>.*)?", RegexOptions.Singleline);
            if (!match.Success)
                return null;

            return new(match.Groups["app"].Value.Trim(), match.Groups["version"].Value.Trim(), "", "", match.Groups["suffix"].Value.Trim(), decoded.Trim());
        }

        return new(
            match.Groups["app"].Value.Trim(), match.Groups["version"].Value.Trim(), match.Groups["device"].Value.Trim(), match.Groups["os"].Value.Trim(),
            match.Groups["suffix"].Value.Trim(), decoded.Trim());
    }
}

/// <summary>Parsed Snapchat-style UserComment fields.</summary>
public sealed record SnapchatUserCommentInfo(string AppName, string AppVersion, string Device, string OsInfo, string Suffix, string Raw);