using System.Text.Json;
using Lyo.Desktop;

namespace Lyo.Web.Automation.Studio;

internal sealed class StudioHotkeySettings
{
    public string StartWatch { get; set; } = "Ctrl+Shift+W";

    public string StopWatch { get; set; } = "Ctrl+Shift+Q";
}

/// <summary>Persists the watch chords at <c>$XDG_CONFIG_HOME/lyo/automation-studio.json</c> or <c>%AppData%\Lyo\automation-studio.json</c>.</summary>
internal static class StudioHotkeyStore
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };

    public static StudioHotkeySettings Load()
    {
        try {
            var path = ConfigPath();
            if (!File.Exists(path))
                return new();
            var loaded = JsonSerializer.Deserialize<StudioHotkeySettings>(File.ReadAllText(path), Json);
            return WithDefaults(loaded ?? new());
        }
        catch (Exception) {
            return new();
        }
    }

    public static void Save(StudioHotkeySettings settings)
    {
        var path = ConfigPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(WithDefaults(settings), Json));
    }

    private static StudioHotkeySettings WithDefaults(StudioHotkeySettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.StartWatch))
            settings.StartWatch = "Ctrl+Shift+W";
        if (string.IsNullOrWhiteSpace(settings.StopWatch))
            settings.StopWatch = "Ctrl+Shift+Q";
        return settings;
    }

    private static string ConfigPath() => DesktopConfig.File("automation-studio.json");
}
