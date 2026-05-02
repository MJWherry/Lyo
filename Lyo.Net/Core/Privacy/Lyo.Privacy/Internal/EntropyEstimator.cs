namespace Lyo.Privacy.Internal;

internal static class EntropyEstimator
{
    /// <summary>Shannon entropy in bits per character (0..~8 for ASCII-heavy secrets).</summary>
    public static double ShannonBitsPerChar(string text)
    {
        if (text.Length == 0)
            return 0;

        Span<int> ascii = stackalloc int[128];
        var high = new Dictionary<char, int>();
        foreach (var c in text) {
            if (c < 128)
                ascii[c]++;
            else
                high[c] = high.TryGetValue(c, out var n) ? n + 1 : 1;
        }

        double sum = 0;
        var len = (double)text.Length;
        for (var i = 0; i < 128; i++) {
            var c = ascii[i];
            if (c <= 0)
                continue;

            var p = c / len;
            sum -= p * Math.Log(p, 2);
        }

        foreach (var c in high.Values) {
            var p = c / len;
            sum -= p * Math.Log(p, 2);
        }

        return sum;
    }
}