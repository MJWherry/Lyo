namespace Lyo.FileManager;

/// <summary>Appends one line to the file-manager log and to the console.</summary>
public static class FileManagerLog
{
    /// <summary><c>file-manager.log</c> beside <see cref="FileManagerSettingsStore.ConfigPath" />.</summary>
    public static string LogPath()
    {
        var settings = FileManagerSettingsStore.ConfigPath();
        return Path.Combine(Path.GetDirectoryName(settings)!, "file-manager.log");
    }

    /// <summary>Appends <paramref name="message" /> to <see cref="LogPath" /> and the console.</summary>
    public static void Write(string message) => Write(LogPath(), message);

    /// <summary>Appends <paramref name="message" /> to <paramref name="path" /> and the console.</summary>
    public static void Write(string path, string message)
    {
        var line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + message;
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        File.AppendAllText(path, line + Environment.NewLine);
        Console.Error.WriteLine(line);
    }
}