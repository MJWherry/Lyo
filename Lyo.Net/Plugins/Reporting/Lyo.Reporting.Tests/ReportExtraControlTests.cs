using Lyo.Reporting.Models.Builders;
using Lyo.Reporting.Models.Composition;
using Lyo.Reporting.Models.Controls;

namespace Lyo.Reporting.Tests;

public sealed class ReportExtraControlTests
{
    [Fact]
    public void Bind_TableOfContents_AutoFillsFromSectionTitles()
    {
        var report = ReportBuilder<object>.New()
            .AddSection(s => s.SetTitle("Intro").AddTableOfContents())
            .AddSection(s => s.SetTitle("Body").AddHeading("Detail", 3))
            .Build();
        var bound = ReportCompositionProcessor.Bind(report, new Dictionary<string, string?>());
        var toc = SectionBody.WalkControls(bound.Sections[0].Controls).OfType<Block>().Single(b => b.ContentType == ContentType.TableOfContents);
        Assert.Contains("Intro", toc.ListItems!);
        Assert.Contains("Body", toc.ListItems!);
        Assert.Contains("Detail", toc.ListItems!);
    }

    [Fact]
    public void Bind_TableOfContents_KeepsExplicitItems()
    {
        var report = ReportBuilder<object>.New().AddSection(s => s.SetTitle("A").AddTableOfContents("Contents", ["Only me"])).Build();
        var bound = ReportCompositionProcessor.Bind(report, new Dictionary<string, string?>());
        Assert.Equal(["Only me"], bound.Sections[0].Controls.OfType<Block>().First().ListItems);
    }

    [Fact]
    public void Serialize_ExtraTypes_RoundTrips()
    {
        var report = ReportBuilder<object>.New()
            .AddSection(
                s => s.AddBadge("Paid", "success")
                    .AddSignature("Authorized by", "Ada", "Date")
                    .AddTimeline(["Start|Kickoff", "End|Done"])
                    .AddAddress("Bill to", ["Acme", "1 Main"])
                    .AddTotals(["Subtotal|$10", "Total|$10"])
                    .AddCheckbox(["Agree"], "Please confirm:")
                    .AddNotes("Dock hours 8–5", "Notes"))
            .Build();
        var back = ReportJson.Deserialize<object>(ReportJson.Serialize(report));
        var blocks = back.Sections[0].Controls.OfType<Block>().ToList();
        Assert.Equal(ContentType.Badge, blocks[0].ContentType);
        Assert.Equal(ContentType.Signature, blocks[1].ContentType);
        Assert.Equal(ContentType.Timeline, blocks[2].ContentType);
        Assert.Equal(ContentType.Address, blocks[3].ContentType);
        Assert.Equal(ContentType.Totals, blocks[4].ContentType);
        Assert.Equal(ContentType.Checkbox, blocks[5].ContentType);
        Assert.Equal(ContentType.Notes, blocks[6].ContentType);
        Assert.Equal("Bill to", blocks[3].Caption);
        Assert.Equal(["Subtotal|$10", "Total|$10"], blocks[4].ListItems);
        Assert.Equal("Please confirm:", blocks[5].Content);
        Assert.Equal("Dock hours 8–5", blocks[6].Content);
    }
}
