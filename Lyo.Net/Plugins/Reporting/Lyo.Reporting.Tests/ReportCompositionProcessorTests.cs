using Lyo.Reporting.Models.Builders;
using Lyo.Reporting.Models.Composition;
using Lyo.Reporting.Models.Controls;
using Lyo.Reporting.Models.Models;
using Lyo.Reporting.Models.Response;

namespace Lyo.Reporting.Tests;

public sealed class ReportCompositionProcessorTests
{
    [Fact]
    public void Bind_ExampleParams_InterpolatesPlaceholders()
    {
        var report = ReportBuilder<object>.New()
            .SetTitle("{ReportTitle}")
            .AddParameter("ReportTitle", example: "\"Q4\"")
            .AddSection(
                "S",
                s => s.AddText("Hello {Name}")
                    .AddGrid(g => g.SetTemplateColumns("repeat(auto-fit, minmax(220px, 1fr))").AddCard("Period", "{Period}")))
            .Build();

        var bound = ReportCompositionProcessor.Bind(
            report, new Dictionary<string, string?> { ["ReportTitle"] = "Q4", ["Name"] = "Ada", ["Period"] = "2024" });

        Assert.Equal("Q4", bound.Title);
        Assert.Equal("Hello Ada", bound.Sections[0].Controls.OfType<Block>().First().Content);
        var card = bound.Sections[0].Controls.OfType<Grid>().SelectMany(g => g.Controls).OfType<Card>().Single();
        Assert.Equal("2024", card.Value);
        Assert.Equal("{ReportTitle}", report.Title);
    }

    [Fact]
    public void Bind_ClonedReport_SelectionIdentityDoesNotMatch()
    {
        var block = new Block { Content = "Hello {Name}" };
        var section = new Section { Title = "S", Controls = [block] };
        var report = new Report<object> { Title = "{ReportTitle}", Sections = [section] };
        var bound = ReportCompositionProcessor.Bind(report, new Dictionary<string, string?> { ["ReportTitle"] = "Q4", ["Name"] = "Ada" });
        var selection = new ReportDesignSelection { Section = section, Block = block };
        Assert.True(selection.Matches(block));
        Assert.False(selection.Matches(bound.Sections[0].Controls.OfType<Block>().First()));
        Assert.Equal("Hello {Name}", block.Content);
        Assert.Equal("Hello Ada", bound.Sections[0].Controls.OfType<Block>().First().Content);
    }

    [Fact]
    public void Interpolate_SourceText_LeavesUnchanged()
    {
        var parameters = new Dictionary<string, string?> { ["Name"] = "Ada" };
        Assert.Equal("Hello Ada", ReportCompositionProcessor.Interpolate("Hello {Name}", parameters));
        Assert.Equal("Ada", ReportCompositionProcessor.InterpolateValue("{Name}", parameters));
    }

    [Fact]
    public void Deserialize_LegacyCompositionJson_Succeeds()
    {
        const string json = """{"Title":"{ReportTitle}","Sections":[{"Title":"S","ContentBlocks":[{"Content":"Hello {Name}"}]}]}""";
        var report = ReportJson.Deserialize<object>(json);
        Assert.Equal("{ReportTitle}", report.Title);
        Assert.Equal("Hello {Name}", Assert.Single(report.Sections[0].Controls.OfType<Block>()).Content);
    }

    [Fact]
    public void Bind_VisibleWhenFails_HidesSection()
    {
        var report = ReportBuilder<object>.New()
            .AddSection("Always", s => s.AddText("keep"))
            .AddSection("Risks", s => s.SetVisibleWhen("ShowRisks=true").AddCallout("hidden", "warning"))
            .Build();

        var hidden = ReportCompositionProcessor.Bind(report, new Dictionary<string, string?> { ["ShowRisks"] = "false" });
        Assert.Single(hidden.Sections);
        Assert.Equal("Always", hidden.Sections[0].Title);

        var shown = ReportCompositionProcessor.Bind(report, new Dictionary<string, string?> { ["ShowRisks"] = "true" });
        Assert.Equal(2, shown.Sections.Count);
    }

