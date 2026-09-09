namespace Lyo.Pdf.Models;

/// <summary>Words that share one visual row, left-to-right.</summary>
/// <param name="Y">Centroid Y in PDF coordinates.</param>
/// <param name="Words">Words on the row, ordered by X.</param>
/// <param name="Text">Concatenated word text.</param>
public sealed record PdfTextLine(double Y, IReadOnlyList<PdfWord> Words, string Text);