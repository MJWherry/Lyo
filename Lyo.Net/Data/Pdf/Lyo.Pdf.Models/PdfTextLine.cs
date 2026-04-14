namespace Lyo.Pdf.Models;

/// <summary>Words on the same visual row, ordered left→right.</summary>
/// <param name="Y">Vertical position ( centroid Y in PDF coordinates).</param>
/// <param name="Words">Words on this line, ordered by horizontal position.</param>
/// <param name="Text">Concatenated text of all words.</param>
public sealed record PdfTextLine(double Y, IReadOnlyList<PdfWord> Words, string Text);