using System.Reflection;

namespace Lyo.Diff.ObjectGraph;

/// <summary>Controls depth, path filters, and leaf equality for <see cref="IObjectGraphDiffService" />.</summary>
public sealed class ObjectGraphDiffOptions
{
    /// <summary>Maximum property nesting depth (1 = top-level only).</summary>
    public int MaxDepth { get; set; } = 32;

    /// <summary>When true, single-rank arrays are walked element-wise (<c>Items[0]</c> paths).</summary>
    public bool CompareArrayElements { get; set; } = true;

    /// <summary>Optional filter; when not null, paths for which this returns false are skipped.</summary>
    public Func<string, bool>? IncludePath { get; set; }

    /// <summary>Optional filter; when not null, paths for which this returns true are skipped.</summary>
    public Func<string, bool>? ExcludePath { get; set; }

    /// <summary>Optional binding flags for selecting properties (default: public instance).</summary>
    public BindingFlags PropertyBindingFlags { get; set; } = BindingFlags.Instance | BindingFlags.Public;

    /// <summary>When set, invoked for candidate leaves before default equality; return true if values are considered equal.</summary>
    public Func<ObjectGraphLeafContext, bool>? CustomEquals { get; set; }
}