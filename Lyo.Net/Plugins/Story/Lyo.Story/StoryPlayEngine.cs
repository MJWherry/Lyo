using Lyo.Common.Core.Identifiers;
using Lyo.Exceptions;
using Lyo.Story.Builders;
using Lyo.Story.Enums;

namespace Lyo.Story;

/// <summary>Pure play engine: graph + snapshot in, new snapshot + events out. No EF. Does not require presentation.</summary>
public static class StoryPlayEngine
{
    public const int MaxChainDepth = 16;

    /// <summary>Seeds defaults, emits StoryStarted, and enters the start node.</summary>
    public static StoryPlaySnapshot Start(Story story)
    {
        ArgumentHelpers.ThrowIfNull(story);
        StoryBuilder.ValidateGraph(story);
        if (string.IsNullOrWhiteSpace(story.StartNodeKey))
            throw new StoryException("Story has no start node.");

        var snapshot = new StoryPlaySnapshot { Status = StoryPlaythroughStatus.InProgress };
        foreach (var variable in story.Variables) {
            if (variable.DefaultValue is not null)
                snapshot.Variables[variable.Key] = variable.DefaultValue;
        }

        var cause = snapshot.Append(StoryPlaythroughEventKind.StoryStarted, null);
        FireReactions(story, snapshot, StoryTrigger.StoryStarted, cause, null);
        EnterNode(story, snapshot, story.StartNodeKey!, cause, 0);
        return snapshot;
    }

    /// <summary>Choices whose visible/enabled conditions pass, plus Advance when that move is meaningful.</summary>
    public static IReadOnlyList<StoryMove> AvailableMoves(Story story, StoryPlaySnapshot snapshot)
    {
        ArgumentHelpers.ThrowIfNull(story);
        ArgumentHelpers.ThrowIfNull(snapshot);
        var node = RequireCurrentNode(story, snapshot);
        var moves = new List<StoryMove>();
        foreach (var choice in node.Choices.OrderBy(c => c.SortOrder)) {
            if (!StoryWhere.Matches(choice.VisibleWhen, snapshot.Variables, snapshot.Chose, snapshot.Visited))
                continue;

            var enabled = StoryWhere.Matches(choice.EnabledWhen, snapshot.Variables, snapshot.Chose, snapshot.Visited);
            moves.Add(
                new() {
                    Kind = StoryMoveKind.Choice,
                    ChoiceKey = choice.Key,
                    Prompt = choice.Prompt,
                    Enabled = enabled
                });
        }

        if (node.PresentationKind == StoryPresentationKind.Cutscene || (moves.Count == 0 && !node.IsEnding))
            moves.Add(new() { Kind = StoryMoveKind.Advance, Enabled = true });

        return moves;
    }

    /// <summary>Takes an enabled choice on the current node.</summary>
    public static StoryPlaySnapshot TakeChoice(Story story, StoryPlaySnapshot snapshot, string choiceKey)
    {
        ArgumentHelpers.ThrowIfNull(story);
        ArgumentHelpers.ThrowIfNull(snapshot);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(choiceKey);
        EnsureInProgress(snapshot);
        var node = RequireCurrentNode(story, snapshot);
        var choice = node.Choices.FirstOrDefault(c => string.Equals(c.Key, choiceKey, StringComparison.Ordinal)) ??
            throw new StoryException($"Choice '{choiceKey}' is not on node '{node.Key}'.");

        if (!StoryWhere.Matches(choice.VisibleWhen, snapshot.Variables, snapshot.Chose, snapshot.Visited))
            throw new StoryException($"Choice '{choiceKey}' is not visible.");

        if (!StoryWhere.Matches(choice.EnabledWhen, snapshot.Variables, snapshot.Chose, snapshot.Visited))
            throw new StoryException($"Choice '{choiceKey}' is not enabled.");

        var cause = snapshot.Append(StoryPlaythroughEventKind.ChoiceTaken, null, node.Key, choice.Key);
        snapshot.Chose.Add(choice.Key);
        ApplyEffects(story, snapshot, choice.Effects, cause, 0);
        FireReactions(story, snapshot, StoryTrigger.ChoiceTaken, cause, null);
        if (string.IsNullOrWhiteSpace(snapshot.PendingGoto) && !string.IsNullOrWhiteSpace(choice.TargetNodeKey))
            snapshot.PendingGoto = choice.TargetNodeKey;

        ApplyPendingGoto(story, snapshot, cause, 0);
        return snapshot;
    }

