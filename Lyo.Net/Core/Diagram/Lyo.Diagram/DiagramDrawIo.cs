using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using Lyo.Common.Core.Identifiers;
using Lyo.Diagram.Models;
using Lyo.Exceptions;

namespace Lyo.Diagram;

/// <summary>Reads and writes a <see cref="DiagramDocument" /> as a draw.io <c>.drawio</c> mxfile.</summary>
/// <remarks>
/// <see cref="Export" /> writes an uncompressed mxfile that diagrams.net opens directly. <see cref="Import" /> accepts that file and the compressed form diagrams.net saves
/// by default: base64 of raw DEFLATE of <c>encodeURIComponent</c> applied to the <c>mxGraphModel</c> XML. Import assigns a new document id. The document name comes from the
/// <c>mxfile</c> <c>name</c> attribute, otherwise the first page name. Root cells <c>0</c> and <c>1</c> are not stored.
/// </remarks>
public static class DiagramDrawIo
{
    /// <summary>Writes <paramref name="diagram" /> as an uncompressed mxfile.</summary>
    public static string Export(DiagramDocument diagram)
    {
        ArgumentHelpers.ThrowIfNull(diagram);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(diagram.Name);
        ArgumentHelpers.ThrowIfNull(diagram.Pages);
        var file = new XElement("mxfile", new XAttribute("host", "Lyo"), new XAttribute("name", diagram.Name));
        foreach (var page in diagram.Pages)
            file.Add(WritePage(page));

        return file.ToString(SaveOptions.DisableFormatting);
    }

