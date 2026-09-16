using Avalonia;
using Lyo.Desktop.Components;

namespace Lyo.FileManager.App;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args) => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp() => AvaloniaDesktop.Configure<App>();
}
