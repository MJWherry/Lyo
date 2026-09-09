using Lyo.Reporting.Models.Composition;
using Lyo.Reporting.Models.Controls;
using Lyo.Reporting.Models.Models;
using Lyo.Reporting.Web.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Lyo.Web.WebRenderer.Tests;

public sealed class ReportViewerPaintTests
{
    [Fact]
    public void GetRootStyles_PreviewLayout_HasNoDesignRing()
    {
        var paint = new ReportViewPainter(new Layout { Padding = "12px" });
        var css = paint.GetRootStyles();
        Assert.DoesNotContain("outline", css, StringComparison.Ordinal);
        Assert.DoesNotContain("grab", css, StringComparison.Ordinal);
        Assert.Contains("padding: 12px", css, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RenderToHtmlAsync_ReportViewer_HasNoDesignChrome()
    {
        var renderer = WebRendererTestHost.CreateService();
        var report = new Report<object> {
            Title = "Quarterly",
            Sections = [new() { Title = "Body", Controls = [new Block { Content = "Hello block" }] }]
        };
        var html = await renderer.RenderToHtmlAsync<ReportViewer<object>>(new() { ["Report"] = report }, TestContext.Current.CancellationToken);
        Assert.Contains("Hello block", html, StringComparison.Ordinal);
        Assert.DoesNotContain("cursor: grab", html, StringComparison.Ordinal);
        Assert.DoesNotContain("draggable", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("lyo-design", html, StringComparison.Ordinal);
        Assert.DoesNotContain("data-lyo-design", html, StringComparison.Ordinal);
    }

    [Fact]
    public void P_PreviewParameters_InterpolateWithoutMutating()
    {
        var block = new Block { Content = "Hello {Name}" };
        var paint = new ReportViewPainter(new Layout(), previewParameters: new Dictionary<string, string?> { ["Name"] = "Ada" });
        Assert.Equal("Hello Ada", paint.P(block.Content));
        Assert.Equal("Hello {Name}", block.Content);
        var generate = new ReportViewPainter(new Layout());
        Assert.Equal("Hello {Name}", generate.P(block.Content));
    }

    [Fact]
    public void GetContentBlockStyles_DefaultChartAndComponent_KeepTogether()
    {
        var paint = new ReportViewPainter(new Layout());
        Assert.Contains("break-inside: avoid", paint.GetContentBlockStyles(new() { ContentType = ContentType.Chart, ChartKind = ChartKind.Bar }), StringComparison.Ordinal);
        Assert.Contains("break-inside: avoid", paint.GetContentBlockStyles(new() { ContentType = ContentType.Component, ComponentType = "X" }), StringComparison.Ordinal);
        Assert.DoesNotContain("break-inside", paint.GetContentBlockStyles(new() { ContentType = ContentType.PageBreak }), StringComparison.Ordinal);
    }

    [Fact]
    public void GetTableWrapperStyles_DefaultTable_SplitsUnlessKeepTogether()
    {
        var paint = new ReportViewPainter(new Layout());
        Assert.DoesNotContain("break-inside", paint.GetTableWrapperStyles(new Table()), StringComparison.Ordinal);
        Assert.Contains("break-inside: avoid", paint.GetTableWrapperStyles(new Table { KeepTogether = true }), StringComparison.Ordinal);
    }

    [Fact]
    public void GetGridWrapperStyles_DefaultGrid_IsDisplayGridAndKeepTogether()
    {
        var paint = new ReportViewPainter(new Layout());
        var css = paint.GetGridWrapperStyles(new Grid());
        Assert.Contains("display:grid", css.Replace(" ", string.Empty), StringComparison.Ordinal);
        Assert.Contains("repeat(2, 1fr)", css, StringComparison.Ordinal);
        Assert.Contains("break-inside: avoid", css, StringComparison.Ordinal);
        Assert.DoesNotContain("break-inside", paint.GetGridWrapperStyles(new Grid { KeepTogether = false }), StringComparison.Ordinal);
    }

    [Fact]
    public void GetGridWrapperStyles_BareIntegerTemplate_UsesColumnCount()
    {
        var paint = new ReportViewPainter(new Layout());
        var css = paint.GetGridWrapperStyles(new Grid { ColumnCount = 2, TemplateColumns = "2" });
        Assert.Contains("repeat(2, 1fr)", css, StringComparison.Ordinal);
        Assert.DoesNotContain("grid-template-columns:2", css.Replace(" ", string.Empty), StringComparison.Ordinal);
    }

    [Fact]
    public void GetGridWrapperStyles_TrackList_IsKept()
    {
        var paint = new ReportViewPainter(new Layout());
        var css = paint.GetGridWrapperStyles(new Grid { TemplateColumns = "1fr 2fr" });
        Assert.Contains("grid-template-columns: 1fr 2fr", css, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RenderToHtmlAsync_InteractiveGrid_DoesNotSpanDropSlotsAcrossColumns()
    {
        var renderer = WebRendererTestHost.CreateService();
        var grid = new Grid {
            ColumnCount = 2,
            Controls = [new Card { Label = "A", Value = "1" }, new Card { Label = "B", Value = "2" }]
        };
        var section = new Section { Controls = [grid] };
        var sink = new GridDropSink();
        var html = await renderer.RenderToHtmlAsync<ReportBodyItemView>(
            new() {
                ["Painter"] = new ReportViewPainter(new Layout()),
                ["Item"] = new SectionBodyItem { Kind = SectionBodyKind.Control, Section = section, Control = grid },
                ["OnHit"] = EventCallback.Factory.Create<ReportDesignHit>(sink, sink.OnHit),
                ["OnCardDrop"] = EventCallback.Factory.Create<ReportDesignCardDrop>(sink, sink.OnCardDrop)
            },
            TestContext.Current.CancellationToken);
        Assert.Contains("display:grid", html.Replace(" ", string.Empty), StringComparison.Ordinal);
        Assert.Contains("repeat(2, 1fr)", html, StringComparison.Ordinal);
        Assert.DoesNotContain("grid-column: 1 / -1", html, StringComparison.Ordinal);
        Assert.DoesNotContain("grid-column:1/-1", html.Replace(" ", string.Empty), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RenderToHtmlAsync_KeepTogether_KeepsBlocksAndLetsTablesSplit()
    {
        var renderer = WebRendererTestHost.CreateService();
        var report = new Report<object> {
            Title = "Keep",
            Sections = [
                new() {
                    Title = "Body",
                    Controls = [
                        new Block { ContentType = ContentType.Chart, ChartKind = ChartKind.Bar, Caption = "Sales", ListItems = ["A|1"] },
                        new Block { ContentType = ContentType.Component, ComponentType = "Missing.Type" },
                        new Table { Title = "Split me", Columns = [new() { Header = "H" }], Rows = [new() { Cells = ["v"] }] }
                    ]
                }
            ]
        };
        var html = await renderer.RenderToHtmlAsync<ReportViewer<object>>(new() { ["Report"] = report }, TestContext.Current.CancellationToken);
        Assert.Contains("break-inside: avoid", html, StringComparison.Ordinal);
        Assert.Contains("<table", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("lyo-design", html, StringComparison.Ordinal);
        var tableOn = new Report<object> {
            Sections = [new() { Controls = [new Table { Title = "Together", KeepTogether = true, Columns = [new() { Header = "H" }], Rows = [new() { Cells = ["v"] }] }] }]
        };
        var together = await renderer.RenderToHtmlAsync<ReportViewer<object>>(new() { ["Report"] = tableOn }, TestContext.Current.CancellationToken);
        Assert.Contains("break-inside: avoid", together, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RenderToHtmlAsync_DocBlocks_PaintsContent()
    {
        var renderer = WebRendererTestHost.CreateService();
        var report = new Report<object> {
            Sections = [
                new() {
                    Controls = [
                        new Block { ContentType = ContentType.Address, Caption = "Bill to", ListItems = ["Acme", "1 Main"] },
                        new Block { ContentType = ContentType.Totals, ListItems = ["Subtotal|$10", "Total|$11"] },
                        new Block { ContentType = ContentType.Checkbox, Content = "Please confirm:", ListItems = ["Agree"] },
                        new Block { ContentType = ContentType.Notes, Caption = "Notes", Content = "Dock hours" }
                    ]
                }
            ]
        };
        var html = await renderer.RenderToHtmlAsync<ReportViewer<object>>(new() { ["Report"] = report }, TestContext.Current.CancellationToken);
        Assert.Contains("Bill to", html, StringComparison.Ordinal);
        Assert.Contains("Acme", html, StringComparison.Ordinal);
        Assert.Contains("Subtotal", html, StringComparison.Ordinal);
        Assert.Contains("$11", html, StringComparison.Ordinal);
        Assert.Contains("Please confirm:", html, StringComparison.Ordinal);
        Assert.Contains("Agree", html, StringComparison.Ordinal);
        Assert.Contains("Dock hours", html, StringComparison.Ordinal);
        Assert.DoesNotContain("lyo-design", html, StringComparison.Ordinal);
        Assert.DoesNotContain("<input", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetNestedStyles_UnsafeDeclarations_AreSanitized()
    {
        var section = new Section { Styles = { ["background"] = "url(javascript:alert(1))" } };
        var block = new Block { Styles = { ["color"] = "expression(alert(1))" } };
        var paint = new ReportViewPainter(new Layout());
        Assert.DoesNotContain("javascript", paint.GetSectionStyles(section, 0), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("expression", paint.GetContentBlockStyles(block), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task WritePreview_TocAndFromParameter_DoNotMutate()
    {
        var toc = new Block { ContentType = ContentType.TableOfContents, Caption = "Contents" };
        var table = new Table {
            DataSourceKind = DataSourceKind.FromParameter,
            DataParameterKey = "Lines",
            Columns = [new() { Header = "Name", Field = "Name" }],
            Rows = [new() { Cells = ["stored"] }]
        };
        var heading = new Section { Title = "Intro", Controls = [new Block { ContentType = ContentType.Heading, Content = "Detail", Level = 3 }, toc, table] };
        var paint = new ReportViewPainter(
            new Layout(),
            previewParameters: new Dictionary<string, string?> { ["Lines"] = """[{"Name":"Ada"}]""" },
            previewSections: [heading]);
        var html = await RenderPaintAsync(paint, (b, p) => {
            p.WriteTableOfContents(b, toc);
            p.WriteTable(b, table);
        });
        Assert.Contains("Intro", html, StringComparison.Ordinal);
        Assert.Contains("Detail", html, StringComparison.Ordinal);
        Assert.Contains("Ada", html, StringComparison.Ordinal);
        Assert.True(toc.ListItems is null || toc.ListItems.Count == 0);
        Assert.Equal("stored", table.Rows[0].Cells[0]);
    }

    [Fact]
    public async Task WriteContentBlock_PageBreak_PlaceholderOnlyInPreview()
    {
        var block = new Block { ContentType = ContentType.PageBreak };
        var generate = await RenderPaintAsync(new ReportViewPainter(new Layout()), (b, p) => p.WriteContentBlock(b, block));
        Assert.DoesNotContain("Page break", generate, StringComparison.Ordinal);
        var preview = await RenderPaintAsync(new ReportViewPainter(new Layout(), previewParameters: new Dictionary<string, string?>()), (b, p) => p.WriteContentBlock(b, block));
        Assert.Contains("Page break", preview, StringComparison.Ordinal);
    }

    [Fact]
    public async Task WriteComponent_PreviewType_InterpolatesWithoutMutating()
    {
        var block = new Block { ContentType = ContentType.Component, ComponentType = "{TypeName}" };
        var paint = new ReportViewPainter(new Layout(), previewParameters: new Dictionary<string, string?> { ["TypeName"] = "Missing.Preview.Type" });
        var html = await RenderPaintAsync(paint, (b, p) => p.WriteComponent(b, block));
        Assert.Contains("Missing.Preview.Type", html, StringComparison.Ordinal);
        Assert.Equal("{TypeName}", block.ComponentType);
    }

    [Fact]
    public async Task RenderToHtmlAsync_PageRule_IsIncluded()
    {
        var renderer = WebRendererTestHost.CreateService();
        var report = new Report<object> {
            Title = "Paged",
            Layout = new Layout { PageSize = "Letter", Margin = "1in" },
            Sections = [new() { Title = "Body", Styles = { ["background"] = "url(javascript:alert(1))" }, Controls = [new Block { Content = "Hello" }] }]
        };
        var html = await renderer.RenderToHtmlAsync<ReportViewer<object>>(new() { ["Report"] = report }, TestContext.Current.CancellationToken);
        Assert.Contains("@page", html, StringComparison.Ordinal);
        Assert.DoesNotContain("javascript", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("lyo-design", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RenderToHtmlAsync_CssGrid_EmitsDisplayGrid()
    {
        var renderer = WebRendererTestHost.CreateService();
        var report = new Report<object> {
            Sections = [
                new() {
                    Controls = [
                        new Grid {
                            Title = "KPIs",
                            TemplateColumns = "repeat(auto-fit, minmax(220px, 1fr))",
                            Controls = [new Card { Label = "Revenue", Value = "10" }, new Block { Content = "note" }]
                        }
                    ]
                }
            ]
        };
        var html = await renderer.RenderToHtmlAsync<ReportViewer<object>>(new() { ["Report"] = report }, TestContext.Current.CancellationToken);
        Assert.Contains("display:grid", html.Replace(" ", string.Empty), StringComparison.Ordinal);
        Assert.Contains("Revenue", html, StringComparison.Ordinal);
        Assert.DoesNotContain("<table", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RenderToHtmlAsync_InvoiceAndGallery_PaintTablesAndKpiCards()
    {
        var renderer = WebRendererTestHost.CreateService();
        var invoiceHtml = await renderer.RenderToHtmlAsync<ReportViewer<object>>(new() { ["Report"] = ReportDesignTemplates.Invoice() }, TestContext.Current.CancellationToken);
        Assert.Contains("<table", invoiceHtml, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Invoice", invoiceHtml, StringComparison.Ordinal);
        Assert.Contains("display:grid", invoiceHtml.Replace(" ", string.Empty), StringComparison.Ordinal);

        var galleryHtml = await renderer.RenderToHtmlAsync<ReportViewer<object>>(new() { ["Report"] = ReportDesignTemplates.ControlsGallery() }, TestContext.Current.CancellationToken);
        Assert.Contains("<table", galleryHtml, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Period", galleryHtml, StringComparison.Ordinal);
        Assert.Contains("display:grid", galleryHtml.Replace(" ", string.Empty), StringComparison.Ordinal);
    }

    private static Task<string> RenderPaintAsync(ReportViewPainter paint, Action<RenderTreeBuilder, ReportViewPainter> draw)
        => WebRendererTestHost.CreateService()
            .RenderToHtmlAsync<ReportPaintProbe>(new() { ["Paint"] = paint, ["Draw"] = draw }, TestContext.Current.CancellationToken);

    private sealed class GridDropSink
    {
        public Task OnHit(ReportDesignHit _) => Task.CompletedTask;

        public Task OnCardDrop(ReportDesignCardDrop _) => Task.CompletedTask;
    }
}
