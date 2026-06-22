using System.Diagnostics;

namespace Lyo.Endato.Client.Models;

/// <summary>API error payload shared by Person Search and Contact Enrichment responses.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record EndatoErrorDetails(
    IReadOnlyList<string>? InputErrors,
    IReadOnlyList<string>? Warnings)
{
    public override string ToString()
    {
        var inputCount = InputErrors?.Count ?? 0;
        var warningCount = Warnings?.Count ?? 0;
        return $"EndatoErrorDetails: InputErrors={inputCount}, Warnings={warningCount}";
    }
}
