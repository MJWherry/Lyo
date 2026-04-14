namespace Lyo.Pdf.Models;

/// <summary>Represents a PDF section spanning one or more pages, containing lines of text.</summary>
/// <param name="Name">Section header name (e.g. "CHARGES").</param>
/// <param name="StartPage">First page (1-based) containing this section.</param>
/// <param name="EndPage">Last page (1-based) containing this section.</param>
/// <param name="Lines">Lines in the section, each with words and bounding boxes. Ordered top-to-bottom within pages.</param>
public sealed record PdfSection(string Name, int StartPage, int EndPage, IReadOnlyList<PdfTextLine> Lines)
{
    /// <summary>All words from all lines, ordered by line then left-to-right within each line.</summary>
    public IReadOnlyList<PdfWord> Words => Lines.SelectMany(l => l.Words.OrderBy(w => w.BoundingBox.Left)).ToList();
}