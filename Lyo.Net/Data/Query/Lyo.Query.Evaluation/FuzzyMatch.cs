using System.Reflection;

namespace Lyo.Query;

/// <summary>
/// Trigram Jaccard similarity matching Postgres <c>pg_trgm.similarity()</c>: lowercase, split on non-alphanumeric characters, pad each word with two leading spaces and one
/// trailing space, then unique 3-character windows. Used for in-memory Fuzzy evaluation and rewritten to <c>EF.Functions.TrigramsSimilarity</c> for SQL.
/// </summary>
public static class FuzzyMatch
{
    /// <summary>Default cutoff when a Fuzzy condition omits <c>Threshold</c>. Matches Postgres <c>pg_trgm.similarity_threshold</c>.</summary>
    public const double DefaultThreshold = 0.3;

    /// <summary><see cref="Similarity(string,string)" /> method handle used when building and rewriting expression trees.</summary>
    public static readonly MethodInfo SimilarityMethod = typeof(FuzzyMatch).GetMethods(BindingFlags.Public | BindingFlags.Static)
        .Single(m => m.Name == nameof(Similarity) && m.GetParameters().Length == 2);

    /// <summary>Jaccard similarity of character trigrams in <c>[0, 1]</c>. Null or empty inputs are 0.</summary>
    /// <param name="a">Left string (typically a stored field).</param>
    /// <param name="b">Right string (typically the query literal).</param>
    /// <returns>Shared unique trigrams divided by the union of unique trigrams.</returns>
    public static double Similarity(string? a, string? b)
    {
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
            return 0;

        var left = ExtractTrigrams(a!);
        var right = ExtractTrigrams(b!);
        if (left.Count == 0 || right.Count == 0)
            return 0;

        var intersection = 0;
        foreach (var tri in left) {
            if (right.Contains(tri))
                intersection++;
        }

        var union = left.Count + right.Count - intersection;
        return union == 0 ? 0 : (double)intersection / union;
    }

    /// <summary>True when <see cref="Similarity(string,string)" /> is at least <paramref name="threshold" />.</summary>
    /// <param name="a">Left string.</param>
    /// <param name="b">Right string.</param>
    /// <param name="threshold">Cutoff in <c>[0, 1]</c>. Defaults to <see cref="DefaultThreshold" />.</param>
    public static bool IsSimilar(string? a, string? b, double threshold = DefaultThreshold) => Similarity(a, b) >= threshold;

    private static HashSet<string> ExtractTrigrams(string value)
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        var lower = value.ToLowerInvariant();
        var i = 0;
        while (i < lower.Length) {
            while (i < lower.Length && !IsWordChar(lower[i]))
                i++;

            var start = i;
            while (i < lower.Length && IsWordChar(lower[i]))
                i++;

            if (i > start)
                AddWordTrigrams(set, lower, start, i - start);
        }

        return set;
    }

    private static void AddWordTrigrams(HashSet<string> set, string s, int start, int length)
    {
        var padded = "  " + s.Substring(start, length) + " ";
        for (var i = 0; i <= padded.Length - 3; i++)
            set.Add(padded.Substring(i, 3));
    }

    private static bool IsWordChar(char c) => char.IsLetterOrDigit(c);
}