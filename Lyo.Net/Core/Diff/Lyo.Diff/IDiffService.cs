using Lyo.Diff.ObjectGraph;
using Lyo.Diff.Text;

namespace Lyo.Diff;

/// <summary>Facade over text and object-graph diff services.</summary>
public interface IDiffService
{
    /// <summary>Tokenizes and diffs text.</summary>
    ITextDiffService Text { get; }

    /// <summary>Compares object graphs by property paths.</summary>
    IObjectGraphDiffService Objects { get; }
}