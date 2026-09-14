namespace Lyo.IO.Temp.Enums;

/// <summary>How <see cref="Lyo.IO.Temp.IOTempService" /> builds an <see cref="Lyo.IO.FileSystem.IFileSystem" /> when none is registered in DI.</summary>
public enum IOTempStorageKind
{
    /// <summary>Local disk at <see cref="Models.IOTempServiceOptions.RootDirectory" />.</summary>
    Local = 0,

    /// <summary>In-memory POSIX store (<see cref="Lyo.IO.FileSystem.MemoryFileSystem" />).</summary>
    Memory = 1
}
