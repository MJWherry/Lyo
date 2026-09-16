using System.Globalization;
using System.Text;
using Lyo.Exceptions;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Enums;
using Lyo.Story;
using Lyo.Story.Enums;

namespace Lyo.Story.Web.Components;

/// <summary>Renders a draft as a <c>StoryBuilder</c> snippet in the shape of <c>StorySamples.TheHeist</c>.</summary>
public static class StoryGraphCsharp
{
    /// <summary>Returns C# that builds <paramref name="draft" />.</summary>
    public static string Format(StoryDraft draft)
    {
        ArgumentHelpers.ThrowIfNull(draft);
        var builder = new StringBuilder();
        builder.Append("var story = StoryBuilder.New(").Append(Quote(draft.Title)).AppendLine(")");
        if (!string.IsNullOrWhiteSpace(draft.Slug))
            builder.Append("    .Slug(").Append(Quote(draft.Slug)).AppendLine(")");
        if (!string.IsNullOrWhiteSpace(draft.Description))
            builder.Append("    .Description(").Append(Quote(draft.Description)).AppendLine(")");
        if (!string.IsNullOrWhiteSpace(draft.Language))
            builder.Append("    .Language(").Append(Quote(draft.Language)).AppendLine(")");
        foreach (var variable in draft.Variables)
            builder.Append("    .Variable(").Append(Quote(variable.Key)).Append(", StoryVariableType.").Append(variable.Type).Append(", ").Append(DefaultLiteral(variable)).AppendLine(")");
        foreach (var scene in draft.Scenes)
            builder.Append("    .Scene(").Append(Quote(scene.Key)).Append(", ").Append(Quote(scene.Title)).AppendLine(")");
        foreach (var action in draft.Actions) {
            builder.Append("    .Action(").Append(Quote(action.Key)).AppendLine(", a => a");
            AppendDo(builder, "        ", action.Effects);
            builder.AppendLine(")");
        }

        var start = draft.StartNodeKey;
        foreach (var node in draft.Nodes.OrderBy(node => node.Key == start ? 0 : 1))
            AppendNode(builder, node, node.Key == start);

        foreach (var reaction in draft.Reactions.Where(reaction => string.IsNullOrWhiteSpace(reaction.ScopeNodeKey)))
            AppendReaction(builder, "    .React", reaction);

        builder.Append("    .Build();");
        return builder.ToString();
    }

    private static void AppendNode(StringBuilder builder, StoryNodeDraft node, bool start)
    {
        builder.Append("    .").Append(start ? "StartNode" : "Node").Append('(').Append(Quote(node.Key)).AppendLine(", n => n");
        if (!string.IsNullOrWhiteSpace(node.Title))
            builder.Append("        .Titled(").Append(Quote(node.Title)).AppendLine(")");
        if (!string.IsNullOrWhiteSpace(node.SceneKey))
            builder.Append("        .InScene(").Append(Quote(node.SceneKey)).AppendLine(")");
        if (node.IsEnding)
            builder.AppendLine("        .Ending()");
        if (node.PresentationKind == StoryPresentationKind.Text && node.Body is not null)
            builder.Append("        .Text(").Append(Quote(node.Body)).AppendLine(")");
        if (node.PresentationKind == StoryPresentationKind.Cutscene && !string.IsNullOrWhiteSpace(node.MediaRef))
            builder.Append("        .Cutscene(").Append(Quote(node.MediaRef)).AppendLine(")");
        foreach (var choice in node.Choices)
            AppendChoice(builder, choice);
        builder.AppendLine(")");
    }

    private static void AppendChoice(StringBuilder builder, StoryChoiceDraft choice)
    {
        builder.Append("        .Choice(").Append(Quote(choice.Key)).Append(", ").Append(Quote(choice.Prompt ?? choice.Key)).AppendLine(", c => c");
        AppendWhen(builder, "            ", ".WhenAll", ".WhenOneOf", choice.EnabledWhen);
        AppendWhen(builder, "            ", ".VisibleWhenAll", ".VisibleWhenOneOf", choice.VisibleWhen);
        if (choice.Effects.Count > 0)
            AppendDo(builder, "            ", choice.Effects);
        builder.AppendLine(")");
    }

    private static void AppendReaction(StringBuilder builder, string method, StoryReactionDraft reaction)
    {
        builder.Append(method).Append('(').Append(Quote(reaction.Key)).AppendLine(", r => r");
        builder.Append("        .On(StoryTrigger.").Append(reaction.Trigger);
        var scope = reaction.ScopeVariableKey ?? reaction.ScopeNodeKey;
        if (!string.IsNullOrWhiteSpace(scope))
            builder.Append(", ").Append(Quote(scope));
        builder.AppendLine(")");
        if (reaction.Once)
            builder.AppendLine("        .Once()");
        AppendWhen(builder, "        ", ".WhenAll", ".WhenOneOf", reaction.When);
        if (reaction.Effects.Count > 0)
            AppendDo(builder, "        ", reaction.Effects);
        builder.AppendLine(")");
    }

