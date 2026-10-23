using System.Diagnostics;
using System.Globalization;
using Lyo.Exceptions;

namespace Lyo.Story.Web.Components;

/// <summary>One node placed on the story canvas.</summary>
/// <param name="Key">Node key.</param>
/// <param name="Node">The draft behind the box.</param>
/// <param name="Column">Stage, counting from zero. A choice target sits one column right of its source.</param>
/// <param name="Row">Position among the siblings sharing that column.</param>
/// <param name="X">Left offset in pixels.</param>
/// <param name="Y">Top offset in pixels.</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record StoryGraphNodePlacement(string Key, StoryNodeDraft Node, int Column, int Row, int X, int Y);

/// <summary>One arrow from a choice to the node its last goto step names.</summary>
/// <param name="FromKey">Source node key.</param>
/// <param name="ToKey">Target node key.</param>
/// <param name="ChoiceKey">Choice that owns the goto.</param>
/// <param name="StartX">Source anchor, on the right edge.</param>
/// <param name="StartY">Source anchor, on the right edge.</param>
/// <param name="EndX">Target anchor, on the left edge.</param>
/// <param name="EndY">Target anchor, on the left edge.</param>
/// <param name="Path">SVG <c>d</c> attribute.</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record StoryGraphEdgePlacement(string FromKey, string ToKey, string ChoiceKey, int StartX, int StartY, int EndX, int EndY, string Path);

/// <summary>
/// Places story nodes left to right from the start node and draws one arrow per choice, aimed at that choice's last goto. A node with no incoming choice is still placed.
/// Nodes caught in a cycle are reported and left off the page.
/// </summary>
public sealed class StoryGraphLayout
{
    /// <summary>Node box width in pixels.</summary>
    public const int NodeWidth = 220;

    /// <summary>Node box height in pixels.</summary>
    public const int NodeHeight = 72;

    /// <summary>Horizontal space between stages.</summary>
    public const int ColumnGap = 96;

    /// <summary>Vertical space between siblings sharing a stage.</summary>
    public const int RowGap = 20;

    private const int EdgeInset = 12;

    /// <summary>Placed nodes. Cyclic nodes are absent.</summary>
    public IReadOnlyList<StoryGraphNodePlacement> Nodes { get; }

    /// <summary>Arrows between placed nodes.</summary>
    public IReadOnlyList<StoryGraphEdgePlacement> Edges { get; }

    /// <summary>Node keys left off the page because their choices form a cycle.</summary>
    public IReadOnlyList<string> Cyclic { get; }

    /// <summary>Canvas width in pixels.</summary>
    public int Width { get; }

    /// <summary>Canvas height in pixels.</summary>
    public int Height { get; }

    private StoryGraphLayout(IReadOnlyList<StoryGraphNodePlacement> nodes, IReadOnlyList<StoryGraphEdgePlacement> edges, IReadOnlyList<string> cyclic)
    {
        Nodes = nodes;
        Edges = edges;
        Cyclic = cyclic;
        Width = nodes.Count == 0 ? 0 : nodes.Max(node => node.X) + NodeWidth;
        Height = nodes.Count == 0 ? 0 : nodes.Max(node => node.Y) + NodeHeight;
    }

    /// <summary>Lays out <paramref name="draft" />. Blank keys are skipped. A goto that names a missing node draws no arrow.</summary>
    public static StoryGraphLayout Build(StoryDraft draft)
    {
        ArgumentHelpers.ThrowIfNull(draft);
        var nodes = draft.Nodes.Where(node => !string.IsNullOrWhiteSpace(node.Key)).GroupBy(node => node.Key, StringComparer.Ordinal).Select(group => group.First()).ToList();
        var byKey = nodes.ToDictionary(node => node.Key, StringComparer.Ordinal);
        var links = Links(nodes, byKey);
        var (levels, cyclic) = Layer(nodes, links);
        var placed = Place(levels);
        return new(placed, BuildEdges(placed, links), cyclic);
    }

