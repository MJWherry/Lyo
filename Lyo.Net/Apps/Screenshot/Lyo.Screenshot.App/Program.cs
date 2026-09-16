using Avalonia;
using Lyo.Desktop.Components;
using Lyo.Desktop;

namespace Lyo.Screenshot.App;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        GlobalHotkeys.InitializeThreading();
        ShotLog.Info("Starting.");
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() => AvaloniaDesktop.Configure<App>();
}