    /// <summary>Advances a cutscene or silent beat.</summary>
    public static StoryPlaySnapshot Advance(Story story, StoryPlaySnapshot snapshot)
    {
        ArgumentHelpers.ThrowIfNull(story);
        ArgumentHelpers.ThrowIfNull(snapshot);
        EnsureInProgress(snapshot);
        var moves = AvailableMoves(story, snapshot);
        if (moves.All(m => m.Kind != StoryMoveKind.Advance))
            throw new StoryException("Advance is not available at this node.");

        var node = RequireCurrentNode(story, snapshot);
        var cause = snapshot.Append(StoryPlaythroughEventKind.Advanced, null, node.Key);
        FireReactions(story, snapshot, StoryTrigger.Advanced, cause, null);
        ApplyPendingGoto(story, snapshot, cause, 0);
        return snapshot;
    }

    /// <summary>
    /// Writes externally owned values (for example game facts) into the variable bag. Each changed value logs a VariableChanged event with no cause and fires VariableChanged
    /// reactions, which may move the cursor. Unchanged values are skipped.
    /// </summary>
    public static StoryPlaySnapshot SetVariables(Story story, StoryPlaySnapshot snapshot, IReadOnlyDictionary<string, string?> values)
    {
        ArgumentHelpers.ThrowIfNull(story);
        ArgumentHelpers.ThrowIfNull(snapshot);
        ArgumentHelpers.ThrowIfNull(values);
        EnsureInProgress(snapshot);
        foreach (var pair in values.OrderBy(p => p.Key, StringComparer.Ordinal))
            SetVariable(story, snapshot, pair.Key, pair.Value, null);

        ApplyPendingGoto(story, snapshot, null, 0);
        return snapshot;
    }

    /// <summary>
    /// Rebuilds a live snapshot from a stored playthrough and its event log so the next move behaves as it would on the original snapshot. Use to preview a move before a store
    /// commits it.
    /// </summary>
    public static StoryPlaySnapshot Restore(Story story, StoryPlaythrough playthrough, IReadOnlyList<StoryPlaythroughEvent> events)
    {
        ArgumentHelpers.ThrowIfNull(story);
        ArgumentHelpers.ThrowIfNull(playthrough);
        ArgumentHelpers.ThrowIfNull(events);
        var onceReactions = new HashSet<string>(story.Reactions.Where(r => r.Once).Select(r => r.Key), StringComparer.Ordinal);
        var snapshot = new StoryPlaySnapshot { CurrentNodeKey = playthrough.CurrentNodeKey, Status = playthrough.Status };
        foreach (var variable in playthrough.Variables)
            snapshot.Variables[variable.VariableKey] = variable.Value;

        foreach (var evt in events.OrderBy(e => e.Sequence)) {
            snapshot.Events.Add(evt);
            if (evt.Kind == StoryPlaythroughEventKind.ChoiceTaken && evt.ChoiceKey is { } choiceKey)
                snapshot.Chose.Add(choiceKey);

            if (evt.Kind == StoryPlaythroughEventKind.NodeEntered && evt.NodeKey is { } nodeKey)
                snapshot.Visited.Add(nodeKey);

            if (evt.Kind == StoryPlaythroughEventKind.ReactionFired && evt.ReactionKey is { } reactionKey && onceReactions.Contains(reactionKey))
                snapshot.FiredOnce.Add(reactionKey);
        }

        return snapshot;
    }

    private static void EnterNode(Story story, StoryPlaySnapshot snapshot, string nodeKey, Guid? causedBy, int depth)
    {
        if (depth > MaxChainDepth)
            throw new StoryException("Reaction or goto chain exceeded the maximum depth.");

        var node = story.Nodes.FirstOrDefault(n => string.Equals(n.Key, nodeKey, StringComparison.Ordinal)) ?? throw new StoryException($"Unknown node '{nodeKey}'.");
        snapshot.CurrentNodeKey = node.Key;
        snapshot.Visited.Add(node.Key);
        var cause = snapshot.Append(StoryPlaythroughEventKind.NodeEntered, causedBy, node.Key);
        FireReactions(story, snapshot, StoryTrigger.EnterNode, cause, null);
        ApplyPendingGoto(story, snapshot, cause, depth + 1);
    }

    private static void ApplyPendingGoto(Story story, StoryPlaySnapshot snapshot, Guid? causedBy, int depth)
    {
        if (snapshot.PendingEnd) {
            EndPlaythrough(snapshot, causedBy);
            snapshot.PendingGoto = null;
            return;
        }

        if (string.IsNullOrWhiteSpace(snapshot.PendingGoto))
            return;

        var target = snapshot.PendingGoto;
        snapshot.PendingGoto = null;
        if (string.Equals(target, snapshot.CurrentNodeKey, StringComparison.Ordinal))
            return;

        var leaveCause = snapshot.Append(StoryPlaythroughEventKind.TriggerFired, causedBy, snapshot.CurrentNodeKey);
        FireReactions(story, snapshot, StoryTrigger.LeaveNode, leaveCause, null);
        EnterNode(story, snapshot, target!, causedBy, depth + 1);
    }

