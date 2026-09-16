using System.CommandLine;
using Avalonia;
using Lyo.Desktop.Components;
using Lyo.Desktop;

namespace Lyo.Web.Automation.Studio;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        GlobalHotkeys.InitializeThreading();
        StudioLaunch.Current = StudioLaunch.Parse(args);
        BuildAvaloniaApp().StartWithClassicDesktopLifetime([]);
    }

    public static AppBuilder BuildAvaloniaApp() => AvaloniaDesktop.Configure<App>();
}

/// <summary>Command-line values the window reads when the browser starts.</summary>
internal sealed class StudioLaunch
{
    public static StudioLaunch Current { get; set; } = new();

    public string? StartUrl { get; init; }

    public string? PlanName { get; init; }

    public string? PlanPath { get; init; }

    public string OutputDirectory { get; init; } = Path.Combine(Environment.CurrentDirectory, "plans");

    public static StudioLaunch Parse(string[] args)
    {
        var url = new Option<string?>("--url") { Description = "Page to open when the browser starts" };
        var output = new Option<string?>("--out") { Description = "Directory Save writes into. Defaults to ./plans." };
        var name = new Option<string?>("--name") { Description = "Plan name stored in the JSON" };
        var plan = new Option<string?>("--plan") { Description = "Plan JSON loaded for Play" };
        var root = new RootCommand("Automation studio");
        root.Options.Add(url);
        root.Options.Add(output);
        root.Options.Add(name);
        root.Options.Add(plan);
        var parsed = root.Parse(args);
        return new() {
            StartUrl = parsed.GetValue(url),
            PlanName = parsed.GetValue(name),
            PlanPath = parsed.GetValue(plan),
            OutputDirectory = ResolveOutputDirectory(parsed.GetValue(output))
        };
    }

    private static string ResolveOutputDirectory(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Path.Combine(Environment.CurrentDirectory, "plans");

        var full = Path.GetFullPath(value);
        if (full.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            return Path.GetDirectoryName(full) ?? Environment.CurrentDirectory;

        return full;
    }
}
