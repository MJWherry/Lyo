namespace Lyo.IO.FileSystem;

/// <summary>Live change notifications for one <see cref="IFileSystem.Watch" /> subscription. Dispose to stop watching.</summary>
public interface IFileSystemWatch : IDisposable
{
    /// <summary>Raised after a file or directory change under the watched path.</summary>
    event EventHandler<FileSystemChange>? Changed;
}