    private static List<(string From, string To, string ChoiceKey)> Links(List<StoryNodeDraft> nodes, Dictionary<string, StoryNodeDraft> byKey)
    {
        List<(string From, string To, string ChoiceKey)> links = [];
        foreach (var node in nodes) {
            foreach (var choice in node.Choices) {
                var target = StoryDraftMap.LastGoto(choice.Effects);
                if (target is not null && byKey.ContainsKey(target))
                    links.Add((node.Key, target, choice.Key));
            }
        }

        return links;
    }

    private static (List<List<StoryNodeDraft>> Levels, List<string> Cyclic) Layer(List<StoryNodeDraft> nodes, List<(string From, string To, string ChoiceKey)> links)
    {
        var incoming = nodes.ToDictionary(node => node.Key, _ => new HashSet<string>(StringComparer.Ordinal), StringComparer.Ordinal);
        foreach (var link in links)
            incoming[link.To].Add(link.From);

        List<List<StoryNodeDraft>> levels = [];
        var placed = new HashSet<string>(StringComparer.Ordinal);
        var remaining = nodes.ToList();
        while (remaining.Count > 0) {
            var ready = remaining.Where(node => incoming[node.Key].All(placed.Contains)).ToList();
            if (ready.Count == 0)
                return (levels, [.. remaining.Select(node => node.Key)]);

            levels.Add(ready);
            foreach (var node in ready) {
                placed.Add(node.Key);
                remaining.Remove(node);
            }
        }

        return (levels, []);
    }

    private static List<StoryGraphNodePlacement> Place(List<List<StoryNodeDraft>> levels)
    {
        List<StoryGraphNodePlacement> nodes = [];
        for (var column = 0; column < levels.Count; column++) {
            for (var row = 0; row < levels[column].Count; row++) {
                var node = levels[column][row];
                nodes.Add(new(node.Key, node, column, row, column * (NodeWidth + ColumnGap), row * (NodeHeight + RowGap)));
            }
        }

        return nodes;
    }

    private static List<StoryGraphEdgePlacement> BuildEdges(List<StoryGraphNodePlacement> nodes, List<(string From, string To, string ChoiceKey)> links)
    {
        var placed = nodes.ToDictionary(node => node.Key, StringComparer.Ordinal);
        var pairs = links.Where(link => placed.ContainsKey(link.From) && placed.ContainsKey(link.To))
            .Select(link => (From: placed[link.From], To: placed[link.To], link.ChoiceKey))
            .ToList();

        List<StoryGraphEdgePlacement> edges = [];
        foreach (var pair in pairs) {
            var exitIndex = pairs.Where(item => item.From == pair.From).ToList().IndexOf(pair);
            var exitCount = pairs.Count(item => item.From == pair.From);
            var entryIndex = pairs.Where(item => item.To == pair.To).ToList().IndexOf(pair);
            var entryCount = pairs.Count(item => item.To == pair.To);
            edges.Add(Connect(pair.From, pair.To, pair.ChoiceKey, exitIndex, exitCount, entryIndex, entryCount));
        }

        return edges;
    }

    private static StoryGraphEdgePlacement Connect(
        StoryGraphNodePlacement from,
        StoryGraphNodePlacement to,
        string choiceKey,
        int exitIndex,
        int exitCount,
        int entryIndex,
        int entryCount)
    {
        var startX = from.X + NodeWidth;
        var startY = Spread(from.Y, exitCount, exitIndex);
        var endX = to.X;
        var endY = Spread(to.Y, entryCount, entryIndex);
        var channelX = endX - ColumnGap / 2 - entryIndex * 8;
        if (channelX <= startX)
            channelX = startX + Math.Max((endX - startX) / 2, 1);

        var path = string.Create(CultureInfo.InvariantCulture, $"M {startX} {startY} L {channelX} {startY} L {channelX} {endY} L {endX} {endY}");
        return new(from.Key, to.Key, choiceKey, startX, startY, endX, endY, path);
    }

    private static int Spread(int origin, int count, int index)
    {
        if (count <= 1)
            return origin + NodeHeight / 2;

        var span = NodeHeight - 2 * EdgeInset;
        return origin + EdgeInset + (int)Math.Round(span * (index / (double)(count - 1)));
    }
}