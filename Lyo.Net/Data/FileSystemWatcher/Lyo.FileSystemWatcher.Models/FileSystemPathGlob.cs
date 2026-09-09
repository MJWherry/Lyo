using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Lyo.Exceptions;

namespace Lyo.FileSystemWatcher.Models;

/// <summary>
/// Regex matching for paths relative to a watch root. Paths use '/' separators. Patterns are .NET regular expressions (not globs) and are not anchored unless the pattern includes <c>^</c>/<c>$</c>.
/// </summary>
public static class FileSystemPathGlob
{
    private static readonly TimeSpan MatchTimeout = TimeSpan.FromMilliseconds(250);
    private static readonly ConcurrentDictionary<string, Regex> RegexCache = new(StringComparer.Ordinal);

    /// <summary>True when <paramref name="relativePath" /> matches the regex <paramref name="pattern" />.</summary>
    public static bool IsMatch(string relativePath, string pattern, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
    {
        ArgumentHelpers.ThrowIfNull(relativePath);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(pattern);
        var path = Normalize(relativePath);
        return GetRegex(pattern, comparison).IsMatch(path);
    }

    /// <summary>
    /// Compiles each pattern so invalid regex fails at options validation rather than during a walk.
    /// </summary>
    public static void ValidatePatterns(IEnumerable<string>? patterns, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
    {
        if (patterns is null)
            return;

        foreach (var pattern in patterns) {
            if (string.IsNullOrWhiteSpace(pattern))
                continue;

            _ = GetRegex(pattern, comparison);
        }
    }

    /// <summary>
    /// File include/exclude: empty include matches all; any matching exclude rejects. Paths are relative to the watch root.
    /// </summary>
    public static bool IsFileIncluded(
        string relativePath,
        IEnumerable<string>? includePatterns,
        IEnumerable<string>? excludePatterns,
        StringComparison comparison = StringComparison.OrdinalIgnoreCase)
    {
        ArgumentHelpers.ThrowIfNull(relativePath);
        if (MatchesAny(relativePath, excludePatterns, comparison))
            return false;

        if (includePatterns is null)
            return true;

        var sawPattern = false;
        foreach (var pattern in includePatterns) {
            if (string.IsNullOrWhiteSpace(pattern))
                continue;

            sawPattern = true;
            if (IsMatch(relativePath, pattern, comparison))
                return true;
        }

        return !sawPattern;
    }

    /// <summary>
    /// True when every path under <paramref name="relativeDirectory" /> would be excluded, so the walker can skip the subtree.
    /// The snapshot root is never excluded this way.
    /// </summary>
    public static bool IsDirectoryExcluded(
        string relativeDirectory,
        IEnumerable<string>? excludePatterns,
        StringComparison comparison = StringComparison.OrdinalIgnoreCase)
    {
        if (excludePatterns is null)
            return false;

        var dir = Normalize(relativeDirectory);
        if (dir.Length == 0)
            return false;

        foreach (var pattern in excludePatterns) {
            if (string.IsNullOrWhiteSpace(pattern))
                continue;

            if (IsMatch(dir, pattern, comparison) || IsMatch(dir + "/", pattern, comparison))
                return true;

            if (IsMatch(dir + "/__probe__", pattern, comparison) && IsMatch(dir + "/__probe__/nested", pattern, comparison))
                return true;
        }

        return false;
    }

    /// <summary>Converts OS paths to a canonical relative path using '/' separators.</summary>
    public static string Normalize(string path)
    {
        ArgumentHelpers.ThrowIfNull(path);
        if (path.Length == 0)
            return path;

        var normalized = path.Replace('\\', '/').Trim('/');
        while (normalized.IndexOf("//", StringComparison.Ordinal) >= 0)
            normalized = normalized.Replace("//", "/");

        return normalized;
    }

    private static bool MatchesAny(string relativePath, IEnumerable<string>? patterns, StringComparison comparison)
    {
        if (patterns is null)
            return false;

        foreach (var pattern in patterns) {
            if (string.IsNullOrWhiteSpace(pattern))
                continue;

            if (IsMatch(relativePath, pattern, comparison))
                return true;
        }

        return false;
    }

    private static Regex GetRegex(string pattern, StringComparison comparison)
        => RegexCache.GetOrAdd(comparison + "\n" + pattern, _ => {
            try {
                return new(pattern, ToRegexOptions(comparison), MatchTimeout);
            }
            catch (ArgumentException ex) {
                throw new ArgumentException($"Invalid include/exclude regex '{pattern}'.", ex);
            }
        });

    private static RegexOptions ToRegexOptions(StringComparison comparison)
        => comparison is StringComparison.OrdinalIgnoreCase or StringComparison.CurrentCultureIgnoreCase or StringComparison.InvariantCultureIgnoreCase
            ? RegexOptions.CultureInvariant | RegexOptions.IgnoreCase
            : RegexOptions.CultureInvariant;
}
