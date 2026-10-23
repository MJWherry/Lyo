using Lyo.Exceptions;

namespace Lyo.Story.Web.Components;

/// <summary>Authoring checks that match <c>StoryBuilder.ValidateGraph</c>, plus duplicate keys the builder would also reject on save.</summary>
public static class StoryGraphChecks
{
    /// <summary>Returns the problems on <paramref name="draft" />. An empty list means the graph can be saved.</summary>
    public static IReadOnlyList<string> Check(StoryDraft draft)
    {
        ArgumentHelpers.ThrowIfNull(draft);
        List<string> issues = [];
        var nodeKeys = Keys(draft.Nodes.Select(node => node.Key), "node", issues);
        Keys(draft.Scenes.Select(scene => scene.Key), "scene", issues);
        Keys(draft.Variables.Select(variable => variable.Key), "variable", issues);
        var actionKeys = Keys(draft.Actions.Select(action => action.Key), "action", issues);
        Keys(draft.Reactions.Select(reaction => reaction.Key), "reaction", issues);
        if (draft.Nodes.Count == 0)
            issues.Add("A story requires at least one node.");

        if (draft.StartNodeKey is { } start && !nodeKeys.Contains(start))
            issues.Add($"Start node '{start}' is not defined.");

        foreach (var node in draft.Nodes) {
            Keys(node.Choices.Select(choice => choice.Key), $"choice on node '{node.Key}'", issues);
            foreach (var choice in node.Choices)
                CheckEffects(choice.Effects, nodeKeys, actionKeys, issues);
        }

        foreach (var action in draft.Actions)
            CheckEffects(action.Effects, nodeKeys, actionKeys, issues);

        foreach (var reaction in draft.Reactions)
            CheckEffects(reaction.Effects, nodeKeys, actionKeys, issues);

        return issues;
    }

    private static HashSet<string> Keys(IEnumerable<string> keys, string kind, List<string> issues)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var key in keys) {
            if (string.IsNullOrWhiteSpace(key)) {
                issues.Add($"A {kind} key is required.");
                continue;
            }

            if (!seen.Add(key))
                issues.Add($"Duplicate {kind} key '{key}'.");
        }

        return seen;
    }

    private static void CheckEffects(IEnumerable<StoryEffectDraft> effects, HashSet<string> nodeKeys, HashSet<string> actionKeys, List<string> issues)
    {
        foreach (var effect in effects) {
            if (effect.Kind == StoryEffectKind.GotoNode && !string.IsNullOrWhiteSpace(effect.TargetNodeKey) && !nodeKeys.Contains(effect.TargetNodeKey))
                issues.Add($"Goto targets unknown node '{effect.TargetNodeKey}'.");

            if (effect.Kind == StoryEffectKind.InvokeAction && !string.IsNullOrWhiteSpace(effect.InvokeActionKey) && !actionKeys.Contains(effect.InvokeActionKey))
                issues.Add($"Invoke targets unknown action '{effect.InvokeActionKey}'.");
        }
    }
}