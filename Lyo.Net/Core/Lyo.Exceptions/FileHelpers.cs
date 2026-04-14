using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Lyo.Exceptions;

/// <summary>
/// Helper methods for file name and path segment validation (e.g. multipart uploads). For directory/file existence, use <see cref="ExceptionThrower" /> or
/// <see cref="ArgumentHelpers" />.
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
    public static void ThrowIfFileNameInvalid([NotNull] string? fileName, string? paramName = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(fileName, paramName ?? nameof(fileName));
        ArgumentHelpers.ThrowIf(fileName.Contains(".."), paramName ?? nameof(fileName), "File name must not contain path traversal (..).");
        ArgumentHelpers.ThrowIf(Path.IsPathRooted(fileName), paramName ?? fileName, "File name must be a relative path or simple file name, not an absolute path.");
        ArgumentHelpers.ThrowIf(fileName.IndexOfAny(InvalidFileNameChars) >= 0, paramName ?? nameof(fileName), $"File name contains invalid characters: {fileName}");
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
    public static string GetValidFileName([NotNull] string? pathOrFileName, string? paramName = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(pathOrFileName, paramName ?? nameof(pathOrFileName));
        var fileName = Path.GetFileName(pathOrFileName);
        ArgumentHelpers.ThrowIfNullOrEmpty(fileName, paramName ?? nameof(fileName));
        ThrowIfFileNameInvalid(fileName, paramName);
        return fileName;
    }

    /// <summary>Attempts to validate a file name. Returns true if valid.</summary>
    public static bool TryGetValidFileName(string? pathOrFileName, out string? fileName)
    {
        fileName = null;
        if (string.IsNullOrWhiteSpace(pathOrFileName))
            return false;

        var name = Path.GetFileName(pathOrFileName);
        if (string.IsNullOrWhiteSpace(name) || name.IndexOf("..", StringComparison.Ordinal) >= 0 || Path.IsPathRooted(pathOrFileName!) ||
            name.IndexOfAny(InvalidFileNameChars) >= 0)
            return false;

        fileName = name;
        return true;
    }
}