using System.Diagnostics;

namespace Lyo.Mathematics.Registry;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record FormulaDescriptor(string Id, string Category, string Library, string Name, string Description, string Signature)
{
    public override string ToString() => $"{Id}: {Name} ({Category})";
}