using Lyo.Diff.ObjectGraph;
using Lyo.Diff.Text;

namespace Lyo.Diff;

/// <summary>Facade over the text and object-graph diff services.</summary>
public interface IDiffService
{
    /// <summary>Tokenizes and diffs text strings.</summary>
    ITextDiffService Text { get; }

    /// <summary>Compares object graphs along property paths.</summary>
    IObjectGraphDiffService Objects { get; }
}