    private static void ApplyEffects(Story story, StoryPlaySnapshot snapshot, IReadOnlyList<StoryEffect> effects, Guid? causedBy, int depth)
    {
        if (depth > MaxChainDepth)
            throw new StoryException("Action invoke chain exceeded the maximum depth.");

        foreach (var effect in effects.OrderBy(e => e.SortOrder)) {
            switch (effect.Kind) {
                case StoryEffectKind.Set:
                    SetVariable(story, snapshot, effect.VariableKey, effect.Value, causedBy);
                    break;
                case StoryEffectKind.Unset:
                    if (!string.IsNullOrWhiteSpace(effect.VariableKey) && snapshot.Variables.TryGetValue(effect.VariableKey, out var oldUnset)) {
                        snapshot.Variables.Remove(effect.VariableKey);
                        snapshot.Append(StoryPlaythroughEventKind.VariableChanged, causedBy, variableKey: effect.VariableKey, oldValue: oldUnset, newValue: null);
                        FireReactions(story, snapshot, StoryTrigger.VariableChanged, causedBy, effect.VariableKey);
                    }

                    break;
                case StoryEffectKind.Increment:
                    IncrementVariable(story, snapshot, effect.VariableKey, effect.Value, causedBy);
                    break;
                case StoryEffectKind.GotoNode:
                    snapshot.PendingGoto = effect.TargetNodeKey;
                    break;
                case StoryEffectKind.EndPlaythrough:
                    snapshot.PendingEnd = true;
                    break;
                case StoryEffectKind.InvokeAction:
                    var action = story.Actions.FirstOrDefault(a => string.Equals(a.Key, effect.InvokeActionKey, StringComparison.Ordinal)) ??
                        throw new StoryException($"Unknown action '{effect.InvokeActionKey}'.");

                    var invokeCause = snapshot.Append(StoryPlaythroughEventKind.ActionInvoked, causedBy, actionKey: action.Key);
                    ApplyEffects(story, snapshot, action.Effects, invokeCause, depth + 1);
                    break;
            }
        }
    }

    private static void FireReactions(Story story, StoryPlaySnapshot snapshot, StoryTrigger trigger, Guid? causedBy, string? changedVariable)
    {
        foreach (var reaction in story.Reactions.Where(r => r.Trigger == trigger).OrderBy(r => r.Key, StringComparer.Ordinal)) {
            if (reaction.Once && snapshot.FiredOnce.Contains(reaction.Key))
                continue;

            if (!string.IsNullOrWhiteSpace(reaction.ScopeNodeKey) && !string.Equals(reaction.ScopeNodeKey, snapshot.CurrentNodeKey, StringComparison.Ordinal))
                continue;

            if (trigger == StoryTrigger.VariableChanged && !string.IsNullOrWhiteSpace(reaction.ScopeVariableKey) &&
                !string.Equals(reaction.ScopeVariableKey, changedVariable, StringComparison.Ordinal))
                continue;

            if (!StoryWhere.Matches(reaction.When, snapshot.Variables, snapshot.Chose, snapshot.Visited))
                continue;

            if (reaction.Once)
                snapshot.FiredOnce.Add(reaction.Key);

            var cause = snapshot.Append(StoryPlaythroughEventKind.ReactionFired, causedBy, snapshot.CurrentNodeKey, reactionKey: reaction.Key);
            ApplyEffects(story, snapshot, reaction.Effects, cause, 0);
        }
    }

    private static void SetVariable(Story story, StoryPlaySnapshot snapshot, string? key, string? value, Guid? causedBy)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new StoryException("Set effect requires a variable key.");

        snapshot.Variables.TryGetValue(key, out var old);
        if (string.Equals(old, value, StringComparison.Ordinal))
            return;