    private static void AppendDo(StringBuilder builder, string indent, IReadOnlyList<StoryEffectDraft> effects)
    {
        if (effects.Count == 0)
            return;
        builder.Append(indent).AppendLine(".Do(d => d");
        foreach (var effect in effects)
            builder.Append(indent).Append("    ").AppendLine(EffectCall(effect));
        builder.Append(indent).Append(')');
    }

    private static string EffectCall(StoryEffectDraft effect)
        => effect.Kind switch {
            StoryEffectKind.Set => ".Set(" + Quote(effect.VariableKey) + ", " + ValueLiteral(effect.Value) + ")",
            StoryEffectKind.Unset => ".Unset(" + Quote(effect.VariableKey) + ")",
            StoryEffectKind.Increment => ".Increment(" + Quote(effect.VariableKey) + IncrementBy(effect.Value) + ")",
            StoryEffectKind.GotoNode => ".GoesTo(" + Quote(effect.TargetNodeKey) + ")",
            StoryEffectKind.EndPlaythrough => ".End()",
            StoryEffectKind.InvokeAction => ".Invoke(" + Quote(effect.InvokeActionKey) + ")",
            var _ => ".End()"
        };

    private static string IncrementBy(string? value)
        => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var by) && by != 1 ? ", " + by.ToString(CultureInfo.InvariantCulture) : "";

    private static void AppendWhen(StringBuilder builder, string indent, string all, string one, WhereClause? clause)
    {
        if (clause is null)
            return;
        var method = clause is GroupClause { Operator: GroupOperatorEnum.Or } ? one : all;
        builder.Append(indent).Append(method).Append("(w => w");
        if (clause is GroupClause group) {
            foreach (var child in group.Children)
                AppendLeaf(builder, child);
        }
        else
            AppendLeaf(builder, clause);

        builder.AppendLine(")");
    }

    private static void AppendLeaf(StringBuilder builder, WhereClause clause)
    {
        if (clause is not ConditionClause condition || condition.Comparison != ComparisonOperatorEnum.Equals)
            return;
        var field = condition.Field ?? "";
        if (field.StartsWith(StoryConditionFields.VarPrefix, StringComparison.Ordinal)) {
            var key = Quote(field[StoryConditionFields.VarPrefix.Length..]);
            if (IsTrue(condition.Value))
                builder.AppendLine().Append("                .Flag(").Append(key).Append(')');
            else if (IsFalse(condition.Value))
                builder.AppendLine().Append("                .NotFlag(").Append(key).Append(')');
            else
                builder.AppendLine().Append("                .Eq(").Append(key).Append(", ").Append(ValueLiteral(condition.Value)).Append(')');
            return;
        }

        if (field.StartsWith(StoryConditionFields.ChosePrefix, StringComparison.Ordinal) && IsTrue(condition.Value))
            builder.AppendLine().Append("                .Chose(").Append(Quote(field[StoryConditionFields.ChosePrefix.Length..])).Append(')');
        else if (field.StartsWith(StoryConditionFields.VisitedPrefix, StringComparison.Ordinal) && IsTrue(condition.Value))
            builder.AppendLine().Append("                .Visited(").Append(Quote(field[StoryConditionFields.VisitedPrefix.Length..])).Append(')');
    }

    private static string DefaultLiteral(StoryVariableDraft variable)
        => variable.Type switch {
            StoryVariableType.Boolean => IsTrue(variable.DefaultValue) ? "true" : "false",
            StoryVariableType.Integer => int.TryParse(variable.DefaultValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number)
                ? number.ToString(CultureInfo.InvariantCulture)
                : "0",
            var _ => Quote(variable.DefaultValue)
        };

    private static string ValueLiteral(object? value)
    {
        if (value is null)
            return "null";
        if (IsTrue(value))
            return "true";
        if (IsFalse(value))
            return "false";
        if (value is int or long)
            return Convert.ToString(value, CultureInfo.InvariantCulture) ?? "0";
        if (int.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) && value is not string)
            return number.ToString(CultureInfo.InvariantCulture);
        return Quote(Convert.ToString(value, CultureInfo.InvariantCulture));
    }

    private static bool IsTrue(object? value)
        => value is true || string.Equals(Convert.ToString(value, CultureInfo.InvariantCulture), "true", StringComparison.OrdinalIgnoreCase);

    private static bool IsFalse(object? value)
        => value is false || string.Equals(Convert.ToString(value, CultureInfo.InvariantCulture), "false", StringComparison.OrdinalIgnoreCase);

    private static string Quote(string? value)
        => "\"" + (value ?? "").Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";
}
