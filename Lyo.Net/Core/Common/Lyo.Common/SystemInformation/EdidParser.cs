using System.Text;

namespace Lyo.Common.SystemInformation;

/// <summary>Minimal parser for EDID (Extended Display Identification Data) blobs exposed by the kernel (e.g. <c>/sys/class/drm/*/edid</c>).</summary>
internal static class EdidParser
{
    private const int MinimumLength = 128;
    private const byte DisplayProductNameTag = 0xFC;
    private static readonly int[] DescriptorOffsets = [54, 72, 90, 108];

    /// <summary>Determines whether <paramref name="edid" /> starts with the fixed 8-byte EDID header (<c>00 FF FF FF FF FF FF 00</c>) and is at least one 128-byte block long.</summary>
    internal static bool IsValid(byte[]? edid) =>
        edid is { Length: >= MinimumLength }
        && edid[0] == 0x00 && edid[1] == 0xFF && edid[2] == 0xFF && edid[3] == 0xFF
        && edid[4] == 0xFF && edid[5] == 0xFF && edid[6] == 0xFF && edid[7] == 0x00;

    /// <summary>Decodes the three-letter PNP manufacturer code packed as three 5-bit letters (big-endian) in bytes 8-9.</summary>
    /// <returns>The manufacturer code (e.g. <c>AUO</c>, <c>DEL</c>), or <see langword="null" /> when the blob is invalid or the letters are out of range.</returns>
    internal static string? GetManufacturerId(byte[]? edid)
    {
        if (!IsValid(edid))
            return null;

        var packed = (edid![8] << 8) | edid[9];
        var letters = new char[3];
        for (var i = 0; i < 3; i++) {
            var value = (packed >> (10 - i * 5)) & 0x1F;
            if (value is < 1 or > 26)
                return null;

            letters[i] = (char)('A' + value - 1);
        }

        return new string(letters);
    }

    /// <summary>Extracts the monitor model name from the display product name descriptor (tag <c>0xFC</c>) in one of the four 18-byte descriptor blocks.</summary>
    /// <returns>The model name trimmed at the <c>0x0A</c> terminator, or <see langword="null" /> when the blob is invalid or contains no product name descriptor.</returns>
    internal static string? GetModelName(byte[]? edid)
    {
        if (!IsValid(edid))
            return null;

        foreach (var offset in DescriptorOffsets) {
            // Display descriptors (as opposed to detailed timing descriptors) start with two zero bytes, then a reserved zero byte, then the tag.
            if (edid![offset] != 0x00 || edid[offset + 1] != 0x00 || edid[offset + 3] != DisplayProductNameTag)
                continue;

            var builder = new StringBuilder(13);
            for (var i = offset + 5; i < offset + 18; i++) {
                if (edid[i] == 0x0A)
                    break;

                builder.Append((char)edid[i]);
            }

            var name = builder.ToString().Trim();
            return name.Length > 0 ? name : null;
        }

        return null;
    }
}
