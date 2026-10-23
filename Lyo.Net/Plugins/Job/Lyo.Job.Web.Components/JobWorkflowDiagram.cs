using System.Diagnostics;
using System.Globalization;
using Lyo.Diagram;
using Lyo.Diagram.Models;
using Lyo.Exceptions;

namespace Lyo.Job.Web.Components;

/// <summary>A workflow drawn as a <see cref="DiagramDocument" />, plus the step names that were left off the page because they form a cycle.</summary>
/// <param name="Document">One page whose shapes are steps and whose edges are dependencies.</param>
/// <param name="Cyclic">Step names that can never run. They are not on the page.</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record JobWorkflowPicture(DiagramDocument Document, IReadOnlyList<string> Cyclic);

/// <summary>Projects a workflow's steps onto a diagram. Positions come from <see cref="JobWorkflowGraphLayout" />; the cells are what the graph view draws.</summary>
public static class JobWorkflowDiagram
{
    /// <summary>Builds the picture for <paramref name="steps" />. <paramref name="name" /> is the diagram name. A blank name becomes "Workflow".</summary>
    public static JobWorkflowPicture Create(string? name, IReadOnlyList<JobWorkflowStepDraft> steps)
    {
        ArgumentHelpers.ThrowIfNull(steps);
        var layout = JobWorkflowGraphLayout.Build(steps);
        var document = new DiagramBuilder(string.IsNullOrWhiteSpace(name) ? "Workflow" : name).Page("flow", "Flow", page => Write(page, layout)).Build();
        return new(document, layout.Cyclic);
    }

    private static void Write(DiagramPageBuilder page, JobWorkflowGraphLayout layout)
    {
        if (layout.Width > 0 && layout.Height > 0)
            page.Size(layout.Width, layout.Height);

        foreach (var node in layout.Nodes)
            page.Shape(CellId(node.Step.Id), node.Name, node.X, node.Y, JobWorkflowGraphLayout.NodeWidth, JobWorkflowGraphLayout.NodeHeight, DiagramShapeKind.Rounded);

        foreach (var edge in layout.Edges) {
            var from = NodeAt(layout, edge.FromName, edge.StartX, edge.StartY, true);
            var to = NodeAt(layout, edge.ToName, edge.EndX, edge.EndY, false);
            page.Connect(
                $"{CellId(from.Step.Id)}-{CellId(to.Step.Id)}", CellId(from.Step.Id), CellId(to.Step.Id), Waypoints(edge), new() { X = edge.StartX, Y = edge.StartY },
                new() { X = edge.EndX, Y = edge.EndY }, EdgeLabel(to.Step));
        }
    }

    /// <summary>What has to be true for the target to run once the source has finished. An ungated step says so, rather than leaving a blank arrow.</summary>
    private static string EdgeLabel(JobWorkflowStepDraft step) => string.IsNullOrWhiteSpace(step.ConditionText) ? "always" : step.ConditionText;

    /// <summary>The layout names an edge by step name. Matching the anchor as well keeps two steps that share a name on the right ends of the arrow.</summary>
    private static JobWorkflowGraphNode NodeAt(JobWorkflowGraphLayout layout, string name, int anchorX, int anchorY, bool source)
    {
        var edgeX = source ? JobWorkflowGraphLayout.NodeWidth : 0;
        return layout.Nodes.Single(node => node.Name == name && anchorX == node.X + edgeX && anchorY >= node.Y && anchorY <= node.Y + JobWorkflowGraphLayout.NodeHeight);
    }

    /// <summary>The two bends between the anchors. The layout path is <c>M sx sy L cx sy L cx ey L ex ey</c>.</summary>
    private static IReadOnlyList<DiagramPoint> Waypoints(JobWorkflowGraphEdge edge)
    {
        var tokens = edge.Path.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length != 12 || tokens[0] != "M" || tokens[3] != "L" || tokens[6] != "L")
            return [];

        return [
            new() { X = int.Parse(tokens[4], CultureInfo.InvariantCulture), Y = int.Parse(tokens[5], CultureInfo.InvariantCulture) },
            new() { X = int.Parse(tokens[7], CultureInfo.InvariantCulture), Y = int.Parse(tokens[8], CultureInfo.InvariantCulture) }
        ];
    }

    private static string CellId(Guid id) => id.ToString("N");
}