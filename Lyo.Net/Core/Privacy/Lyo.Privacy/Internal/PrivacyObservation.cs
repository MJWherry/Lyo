using Lyo.Privacy.Enums;

namespace Lyo.Privacy.Internal;

/// <summary>Stable metric tag sets for redaction (no secret material).</summary>
internal static class PrivacyObservation
{
    public static IEnumerable<(string Key, string Value)>? TagsForPolicy(string? policyName) => string.IsNullOrEmpty(policyName) ? null : new[] { ("policy", policyName!) };

    public static IEnumerable<(string Key, string Value)> TagsForKind(RedactionKind kind, string? policyName)
        => string.IsNullOrEmpty(policyName) ? [("kind", kind.ToString())] : [("kind", kind.ToString()), ("policy", policyName!)];
}