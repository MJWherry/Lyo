using System.Text.Json;
using Lyo.Desktop;
using Lyo.Desktop.Components;
using Lyo.Screenshot;

namespace Lyo.Screenshot.App;

internal sealed class ShotSettings
{
    public ShotKind Mode { get; set; } = ShotKind.Receipt;

    public int FramesPerSecond { get; set; } = 5;

    public int DelaySeconds { get; set; }

    public string SaveDirectory { get; set; } = "";

    public bool? SaveToFile { get; set; }

    public bool CopyToClipboard { get; set; } = true;

    public bool OpenAfterSave { get; set; }

    public bool IncludeCursor { get; set; }

    public bool AlwaysOnTop { get; set; }

    public string StartHotkey { get; set; } = "Ctrl+Shift+S";

    public string StopHotkey { get; set; } = "Ctrl+Shift+X";

    public bool FullScreen { get; set; }

    public long WindowId { get; set; }

    public string WindowTitle { get; set; } = "";

    public int RegionX { get; set; }

    public int RegionY { get; set; }

    public int RegionWidth { get; set; }

    public int RegionHeight { get; set; }

    public bool Docked { get; set; }

    public ScreenEdge DockEdge { get; set; }

    public int DockX { get; set; }

    public int DockY { get; set; }

    public int DockRestX { get; set; }

    public int DockRestY { get; set; }
}

internal static class ShotSettingsStore
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };

    public static ShotSettings Load()
    {
        try {
            var path = ConfigPath();
            if (!File.Exists(path))
                return WithDefaultDirectory(new());
            var loaded = JsonSerializer.Deserialize<ShotSettings>(File.ReadAllText(path), Json) ?? new ShotSettings();
            return WithDefaultDirectory(loaded);
        }
        catch (Exception) {
            return WithDefaultDirectory(new());
        }
    }

    public static void Save(ShotSettings settings)
    {
        var path = ConfigPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(settings, Json));
    }

    public static string DefaultSaveDirectory()
    {
        var pictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        var screenshots = Path.Combine(pictures, "Screenshots");
        if (OperatingSystem.IsWindows() || Directory.Exists(screenshots))
            return screenshots;
        return string.IsNullOrEmpty(pictures) ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) : pictures;
    }

    private static ShotSettings WithDefaultDirectory(ShotSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.SaveDirectory))
            settings.SaveDirectory = DefaultSaveDirectory();
        if (settings.FramesPerSecond < 1)
            settings.FramesPerSecond = 5;
        if (settings.DelaySeconds < 0)
            settings.DelaySeconds = 0;
        if (settings.DelaySeconds > 10)
            settings.DelaySeconds = 10;
        settings.SaveToFile ??= true;
        return settings;
    }

    internal static string ConfigDirectory()
    {
        var directory = Path.GetDirectoryName(ConfigPath());
        Directory.CreateDirectory(directory!);
        return directory!;
    }

    private static string ConfigPath() => DesktopConfig.File("screenshot.json");
}
