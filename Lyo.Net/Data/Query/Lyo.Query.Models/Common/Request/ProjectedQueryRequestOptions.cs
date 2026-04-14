using System.Diagnostics;

namespace Lyo.Query.Models.Common.Request;

/// <summary>Options for <c>/QueryProject</c> (projection). Extends <see cref="QueryRequestOptions" /> with projection shape flags.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class ProjectedQueryRequestOptions : QueryRequestOptions
{
    /// <summary>
    /// When <c>true</c> (default when omitted: treat as <c>true</c>), QueryProject merges sibling fields under the same collection into one array of objects (e.g. <c>items.a</c> + <c>items.b</c> → <c>items: [{{ a, b }}]</c>).
    /// When <c>false</c>, each selected path stays a separate column (parallel arrays per path).
    /// </summary>
    public bool ZipSiblingCollectionSelections { get; set; } = true;

    public override string ToString()
        => $"{base.ToString()}, ZipSiblingCollectionSelections: {ZipSiblingCollectionSelections}";
}
