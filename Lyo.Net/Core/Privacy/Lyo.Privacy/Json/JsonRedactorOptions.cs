using System.Diagnostics;
using Lyo.Privacy.Abstractions;
using Lyo.Privacy.Enums;
using Lyo.Privacy.Text;

namespace Lyo.Privacy.Json;

/// <summary>Options for <see cref="JsonRedactor" />.</summary>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public sealed class JsonRedactorOptions
{
    public string Placeholder { get; set; } = "[redacted]";

    /// <summary>Used for <see cref="JsonKeyRedactionStrategy.HashStable" />.</summary>
    public byte[]? StableHashSalt { get; set; }

    /// <summary>Case-insensitive property names mapped to strategy.</summary>
    public IReadOnlyDictionary<string, JsonKeyRedactionStrategy> SensitiveKeys { get; set; } = DefaultSensitiveKeys;

    /// <summary>When true, runs <see cref="ITextRedactor" /> on every string value. Requires passing <see cref="ITextRedactor" /> to the <see cref="JsonRedactor" /> constructor.</summary>
    public bool ApplyTextRulesToAllStringValues { get; set; }

    /// <summary>Optional stable label for metrics (tag <c>policy</c>) and <see cref="RedactionResult.PolicyName" />.</summary>
    public string? PolicyName { get; set; }

    private string DebuggerDisplay => $"JsonRedactorOptions(policy={PolicyName ?? "∅"}, keys={SensitiveKeys.Count}, applyText={ApplyTextRulesToAllStringValues})";

    public static IReadOnlyDictionary<string, JsonKeyRedactionStrategy> DefaultSensitiveKeys { get; } =
        new Dictionary<string, JsonKeyRedactionStrategy>(StringComparer.OrdinalIgnoreCase) {
            ["password"] = JsonKeyRedactionStrategy.HashStable,
            ["secret"] = JsonKeyRedactionStrategy.HashStable,
            ["token"] = JsonKeyRedactionStrategy.HashStable,
            ["access_token"] = JsonKeyRedactionStrategy.HashStable,
            ["refresh_token"] = JsonKeyRedactionStrategy.HashStable,
            ["ssn"] = JsonKeyRedactionStrategy.Placeholder,
            ["mrn"] = JsonKeyRedactionStrategy.Placeholder,
            ["dob"] = JsonKeyRedactionStrategy.Placeholder,
            ["email"] = JsonKeyRedactionStrategy.Placeholder
        };

    /// <inheritdoc />
    public override string ToString()
        => $"JsonRedactorOptions {{ PolicyName = {PolicyName ?? "null"}, ApplyTextRulesToAllStringValues = {ApplyTextRulesToAllStringValues}, SensitiveKeys.Count = {SensitiveKeys.Count} }}";
}