using System.Text;
using System.Text.RegularExpressions;

namespace Lyo.Images;

/// <summary>Decodes EXIF UserComment values, which may be base64-encoded (e.g. Snapchat, some Android cameras).</summary>
public static class ExifUserCommentDecoder
{
    /// <summary>EXIF UserComment has an 8-byte character code prefix (e.g. "ASCII\0\0\0", "UNICODE\0", or zeros).</summary>
    private const int ExifUserCommentPrefixLength = 8;

    /// <summary>Attempts to decode a UserComment value. Handles base64-encoded UTF-8 (common with Snapchat/Android).</summary>
    /// <param name="value">The raw UserComment string (may be base64 or plain text).</param>
    /// <returns>Decoded text, or the original value if decoding fails.</returns>
    public static string Decode(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return value ?? string.Empty;

        // Try base64 decode first (Snapchat and some Android apps use this)
        if (TryBase64Decode(value, out var decoded))
            return decoded;

        return value;
    }

    /// <summary>Attempts to decode and strip the EXIF character-code prefix from a UserComment.</summary>
    /// <param name="value">The raw UserComment string.</param>
    /// <param name="stripExifPrefix">If true, removes the standard 8-byte EXIF character code prefix when present.</param>
    /// <returns>Decoded text with optional prefix stripped.</returns>
    public static string Decode(string? value, bool stripExifPrefix)
    {
        var decoded = Decode(value);
        if (!stripExifPrefix || decoded.Length <= ExifUserCommentPrefixLength)
            return decoded;

        var bytes = Encoding.UTF8.GetBytes(decoded);
        if (bytes.Length <= ExifUserCommentPrefixLength)
            return decoded;

        // Check for common EXIF prefixes: ASCII\0\0\0, UNICODE\0, or all zeros
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

        // Base64 allows only A-Za-z0-9+/=
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

    /// <summary>Parses Snapchat-style UserComment: "LSnapchat/13.73.0.61 Beta (Pixel 7a; Android 16#...)" into app, device, and OS.</summary>
    /// <param name="decoded">The decoded UserComment text (e.g. from Decode).</param>
    /// <returns>A record with AppName, AppVersion, Device, OsInfo, and Raw if parsing succeeds.</returns>
    public static SnapchatUserCommentInfo? TryParseSnapchatFormat(string? decoded)
    {
        if (string.IsNullOrWhiteSpace(decoded))
            return null;

        // Format: "N\nLSnapchat/13.73.0.61 Beta (Pixel 7a; Android 16#14339231#36; gzip) V/MUSHROOM"
        // Structure: L{app}/{version} ({device}; {os}) {suffix}
        // Require the paren group so greedy [^(]+ backtracks to leave space for \s+ before (
        var match = Regex.Match(decoded, @"L(?<app>[^/]+)/(?<version>[^(]+)\s+\((?<device>[^;]+);\s*(?<os>[^)]+)\)\s*(?<suffix>.*)?", RegexOptions.Singleline);
        if (!match.Success) {
            // Fallback: no device block, e.g. "LSnapchat/13.73.0.61 Beta V/MUSHROOM"
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

/// <summary>Parsed Snapchat-style UserComment metadata.</summary>
public sealed record SnapchatUserCommentInfo(string AppName, string AppVersion, string Device, string OsInfo, string Suffix, string Raw);