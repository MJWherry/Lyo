using Lyo.Exceptions;

namespace Lyo.Testing;

public static class Utilities
{
    public static void AppendBytesToFile(string path, long sizeInBytes)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(path, nameof(path));
        ArgumentHelpers.ThrowIfNegative(sizeInBytes, nameof(sizeInBytes));
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        using var fileStream = new FileStream(path, FileMode.Append, FileAccess.Write);
        var newLength = fileStream.Length + sizeInBytes;
        fileStream.SetLength(newLength);
    }
}