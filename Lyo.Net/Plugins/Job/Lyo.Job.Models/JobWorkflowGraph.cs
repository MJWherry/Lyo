using Lyo.Job.Models.Response;

namespace Lyo.Job.Models;

/// <summary>
/// Dependency-graph math over workflow steps: parsing <c>DependsOnStepIds</c> and finding cycles. Shared by the builder (which rejects a bad graph at authoring time), the
/// scheduler's engine (which fails a run whose graph cannot execute), and the workflow editor, so all three agree on what "valid" means.
/// </summary>
public static class JobWorkflowGraph
{
    /// <summary>
    /// Parses a step's comma-separated <c>DependsOnStepIds</c>. Returns false when any entry is not a GUID: malformed configuration is permanent, so callers fail the graph
    /// rather than retrying.
    /// </summary>
    /// <param name="dependsOnStepIds">Raw column value. Null or blank means no dependencies and parses successfully.</param>
    /// <param name="dependencyIds">Parsed ids, empty when there are none.</param>
    public static bool TryParseDependencies(string? dependsOnStepIds, out HashSet<Guid> dependencyIds)
    {
        dependencyIds = [];
        if (string.IsNullOrWhiteSpace(dependsOnStepIds))
            return true;

        foreach (var raw in dependsOnStepIds!.Split([','], StringSplitOptions.RemoveEmptyEntries)) {
            if (!Guid.TryParse(raw.Trim(), out var id))
                return false;

            dependencyIds.Add(id);
        }

        return true;
    }

    /// <summary>
    /// The step ids that participate in a dependency cycle (including self-references), or an empty set when the graph is acyclic. Without this check a circular
    /// <c>DependsOnStepIds</c> leaves every step pending and the workflow run running forever.
    /// </summary>
    /// <param name="steps">Each step's id paired with its raw <c>DependsOnStepIds</c>. Unparseable dependencies are treated as no edges; callers report those separately.</param>
    public static HashSet<Guid> FindDependencyCycle(IEnumerable<KeyValuePair<Guid, string?>> steps)
    {
        var edges = new Dictionary<Guid, HashSet<Guid>>();
        foreach (var step in steps)
            edges[step.Key] = TryParseDependencies(step.Value, out var deps) ? deps : [];

        var permanent = new HashSet<Guid>();
        var inStack = new HashSet<Guid>();
        var cycle = new HashSet<Guid>();

        bool Visit(Guid id)
        {
            if (permanent.Contains(id))
                return false;

            if (!inStack.Add(id)) {
                cycle.Add(id);
                return true;
            }

            if (edges.TryGetValue(id, out var deps)) {
                foreach (var dep in deps) {
                    if (!Visit(dep))
                        continue;

                    cycle.Add(id);
                    inStack.Remove(id);
                    return true;
                }
            }

            inStack.Remove(id);
            permanent.Add(id);
            return false;
        }

        foreach (var id in edges.Keys.ToList())
            Visit(id);

        return cycle;
    }

    /// <inheritdoc cref="FindDependencyCycle(IEnumerable{KeyValuePair{Guid, string}})" />
    public static HashSet<Guid> FindDependencyCycle(IReadOnlyList<JobWorkflowStepRes> steps)
        => FindDependencyCycle((steps ?? []).Select(s => new KeyValuePair<Guid, string?>(s.Id, s.DependsOnStepIds)));
}