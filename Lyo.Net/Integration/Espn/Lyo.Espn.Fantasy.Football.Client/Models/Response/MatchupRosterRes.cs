using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lyo.Espn.Fantasy.Football.Client.Models.Response;

/// <summary>Roster ESPN attached to one side of a scoreboard matchup.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record MatchupRosterRes(IReadOnlyList<RosterEntryRes>? Entries)
{
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }

    public override string ToString() => $"Entries={Entries?.Count ?? 0}";
}
