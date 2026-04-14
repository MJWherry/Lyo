using System.Diagnostics;
using Lyo.Common.Records;
using UglyToad.PdfPig;

namespace Lyo.Pdf.Models;

[DebuggerDisplay("{ToString(),nq}")]
internal sealed record LoadedPdf(Guid Id, PdfDocument Document, byte[] OriginalBytes, string? FilePath, string? Url)
{
    public DateTime LoadedAt { get; init; } = DateTime.UtcNow;

    public override string ToString()
        => $"LoadedPdf: Id={Id}, Size={FileSizeUnitInfo.FormatBestFitAbbreviation(OriginalBytes.LongLength)}, FilePath='{FilePath}', Url='{Url}', LoadedAt='{LoadedAt}'";
}