using System.Text;
using Lyo.Common.Extensions;
using Lyo.Exceptions;

namespace Lyo.Authentication.Models.Format;

/// <summary>Base64url (RFC 4648 §5) without padding. Used both for the Format-B opaque token <c>secret</c> segment and for Lyo JWT components.</summary>
public static class Base64Url
{
    /// <summary>Encodes <paramref name="bytes" /> as base64url without trailing <c>=</c> padding.</summary>
    public static string Encode(ReadOnlySpan<byte> bytes)
    {
        if (bytes.IsEmpty)
            return string.Empty;

#if NET10_0_OR_GREATER
        var base64 = Convert.ToBase64String(bytes);
#else
        var base64 = Convert.ToBase64String(bytes.ToArray());
#endif
        return Sanitize(base64);
    }

    /// <summary>Decodes a base64url string into bytes. Throws <see cref="FormatException" /> on malformed input.</summary>
    public static byte[] Decode(string input)
    {
        ArgumentHelpers.ThrowIfNull(input);
        if (input.Length == 0)
            return [];

        var padded = Pad(input.Replace('-', '+').Replace('_', '/'));
        return Convert.FromBase64String(padded);
    }

    /// <summary>True if <paramref name="input" /> is non-empty and only contains base64url characters.</summary>
    public static bool IsValid(string? input)
    {
        if (input.IsNullOrEmpty())
            return false;

        foreach (var c in input) {
            if (c >= 'A' && c <= 'Z')
                continue;

            if (c >= 'a' && c <= 'z')
                continue;

            if (c >= '0' && c <= '9')
                continue;

            if (c is '-' or '_')
                continue;

            return false;
        }

        return true;
    }

    private static string Sanitize(string base64)
    {
        var trimmed = base64.TrimEnd('=');
        var sb = new StringBuilder(trimmed.Length);
        foreach (var c in trimmed) {
            sb.Append(
                c switch {
                    '+' => '-',
                    '/' => '_',
                    var _ => c
                });
        }

        return sb.ToString();
    }

    private static string Pad(string input)
        => (input.Length % 4) switch {
            2 => input + "==",
            3 => input + "=",
            var _ => input
        };
}