using Lyo.IO.FileSystem;

namespace Lyo.FileStorage;

/// <summary>Optional companion on FileStorage backends that can list raw object keys. Not the metadata catalog.</summary>
public interface IFileStoragePhysical
{
    /// <summary>Raw backend keys jailed to the storage prefix (disk root, S3 key prefix, or blob prefix).</summary>
    IFileSystem Physical { get; }
}
