using Lyo.Query.Models.Common;
using Lyo.Query.Models.Enums;
using Lyo.Story.Enums;

namespace Lyo.Story.Web.Components.Tests;

internal static class StoryDraftFixtures
{
    internal static StoryDraft Goto(string target)
        => new() {
            Title = "Door",
            Slug = "door",
            StartNodeKey = "vault_door",
            Nodes = [
                new() { Key = "vault_door", Choices = [new() { Key = "go", Effects = [new() { Kind = StoryEffectKind.GotoNode, TargetNodeKey = target }] }] },
                new() { Key = "inside" }
            ]
        };

    internal static StoryDraft HeistShape()
        => new() {
            Title = "The Heist",
            Slug = "the-heist",
            StartNodeKey = "vault_door",
            Nodes = [
                new() { Key = "vault_door", Choices = [new() { Key = "pick_lock", Effects = [new() { Kind = StoryEffectKind.GotoNode, TargetNodeKey = "inside" }] }] },
                new() { Key = "inside" }, new() { Key = "loose" }
            ]
        };

    internal static StoryDraft Cycle()
        => new() {
            Title = "Loop",
            StartNodeKey = "a",
            Nodes = [
                new() { Key = "a", Choices = [new() { Key = "to_b", Effects = [new() { Kind = StoryEffectKind.GotoNode, TargetNodeKey = "b" }] }] },
                new() { Key = "b", Choices = [new() { Key = "to_a", Effects = [new() { Kind = StoryEffectKind.GotoNode, TargetNodeKey = "a" }] }] }
            ]
        };

    internal static StoryDraft Snippet()
        => new() {
            Title = "The Heist",
            Slug = "the-heist",
            StartNodeKey = "vault_door",
            Variables = [new() { Key = "alarm", Type = StoryVariableType.Boolean, DefaultValue = "false" }],
            Nodes = [
                new() {
                    Key = "vault_door",
                    Choices = [
                        new() {
                            Key = "pick_lock",
                            Prompt = "Pick the lock",
                            EnabledWhen = new GroupClause(
                                GroupOperatorEnum.And, [new ConditionClause(StoryConditionFields.Var("has_lockpick"), ComparisonOperatorEnum.Equals, true)])
                        }
                    ]
                }
            ]
        };
}