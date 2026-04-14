using System.Diagnostics;

namespace Lyo.Pdf.Models;

/// <summary>Metadata about a loaded PDF.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record PdfInfo(
    int PageCount,
    string? Title,
    string? Author,
    string? Subject,
    string? Creator,
    string? Producer,
    string? FilePath,
    string? Url,
    DateTime? CreationDate,
    DateTime? ModifiedDate)
{
    public override string ToString()
        => $"PDF Info: Title='{Title}', Author='{Author}', Subject='{Subject}', Creator='{Creator}', Producer='{Producer}', FilePath='{FilePath}', Url='{Url}', CreationDate='{CreationDate}', ModifiedDate='{ModifiedDate}', PageCount={PageCount}";
}