namespace Lyo.Pdf.Models;

/// <summary>Named span of lines that may cross page boundaries.</summary>
/// <param name="Name">Header text (e.g. "CHARGES").</param>
/// <param name="StartPage">First 1-based page that contains this section.</param>
/// <param name="EndPage">Last 1-based page that contains this section.</param>
/// <param name="Lines">Section lines with words and boxes, top-to-bottom within each page.</param>
public sealed record PdfSection(string Name, int StartPage, int EndPage, IReadOnlyList<PdfTextLine> Lines)
{
    /// <summary>Flattened words from every line, left-to-right within each line.</summary>
    public IReadOnlyList<PdfWord> Words => Lines.SelectMany(l => l.Words.OrderBy(w => w.BoundingBox.Left)).ToList();
}