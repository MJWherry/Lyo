using System.Diagnostics;

namespace Lyo.Diagram.Models;

/// <summary>A diagram document: a name and one or more pages of cells. This is the stored model. draw.io files are produced from it.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record DiagramDocument
{
    /// <summary>Store key. The builder mints a version-7 GUID. Importing a draw.io file assigns a new id.</summary>
    public Guid Id { get; init; }

    /// <summary>Display name. Export writes it on the <c>mxfile</c> <c>name</c> attribute.</summary>
    public required string Name { get; init; }

    /// <summary>Pages in file order.</summary>
    public IReadOnlyList<DiagramPage> Pages { get; init; } = [];
}

/// <summary>One page in a <see cref="DiagramDocument" />. Maps to a draw.io <c>diagram</c> element.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record DiagramPage
{
    /// <summary>Page id. Export writes it on the <c>diagram</c> element. draw.io reserves cell ids <c>0</c> and <c>1</c> for the root, so do not reuse those here.</summary>
    public required string Id { get; init; }

    /// <summary>Page name shown as the draw.io tab title.</summary>
    public required string Name { get; init; }

    /// <summary>Page width in draw.io units. Defaults to the diagrams.net page width of 850.</summary>
    public double PageWidth { get; init; } = 850;

    /// <summary>Page height in draw.io units. Defaults to the diagrams.net page height of 1100.</summary>
    public double PageHeight { get; init; } = 1100;

    /// <summary>Shapes and connectors on this page, excluding the synthetic draw.io root cells.</summary>
    public IReadOnlyList<DiagramCell> Cells { get; init; } = [];
}

/// <summary>A vertex or edge. <see cref="ParentId" /> null means the default layer (draw.io cell <c>1</c>).</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record DiagramCell
{
    /// <summary>Cell id, unique on the page. Ids <c>0</c> and <c>1</c> are reserved by draw.io.</summary>
    public required string Id { get; init; }

    /// <summary>Parent cell id. Null places the cell on the default layer.</summary>
    public string? ParentId { get; init; }

    /// <summary>Label. draw.io stores this as the cell <c>value</c>, including HTML when the style uses <c>html=1</c>.</summary>
    public string? Value { get; init; }

    /// <summary>draw.io style string, for example <c>rounded=0;whiteSpace=wrap;html=1;</c>.</summary>
    public string? Style { get; init; }

    /// <summary>True when this cell is a shape.</summary>
    public bool IsVertex { get; init; }

    /// <summary>True when this cell is a connector.</summary>
    public bool IsEdge { get; init; }

    /// <summary>Source cell id for an edge.</summary>
    public string? SourceId { get; init; }

    /// <summary>Target cell id for an edge.</summary>
    public string? TargetId { get; init; }

    /// <summary>Position and size. Null when the cell has no geometry.</summary>
    public DiagramGeometry? Geometry { get; init; }
}

/// <summary>draw.io <c>mxGeometry</c>: bounds, edge routing, and waypoints.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record DiagramGeometry
{
    /// <summary>Left edge, in page coordinates. Null when the cell does not set <c>x</c> (typical for a relative edge).</summary>
    public double? X { get; init; }

    /// <summary>Top edge, in page coordinates.</summary>
    public double? Y { get; init; }

    /// <summary>Width. Null on a relative edge.</summary>
    public double? Width { get; init; }

    /// <summary>Height. Null on a relative edge.</summary>
    public double? Height { get; init; }

    /// <summary>True for connectors whose geometry is relative to the endpoints (<c>relative="1"</c>).</summary>
    public bool Relative { get; init; }

    /// <summary>Intermediate points on an edge.</summary>
    public IReadOnlyList<DiagramPoint> Points { get; init; } = [];

    /// <summary>Floating source point, used when an edge has no attached source cell.</summary>
    public DiagramPoint? SourcePoint { get; init; }

    /// <summary>Floating target point, used when an edge has no attached target cell.</summary>
    public DiagramPoint? TargetPoint { get; init; }
}

/// <summary>A point in page coordinates.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record DiagramPoint
{
    /// <summary>X coordinate.</summary>
    public double X { get; init; }

    /// <summary>Y coordinate.</summary>
    public double Y { get; init; }
}