namespace Lyo.Screenshot;

/// <summary>Scratch folder for unsaved shots and receipt spill files. The lyo-shot window deletes it on close.</summary>
public static class ShotScratch
{
    private const int WipeAttempts = 3;
    private const int WipeDelayMs = 150;

    /// <summary>OS temp directory <c>lyo-shot</c>.</summary>
    public static string Directory => Path.Combine(Path.GetTempPath(), "lyo-shot");

    /// <summary>Deletes <paramref name="directory" /> and everything inside it. A null, empty, or missing path is ignored. A file that is still open is retried a few times.</summary>
    /// <param name="directory">Folder to remove.</param>
    /// <returns>True when the folder is gone or was already missing.</returns>
    public static bool Wipe(string? directory)
    {
        if (string.IsNullOrWhiteSpace(directory) || !System.IO.Directory.Exists(directory))
            return true;

        for (var attempt = 1; attempt <= WipeAttempts; attempt++) {
            try {
                System.IO.Directory.Delete(directory, true);
                return true;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) {
                if (attempt < WipeAttempts)
                    Thread.Sleep(WipeDelayMs);
            }
        }

        return false;
    }
}