using Lyo.Common.Metadata.Records;
using Lyo.Exceptions;
using Lyo.Story;
using Lyo.Story.Enums;

namespace Lyo.Story.Web.Components;

/// <summary>Field presets for story conditions: <c>var.*</c>, <c>chose.*</c>, and <c>visited.*</c>.</summary>
public static class StoryConditionCatalog
{
    /// <summary>One filter field per declared variable, choice, and node.</summary>
    public static IReadOnlyList<FilterPropertyDefinition> Fields(StoryDraft draft)
    {
        ArgumentHelpers.ThrowIfNull(draft);
        List<FilterPropertyDefinition> fields = [];
        foreach (var variable in draft.Variables.Where(variable => !string.IsNullOrWhiteSpace(variable.Key)))
            fields.Add(new(StoryConditionFields.Var(variable.Key), variable.Name ?? variable.Key, TypeOf(variable.Type)));
        foreach (var node in draft.Nodes.Where(node => !string.IsNullOrWhiteSpace(node.Key))) {
            fields.Add(new(StoryConditionFields.Visited(node.Key), "Visited " + node.Key, LyoTypeInfo.Bool));
            foreach (var choice in node.Choices.Where(choice => !string.IsNullOrWhiteSpace(choice.Key)))
                fields.Add(new(StoryConditionFields.Chose(choice.Key), "Chose " + choice.Key, LyoTypeInfo.Bool));
        }

        return fields;
    }

    private static LyoTypeInfo TypeOf(StoryVariableType type)
        => type switch {
            StoryVariableType.Boolean => LyoTypeInfo.Bool,
            StoryVariableType.Integer => LyoTypeInfo.Int,
            var _ => LyoTypeInfo.String
        };
}
