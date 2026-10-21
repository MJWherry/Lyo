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
    Watch = 64,

    /// <summary>Open an existing file for append. Object stores clear this flag and throw from <see cref="IFileSystem.OpenAppend" />.</summary>
    Append = 128,

    /// <summary>Move or rename a directory and everything under it. Backends without it throw <see cref="NotSupportedException" /> for directory moves.</summary>
    MoveDirectory = 256
}