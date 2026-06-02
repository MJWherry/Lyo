using System.Diagnostics;

namespace Lyo.Authentication.Models.Scopes;

/// <summary>A registered authorization scope. Names are <c>{resource}.{action}</c> lowercase dot-notation (e.g. <c>people.read</c>, <c>config.admin</c>).</summary>
/// <param name="Name">The wire name. Must be lowercase ASCII with optional dot separators.</param>
/// <param name="Description">Human-readable description shown in token-management UIs.</param>
/// <param name="Implies">Other scope names this scope grants transitively. Resolved at registration time into <see cref="TransitiveImplies" />.</param>
/// <param name="TransitiveImplies">Fully-flattened set of implied scopes (including this scope's name). Populated by the registry.</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record Scope(string Name, string Description, IReadOnlyList<string> Implies, IReadOnlyCollection<string> TransitiveImplies)
{
    public override string ToString() => $"Scope: name={Name}, implies={Implies.Count}, transitive={TransitiveImplies.Count}";
}