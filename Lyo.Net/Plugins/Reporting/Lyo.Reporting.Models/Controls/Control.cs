using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Lyo.Reporting.Models.Controls;

/// <summary>A body control in a <see cref="Models.Section" /> or <see cref="Grid" />. Discriminator property is <c>kind</c>.</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(Card), "card")]
[JsonDerivedType(typeof(Block), "block")]
[JsonDerivedType(typeof(Table), "table")]
[JsonDerivedType(typeof(Grid), "grid")]
[DebuggerDisplay("{ToString(),nq}")]
public abstract class Control
{
    /// <summary>Paint order among mixed section body items (controls and subsections). Zero together with sibling zeros means list order then subsections.</summary>
    public int Order { get; set; }

    /// <summary>How many grid columns this control spans when it is a <see cref="Grid" /> child.</summary>
    public int ColumnSpan { get; set; } = 1;

    /// <summary>How many grid rows this control spans when it is a <see cref="Grid" /> child.</summary>
    public int RowSpan { get; set; } = 1;

    /// <summary>
    /// When true, print/PDF keeps this control on one page. Null uses the type default (on for cards, blocks, and grids; off for tables).
    /// </summary>
    public bool? KeepTogether { get; set; }

    /// <summary>Custom CSS styles for this control.</summary>
    public Dictionary<string, string> Styles { get; set; } = [];
}

/// <summary>Where a table or chart reads its rows.</summary>
public enum DataSourceKind
{
    /// <summary>Use the authored rows or series.</summary>
    Static,

    /// <summary>Materialize rows from a report parameter whose value is a JSON array of objects.</summary>
    FromParameter,

    /// <summary>Run a root <c>QueryReq</c> stored in <c>Options</c> JSON.</summary>
    Query,

    /// <summary>Run a stored procedure stored in <c>Options</c> JSON.</summary>
    Sproc
}
