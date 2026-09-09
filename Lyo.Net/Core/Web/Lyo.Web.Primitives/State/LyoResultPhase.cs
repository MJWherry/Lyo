namespace Lyo.Web.Primitives;

/// <summary>Which branch <see cref="LyoResultBoundary{T}" /> is currently drawing.</summary>
public enum LyoResultPhase
{
    /// <summary>No result yet, or the caller flagged a reload already in progress.</summary>
    Loading,

    /// <summary>The result failed. Its errors are displayed.</summary>
    Error,

    /// <summary>The result succeeded but carries nothing to draw.</summary>
    Empty,

    /// <summary>The result succeeded and carries data.</summary>
    Content
}
