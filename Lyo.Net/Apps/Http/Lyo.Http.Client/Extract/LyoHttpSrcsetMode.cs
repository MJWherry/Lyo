namespace Lyo.Http.Client.Extract;

/// <summary>How to expand a <c>srcset</c> (or <c>data-srcset</c>) attribute.</summary>
public enum LyoHttpSrcsetMode
{
    /// <summary>Keep the first candidate only (automation default).</summary>
    First = 0,

    /// <summary>Keep every candidate URL.</summary>
    AllCandidates = 1
}
