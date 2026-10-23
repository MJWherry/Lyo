using Lyo.Exceptions;

namespace Lyo.Story;

/// <summary>Field prefixes on persisted <c>WhereClause</c> leaves so the play engine can evaluate against a bag, not a CLR entity.</summary>
public static class StoryConditionFields
{
    public const string VarPrefix = "var.";

    public const string ChosePrefix = "chose.";

    public const string VisitedPrefix = "visited.";

    /// <summary>Builds <c>var.{key}</c> for a declared story variable.</summary>
    public static string Var(string key)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key);
        return VarPrefix + key;
    }

    /// <summary>Builds <c>chose.{choiceKey}</c> for a choice taken on this playthrough.</summary>
    public static string Chose(string choiceKey)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(choiceKey);
        return ChosePrefix + choiceKey;
    }

    /// <summary>Builds <c>visited.{nodeKey}</c> for a node entered on this playthrough.</summary>
    public static string Visited(string nodeKey)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(nodeKey);
        return VisitedPrefix + nodeKey;
    }
}