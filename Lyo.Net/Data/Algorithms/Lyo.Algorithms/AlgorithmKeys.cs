using System.Text.RegularExpressions;
using Lyo.Exceptions;

namespace Lyo.Algorithms;

/// <summary>Key and label grammar shared by the catalog and startup registration.</summary>
public static class AlgorithmKeys
{
    /// <summary>Catalog and startup keys: <c>attack.physical</c>.</summary>
    public const string KeyPattern = "^[a-z][a-z0-9_.]*$";

    /// <summary>Labels: <c>live</c>, <c>game.1.0</c>.</summary>
    public const string LabelPattern = "^[a-z][a-z0-9._-]*$";

    private static readonly Regex KeyRegex = new(KeyPattern, RegexOptions.CultureInvariant);

    private static readonly Regex LabelRegex = new(LabelPattern, RegexOptions.CultureInvariant);

    /// <summary>True when <paramref name="key" /> matches <see cref="KeyPattern" />.</summary>
    public static bool IsKey(string? key) => key is not null && KeyRegex.IsMatch(key);

    /// <summary>True when <paramref name="label" /> matches <see cref="LabelPattern" />.</summary>
    public static bool IsLabel(string? label) => label is not null && LabelRegex.IsMatch(label);

    /// <summary>Throws <see cref="ArgumentException" /> when <paramref name="key" /> is not a catalog key.</summary>
    public static void ThrowIfInvalidKey(string key)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key);
        if (!IsKey(key))
            throw new ArgumentException($"Algorithm key '{key}' must match {KeyPattern}.", nameof(key));
    }

    /// <summary>Throws <see cref="ArgumentException" /> when <paramref name="label" /> is not a label.</summary>
    public static void ThrowIfInvalidLabel(string label)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(label);
        if (!IsLabel(label))
            throw new ArgumentException($"Algorithm label '{label}' must match {LabelPattern}.", nameof(label));
    }
}