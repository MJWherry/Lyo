using System.Diagnostics;
using Lyo.Common.Metadata.Records;

namespace Lyo.Pdf.Models;

/// <summary>Page-scoped rectangle in PDF space.</summary>
/// <param name="Page">1-based page index.</param>
/// <param name="Box">Rectangle in PDF coordinates (points).</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record PdfBoundingBox(int Page, BoundingBox2D Box)
{
    public override string ToString() => $"Page: {Page}, Box: {Box}";
}