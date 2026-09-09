using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;
#if NET6_0_OR_GREATER
using System.Diagnostics;
#endif

namespace Lyo.Common.Core.Pathing;

/// <summary>
/// Path combine, normalize, and jail helpers for host filesystems and POSIX-style virtual roots (in-memory, SFTP). Throw helpers mirror <see cref="UriHelpers" /> and use
/// <see cref="InvalidFormatException" />.
/// </summary>
/// <remarks>Optional name parameters use <see cref="CallerArgumentExpressionAttribute" /> like <see cref="ArgumentHelpers" />.</remarks>
public static class PathHelpers
{
    private const char PosixSeparator = '/';
    private static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();

    [DoesNotReturn]
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    private static void ThrowInvalidFormat(string message, string? paramName, string? invalidValue, string expectedFormat)
        => throw new InvalidFormatException(message, paramName, invalidValue, expectedFormat);

    /// <summary>Directory separator character for <paramref name="style" />.</summary>
    public static char GetDirectorySeparator(PathStyle style) => style == PathStyle.Host ? Path.DirectorySeparatorChar : PosixSeparator;

    /// <summary>Throws <see cref="ArgumentException" /> when <paramref name="path" /> is null, empty, or only whitespace.</summary>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNullOrWhiteSpace([NotNull] string? path, [CallerArgumentExpression("path")] string? paramName = null)
        => ArgumentHelpers.ThrowIfNullOrWhiteSpace(path, paramName);

    /// <summary>Throws when <paramref name="path" /> is null or whitespace, or contains invalid path characters for <paramref name="style" />.</summary>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    public static void ThrowIfInvalidPath([NotNull] string? path, PathStyle style = PathStyle.Host, [CallerArgumentExpression("path")] string? paramName = null)
    {
        ThrowIfNullOrWhiteSpace(path, paramName);
        if (style == PathStyle.Host) {
            var invalid = Path.GetInvalidPathChars();
            if (path.IndexOfAny(invalid) >= 0)
                ThrowInvalidFormat($"Path contains invalid characters: {path}", paramName, path, "Path without invalid path characters");

            return;
        }

        // Posix: reject NUL only (remote/virtual roots may allow characters Host forbids).
        if (path.IndexOf('\0') >= 0)
            ThrowInvalidFormat($"Path contains invalid characters: {path}", paramName, path, "POSIX path without NUL");
    }

    /// <summary>Throws when <paramref name="candidate" /> lies outside <paramref name="root" /> after normalization.</summary>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    public static void ThrowIfEscapesRoot(PathStyle style, string root, [NotNull] string? candidate, [CallerArgumentExpression("candidate")] string? paramName = null)
    {
        ThrowIfNullOrWhiteSpace(root);
        ThrowIfNullOrWhiteSpace(candidate, paramName);
        if (!IsUnderRoot(style, root, candidate))
            ThrowInvalidFormat($"Path escapes root '{root}': {candidate}", paramName, candidate, $"Path under root {root}");
    }

    /// <summary>True when <paramref name="candidate" /> is equal to or under <paramref name="root" /> after normalization.</summary>
    public static bool IsUnderRoot(PathStyle style, string root, string candidate)
    {
        if (string.IsNullOrWhiteSpace(root) || string.IsNullOrWhiteSpace(candidate))
            return false;

        var sep = GetDirectorySeparator(style);
        var fullRootTrimmed = TrimTrailingSeparators(GetFullPath(style, root), style);
        var fullCandidate = GetFullPath(style, candidate);
        var comparison = style == PathStyle.Host && OperatingSystemIsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        // POSIX filesystem root: every absolute path sits under "/".
        if (style == PathStyle.Posix && fullRootTrimmed is "/")
            return IsPathRooted(PathStyle.Posix, fullCandidate);

        var fullRoot = fullRootTrimmed + sep;
        if (string.Equals(TrimTrailingSeparators(fullCandidate, style), fullRootTrimmed, comparison))
            return true;

        return fullCandidate.StartsWith(fullRoot, comparison);
    }

