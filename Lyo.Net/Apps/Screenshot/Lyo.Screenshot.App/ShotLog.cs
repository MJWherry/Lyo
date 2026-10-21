namespace Lyo.Screenshot.App;

internal static class ShotLog
{
    private static readonly Lock Gate = new();

    public static void Info(string message)
    {
        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}";
        try {
            Console.Error.WriteLine("lyo-shot: " + message);
        }
        catch (IOException) { }

        try {
            var path = Path.Combine(ShotSettingsStore.ConfigDirectory(), "screenshot.log");
            lock (Gate)
                File.AppendAllText(path, line + Environment.NewLine);
        }
        catch (Exception) { }
    }
}