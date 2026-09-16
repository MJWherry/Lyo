using System.Globalization;
using Lyo.Exceptions;

namespace Lyo.Job.Web.Components;

/// <summary>One node placed on the graph canvas.</summary>
/// <param name="Name">Step name, unique within the workflow.</param>
/// <param name="Step">The draft behind the node, for the label, condition, and definition the view renders.</param>
/// <param name="Column">Dependency stage, counting from zero. Every step sits one column right of the last of its dependencies.</param>
/// <param name="Row">Position among the siblings sharing that column.</param>
/// <param name="X">Left offset in pixels.</param>
/// <param name="Y">Top offset in pixels.</param>
public sealed record JobWorkflowGraphNode(string Name, JobWorkflowStepDraft Step, int Column, int Row, int X, int Y);

/// <summary>One arrow from a dependency to the step that waits on it.</summary>
/// <param name="FromName">Source step name.</param>
/// <param name="ToName">Target step name.</param>
/// <param name="StartX">Source anchor, the middle of its right edge.</param>
/// <param name="StartY">Source anchor, the middle of its right edge.</param>
/// <param name="EndX">Target anchor, the middle of its left edge.</param>
/// <param name="EndY">Target anchor, the middle of its left edge.</param>
/// <param name="Path">SVG <c>d</c> attribute: an orthogonal polyline that leaves the source to the right and arrives at the target from the left.</param>
public sealed record JobWorkflowGraphEdge(string FromName, string ToName, int StartX, int StartY, int EndX, int EndY, string Path);

/// <summary>
/// Places a workflow's steps left to right by dependency stage and precomputes the arrow between every dependency pair.
/// <para>
/// The geometry is arithmetic over fixed box and gap sizes rather than something measured from the DOM, so the graph needs no JavaScript and the layout can be asserted
/// in tests. <see cref="JobWorkflowDiagram" /> copies it onto a diagram, and the view draws that diagram.
/// </para>
/// </summary>
public sealed class JobWorkflowGraphLayout
{
    /// <summary>Node box width in pixels. Wide enough for a step name and the one-line gate at the graph's reading size.</summary>
    public const int NodeWidth = 240;

    /// <summary>Node box height in pixels. Two lines of text, with padding above, between, and below.</summary>
    public const int NodeHeight = 84;

    /// <summary>Horizontal space between stages. The arrow's vertical segment and its arrowhead are drawn in this gap.</summary>
    public const int ColumnGap = 96;

    /// <summary>How far a shared edge's anchors sit in from the top and bottom of a node, so two arrows do not land on the same point or on a corner.</summary>
    private const int EdgeInset = 12;

    /// <summary>Vertical space between siblings sharing a stage.</summary>
    public const int RowGap = 20;

    private JobWorkflowGraphLayout(IReadOnlyList<JobWorkflowGraphNode> nodes, IReadOnlyList<JobWorkflowGraphEdge> edges, IReadOnlyList<string> cyclic)
    {
        Nodes = nodes;
        Edges = edges;
        Cyclic = cyclic;
        Width = nodes.Count == 0 ? 0 : nodes.Max(n => n.X) + NodeWidth;
        Height = nodes.Count == 0 ? 0 : nodes.Max(n => n.Y) + NodeHeight;
    }

    /// <summary>Placed nodes. Steps caught in a cycle are absent: they have no stage, so there is nowhere to draw them.</summary>
    public IReadOnlyList<JobWorkflowGraphNode> Nodes { get; }

    /// <summary>Arrows between placed nodes. A dependency on a missing or cyclic step produces none.</summary>
    public IReadOnlyList<JobWorkflowGraphEdge> Edges { get; }

    /// <summary>Steps that can never run because their dependencies form a cycle.</summary>
    public IReadOnlyList<string> Cyclic { get; }

    /// <summary>Canvas width in pixels, covering the furthest node.</summary>
    public int Width { get; }

    /// <summary>Canvas height in pixels, covering the lowest node.</summary>
    public int Height { get; }

    /// <summary>Lays out <paramref name="steps" />. Unnamed steps are skipped, and a dependency whose draft id is not in the list is ignored rather than invented.</summary>
    public static JobWorkflowGraphLayout Build(IReadOnlyList<JobWorkflowStepDraft> steps)
    {
        ArgumentHelpers.ThrowIfNull(steps);
        var byId = steps.Where(s => s.Id != Guid.Empty && !string.IsNullOrWhiteSpace(s.StepName))
            .GroupBy(s => s.Id)
            .ToDictionary(g => g.Key, g => g.First());

        var (levels, cyclic) = Layer(byId);
        var nodes = Place(levels);
        return new(nodes, BuildEdges(nodes, byId), cyclic);
    }

