using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using Lyo.Common.Core.Identifiers;
using Lyo.Diagram.Models;

namespace Lyo.Diagram.Tests;

public class DiagramDrawIoTests
{
    [Fact]
    public void Export_TwoShapesAndEdge_WritesUncompressedMxFile()
    {
        var xml = DiagramDrawIo.Export(Checkout());
        var mxfile = XDocument.Parse(xml).Root!;
        Assert.Equal("mxfile", mxfile.Name.LocalName);
        Assert.Equal("Checkout", Attr(mxfile, "name"));
        var page = mxfile.Elements().Single(e => e.Name.LocalName == "diagram");
        Assert.Equal("Flow", Attr(page, "name"));
        var model = page.Elements().Single(e => e.Name.LocalName == "mxGraphModel");
        Assert.Equal("850", Attr(model, "pageWidth"));
        Assert.Equal("1100", Attr(model, "pageHeight"));
        var cells = model.Elements().Single(e => e.Name.LocalName == "root").Elements().Where(e => e.Name.LocalName == "mxCell").ToArray();
        Assert.Equal("0", Attr(cells[0], "id"));
        Assert.Equal("1", Attr(cells[1], "id"));
        Assert.Equal("0", Attr(cells[1], "parent"));
        var start = Cell(cells, "start");
        Assert.Equal("Start", Attr(start, "value"));
        Assert.Equal("1", Attr(start, "vertex"));
        Assert.Equal("1", Attr(start, "parent"));
        Assert.Equal("rounded=0;whiteSpace=wrap;html=1;", Attr(start, "style"));
        var startBox = start.Elements().Single(e => e.Name.LocalName == "mxGeometry");
        Assert.Equal("40", Attr(startBox, "x"));
        Assert.Equal("40", Attr(startBox, "y"));
        Assert.Equal("120", Attr(startBox, "width"));
        Assert.Equal("60", Attr(startBox, "height"));
        var end = Cell(cells, "end");
        Assert.Equal("End", Attr(end, "value"));
        Assert.Equal("ellipse;whiteSpace=wrap;html=1;", Attr(end, "style"));
        var endBox = end.Elements().Single(e => e.Name.LocalName == "mxGeometry");
        Assert.Equal("180", Attr(endBox, "y"));
        var edge = Cell(cells, "go");
        Assert.Equal("next", Attr(edge, "value"));
        Assert.Equal("1", Attr(edge, "edge"));
        Assert.Equal("start", Attr(edge, "source"));
        Assert.Equal("end", Attr(edge, "target"));
        Assert.Equal("edgeStyle=orthogonalEdgeStyle;rounded=0;orthogonalLoop=1;jettySize=auto;html=1;endArrow=classic;", Attr(edge, "style"));
        Assert.Equal("1", Attr(edge.Elements().Single(e => e.Name.LocalName == "mxGeometry"), "relative"));
    }

    [Fact]
    public void Import_ExportedMxFile_ReturnsSameCells()
    {
        var document = Grouped();
        var imported = DiagramDrawIo.Import(DiagramDrawIo.Export(document));
        Assert.NotEqual(Guid.Empty, imported.Id);
        Assert.NotEqual(document.Id, imported.Id);
        Assert.Equal(document.Name, imported.Name);
        Assert.Equal("flow", imported.Pages[0].Id);
        Assert.Equal("Flow", imported.Pages[0].Name);
        Assert.Equal(900, imported.Pages[0].PageWidth);
        Assert.Equal(1200, imported.Pages[0].PageHeight);
        var box = imported.Pages[0].Cells.Single(c => c.Id == "box");
        Assert.Null(box.ParentId);
        Assert.Equal("Box", box.Value);
        Assert.Equal("rounded=0;whiteSpace=wrap;html=1;", box.Style);
        Assert.True(box.IsVertex);
        Assert.False(box.IsEdge);
        Assert.Equal(10, box.Geometry!.X);
        Assert.Equal(20, box.Geometry.Y);
        Assert.Equal(100, box.Geometry.Width);
        Assert.Equal(40, box.Geometry.Height);
        var inner = imported.Pages[0].Cells.Single(c => c.Id == "inner");
        Assert.Equal("box", inner.ParentId);
        Assert.Equal("ellipse;whiteSpace=wrap;html=1;", inner.Style);
        var edge = imported.Pages[0].Cells.Single(c => c.Id == "go");
        Assert.True(edge.IsEdge);
        Assert.Equal("box", edge.SourceId);
        Assert.Equal("inner", edge.TargetId);
        Assert.Equal("next", edge.Value);
        Assert.True(edge.Geometry!.Relative);
        Assert.Equal(50, edge.Geometry.Points[0].X);
        Assert.Equal(80, edge.Geometry.Points[0].Y);
        Assert.Equal(12, edge.Geometry.SourcePoint!.X);
        Assert.Equal(24, edge.Geometry.TargetPoint!.Y);
    }

