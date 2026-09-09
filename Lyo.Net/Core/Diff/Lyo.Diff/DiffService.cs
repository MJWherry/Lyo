using Lyo.Diff.ObjectGraph;
using Lyo.Diff.Text;

namespace Lyo.Diff;

internal sealed class DiffService(ITextDiffService text, IObjectGraphDiffService objects) : IDiffService
{
    public ITextDiffService Text { get; } = text;

    public IObjectGraphDiffService Objects { get; } = objects;
}