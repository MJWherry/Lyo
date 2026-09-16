using Lyo.Diagram.Models;

namespace Lyo.Diagram.Tests;

public class DiagramBuilderTests
{
    [Fact]
    public void Builder_Presets_WritesDrawIoStyles()
    {
        var diagram = new DiagramBuilder("Styles")
            .Page("page", "Page", page =>
            {
                page.Shape("rect", "R", 0, 0, 80, 40, DiagramShapeKind.Rectangle);
                page.Shape("round", "U", 0, 0, 80, 40, DiagramShapeKind.Rounded);
                page.Shape("ellipse", "E", 0, 0, 80, 40, DiagramShapeKind.Ellipse);
                page.Shape("diamond", "D", 0, 0, 80, 40, DiagramShapeKind.Diamond);
                page.Connect("link", "rect", "ellipse", "go");
            })
            .Build();

        var cells = diagram.Pages[0].Cells;
        Assert.Equal("rounded=0;whiteSpace=wrap;html=1;", cells.Single(c => c.Id == "rect").Style);
        Assert.Equal("rounded=1;whiteSpace=wrap;html=1;", cells.Single(c => c.Id == "round").Style);
        Assert.Equal("ellipse;whiteSpace=wrap;html=1;", cells.Single(c => c.Id == "ellipse").Style);
        Assert.Equal("rhombus;whiteSpace=wrap;html=1;", cells.Single(c => c.Id == "diamond").Style);
        Assert.Equal("edgeStyle=orthogonalEdgeStyle;rounded=0;orthogonalLoop=1;jettySize=auto;html=1;endArrow=classic;", cells.Single(c => c.Id == "link").Style);
        Assert.True(cells.Single(c => c.Id == "link").IsEdge);
        Assert.Equal("rect", cells.Single(c => c.Id == "link").SourceId);
        Assert.Equal("ellipse", cells.Single(c => c.Id == "link").TargetId);
    }

    [Fact]
    public void Builder_SizeAndWaypoints_AreOnTheDocument()
    {
        var diagram = new DiagramBuilder("Checkout")
            .Page("flow", "Flow", page =>
            {
                page.Size(600, 400);
                page.Shape("start", "Start", 40, 40, 120, 60, DiagramShapeKind.Rectangle);
                page.Shape("end", "End", 240, 40, 120, 60, DiagramShapeKind.Rectangle);
                page.Connect(
                    "go",
                    "start",
                    "end",
                    [new DiagramPoint { X = 180, Y = 70 }, new DiagramPoint { X = 180, Y = 70 }],
                    new DiagramPoint { X = 160, Y = 70 },
                    new DiagramPoint { X = 240, Y = 70 },
                    "next");
            })
            .Build();

        var page = diagram.Pages[0];
        Assert.Equal(600, page.PageWidth);
        Assert.Equal(400, page.PageHeight);
        var edge = page.Cells.Single(c => c.Id == "go");
        Assert.Equal("next", edge.Value);
        Assert.Equal(160, edge.Geometry!.SourcePoint!.X);
        Assert.Equal(70, edge.Geometry.SourcePoint.Y);
        Assert.Equal(240, edge.Geometry.TargetPoint!.X);
        Assert.Equal(180, edge.Geometry.Points[0].X);
        Assert.Equal("start", edge.SourceId);
        Assert.Equal("end", edge.TargetId);
    }
}
