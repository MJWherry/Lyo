using System.Text;

namespace Lyo.Privacy.Internal;

internal static class IbanValidator
{
    public static bool IsValidMod97(string normalized)
    {
        if (normalized is null)
            throw new ArgumentNullException(nameof(normalized));
#if NETSTANDARD2_0
        if (normalized.Length is < 15 or > 34)
            return false;

        var a = normalized.ToCharArray();
        return IsValidMod97(new ReadOnlySpan<char>(a));
#else
        return IsValidMod97(normalized.AsSpan());
#endif
    }

    /// <summary>MOD-97-10 check; <paramref name="normalized" /> must be A-Z0-9 only, length 15–34.</summary>
    public static bool IsValidMod97(ReadOnlySpan<char> normalized)
    {
        if (normalized.Length is < 15 or > 34)
            return false;

        var rem = 0;
        for (var pass = 0; pass < 2; pass++) {
            var start = pass == 0 ? 4 : 0;
            var end = pass == 0 ? normalized.Length : 4;
            for (var i = start; i < end; i++)
                Feed(normalized[i], ref rem);
        }

        return rem == 1;

        static void Feed(char c, ref int rem)
        {
            if (c is >= '0' and <= '9')
                rem = (rem * 10 + (c - '0')) % 97;
            else if (c is >= 'A' and <= 'Z') {
                var v = c - 'A' + 10;
                rem = (rem * 10 + v / 10) % 97;
                rem = (rem * 10 + v % 10) % 97;
            }
        }
    }

    public static string NormalizeIban(string raw)
    {
        var sb = new StringBuilder(raw.Length);
        foreach (var c in raw.Where(c => c is not (' ' or '-')))
            sb.Append(char.ToUpperInvariant(c));

        return sb.ToString();
    }
}