    /// <summary>
    /// Kahn-style layering: each pass emits every step whose dependencies are already placed. What is left when nothing can be emitted is exactly the cyclic remainder.
    /// </summary>
    private static (List<List<JobWorkflowStepDraft>> Levels, List<string> Cyclic) Layer(Dictionary<Guid, JobWorkflowStepDraft> byId)
    {
        List<List<JobWorkflowStepDraft>> levels = [];
        var placed = new HashSet<Guid>();
        var remaining = byId.Values.ToList();
        while (remaining.Count > 0) {
            var ready = remaining.Where(s => s.DependsOn.Where(byId.ContainsKey).All(placed.Contains)).ToList();
            if (ready.Count == 0)
                return (levels, [.. remaining.Select(s => s.StepName)]);

            levels.Add(ready);
            foreach (var step in ready) {
                placed.Add(step.Id);
                remaining.Remove(step);
            }
        }

        return (levels, []);
    }

    private static List<JobWorkflowGraphNode> Place(List<List<JobWorkflowStepDraft>> levels)
    {
        List<JobWorkflowGraphNode> nodes = [];
        for (var column = 0; column < levels.Count; column++) {
            for (var row = 0; row < levels[column].Count; row++)
                nodes.Add(new(levels[column][row].StepName, levels[column][row], column, row, column * (NodeWidth + ColumnGap), row * (NodeHeight + RowGap)));
        }

        return nodes;
    }

    private static List<JobWorkflowGraphEdge> BuildEdges(List<JobWorkflowGraphNode> nodes, Dictionary<Guid, JobWorkflowStepDraft> byId)
    {
        var placed = nodes.ToDictionary(n => n.Step.Id);
        List<(JobWorkflowGraphNode From, JobWorkflowGraphNode To)> pairs = [];
        foreach (var node in nodes) {
            foreach (var dependency in node.Step.DependsOn.Where(byId.ContainsKey)) {
                if (placed.TryGetValue(dependency, out var from))
                    pairs.Add((from, node));
            }
        }

        List<JobWorkflowGraphEdge> edges = [];
        foreach (var (from, to) in pairs) {
            var exitIndex = pairs.Where(p => p.From == from).ToList().IndexOf((from, to));
            var exitCount = pairs.Count(p => p.From == from);
            var entryIndex = pairs.Where(p => p.To == to).ToList().IndexOf((from, to));
            var entryCount = pairs.Count(p => p.To == to);
            edges.Add(Connect(from, to, exitIndex, exitCount, entryIndex, entryCount));
        }

        return edges;
    }

    /// <summary>
    /// Leaves the source's right edge, runs vertically in the gap just left of the target, and enters the target's left edge. Several edges sharing a side are spread down
    /// that side so they do not draw on top of each other.
    /// </summary>
    private static JobWorkflowGraphEdge Connect(JobWorkflowGraphNode from, JobWorkflowGraphNode to, int exitIndex, int exitCount, int entryIndex, int entryCount)
    {
        var startX = from.X + NodeWidth;
        var startY = Spread(from.Y, exitCount, exitIndex);
        var endX = to.X;
        var endY = Spread(to.Y, entryCount, entryIndex);

        var channelX = endX - (ColumnGap / 2) - (entryIndex * 8);
        if (channelX <= startX)
            channelX = startX + Math.Max((endX - startX) / 2, 1);

        var path = string.Create(CultureInfo.InvariantCulture, $"M {startX} {startY} L {channelX} {startY} L {channelX} {endY} L {endX} {endY}");
        return new(from.Name, to.Name, startX, startY, endX, endY, path);
    }

    /// <summary>One anchor down a node's edge. A single edge uses the middle; several share the edge with an inset so none sits on a corner.</summary>
    private static int Spread(int origin, int count, int index)
    {
        if (count <= 1)
            return origin + (NodeHeight / 2);

        var span = NodeHeight - (2 * EdgeInset);
        return origin + EdgeInset + (int)Math.Round(span * (index / (double)(count - 1)));
    }
}