    [Fact]
    public void Import_CompressedDiagram_InflatesCells()
    {
        var exported = XDocument.Parse(DiagramDrawIo.Export(Checkout()));
        var model = exported.Root!.Elements().Single(e => e.Name.LocalName == "diagram").Elements().Single(e => e.Name.LocalName == "mxGraphModel");
        var compressed = new XElement(
            "mxfile", new XAttribute("name", "Checkout"),
            new XElement("diagram", new XAttribute("id", "flow"), new XAttribute("name", "Flow"), Compress(model.ToString(SaveOptions.DisableFormatting))));

        var imported = DiagramDrawIo.Import(compressed.ToString(SaveOptions.DisableFormatting));
        Assert.Equal("Checkout", imported.Name);
        Assert.Equal("Start", imported.Pages[0].Cells.Single(c => c.Id == "start").Value);
        Assert.Equal("End", imported.Pages[0].Cells.Single(c => c.Id == "end").Value);
        var edge = imported.Pages[0].Cells.Single(c => c.Id == "go");
        Assert.Equal("start", edge.SourceId);
        Assert.Equal("end", edge.TargetId);
        Assert.Equal("next", edge.Value);
    }

    private static DiagramDocument Checkout()
        => new DiagramBuilder("Checkout").Page(
                "flow", "Flow", page => {
                    page.Shape("start", "Start", 40, 40, 120, 60, DiagramShapeKind.Rectangle);
                    page.Shape("end", "End", 40, 180, 120, 60, DiagramShapeKind.Ellipse);
                    page.Connect("go", "start", "end", "next");
                })
            .Build();

    private static DiagramDocument Grouped()
        => new() {
            Id = LyoGuid.CreateV7(),
            Name = "Checkout",
            Pages = [
                new() {
                    Id = "flow",
                    Name = "Flow",
                    PageWidth = 900,
                    PageHeight = 1200,
                    Cells = [
                        new() {
                            Id = "box",
                            Value = "Box",
                            Style = "rounded=0;whiteSpace=wrap;html=1;",
                            IsVertex = true,
                            Geometry = new() {
                                X = 10,
                                Y = 20,
                                Width = 100,
                                Height = 40
                            }
                        },
                        new() {
                            Id = "inner",
                            ParentId = "box",
                            Value = "Inner",
                            Style = "ellipse;whiteSpace=wrap;html=1;",
                            IsVertex = true,
                            Geometry = new() {
                                X = 5,
                                Y = 5,
                                Width = 30,
                                Height = 20
                            }
                        },
                        new() {
                            Id = "go",
                            Value = "next",
                            Style = "edgeStyle=orthogonalEdgeStyle;rounded=0;orthogonalLoop=1;jettySize=auto;html=1;endArrow=classic;",
                            IsEdge = true,
                            SourceId = "box",
                            TargetId = "inner",
                            Geometry = new() {
                                Relative = true,
                                Points = [new() { X = 50, Y = 80 }],
                                SourcePoint = new() { X = 12, Y = 6 },
                                TargetPoint = new() { X = 3, Y = 24 }
                            }
                        }
                    ]
                }
            ]
        };

    private static XElement Cell(IEnumerable<XElement> cells, string id) => cells.Single(cell => Attr(cell, "id") == id);

    private static string? Attr(XElement element, string name) => element.Attribute(name)?.Value;

    /// <summary>diagrams.net <c>Graph.compress</c>: <c>encodeURIComponent</c>, raw DEFLATE, standard base64.</summary>
    private static string Compress(string xml)
    {
        var encoded = EncodeUriComponent(xml);
        var payload = Encoding.UTF8.GetBytes(encoded);
        using var output = new MemoryStream();
        using (var deflate = new DeflateStream(output, CompressionLevel.Optimal, true))
            deflate.Write(payload, 0, payload.Length);

        return Convert.ToBase64String(output.ToArray());
    }

    private static string EncodeUriComponent(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        var sb = new StringBuilder(bytes.Length);
        foreach (var b in bytes) {
            if (IsUnreserved(b))
                sb.Append((char)b);
            else
                sb.Append('%').Append(b.ToString("X2", CultureInfo.InvariantCulture));
        }

        return sb.ToString();
    }

    private static bool IsUnreserved(byte b)
        => b is >= (byte)'A' and <= (byte)'Z' or >= (byte)'a' and <= (byte)'z' or >= (byte)'0' and <= (byte)'9' or (byte)'-' or (byte)'_' or (byte)'.' or (byte)'!' or (byte)'~'
            or (byte)'*' or (byte)'\'' or (byte)'(' or (byte)')';
}