using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Lyo.Exceptions;

/// <summary>Helper methods for throwing generic exceptions (FileNotFoundException, DirectoryNotFoundException, UnauthorizedAccessException, IOException).</summary>
public static class ExceptionThrower
{
#if NET6_0_OR_GREATER
    [DoesNotReturn]
    [StackTraceHidden]
#endif
    private static void ThrowDirectoryNotFound(string message) => throw new DirectoryNotFoundException(message);

#if NET6_0_OR_GREATER
    [DoesNotReturn]
    [StackTraceHidden]
#endif
    private static void ThrowUnauthorizedAccess(string message) => throw new UnauthorizedAccessException(message);

#if NET6_0_OR_GREATER
    [DoesNotReturn]
    [StackTraceHidden]
#endif
    private static void ThrowIOException(string message, Exception inner) => throw new IOException(message, inner);

    /// <summary>Throws a DirectoryNotFoundException if the directory does not exist.</summary>
    /// <param name="directoryPath">The directory path to check.</param>
    /// <param name="paramName">The parameter name.</param>
    /// <exception cref="ArgumentNullException">Thrown when directoryPath is null or empty.</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown when the directory does not exist.</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfDirectoryNotFound([NotNull] string? directoryPath, string? paramName = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(directoryPath, paramName ?? nameof(directoryPath));
        if (!Directory.Exists(directoryPath!))
            ThrowDirectoryNotFound($"Directory not found: {directoryPath}");
    }

    /// <summary>Throws a DirectoryNotFoundException if the directory does not exist.</summary>
    /// <param name="directoryInfo">The DirectoryInfo to check.</param>
    /// <param name="paramName">The parameter name.</param>
    /// <exception cref="ArgumentNullException">Thrown when directoryInfo is null.</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown when the directory does not exist.</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfDirectoryNotFound([NotNull] DirectoryInfo? directoryInfo, string? paramName = null)
    {
        ArgumentHelpers.ThrowIfNull(directoryInfo, paramName ?? nameof(directoryInfo));
        if (!directoryInfo!.Exists)
            ThrowDirectoryNotFound($"Directory not found: {directoryInfo.FullName}");
    }

    /// <summary>Throws an UnauthorizedAccessException or IOException if the file is not accessible.</summary>
    /// <param name="filePath">The file path to check.</param>
    /// <param name="paramName">The parameter name.</param>
    /// <exception cref="ArgumentException">Thrown when filePath is null or empty.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when the file is not accessible due to access restrictions.</exception>
    /// <exception cref="IOException">Thrown when the file is not accessible due to I/O errors.</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfFileNotAccessible([NotNull] string? filePath, string? paramName = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(filePath, paramName ?? nameof(filePath));
        try {
            var fileInfo = new FileInfo(filePath!);
            if (!fileInfo.Exists)
                return;

            using (fileInfo.Open(FileMode.Open, FileAccess.Read, FileShare.Read)) { }
        }
        catch (UnauthorizedAccessException) {
            ThrowUnauthorizedAccess($"File is not accessible: {filePath}");
        }
        catch (IOException ex) {
            ThrowIOException($"File is not accessible: {filePath}", ex);
        }
    }

    /// <summary>Throws an UnauthorizedAccessException or IOException if the file is not accessible.</summary>
    /// <param name="fileInfo">The FileInfo to check.</param>
    /// <param name="paramName">The parameter name.</param>
    /// <exception cref="ArgumentNullException">Thrown when fileInfo is null.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when the file is not accessible due to access restrictions.</exception>
    /// <exception cref="IOException">Thrown when the file is not accessible due to I/O errors.</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfFileNotAccessible([NotNull] FileInfo? fileInfo, string? paramName = null)
    {
        ArgumentHelpers.ThrowIfNull(fileInfo, paramName ?? nameof(fileInfo));
        try {
            if (!fileInfo!.Exists)
                return;

            using (fileInfo.Open(FileMode.Open, FileAccess.Read, FileShare.Read)) { }
        }
        catch (UnauthorizedAccessException) {
            ThrowUnauthorizedAccess($"File is not accessible: {fileInfo!.FullName}");
        }
        catch (IOException ex) {
            ThrowIOException($"File is not accessible: {fileInfo!.FullName}", ex);
        }
    }

    /// <summary>Throws an UnauthorizedAccessException or IOException if the directory is not accessible.</summary>
    /// <param name="directoryPath">The directory path to check.</param>
    /// <param name="paramName">The parameter name.</param>
    /// <exception cref="ArgumentException">Thrown when directoryPath is null or empty.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when the directory is not accessible due to access restrictions.</exception>
    /// <exception cref="IOException">Thrown when the directory is not accessible due to I/O errors.</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfDirectoryNotAccessible([NotNull] string? directoryPath, string? paramName = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(directoryPath, paramName ?? nameof(directoryPath));
        try {
            var directoryInfo = new DirectoryInfo(directoryPath!);
            if (directoryInfo.Exists)
                _ = directoryInfo.GetFileSystemInfos();
        }
        catch (UnauthorizedAccessException) {
            ThrowUnauthorizedAccess($"Directory is not accessible: {directoryPath}");
        }
        catch (IOException ex) {
            ThrowIOException($"Directory is not accessible: {directoryPath}", ex);
        }
    }

    /// <summary>Throws an UnauthorizedAccessException or IOException if the directory is not accessible.</summary>
    /// <param name="directoryInfo">The DirectoryInfo to check.</param>
    /// <param name="paramName">The parameter name.</param>
    /// <exception cref="ArgumentNullException">Thrown when directoryInfo is null.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when the directory is not accessible due to access restrictions.</exception>
    /// <exception cref="IOException">Thrown when the directory is not accessible due to I/O errors.</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfDirectoryNotAccessible([NotNull] DirectoryInfo? directoryInfo, string? paramName = null)
    {
        ArgumentHelpers.ThrowIfNull(directoryInfo, paramName ?? nameof(directoryInfo));
        try {
            if (directoryInfo!.Exists)
                _ = directoryInfo.GetFileSystemInfos();
        }
        catch (UnauthorizedAccessException) {
            ThrowUnauthorizedAccess($"Directory is not accessible: {directoryInfo.FullName}");
        }
        catch (IOException ex) {
            ThrowIOException($"Directory is not accessible: {directoryInfo.FullName}", ex);
        }
    }
}