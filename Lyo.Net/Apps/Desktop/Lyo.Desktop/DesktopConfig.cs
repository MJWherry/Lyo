using Lyo.Exceptions;

namespace Lyo.Desktop;

/// <summary>Config files under <c>$XDG_CONFIG_HOME/lyo</c> or <c>%AppData%\Lyo</c>.</summary>
public static class DesktopConfig
{
    /// <summary><c>{name}</c> inside the Lyo config directory. <paramref name="name" /> is the file name, such as <c>screenshot.json</c>.</summary>
    public static string File(string name)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(name);
        return File(
            name,
            OperatingSystem.IsWindows(),
            Environment.GetEnvironmentVariable("XDG_CONFIG_HOME"),
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
    }

    internal static string File(string name, bool windows, string? xdg, string appData, string profile)
    {
        if (windows)
            return Path.Combine(appData, "Lyo", name);
        var root = string.IsNullOrWhiteSpace(xdg) ? Path.Combine(profile, ".config") : xdg;
        return Path.Combine(root, "lyo", name);
    }
}
