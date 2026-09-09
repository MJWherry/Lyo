using System.Diagnostics;

namespace Lyo.Endato.Client.Models.Person.Response;

/// <summary>A matched person deceased indicator and source metadata.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record DeathRecord(bool IsDeceased, string? SourceSummary = null)
{
    public override string ToString() => $"DeathRecord: IsDeceased={IsDeceased}, SourceSummary='{SourceSummary}'";
}