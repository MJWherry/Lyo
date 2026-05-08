using Lyo.Exceptions;

namespace Lyo.Privacy.Internal;

internal static class Luhn
{
    public static bool IsValid(string? digits)
    {
        ArgumentHelpers.ThrowIfNull(digits);
        if (digits.Length < 2)
            return false;

        var sum = 0;
        var alt = true;
        for (var i = digits.Length - 1; i >= 0; i--) {
            var c = digits[i];
            if (c is < '0' or > '9')
                return false;

            var n = c - '0';
            if (alt)
                sum += n;
            else {
                n *= 2;
                if (n > 9)
                    n -= 9;

                sum += n;
            }

            alt = !alt;
        }

        return sum % 10 == 0;
    }

    public static bool IsValid(ReadOnlySpan<char> digits)
    {
        if (digits.Length < 2)
            return false;

        var sum = 0;
        var alt = true;
        for (var i = digits.Length - 1; i >= 0; i--) {
            var c = digits[i];
            if (c is < '0' or > '9')
                return false;

            var n = c - '0';
            if (alt)
                sum += n;
            else {
                n *= 2;
                if (n > 9)
                    n -= 9;

                sum += n;
            }

            alt = !alt;
        }

        return sum % 10 == 0;
    }
}