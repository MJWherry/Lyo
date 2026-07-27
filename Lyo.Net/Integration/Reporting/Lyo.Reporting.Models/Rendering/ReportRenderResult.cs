namespace Lyo.Reporting.Models.Rendering;

/// <summary>Result of rendering a report to a staged file.</summary>
public sealed class ReportRenderResult
{
    public string FilePath { get; init; } = null!;

    public string ContentType { get; init; } = null!;

    public string FileName { get; init; } = null!;

    public long? ByteLength { get; init; }
}