    /// <summary>Joins path segments using <paramref name="style" /> separators.</summary>
    public static string Combine(PathStyle style, params string[] segments)
    {
        ArgumentHelpers.ThrowIfNull(segments);
        if (segments.Length == 0)
            return string.Empty;

        if (style == PathStyle.Host)
            return Path.Combine(segments);

        string? result = null;
        foreach (var segment in segments) {
            if (string.IsNullOrEmpty(segment))
                continue;

            if (result is null) {
                result = NormalizeSeparators(segment, PathStyle.Posix);
                continue;
            }

            if (IsPathRooted(PathStyle.Posix, segment)) {
                result = NormalizeSeparators(segment, PathStyle.Posix);
                continue;
            }

            result = TrimTrailingSeparators(result, PathStyle.Posix) + PosixSeparator + NormalizeSeparators(segment, PathStyle.Posix).TrimStart(PosixSeparator);
        }

        return result ?? string.Empty;
    }

    /// <summary>
    /// Normalizes <paramref name="path" /> and resolves <c>.</c> / <c>..</c> under <paramref name="style" /> semantics. For <see cref="PathStyle.Host" />, delegates to
    /// <see cref="Path.GetFullPath(string)" />. For <see cref="PathStyle.Posix" />, does not consult the OS; absolute paths start with <c>/</c>.
    /// </summary>
    public static string GetFullPath(PathStyle style, string path)
    {
        ThrowIfInvalidPath(path, style);
        if (style == PathStyle.Host)
            return Path.GetFullPath(path);

        var normalized = NormalizeSeparators(path, PathStyle.Posix);
        var absolute = IsPathRooted(PathStyle.Posix, normalized);
        var parts = normalized.Split([PosixSeparator], StringSplitOptions.RemoveEmptyEntries);
        var stack = new List<string>(parts.Length);
        foreach (var part in parts) {
            if (part is ".")
                continue;

            if (part is "..") {
                if (stack.Count > 0)
                    stack.RemoveAt(stack.Count - 1);
                else if (!absolute)
                    stack.Add("..");

                continue;
            }

            stack.Add(part);
        }

        if (!absolute)
            return stack.Count == 0 ? "." : string.Join(PosixSeparator.ToString(), stack);

        return PosixSeparator + string.Join(PosixSeparator.ToString(), stack);
    }

    /// <summary>Parent directory path, or null when there is no parent.</summary>
    public static string? GetDirectoryName(PathStyle style, string path)
    {
        if (string.IsNullOrEmpty(path))
            return null;

        if (style == PathStyle.Host)
            return Path.GetDirectoryName(path);

        var normalized = NormalizeSeparators(path, PathStyle.Posix);
        if (normalized.Length == 1 && normalized[0] == PosixSeparator)
            return null;

        var trimmed = TrimTrailingSeparators(normalized, PathStyle.Posix);
        var idx = trimmed.LastIndexOf(PosixSeparator);
        if (idx < 0)
            return null;

        if (idx == 0)
            return PosixSeparator.ToString();

        return trimmed[..idx];
    }

    /// <summary>File or directory name portion of <paramref name="path" />.</summary>
    public static string GetFileName(PathStyle style, string path)
    {
        if (string.IsNullOrEmpty(path))
            return string.Empty;

        if (style == PathStyle.Host)
            return Path.GetFileName(path);

        var normalized = TrimTrailingSeparators(NormalizeSeparators(path, PathStyle.Posix), PathStyle.Posix);
        var idx = normalized.LastIndexOf(PosixSeparator);
        return idx < 0 ? normalized : normalized[(idx + 1)..];
    }

    /// <summary>File name without its extension.</summary>
    public static string GetFileNameWithoutExtension(PathStyle style, string path)
    {
        var name = GetFileName(style, path);
        if (style == PathStyle.Host)
            return Path.GetFileNameWithoutExtension(name);

        var idx = name.LastIndexOf('.');
        return idx <= 0 ? name : name[..idx];
    }

