using System.Diagnostics;
using Lyo.Common.Records;

namespace Lyo.Pdf.Models;

/// <summary>A region on a PDF page defined by a bounding box and page number.</summary>
/// <param name="Page">1-based page number.</param>
/// <param name="Box">Bounding box in PDF coordinates (points).</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record PdfBoundingBox(int Page, BoundingBox2D Box)
{
    public override string ToString() => $"Page: {Page}, Box: {Box}";
}