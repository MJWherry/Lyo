using Avalonia;

namespace Lyo.Desktop.Components;

/// <summary>Shared startup for an Avalonia desktop app. Matches the Blazor host using one component library.</summary>
public static class AvaloniaDesktop
{
    /// <summary>Platform detection, the Inter font, and trace logging. The app still adds <c>FluentTheme</c> in its application XAML.</summary>
    public static AppBuilder Configure<TApp>()
        where TApp : Application, new()
        => AppBuilder.Configure<TApp>().UsePlatformDetect().WithInterFont().LogToTrace();
}