using System.Diagnostics;

namespace Lyo.Mathematics.Registry;

/// <summary>Describes a discoverable mathematics capability: stable id, taxonomy, owning assembly, and a human-readable signature hint.</summary>
/// <remarks>Shared shape with scientific registries so tooling can treat mathematics and scientific formulas uniformly.</remarks>
/// <param name="Id">Stable, namespaced identifier (for example <c>signal.fft</c>).</param>
/// <param name="Category">High-level grouping for UI or documentation (for example <c>Linear Algebra</c>).</param>
/// <param name="Library">Assembly that implements the capability (for example <c>Lyo.Mathematics.Functions</c>).</param>
/// <param name="Name">Short display title.</param>
/// <param name="Description">One-line explanation of what the formula computes.</param>
/// <param name="Signature">Informal signature or entry-point hint (not a strict language signature).</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record FormulaDescriptor(string Id, string Category, string Library, string Name, string Description, string Signature)
{
    /// <inheritdoc />
    public override string ToString() => $"{Id}: {Name} ({Category})";
}