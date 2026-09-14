namespace Lyo.IO.FileSystem;

/// <summary>Operations a <see cref="IFileSystem" /> implementation supports.</summary>
[Flags]
public enum FileSystemCapabilities
{
    /// <summary>No operations.</summary>
    None = 0,

    /// <summary>Read files and list directories.</summary>
    Read = 1,

    /// <summary>Create and overwrite files.</summary>
    Write = 2,

    /// <summary>Create directories (including parents).</summary>
    CreateDirectory = 4,

    /// <summary>Delete files and directories.</summary>
    Delete = 8,

    /// <summary>Move or rename files and directories.</summary>
    Move = 16,

    /// <summary>Copy files.</summary>
    Copy = 32,

    /// <summary>Raise change notifications via <see cref="IFileSystem.Watch" />.</summary>
    Watch = 64
}
