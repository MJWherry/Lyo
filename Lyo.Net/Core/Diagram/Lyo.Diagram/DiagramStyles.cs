namespace Lyo.Diagram;

/// <summary>draw.io style strings for the shapes <see cref="DiagramBuilder" /> writes. Pass any other style string to <c>Shape</c> when you need a stencil these presets do not cover.</summary>
public static class DiagramStyles
{
    /// <summary>Rectangle with wrapped HTML text.</summary>
    public const string Rectangle = "rounded=0;whiteSpace=wrap;html=1;";

    /// <summary>Rounded rectangle with wrapped HTML text.</summary>
    public const string Rounded = "rounded=1;whiteSpace=wrap;html=1;";

    /// <summary>Ellipse with wrapped HTML text.</summary>
    public const string Ellipse = "ellipse;whiteSpace=wrap;html=1;";

    /// <summary>Diamond (draw.io rhombus) with wrapped HTML text.</summary>
    public const string Diamond = "rhombus;whiteSpace=wrap;html=1;";

    /// <summary>Orthogonal connector with a classic arrow head.</summary>
    public const string Connector = "edgeStyle=orthogonalEdgeStyle;rounded=0;orthogonalLoop=1;jettySize=auto;html=1;endArrow=classic;";
}

/// <summary>Preset passed to <see cref="DiagramPageBuilder.Shape(string, string, double, double, double, double, DiagramShapeKind)" />.</summary>
public enum DiagramShapeKind
{
    /// <summary><see cref="DiagramStyles.Rectangle" />.</summary>
    Rectangle,

    /// <summary><see cref="DiagramStyles.Rounded" />.</summary>
    Rounded,

    /// <summary><see cref="DiagramStyles.Ellipse" />.</summary>
    Ellipse,

    /// <summary><see cref="DiagramStyles.Diamond" />.</summary>
    Diamond
}