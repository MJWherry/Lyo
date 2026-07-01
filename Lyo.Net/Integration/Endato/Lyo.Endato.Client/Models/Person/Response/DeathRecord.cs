using System.Diagnostics;

namespace Lyo.Endato.Client.Models.Person.Response;

/// <summary>Deceased indicator and source metadata for a matched person.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record DeathRecord(bool IsDeceased, string? SourceSummary = null)
{
    public override string ToString() => $"DeathRecord: IsDeceased={IsDeceased}, SourceSummary='{SourceSummary}'";
}