namespace Lyo.DataTable.Models;

/// <summary>Common interface for data table cells of any type. Formatting lives on <see cref="DataTable" /> via <see cref="DataTable.GetFormat" />, not on the cell.</summary>
public interface IDataTableCell
{
    /// <summary>The display string for the cell.</summary>
    string DisplayValue { get; }

    /// <summary>Number of columns this cell spans (1 = no spanning).</summary>
    int ColSpan { get; }

    /// <summary>Number of rows this cell spans (1 = no spanning).</summary>
    int RowSpan { get; }
}