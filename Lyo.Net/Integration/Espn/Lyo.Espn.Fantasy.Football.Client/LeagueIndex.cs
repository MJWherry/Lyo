using Lyo.Espn.Fantasy.Football.Client.Models.Response;
using Lyo.Exceptions;

namespace Lyo.Espn.Fantasy.Football.Client;

/// <summary>
/// In-memory directory of one league. Ids on the wire stay ids; <see cref="Member" />, <see cref="Team" />, and <see cref="TeamByOwner" /> turn them into the records from
/// the last <see cref="FantasyFootballClient.Remember" /> call. Author <c>LM</c> is the league manager, not a member.
/// </summary>
public sealed class LeagueIndex
{
    private readonly Dictionary<string, MemberRes> _members;
    private readonly Dictionary<string, TeamRes> _owners;
    private readonly Dictionary<int, TeamRes> _teams;

    /// <summary>Scoring period on the league this index was built from.</summary>
    public int ScoringPeriodId { get; }

    /// <summary>Indexes <paramref name="league" /> by member id, team id, and primary owner.</summary>
    public LeagueIndex(LeagueRes league)
    {
        ArgumentHelpers.ThrowIfNull(league);
        ScoringPeriodId = league.ScoringPeriodId;
        _members = new(StringComparer.OrdinalIgnoreCase);
        foreach (var member in league.Members ?? []) {
            if (!string.IsNullOrWhiteSpace(member.Id))
                _members[member.Id] = member;
        }

        _teams = new();
        _owners = new(StringComparer.OrdinalIgnoreCase);
        foreach (var team in league.Teams ?? []) {
            _teams[team.Id] = team;
            if (team.PrimaryOwner is string owner && owner.Trim().Length > 0)
                _owners[owner] = team;
        }
    }

    /// <summary>True when <paramref name="author" /> is the league-manager marker <c>LM</c>.</summary>
    public static bool IsLeagueManager(string? author) => string.Equals(author, "LM", StringComparison.Ordinal);

    /// <summary>Member with this SWID, ignoring case. Returns null for a blank id, <c>LM</c>, or an unknown member.</summary>
    public MemberRes? Member(string? id)
    {
        if (id is not string key || string.IsNullOrWhiteSpace(key))
            return null;

        return _members.TryGetValue(key, out var member) ? member : null;
    }

    /// <summary>Team with this ESPN team id, or null when the league has no such team.</summary>
    public TeamRes? Team(int teamId) => _teams.TryGetValue(teamId, out var team) ? team : null;

    /// <summary>Team whose <see cref="TeamRes.PrimaryOwner" /> is this SWID, ignoring case.</summary>
    public TeamRes? TeamByOwner(string? swid)
    {
        if (swid is not string key || string.IsNullOrWhiteSpace(key))
            return null;

        return _owners.TryGetValue(key, out var team) ? team : null;
    }

    /// <summary>The one member whose display name or "First Last" matches <paramref name="name" />, ignoring case. Zero or several matches throw.</summary>
    public MemberRes FindMember(string name)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(name);
        var matches = _members.Values.Where(member => MemberNameMatches(member, name)).ToArray();
        OperationHelpers.ThrowIf(matches.Length == 0, $"No league member matches {name}.");
        OperationHelpers.ThrowIf(matches.Length > 1, $"More than one league member matches {name}.");
        return matches[0];
    }

    /// <summary>The one team whose name, abbreviation, or "Location Nickname" matches <paramref name="name" />, ignoring case. Zero or several matches throw.</summary>
    public TeamRes FindTeam(string name)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(name);
        var matches = _teams.Values.Where(team => TeamNameMatches(team, name)).ToArray();
        OperationHelpers.ThrowIf(matches.Length == 0, $"No league team matches {name}.");
        OperationHelpers.ThrowIf(matches.Length > 1, $"More than one league team matches {name}.");
        return matches[0];
    }

    private static bool MemberNameMatches(MemberRes member, string name)
    {
        if (string.Equals(member.DisplayName, name, StringComparison.OrdinalIgnoreCase))
            return true;

        var full = $"{member.FirstName} {member.LastName}".Trim();
        return full.Length > 0 && string.Equals(full, name, StringComparison.OrdinalIgnoreCase);
    }

    private static bool TeamNameMatches(TeamRes team, string name)
    {
        if (string.Equals(team.Name, name, StringComparison.OrdinalIgnoreCase) || string.Equals(team.Abbrev, name, StringComparison.OrdinalIgnoreCase))
            return true;

        var composed = $"{team.Location} {team.Nickname}".Trim();
        return composed.Length > 0 && string.Equals(composed, name, StringComparison.OrdinalIgnoreCase);
    }
}