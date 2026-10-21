namespace Lyo.Api.Models.Common.Response;

/// <summary>One row or element that was left out of the good list. The rest of the file can still succeed.</summary>
public sealed class ImportIssue
{
    /// <summary>Zero-based JSON index, or the 1-based CSV/XLSX row (header is row 1).</summary>
    public int Index { get; set; }

    /// <summary>Column or property that failed.</summary>
    public string? Field { get; set; }

    /// <summary>Why the value was rejected.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Source text.</summary>
    public string? Raw { get; set; }
}