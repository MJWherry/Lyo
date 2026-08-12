using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;
#if NET6_0_OR_GREATER
using System.Diagnostics;
#endif

namespace Lyo.Common.Pathing;

/// <summary>
/// Path combine / normalize / jail helpers for host filesystems and POSIX-style virtual roots (in-memory, SFTP).
/// Throw helpers mirror <see cref="UriHelpers" /> and use <see cref="InvalidFormatException" />.
/// </summary>
/// <remarks>
/// Optional name parameters use <see cref="CallerArgumentExpressionAttribute" /> like <see cref="ArgumentHelpers" />.
/// </remarks>
public static class PathHelpers
{
    private const char PosixSeparator = '/';

    [DoesNotReturn]
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    private static void ThrowInvalidFormat(string message, string? paramName, string? invalidValue, string expectedFormat)
        => throw new InvalidFormatException(message, paramName, invalidValue, expectedFormat);

    /// <summary>Returns the directory separator character for <paramref name="style" />.</summary>
    public static char GetDirectorySeparator(PathStyle style)
        => style == PathStyle.Host ? Path.DirectorySeparatorChar : PosixSeparator;

    /// <summary>Throws <see cref="ArgumentException" /> when <paramref name="path" /> is null, empty, or whitespace.</summary>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNullOrWhiteSpace([NotNull] string? path, [CallerArgumentExpression("path")] string? paramName = null)
        => ArgumentHelpers.ThrowIfNullOrWhiteSpace(path, paramName);

    /// <summary>Throws when <paramref name="path" /> is null/whitespace or contains invalid path characters for <paramref name="style" />.</summary>
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

        // Posix: reject NUL only (remote/virtual roots may allow characters that Host forbids).
        if (path.IndexOf('\0') >= 0)
            ThrowInvalidFormat($"Path contains invalid characters: {path}", paramName, path, "POSIX path without NUL");
    }

    /// <summary>Throws when <paramref name="candidate" /> is outside <paramref name="root" /> after normalization.</summary>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    public static void ThrowIfEscapesRoot(
        PathStyle style,
        string root,
        [NotNull] string? candidate,
        [CallerArgumentExpression("candidate")] string? paramName = null)
    {
        ThrowIfNullOrWhiteSpace(root, nameof(root));
        ThrowIfNullOrWhiteSpace(candidate, paramName);
        if (!IsUnderRoot(style, root, candidate))
            ThrowInvalidFormat($"Path escapes root '{root}': {candidate}", paramName, candidate, $"Path under root {root}");
    }

    /// <summary>Returns whether <paramref name="candidate" /> is equal to or under <paramref name="root" /> after normalization.</summary>
    public static bool IsUnderRoot(PathStyle style, string root, string candidate)
    {
        if (string.IsNullOrWhiteSpace(root) || string.IsNullOrWhiteSpace(candidate))
            return false;

        var sep = GetDirectorySeparator(style);
        var fullRootTrimmed = TrimTrailingSeparators(GetFullPath(style, root), style);
        var fullCandidate = GetFullPath(style, candidate);
        var comparison = style == PathStyle.Host && OperatingSystemIsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        // POSIX filesystem root: every absolute path is under "/".
        if (style == PathStyle.Posix && fullRootTrimmed is "/")
            return IsPathRooted(PathStyle.Posix, fullCandidate);

        var fullRoot = fullRootTrimmed + sep;
        if (string.Equals(TrimTrailingSeparators(fullCandidate, style), fullRootTrimmed, comparison))
            return true;

        return fullCandidate.StartsWith(fullRoot, comparison);
    }

    /// <summary>Combines path segments using <paramref name="style" /> separators.</summary>
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

            result = TrimTrailingSeparators(result, PathStyle.Posix) + PosixSeparator
                     + NormalizeSeparators(segment, PathStyle.Posix).TrimStart(PosixSeparator);
        }

        return result ?? string.Empty;
    }

    /// <summary>
    /// Normalizes <paramref name="path" /> and resolves <c>.</c> / <c>..</c> within <paramref name="style" /> semantics.
    /// For <see cref="PathStyle.Host" />, delegates to <see cref="Path.GetFullPath(string)" />.
    /// For <see cref="PathStyle.Posix" />, does not consult the OS; absolute paths start with <c>/</c>.
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

    /// <summary>Returns the parent directory path, or null when there is no parent.</summary>
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

    /// <summary>Returns the file or directory name portion of <paramref name="path" />.</summary>
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

    /// <summary>Returns the file name without its extension.</summary>
    public static string GetFileNameWithoutExtension(PathStyle style, string path)
    {
        var name = GetFileName(style, path);
        if (style == PathStyle.Host)
            return Path.GetFileNameWithoutExtension(name);

        var idx = name.LastIndexOf('.');
        return idx <= 0 ? name : name[..idx];
    }

    /// <summary>Returns the extension including the leading dot, or empty string.</summary>
    public static string GetExtension(PathStyle style, string path)
    {
        var name = GetFileName(style, path);
        if (style == PathStyle.Host)
            return Path.GetExtension(name);

        var idx = name.LastIndexOf('.');
        return idx <= 0 ? string.Empty : name[idx..];
    }

    /// <summary>Returns whether <paramref name="path" /> is rooted for <paramref name="style" />.</summary>
    public static bool IsPathRooted(PathStyle style, string path)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        if (style == PathStyle.Host)
            return Path.IsPathRooted(path);

        return path[0] == PosixSeparator || path[0] == '\\';
    }

    /// <summary>Replaces alternate separators with the style separator.</summary>
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

    /// <summary>Removes trailing directory separators (keeps a single <c>/</c> root for Posix).</summary>
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
