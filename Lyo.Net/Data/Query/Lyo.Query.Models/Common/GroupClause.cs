using System.Diagnostics;
using Lyo.Common;
using Lyo.Query.Models.Enums;

namespace Lyo.Query.Models.Common;

/// <summary>Boolean combination of child clauses with <see cref="Operator" /> (AND/OR).</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class GroupClause : WhereClause, IEquatable<GroupClause>
{
    /// <summary>Whether child clauses are combined with AND or OR.</summary>
    public GroupOperatorEnum Operator { get; set; }

    /// <summary>Child nodes evaluated according to <see cref="Operator" />.</summary>
    public List<WhereClause> Children { get; set; }

    /// <summary>Initializes a group with <see cref="GroupOperatorEnum.And" /> and an empty child list.</summary>
    public GroupClause()
    {
        Operator = GroupOperatorEnum.And;
        Children = [];
    }

    /// <summary>Initializes a group with the given operator and children.</summary>
    /// <param name="groupOperator">AND or OR.</param>
    /// <param name="children">Child clauses (must not be null).</param>
    /// <param name="description">Optional <see cref="WhereClause.Description" />.</param>
    public GroupClause(GroupOperatorEnum groupOperator, List<WhereClause> children, string? description = null)
    {
        Operator = groupOperator;
        Children = children;
        Description = description;
    }

    /// <summary>Initializes a group with the given operator, optional description, and child nodes.</summary>
    public GroupClause(GroupOperatorEnum op, string? description = null, params WhereClause[] children)
    {
        Operator = op;
        Description = description;
        Children = new(children);
    }

    /// <summary>Initializes a group from a sequence of child clauses.</summary>
    public GroupClause(GroupOperatorEnum op, IEnumerable<WhereClause> children, string? description = null)
    {
        Operator = op;
        Children = [..children];
        Description = description;
    }

    public bool Equals(GroupClause? other)
    {
        if (other is null)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        if (Operator != other.Operator)
            return false;

        if (Children.Count != other.Children.Count)
            return false;

        for (var i = 0; i < Children.Count; i++) {
            if (!StructuralEquals(Children[i], other.Children[i]))
                return false;
        }

        return true;
    }

    public override string Print(int indent = 0)
    {
        var pad = new string(' ', indent * 2);
        var op = Operator.ToString().ToUpperInvariant();
        if (Children.Count == 0)
            return $"{pad}({op})";

        if (Children.Count == 1)
            return pad + Children[0].Print(indent);

        // Note: we compute operator indentation per-child based on the child's first line
        var lines = new List<string> { pad + "(" };
        for (var i = 0; i < Children.Count; i++) {
            var childText = Children[i].Print(indent + 1);
            var childLines = childText.Split('\n');
            if (i == 0) {
                // First child: append its lines as-is (they already start with the correct inner indent)
                foreach (var line in childLines)
                    lines.Add(line);
            }
            else {
                // Determine the actual leading-space indentation of the child's first line
                var firstLine = childLines.Length > 0 ? childLines[0] : string.Empty;
                var leading = 0;
                while (leading < firstLine.Length && firstLine[leading] == ' ')
                    leading++;

                var operatorPad = new string(' ', leading);
                var trimmedFirst = firstLine.Length > leading ? firstLine.Substring(leading) : string.Empty;

                // Place operator at the same column where the child's first line content starts
                lines.Add(operatorPad + op + (trimmedFirst.Length > 0 ? " " + trimmedFirst : string.Empty));

                // Remaining lines: preserve relative indentation w.r.t the child's first-line indent
                for (var j = 1; j < childLines.Length; j++) {
                    var l = childLines[j];
                    var lLeading = 0;
                    while (lLeading < l.Length && l[lLeading] == ' ')
                        lLeading++;

                    var rel = lLeading > leading ? l.Substring(leading) : l.TrimStart();
                    lines.Add(operatorPad + rel);
                }
            }
        }

        lines.Add(pad + ")");
        return string.Join("\n", lines);
    }

    public override bool Equals(object? obj) => Equals(obj as GroupClause);

    public override int GetHashCode()
    {
        var values = new object?[Children.Count + 1];
        values[0] = Operator;
        for (var i = 0; i < Children.Count; i++)
            values[i + 1] = StructuralHashCode(Children[i]);

        return HashCodeHelpers.Combine(values);
    }

    private static bool StructuralEquals(WhereClause? a, WhereClause? b)
    {
        if (ReferenceEquals(a, b))
            return true;

        if (a is null || b is null)
            return false;

        if (a is ConditionClause ca && b is ConditionClause cb)
            return ca.Equals(cb);

        if (a is GroupClause ga && b is GroupClause gb)
            return ga.Equals(gb);

        return false;
    }

    private static int StructuralHashCode(WhereClause? node)
        => node switch {
            null => 0,
            ConditionClause c => c.GetHashCode(),
            GroupClause g => g.GetHashCode(),
            var _ => node.GetHashCode()
        };

    public override string ToString() => Description ?? $"({string.Join($" {Operator.ToString().ToUpperInvariant()} ", Children)})";
}