    /// <summary>Reads an uncompressed or compressed mxfile into a new <see cref="DiagramDocument" />.</summary>
    public static DiagramDocument Import(string xml)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(xml);
        var file = XDocument.Parse(xml).Root;
        ArgumentHelpers.ThrowIfNull(file);
        ArgumentHelpers.ThrowIf(file.Name.LocalName != "mxfile", "Expected an mxfile root.", nameof(xml));
        var pages = file.Elements().Where(element => element.Name.LocalName == "diagram").Select(ReadPage).ToArray();
        var rawName = file.Attribute("name")?.Value;
        var name = string.IsNullOrWhiteSpace(rawName) ? pages.FirstOrDefault()?.Name ?? "Diagram" : rawName!;
        return new() { Id = LyoGuid.CreateV7(), Name = name, Pages = pages };
    }

    private static XElement WritePage(DiagramPage page)
    {
        ArgumentHelpers.ThrowIfNull(page);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(page.Id);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(page.Name);
        ArgumentHelpers.ThrowIfNull(page.Cells);
        var model = new XElement(
            "mxGraphModel", new XAttribute("grid", "1"), new XAttribute("gridSize", "10"), new XAttribute("guides", "1"), new XAttribute("tooltips", "1"),
            new XAttribute("connect", "1"), new XAttribute("arrows", "1"), new XAttribute("fold", "1"), new XAttribute("page", "1"), new XAttribute("pageScale", "1"),
            new XAttribute("pageWidth", Format(page.PageWidth)), new XAttribute("pageHeight", Format(page.PageHeight)), new XAttribute("math", "0"), new XAttribute("shadow", "0"));

        var root = new XElement("root", new XElement("mxCell", new XAttribute("id", "0")), new XElement("mxCell", new XAttribute("id", "1"), new XAttribute("parent", "0")));
        foreach (var cell in page.Cells)
            root.Add(WriteCell(cell));

        model.Add(root);
        return new("diagram", new XAttribute("id", page.Id), new XAttribute("name", page.Name), model);
    }

    private static XElement WriteCell(DiagramCell cell)
    {
        ArgumentHelpers.ThrowIfNull(cell);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(cell.Id);
        ArgumentHelpers.ThrowIf(cell.Id is "0" or "1", "Cell ids 0 and 1 are reserved for the draw.io root.", nameof(cell));
        var element = new XElement("mxCell", new XAttribute("id", cell.Id));
        if (cell.Value != null)
            element.SetAttributeValue("value", cell.Value);

        if (!string.IsNullOrEmpty(cell.Style))
            element.SetAttributeValue("style", cell.Style);

        if (cell.IsVertex)
            element.SetAttributeValue("vertex", "1");

        if (cell.IsEdge)
            element.SetAttributeValue("edge", "1");

        element.SetAttributeValue("parent", string.IsNullOrEmpty(cell.ParentId) ? "1" : cell.ParentId);
        if (cell.SourceId != null)
            element.SetAttributeValue("source", cell.SourceId);

        if (cell.TargetId != null)
            element.SetAttributeValue("target", cell.TargetId);

        if (cell.Geometry != null)
            element.Add(WriteGeometry(cell.Geometry));

        return element;
    }

    private static XElement WriteGeometry(DiagramGeometry geometry)
    {
        var element = new XElement("mxGeometry", new XAttribute("as", "geometry"));
        WriteCoord(element, "x", geometry.X);
        WriteCoord(element, "y", geometry.Y);
        WriteCoord(element, "width", geometry.Width);
        WriteCoord(element, "height", geometry.Height);
        if (geometry.Relative)
            element.SetAttributeValue("relative", "1");

        if (geometry.SourcePoint != null)
            element.Add(WritePoint(geometry.SourcePoint, "sourcePoint"));

        if (geometry.TargetPoint != null)
            element.Add(WritePoint(geometry.TargetPoint, "targetPoint"));

        if (geometry.Points is { Count: > 0 }) {
            var array = new XElement("Array", new XAttribute("as", "points"));
            foreach (var point in geometry.Points)
                array.Add(WritePoint(point, null));

            element.Add(array);
        }

        return element;
    }

    private static void WriteCoord(XElement element, string name, double? value)
    {
        if (value != null)
            element.SetAttributeValue(name, Format(value.Value));
    }

    private static XElement WritePoint(DiagramPoint point, string? role)
    {
        var element = new XElement("mxPoint", new XAttribute("x", Format(point.X)), new XAttribute("y", Format(point.Y)));
        if (role != null)
            element.SetAttributeValue("as", role);

        return element;
    }

    private static DiagramPage ReadPage(XElement diagram)
    {
        var id = Required(diagram.Attribute("id")?.Value);
        var rawName = diagram.Attribute("name")?.Value;
        var name = string.IsNullOrWhiteSpace(rawName) ? "Page" : rawName!;
        var model = diagram.Elements().FirstOrDefault(element => element.Name.LocalName == "mxGraphModel") ?? ParseCompressed(diagram.Value);
        var root = model.Elements().FirstOrDefault(element => element.Name.LocalName == "root");
        var cells = root == null ? [] : root.Elements().Where(element => element.Name.LocalName == "mxCell").Select(ReadCell).OfType<DiagramCell>().ToArray();
        return new() {
            Id = id,
            Name = name,
            PageWidth = ReadDouble(model.Attribute("pageWidth")) ?? 850,
            PageHeight = ReadDouble(model.Attribute("pageHeight")) ?? 1100,
            Cells = cells
        };
    }

    private static DiagramCell? ReadCell(XElement element)
    {
        var rawId = element.Attribute("id")?.Value;
        if (string.IsNullOrEmpty(rawId) || rawId is "0" or "1")
            return null;

        var id = rawId!;
        var parent = element.Attribute("parent")?.Value;
        return new() {
            Id = id,
            ParentId = string.IsNullOrEmpty(parent) || parent is "0" or "1" ? null : parent,
            Value = element.Attribute("value")?.Value,
            Style = element.Attribute("style")?.Value,
            IsVertex = element.Attribute("vertex")?.Value == "1",
            IsEdge = element.Attribute("edge")?.Value == "1",
            SourceId = element.Attribute("source")?.Value,
            TargetId = element.Attribute("target")?.Value,
            Geometry = ReadGeometry(element.Elements().FirstOrDefault(child => child.Name.LocalName == "mxGeometry"))
        };
    }

    private static DiagramGeometry? ReadGeometry(XElement? element)
    {
        if (element == null)
            return null;

        var points = element.Elements().FirstOrDefault(child => child.Name.LocalName == "Array" && child.Attribute("as")?.Value == "points");
        return new() {
            X = ReadDouble(element.Attribute("x")),
            Y = ReadDouble(element.Attribute("y")),
            Width = ReadDouble(element.Attribute("width")),
            Height = ReadDouble(element.Attribute("height")),
            Relative = element.Attribute("relative")?.Value == "1",
            Points = points == null ? [] : points.Elements().Where(child => child.Name.LocalName == "mxPoint").Select(ReadPoint).ToArray(),
            SourcePoint = ReadNamedPoint(element, "sourcePoint"),
            TargetPoint = ReadNamedPoint(element, "targetPoint")
        };
    }

    private static DiagramPoint? ReadNamedPoint(XElement geometry, string role)
    {
        var point = geometry.Elements().FirstOrDefault(child => child.Name.LocalName == "mxPoint" && child.Attribute("as")?.Value == role);
        return point == null ? null : ReadPoint(point);
    }

    private static DiagramPoint ReadPoint(XElement point) => new() { X = ReadDouble(point.Attribute("x")) ?? 0, Y = ReadDouble(point.Attribute("y")) ?? 0 };

    private static XElement ParseCompressed(string payload)
    {
        var text = payload.Trim();
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(text);
        if (!text.StartsWith("<", StringComparison.Ordinal))
            text = Inflate(text);

        var model = XElement.Parse(text);
        ArgumentHelpers.ThrowIf(model.Name.LocalName != "mxGraphModel", "Diagram did not contain an mxGraphModel.", nameof(payload));
        return model;
    }

    private static string Inflate(string base64)
    {
        var compressed = Convert.FromBase64String(StripWhitespace(base64));
        using var input = new MemoryStream(compressed);
        using var deflate = new DeflateStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        deflate.CopyTo(output);
        return DecodeUriComponent(Encoding.UTF8.GetString(output.ToArray()));
    }

    private static string DecodeUriComponent(string value)
    {
        var bytes = new List<byte>(value.Length);
        for (var i = 0; i < value.Length; i++) {
            if (value[i] == '%' && i + 2 < value.Length && IsHex(value[i + 1]) && IsHex(value[i + 2])) {
                bytes.Add(Convert.ToByte(value.Substring(i + 1, 2), 16));
                i += 2;
            }
            else
                bytes.Add((byte)value[i]);
        }

        return Encoding.UTF8.GetString([.. bytes]);
    }

    private static string StripWhitespace(string value)
    {
        var sb = new StringBuilder(value.Length);
        foreach (var c in value) {
            if (!char.IsWhiteSpace(c))
                sb.Append(c);
        }

        return sb.ToString();
    }

    private static bool IsHex(char c) => c is >= '0' and <= '9' or >= 'a' and <= 'f' or >= 'A' and <= 'F';

    private static string Required(string? value)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(value);
        return value!;
    }

    private static double? ReadDouble(XAttribute? attribute)
        => attribute == null || string.IsNullOrWhiteSpace(attribute.Value) ? null : double.Parse(attribute.Value, CultureInfo.InvariantCulture);

    private static string Format(double value)
    {
        if (!double.IsNaN(value) && !double.IsInfinity(value) && value == Math.Truncate(value) && value is >= long.MinValue and <= long.MaxValue)
            return ((long)value).ToString(CultureInfo.InvariantCulture);

        return value.ToString("0.########", CultureInfo.InvariantCulture);
    }
}