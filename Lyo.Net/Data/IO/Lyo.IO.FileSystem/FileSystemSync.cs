namespace Lyo.IO.FileSystem;

/// <summary>Parks the calling thread on an async file-system call. Same pattern as Lyo SFTP/FTP clients.</summary>
public static class FileSystemSync
{
    /// <summary>Blocks until <paramref name="task" /> completes and returns its result.</summary>
    public static T Wait<T>(Task<T> task) => task.ConfigureAwait(false).GetAwaiter().GetResult();

    /// <summary>Blocks until <paramref name="task" /> completes.</summary>
    public static void Wait(Task task) => task.ConfigureAwait(false).GetAwaiter().GetResult();
}
