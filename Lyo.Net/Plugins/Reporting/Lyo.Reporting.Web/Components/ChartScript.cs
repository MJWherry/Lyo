namespace Lyo.Reporting.Web.Components;

/// <summary>
/// Chart.js bundle this package vendors, both as a static web asset (<c>_content/Lyo.Reporting.Web/scripts/chart.umd.min.js</c>) and as an embedded resource for inlining.
/// Inlining is what lets offline PDF and standalone HTML exports draw charts: the rendered document is written to a temp file and handed to the PDF converter, where a
/// relative asset URL resolves against nothing.
/// </summary>
public static class ChartScript
{
    private const string ResourceName = "Lyo.Reporting.Web.wwwroot.scripts.chart.umd.min.js";

    private static readonly Lazy<string> Lazy = new(Load, LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>Bundle source, read once per process. About 200 KB, so prefer the static asset URL for interactive hosting and keep this for file exports.</summary>
    public static string Bundle => Lazy.Value;

    private static string Load()
    {
        using var stream = typeof(ChartScript).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{ResourceName}' is missing from {typeof(ChartScript).Assembly.GetName().Name}.");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
