using Lyo.Common.Core.Identifiers;
using Lyo.Diagram.Models;
using Lyo.Exceptions;

namespace Lyo.Diagram;

/// <summary>Builds a <see cref="DiagramDocument" />. Call <see cref="Page" /> for each tab, then <see cref="Build" />.</summary>
/// <remarks>The document id is a version-7 GUID minted at <see cref="Build" />. Cell ids <c>0</c> and <c>1</c> are reserved by draw.io and rejected.</remarks>
public sealed class DiagramBuilder
{
    private readonly string _name;
    private readonly List<DiagramPage> _pages = [];

    /// <summary>Starts a diagram with <paramref name="name" />. That name is stored and written on the draw.io <c>mxfile</c>.</summary>
    public DiagramBuilder(string name)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(name);
        _name = name;
    }

    /// <summary>Adds a page. <paramref name="configure" /> adds shapes and connectors. Page size defaults to 850 by 1100.</summary>
    public DiagramBuilder Page(string id, string name, Action<DiagramPageBuilder> configure)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(id);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(name);
        ArgumentHelpers.ThrowIfNull(configure);
        var page = new DiagramPageBuilder(id, name);
        configure(page);
        _pages.Add(page.Build());
        return this;
    }

    /// <summary>Returns the document. Each call mints a new id.</summary>
    public DiagramDocument Build() => new() { Id = LyoGuid.CreateV7(), Name = _name, Pages = [.. _pages] };
}

/// <summary>Adds cells to one page. Obtained from <see cref="DiagramBuilder.Page" />.</summary>
public sealed class DiagramPageBuilder
{
    private readonly List<DiagramCell> _cells = [];
    private readonly string _id;
    private readonly string _name;
    private double _pageHeight = 1100;
    private double _pageWidth = 850;

    internal DiagramPageBuilder(string id, string name)
    {
        _id = id;
        _name = name;
    }

    /// <summary>Sets the page size in draw.io units. The default is 850 by 1100.</summary>
    public DiagramPageBuilder Size(double width, double height)
    {
        ArgumentHelpers.ThrowIfNegative(width);
        ArgumentHelpers.ThrowIfNegative(height);
        _pageWidth = width;
        _pageHeight = height;
        return this;
    }

    /// <summary>Adds a shape using a <see cref="DiagramShapeKind" /> preset style.</summary>
    public DiagramPageBuilder Shape(string id, string label, double x, double y, double width, double height, DiagramShapeKind kind)
        => Shape(id, label, x, y, width, height, Style(kind));

    /// <summary>Adds a shape with a raw draw.io style string.</summary>
    public DiagramPageBuilder Shape(string id, string label, double x, double y, double width, double height, string style)
    {
        RejectReservedId(id);
        ArgumentHelpers.ThrowIfNull(label);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(style);
        _cells.Add(
            new() {
                Id = id,
                Value = label,
                Style = style,
                IsVertex = true,
                Geometry = new() {
                    X = x,
                    Y = y,
                    Width = width,
                    Height = height
                }
            });

        return this;
    }

    /// <summary>Adds an orthogonal connector from <paramref name="sourceId" /> to <paramref name="targetId" />. <paramref name="label" /> may be null.</summary>
    public DiagramPageBuilder Connect(string id, string sourceId, string targetId, string? label = null) => Connect(id, sourceId, targetId, [], null, null, label);

    /// <summary>
    /// Adds a connector that keeps its anchors and bend points. <paramref name="points" /> are the corners between <paramref name="sourcePoint" /> and
    /// <paramref name="targetPoint" />. A null anchor means the edge is attached only by cell id.
    /// </summary>
    public DiagramPageBuilder Connect(
        string id,
        string sourceId,
        string targetId,
        IReadOnlyList<DiagramPoint> points,
        DiagramPoint? sourcePoint,
        DiagramPoint? targetPoint,
        string? label = null)
    {
        RejectReservedId(id);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(sourceId);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(targetId);
        ArgumentHelpers.ThrowIfNull(points);
        _cells.Add(
            new() {
                Id = id,
                Value = label,
                Style = DiagramStyles.Connector,
                IsEdge = true,
                SourceId = sourceId,
                TargetId = targetId,
                Geometry = new() {
                    Relative = true,
                    Points = points.ToArray(),
                    SourcePoint = sourcePoint,
                    TargetPoint = targetPoint
                }
            });

        return this;
    }

    internal DiagramPage Build()
        => new() {
            Id = _id,
            Name = _name,
            PageWidth = _pageWidth,
            PageHeight = _pageHeight,
            Cells = _cells.ToArray()
        };

    private static string Style(DiagramShapeKind kind)
        => kind switch {
            DiagramShapeKind.Rectangle => DiagramStyles.Rectangle,
            DiagramShapeKind.Rounded => DiagramStyles.Rounded,
            DiagramShapeKind.Ellipse => DiagramStyles.Ellipse,
            DiagramShapeKind.Diamond => DiagramStyles.Diamond,
            var _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };

    private static void RejectReservedId(string id)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(id);
        ArgumentHelpers.ThrowIf(id is "0" or "1", "Cell ids 0 and 1 are reserved for the draw.io root.", nameof(id));
    }
}