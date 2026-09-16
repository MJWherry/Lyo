using System.Text.Json;
using Lyo.Json;
using Lyo.Exceptions;
using Lyo.StateMachine.Models;

namespace Lyo.StateMachine;

/// <summary>JSON for <see cref="StateMachineDocument" /> using <c>LyoJsonSerializerOptions</c> (camelCase, camelCase string enums, nulls omitted).</summary>
public static class StateMachineDocumentJson
{
    private static readonly JsonSerializerOptions Options = LyoJsonSerializerOptions.Create();

    /// <summary>Serializes a document.</summary>
    public static string Serialize(StateMachineDocument document)
    {
        ArgumentHelpers.ThrowIfNull(document);
        return JsonSerializer.Serialize(document, Options);
    }

    /// <summary>Deserializes a document.</summary>
    /// <exception cref="JsonException">The JSON is invalid or is the literal <c>null</c>.</exception>
    public static StateMachineDocument Deserialize(string json)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(json);
        return JsonSerializer.Deserialize<StateMachineDocument>(json, Options) ?? throw new JsonException("State machine document JSON was null.");
    }
}

/// <summary>Checks a new document revision against the previous one before publishing.</summary>
public static class StateMachineDocumentCompatibility
{
    /// <summary>
    /// States in <paramref name="previous" /> that <paramref name="updated" /> no longer declares, in their previous order. Saved instances in those states would fail
    /// to fire under the new revision; migrate them first.
    /// </summary>
    public static IReadOnlyList<string> FindRemovedStates(StateMachineDocument previous, StateMachineDocument updated)
    {
        ArgumentHelpers.ThrowIfNull(previous);
        ArgumentHelpers.ThrowIfNull(updated);
        var kept = new HashSet<string>(updated.States.Select(s => s.Name), StringComparer.Ordinal);
        return previous.States.Select(s => s.Name).Where(n => !kept.Contains(n)).ToArray();
    }
}
