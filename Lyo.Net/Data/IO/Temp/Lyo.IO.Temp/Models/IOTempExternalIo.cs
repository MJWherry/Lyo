using Lyo.Common.Core.Pathing;
using Lyo.Exceptions.Models;
using Lyo.IO.FileSystem;

namespace Lyo.IO.Temp.Models;

/// <summary>
/// CopyFrom/MoveFrom/ExtractZip import a host path that may sit outside <see cref="IFileSystem.RootPath" />. In-jail paths use the VFS; escaped paths fall back to
/// <see cref="File" /> / <see cref="Directory" />.
/// </summary>
internal static class IOTempExternalIo
{
    public static bool IsOnStorage(IFileSystem storage, string path)
    {
        try {
            PathHelpers.ThrowIfEscapesRoot(storage.PathStyle, storage.RootPath, path);
            return true;
        }
        catch (InvalidFormatException) {
            return false;
        }
    }

    public static bool FileExists(IFileSystem storage, string path)
    {
        try {
            if (storage.FileExists(path))
                return true;
        }
        catch (InvalidFormatException) {
        }

        return File.Exists(path);
    }

    public static bool DirectoryExists(IFileSystem storage, string path)
    {
        try {
            if (storage.DirectoryExists(path))
                return true;
        }
        catch (InvalidFormatException) {
        }

        return Directory.Exists(path);
    }

    public static long GetLength(IFileSystem storage, string path)
        => IsOnStorage(storage, path) ? storage.GetLength(path) : new FileInfo(path).Length;

    public static Stream OpenRead(IFileSystem storage, string path)
    {
        if (IsOnStorage(storage, path))
            return storage.OpenRead(path);

        if (!File.Exists(path))
            throw new FileNotFoundException($"File not found: {path}", path);

        return File.OpenRead(path);
    }
}