    [Fact]
    public void BindJson_JsonStringParameter_UnwrapsValue()
    {
        var json = ReportJson.Serialize(ReportBuilder<object>.New().SetTitle("{Period}").Build());
        var bound = ReportCompositionProcessor.BindJson(
            json, [new ReportGenerationParameterRes(Guid.Empty, Guid.Empty, "Period", "System.String", "\"Q4 2024\"", null, null)]);

        Assert.Equal("Q4 2024", bound.Title);
    }

    [Fact]
    public void IsVisible_NegationAndInequality_Evaluates()
    {
        var map = new Dictionary<string, string?> { ["Status"] = "Draft" };
        Assert.True(ReportCompositionProcessor.IsVisible(null, map));
        Assert.True(ReportCompositionProcessor.IsVisible("Status", map));
        Assert.False(ReportCompositionProcessor.IsVisible("!Status", map));
        Assert.True(ReportCompositionProcessor.IsVisible("Status=Draft", map));
        Assert.True(ReportCompositionProcessor.IsVisible("Status!=Live", map));
        Assert.False(ReportCompositionProcessor.IsVisible("Missing", map));
    }

    [Fact]
    public void ControlsGallery_ExampleParameters_AreDeclared()
    {
        var gallery = ReportDesignTemplates.ControlsGallery();
        Assert.Contains(gallery.ParameterSpecs, p => p.Key == "Period" && p.Required);
        var blocks = gallery.Sections.SelectMany(s => SectionBody.WalkControls(s.Controls)).OfType<Block>();
        Assert.Contains(blocks, b => b.ContentType == ContentType.Callout);
        Assert.Contains(blocks, b => b.ContentType == ContentType.Progress);
        Assert.Contains(blocks, b => b.ContentType == ContentType.TableOfContents);
        Assert.Contains(blocks, b => b.ContentType == ContentType.PageBreak);
        Assert.Contains(gallery.Sections, s => s.Collapsed);
        Assert.Contains(blocks, b => b.ContentType == ContentType.Chart && b.DataSourceKind == DataSourceKind.FromParameter && b.DataParameterKey == "RegionSeries");
        Assert.Contains(gallery.Sections.SelectMany(s => s.Controls).OfType<Grid>(), g => g.Controls.OfType<Card>().Any());
    }

    [Fact]
    public void Invoice_LineItems_UseFromParameter()
    {
        var invoice = ReportDesignTemplates.Invoice();
        var blocks = invoice.Sections.SelectMany(s => SectionBody.WalkControls(s.Controls)).OfType<Block>();
        Assert.Contains(blocks, b => b.ContentType == ContentType.TableOfContents);
        var table = Assert.Single(SectionBody.CollectTables(invoice.Sections));
        Assert.Equal(DataSourceKind.FromParameter, table.DataSourceKind);
        Assert.Equal("LineItems", table.DataParameterKey);
        Assert.Contains(invoice.ParameterSpecs, p => p.Key == "LineItems" && p.ExampleValue != null);
        Assert.Contains(invoice.Sections.SelectMany(s => s.Controls).OfType<Grid>(), g => g.Controls.OfType<Card>().Any());
    }

    [Fact]
    public void OperationsDashboard_Chart_UsesFromParameter()
    {
        var ops = ReportDesignTemplates.OperationsDashboard();
        Assert.Contains(ops.Sections, s => s.Collapsed);
        Assert.Contains(
            ops.Sections.SelectMany(s => SectionBody.WalkControls(s.Controls)).OfType<Block>(),
            b => b.ContentType == ContentType.Chart && b.DataSourceKind == DataSourceKind.FromParameter);
    }

    [Fact]
    public void Samples_BlankExcluded_RoundTripsToDefinitionReq()
    {
        Assert.Equal(4, ReportDesignTemplates.Samples.Count);
        Assert.DoesNotContain(ReportDesignTemplates.Samples, s => s.Name == "Untitled report");
        var req = ReportDesignTemplates.Samples.Single(s => s.Name == "Invoice").ToDefinitionReq();
        Assert.Equal("Invoice", req.Name);
        Assert.Contains("LineItems", req.CreateParameters.Select(p => p.Key));
        Assert.False(string.IsNullOrWhiteSpace(req.CreateParameters.First(p => p.Key == "LineItems").Value));
    }
}
