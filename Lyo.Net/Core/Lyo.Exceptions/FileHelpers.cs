using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
#if NET6_0_OR_GREATER
using System.Diagnostics;
// ReSharper disable RedundantSuppressNullableWarningExpression
#endif

namespace Lyo.Exceptions;

/// <summary>
/// Helper methods for file name and path segment validation (e.g. multipart uploads). For directory/file existence, use <see cref="ExceptionThrower" /> or
/// <see cref="ArgumentHelpers" />. Optional <c>paramName</c> uses <see cref="CallerArgumentExpressionAttribute" /> like <see cref="ArgumentHelpers" />.
/// </summary>
public static class FileHelpers
{
    private static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();

    /// <summary>Throws if the file name is invalid for use in multipart uploads or safe path operations. Rejects path traversal, absolute paths, and invalid characters.</summary>
    /// <param name="fileName">The file name to validate (e.g. "document.pdf", not a full path).</param>
    /// <param name="paramName">The parameter name.</param>
    /// <exception cref="ArgumentException">Thrown when fileName is null, empty, whitespace, contains path traversal (..), is an absolute path, or contains invalid characters.</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfFileNameInvalid([NotNull] string? fileName, [CallerArgumentExpression("fileName")] string? paramName = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(fileName, paramName);
        ArgumentHelpers.ThrowIf(fileName.Contains(".."), "File name must not contain path traversal (..).", paramName);
        ArgumentHelpers.ThrowIf(Path.IsPathRooted(fileName), "File name must be a relative path or simple file name, not an absolute path.", paramName);
        ArgumentHelpers.ThrowIf(fileName.IndexOfAny(InvalidFileNameChars) >= 0, $"File name contains invalid characters: {fileName}", paramName);
    }

    /// <summary>Validates and returns a safe file name for multipart uploads. Trims path and returns only the final segment.</summary>
    /// <param name="pathOrFileName">A file path or file name.</param>
    /// <param name="paramName">The parameter name.</param>
    /// <returns>The validated file name (e.g. "document.pdf").</returns>
    /// <exception cref="ArgumentException">Thrown when pathOrFileName is null, empty, whitespace, or contains invalid characters.</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetValidFileName([NotNull] string? pathOrFileName, [CallerArgumentExpression("pathOrFileName")] string? paramName = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(pathOrFileName, paramName);
        var fileName = Path.GetFileName(pathOrFileName);
        ArgumentHelpers.ThrowIfNullOrEmpty(fileName, paramName);
        ThrowIfFileNameInvalid(fileName, paramName);
        return fileName;
    }

    /// <summary>Attempts to validate a file name. Returns true if valid.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetValidFileName(string? pathOrFileName, out string? fileName)
    {
        fileName = null;
        if (string.IsNullOrWhiteSpace(pathOrFileName))
            return false;

        var name = Path.GetFileName(pathOrFileName);
        if (string.IsNullOrWhiteSpace(name) || name.IndexOf("..", StringComparison.Ordinal) >= 0 || Path.IsPathRooted(pathOrFileName) || name.IndexOfAny(InvalidFileNameChars) >= 0)
            return false;

        fileName = name;
        return true;
    }

    /// <summary>
    /// Trims whitespace plus leading/trailing forward and back slashes from <paramref name="value" />. Returns <c>""</c> when the input is <see langword="null" />, empty, or
    /// whitespace-only. Designed for callers that build storage keys / listing prefixes from user-supplied input.
    /// </summary>
    /// <param name="value">Optional path-prefix string (e.g. <c>"/tenant/alpha/"</c>).</param>
    /// <returns>The trimmed prefix, or <c>""</c> when the input is null / empty / whitespace.</returns>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string NormalizePathPrefix(string? value) => string.IsNullOrWhiteSpace(value) ? "" : value!.Trim().TrimStart('/', '\\').TrimEnd('/', '\\');

    /// <summary>
    /// Throws <see cref="ArgumentException" /> when <paramref name="value" /> contains a path-traversal pattern when interpreted as a multi-segment path prefix. Rejects any
    /// segment that equals <c>..</c>, doubled separators (<c>//</c> / <c>\\</c>), and embedded <c>\0</c>. Empty / whitespace input is treated as "no prefix" and accepted.
    /// </summary>
    /// <param name="value">Optional path-prefix (raw or already-normalized).</param>
    /// <param name="paramName">Supplies <see cref="ArgumentException.ParamName" />. Omitted: caller's argument expression. Override with <c>nameof(...)</c> when needed.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value" /> contains a traversal pattern.</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfPathPrefixTraversal(string? value, [CallerArgumentExpression("value")] string? paramName = null)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        ArgumentHelpers.ThrowIf(
            value!.Contains("..") || value.Contains("//") || value.Contains("\\\\") || value.IndexOf('\0') >= 0,
            $"Path prefix '{value}' contains a traversal pattern ('..', '//', '\\\\', or NULL).", paramName);

        foreach (var segment in value.Replace('\\', '/').Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries)) {
            ArgumentHelpers.ThrowIf(string.Equals(segment, "..", StringComparison.Ordinal), $"Path prefix '{value}' has a segment equal to '..'.", paramName);
        }
    }

    /// <summary>
    /// Combines <see cref="NormalizePathPrefix" /> and <see cref="ThrowIfPathPrefixTraversal" />: returns the trimmed prefix on success, throws <see cref="ArgumentException" />
    /// when the (normalized) value contains a traversal pattern.
    /// </summary>
    /// <param name="value">Optional path-prefix.</param>
    /// <param name="paramName">Supplies <see cref="ArgumentException.ParamName" />. Omitted: caller's argument expression. Override with <c>nameof(...)</c> when needed.</param>
    /// <returns>Trimmed prefix (<c>""</c> when null / empty / whitespace).</returns>
    /// <exception cref="ArgumentException">Thrown when the prefix contains a traversal pattern.</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string NormalizeAndValidatePathPrefix(string? value, [CallerArgumentExpression("value")] string? paramName = null)
    {
        var trimmed = NormalizePathPrefix(value);
        ThrowIfPathPrefixTraversal(trimmed, paramName);
        return trimmed;
    }
}