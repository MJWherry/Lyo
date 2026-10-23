using Avalonia;
using Lyo.Desktop;
using Lyo.Desktop.Components;

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