    /// <summary>Extension including the leading dot, or empty string.</summary>
    public static string GetExtension(PathStyle style, string path)
    {
        var name = GetFileName(style, path);
        if (style == PathStyle.Host)
            return Path.GetExtension(name);

        var idx = name.LastIndexOf('.');
        return idx <= 0 ? string.Empty : name[idx..];
    }

    /// <summary>
    /// Replaces invalid filename characters (including <c>/</c> and <c>\</c>) with <c>_</c> and trims dots/spaces. Returns <see langword="null" /> when nothing usable remains.
    /// </summary>
    /// <param name="name">Candidate file name, or a path when <paramref name="takeLeaf" /> is set.</param>
    /// <param name="replacement">Character substituted for invalid characters, or <see langword="null" /> to drop them instead of replacing them.</param>
    /// <param name="stripControlCharacters">
    /// Also treat control characters as invalid. Needed on Unix, where <see cref="Path.GetInvalidFileNameChars" /> only reports <c>\0</c> and <c>/</c>, so names carrying
    /// newlines or escape sequences would otherwise survive.
    /// </param>
    /// <param name="takeLeaf">
    /// Strip directory segments first, so a full path collapses to its file name. Off by default: callers that already hold a leaf keep their behavior.
    /// </param>
    /// <param name="maxLength">When greater than zero, caps the result at this many characters while preserving the extension.</param>
    public static string? SanitizeFileName(string? name, char? replacement = '_', bool stripControlCharacters = false, bool takeLeaf = false, int maxLength = 0)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        var candidate = takeLeaf ? Path.GetFileName(name!.Replace('\\', PosixSeparator)) : name!;
        var builder = new StringBuilder(candidate.Length);
        foreach (var c in candidate.Trim()) {
            var invalid = c is '/' or '\\' || Array.IndexOf(InvalidFileNameChars, c) >= 0 || (stripControlCharacters && char.IsControl(c));
            if (!invalid)
                builder.Append(c);
            else if (replacement is { } fill)
                builder.Append(fill);
        }

        var result = builder.ToString().Trim().Trim('.');
        if (result.Length == 0)
            return null;

        if (maxLength > 0 && result.Length > maxLength) {
            var extension = Path.GetExtension(result);
            var stem = Path.GetFileNameWithoutExtension(result);
            result = stem[..Math.Min(stem.Length, Math.Max(1, maxLength - extension.Length))] + extension;
        }

        return string.IsNullOrWhiteSpace(result) ? null : result;
    }

    /// <summary>True when <paramref name="path" /> is rooted for <paramref name="style" />.</summary>
    public static bool IsPathRooted(PathStyle style, string path)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        if (style == PathStyle.Host)
            return Path.IsPathRooted(path);

        return path[0] == PosixSeparator || path[0] == '\\';
    }

    /// <summary>Rewrites alternate separators to the style separator.</summary>
    public static string NormalizeSeparators(string path, PathStyle style)
    {
        if (string.IsNullOrEmpty(path))
            return path;

        if (style == PathStyle.Host) {
            var sep = Path.DirectorySeparatorChar;
            var alt = Path.AltDirectorySeparatorChar;
            return sep == alt ? path : path.Replace(alt, sep);
        }

        return path.Replace('\\', PosixSeparator);
    }

    /// <summary>Strips trailing directory separators (keeps a single <c>/</c> root for Posix).</summary>
    public static string TrimTrailingSeparators(string path, PathStyle style)
    {
        if (string.IsNullOrEmpty(path))
            return path;

        if (style == PathStyle.Host) {
            var trimmed = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return trimmed.Length == 0 ? path[..1] : trimmed;
        }

        if (path is "/" or "\\")
            return PosixSeparator.ToString();

        var n = NormalizeSeparators(path, PathStyle.Posix).TrimEnd(PosixSeparator);
        return n.Length == 0 ? PosixSeparator.ToString() : n;
    }

    private static bool OperatingSystemIsWindows()
    {
#if NET6_0_OR_GREATER
        return OperatingSystem.IsWindows();
#else
        return Environment.OSVersion.Platform is PlatformID.Win32NT or PlatformID.Win32Windows or PlatformID.Win32S;
#endif
    }
}