        snapshot.Variables[key] = value;
        var change = snapshot.Append(StoryPlaythroughEventKind.VariableChanged, causedBy, variableKey: key, oldValue: old, newValue: value);
        FireReactions(story, snapshot, StoryTrigger.VariableChanged, change, key);
    }

    private static void IncrementVariable(Story story, StoryPlaySnapshot snapshot, string? key, string? byText, Guid? causedBy)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new StoryException("Increment effect requires a variable key.");

        snapshot.Variables.TryGetValue(key, out var old);
        var current = 0;
        if (!string.IsNullOrWhiteSpace(old) && !int.TryParse(old, out current))
            throw new StoryException($"Cannot increment variable '{key}': its value '{old}' is not an integer.");

        var by = 1;
        if (!string.IsNullOrWhiteSpace(byText) && !int.TryParse(byText, out by))
            throw new StoryException($"Cannot increment variable '{key}' by '{byText}': the amount is not an integer.");

        var next = (current + by).ToString();
        snapshot.Variables[key] = next;
        var change = snapshot.Append(StoryPlaythroughEventKind.VariableChanged, causedBy, variableKey: key, oldValue: old, newValue: next);
        FireReactions(story, snapshot, StoryTrigger.VariableChanged, change, key);
    }

    private static void EndPlaythrough(StoryPlaySnapshot snapshot, Guid? causedBy)
    {
        if (snapshot.Status == StoryPlaythroughStatus.Completed)
            return;

        snapshot.Status = StoryPlaythroughStatus.Completed;
        snapshot.Append(StoryPlaythroughEventKind.PlaythroughEnded, causedBy, snapshot.CurrentNodeKey);
    }

    private static StoryNode RequireCurrentNode(Story story, StoryPlaySnapshot snapshot)
    {
        if (string.IsNullOrWhiteSpace(snapshot.CurrentNodeKey))
            throw new StoryException("Playthrough has no current node.");

        return story.Nodes.FirstOrDefault(n => string.Equals(n.Key, snapshot.CurrentNodeKey, StringComparison.Ordinal)) ??
            throw new StoryException($"Unknown current node '{snapshot.CurrentNodeKey}'.");
    }

    private static void EnsureInProgress(StoryPlaySnapshot snapshot)
    {
        if (snapshot.Status != StoryPlaythroughStatus.InProgress)
            throw new StoryException($"Playthrough is {snapshot.Status}.");
    }

    /// <summary>Walks consequence pointers for <paramref name="variableKey" /> (the variable-changed event plus its cause chain).</summary>
    public static IReadOnlyList<StoryPlaythroughEvent> GetCauses(IReadOnlyList<StoryPlaythroughEvent> events, string variableKey)
    {
        ArgumentHelpers.ThrowIfNull(events);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(variableKey);
        var byId = events.ToDictionary(e => e.Id);
        var hits = events.Where(e => e.Kind == StoryPlaythroughEventKind.VariableChanged && string.Equals(e.VariableKey, variableKey, StringComparison.Ordinal)).ToList();
        var chain = new List<StoryPlaythroughEvent>();
        var seen = new HashSet<Guid>();
        foreach (var hit in hits) {
            var current = hit;
            while (true) {
                if (!seen.Add(current.Id))
                    break;

                chain.Add(current);
                if (current.CausedByEventId is not Guid parentId || !byId.TryGetValue(parentId, out var parent))
                    break;

                current = parent;
            }
        }

        return chain.OrderBy(e => e.Sequence).ToArray();
    }
}

/// <summary>In-memory playthrough used by <see cref="StoryPlayEngine" />.</summary>
public sealed class StoryPlaySnapshot
{
    public string? CurrentNodeKey { get; set; }

    public StoryPlaythroughStatus Status { get; set; }

    public Dictionary<string, string?> Variables { get; } = new(StringComparer.Ordinal);

    public HashSet<string> Chose { get; } = new(StringComparer.Ordinal);

    public HashSet<string> Visited { get; } = new(StringComparer.Ordinal);

    public HashSet<string> FiredOnce { get; } = new(StringComparer.Ordinal);

    public List<StoryPlaythroughEvent> Events { get; } = [];

    internal string? PendingGoto { get; set; }

    internal bool PendingEnd { get; set; }

    internal Guid Append(
        StoryPlaythroughEventKind kind,
        Guid? causedBy,
        string? nodeKey = null,
        string? choiceKey = null,
        string? reactionKey = null,
        string? actionKey = null,
        string? variableKey = null,
        string? oldValue = null,
        string? newValue = null)
    {
        var id = LyoGuid.CreateV7();
        Events.Add(
            new() {
                Id = id,
                Sequence = Events.Count + 1,
                Kind = kind,
                NodeKey = nodeKey,
                ChoiceKey = choiceKey,
                ReactionKey = reactionKey,
                ActionKey = actionKey,
                VariableKey = variableKey,
                OldValue = oldValue,
                NewValue = newValue,
                CausedByEventId = causedBy,
                OccurredAt = DateTime.UtcNow
            });

        return id;
    }
}