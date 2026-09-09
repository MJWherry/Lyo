using Lyo.Reporting.Models.Composition;
using Lyo.Reporting.Models.Controls;
using Lyo.Reporting.Models.Models;
using Microsoft.AspNetCore.Components;

namespace Lyo.Reporting.Web.Components;

public partial class ReportViewer<T>
{
    [Parameter]
    [EditorRequired]
    public Report<T> Report { get; set; } = null!;

    /// <summary>
    /// Chart.js bundle used to draw <see cref="ContentType.Chart" /> blocks. Defaults to the copy this package ships as a static web asset, so charts render
    /// with no outbound network. Set <see cref="Lyo.Reporting.Models.Constants.Charts.CdnScriptUrl" /> (or any host-served URL) to override. Empty suppresses the script and leaves canvases blank.
    /// </summary>
    [Parameter]
    public string ChartScriptUrl { get; set; } = Lyo.Reporting.Models.Constants.Charts.DefaultScriptUrl;

    /// <summary>
    /// Emit the Chart.js bundle inline instead of as a <c>src</c> reference. Set by the HTML and PDF renderers, whose output is a standalone document where a relative asset URL
    /// resolves against nothing. Adds about 200 KB to the document, so leave it off for interactive hosting.
    /// </summary>
    [Parameter]
    public bool InlineChartScript { get; set; }

    private Layout Layout => Report.Layout ?? new();

    private ReportViewPainter Paint => new(Layout, Report.Styles);

    private string AccentColor => Paint.AccentColor;

    private string InkColor => Paint.InkColor;

    private string BodyColor => Paint.BodyColor;

    private string MutedColor => Paint.MutedColor;

    private bool IsCompact => Paint.IsCompact;

    private string? SafeLogoUrl => Paint.SafeLogoUrl;

    private bool HasChartBlocks => ReportChartBootstrap.ReportHasChart(Report.Sections);

    private string GetRootStyles() => Paint.GetRootStyles();

    private string GetWatermarkStyles() => Paint.GetWatermarkStyles();

    private string? PageRuleCss => Paint.PageRuleCss;

    private RenderFragment RenderSection(Section section, int depth) => Paint.RenderSection(section, depth);

    private RenderFragment RenderChartBootstrap() => ReportChartBootstrap.Render(InlineChartScript, ChartScriptUrl);
}
