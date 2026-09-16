using System.Text;

namespace Lyo.StateMachine.Internal;

internal static class StateMachineDiagram
{
    private const string Indent = "    ";

    public static string Mermaid<TState, TTrigger, TContext>(StateMachineDefinition<TState, TTrigger, TContext> definition)
        where TState : notnull where TTrigger : notnull
    {
        var sb = new StringBuilder("stateDiagram-v2\n");
        sb.Append(Indent).Append("[*] --> ").Append(definition.InitialState).Append('\n');
        foreach (var state in Roots(definition))
            WriteMermaidState(sb, definition, state, 1);

        foreach (var (source, destination, label) in Edges(definition))
            sb.Append(Indent).Append(source).Append(" --> ").Append(destination).Append(" : ").Append(label).Append('\n');

        foreach (var state in definition.States.Where(s => definition.Node(s).IsFinal))
            sb.Append(Indent).Append(state).Append(" --> [*]\n");

        return sb.ToString();
    }

    public static string Dot<TState, TTrigger, TContext>(StateMachineDefinition<TState, TTrigger, TContext> definition)
        where TState : notnull where TTrigger : notnull
    {
        var sb = new StringBuilder();
        sb.Append("digraph ").Append(Quote(definition.Name)).Append(" {\n");
        sb.Append(Indent).Append("rankdir=LR;\n");
        sb.Append(Indent).Append("\"__start\" [shape=point];\n");
        sb.Append(Indent).Append("\"__start\" -> ").Append(Quote(definition.InitialState)).Append(";\n");
        foreach (var state in Roots(definition))
            WriteDotState(sb, definition, state, 1);

        foreach (var (source, destination, label) in Edges(definition))
            sb.Append(Indent).Append(Quote(source)).Append(" -> ").Append(Quote(destination)).Append(" [label=").Append(Quote(label)).Append("];\n");

        sb.Append("}\n");
        return sb.ToString();
    }

    private static void WriteMermaidState<TState, TTrigger, TContext>(StringBuilder sb, StateMachineDefinition<TState, TTrigger, TContext> definition, TState state, int depth)
        where TState : notnull where TTrigger : notnull
    {
        var indent = string.Concat(Enumerable.Repeat(Indent, depth));
        var children = Children(definition, state).ToArray();
        if (children.Length == 0) {
            sb.Append(indent).Append(state).Append('\n');
            return;
        }

        sb.Append(indent).Append("state ").Append(state).Append(" {\n");
        var node = definition.Node(state);
        if (node.HasInitialSubstate)
            sb.Append(indent).Append(Indent).Append("[*] --> ").Append(node.InitialSubstate).Append('\n');

        foreach (var child in children)
            WriteMermaidState(sb, definition, child, depth + 1);

        sb.Append(indent).Append("}\n");
    }

    private static void WriteDotState<TState, TTrigger, TContext>(StringBuilder sb, StateMachineDefinition<TState, TTrigger, TContext> definition, TState state, int depth)
        where TState : notnull where TTrigger : notnull
    {
        var indent = string.Concat(Enumerable.Repeat(Indent, depth));
        var children = Children(definition, state).ToArray();
        if (children.Length == 0) {
            sb.Append(indent).Append(Quote(state)).Append(";\n");
            return;
        }

        sb.Append(indent).Append("subgraph ").Append(Quote($"cluster_{state}")).Append(" {\n");
        sb.Append(indent).Append(Indent).Append("label=").Append(Quote(state)).Append(";\n");
        sb.Append(indent).Append(Indent).Append(Quote(state)).Append(";\n");
        foreach (var child in children)
            WriteDotState(sb, definition, child, depth + 1);

        sb.Append(indent).Append("}\n");
    }

    private static IEnumerable<TState> Roots<TState, TTrigger, TContext>(StateMachineDefinition<TState, TTrigger, TContext> definition)
        where TState : notnull where TTrigger : notnull
        => definition.States.Where(s => !definition.Node(s).HasParent);

    private static IEnumerable<TState> Children<TState, TTrigger, TContext>(StateMachineDefinition<TState, TTrigger, TContext> definition, TState parent)
        where TState : notnull where TTrigger : notnull
        => definition.States.Where(s => definition.Node(s).HasParent && EqualityComparer<TState>.Default.Equals(definition.Node(s).Parent, parent));

    private static IEnumerable<(TState Source, TState Destination, string Label)> Edges<TState, TTrigger, TContext>(StateMachineDefinition<TState, TTrigger, TContext> definition)
        where TState : notnull where TTrigger : notnull
    {
        foreach (var source in definition.States) {
            foreach (var rule in definition.Node(source).Rules) {
                var label = rule.Guard is null ? $"{rule.Trigger}" : $"{rule.Trigger} [{rule.GuardDescription ?? "guard"}]";
                switch (rule.Kind) {
                    case RuleKind.Permit:
                        yield return (source, rule.Destination, label);
                        break;
                    case RuleKind.Reentry:
                        yield return (source, source, label);
                        break;
                    case RuleKind.Internal:
                        yield return (source, source, $"{label} (internal)");
                        break;
                    case RuleKind.Dynamic:
                        foreach (var destination in rule.Destinations.Distinct())
                            yield return (source, destination, $"{label} (dynamic)");

                        break;
                }
            }
        }
    }

    private static string Quote(object? value) => $"\"{value?.ToString()?.Replace("\"", "\\\"")}\"";
}
