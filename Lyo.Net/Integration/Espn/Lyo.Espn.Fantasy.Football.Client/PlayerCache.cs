using System.Collections.Concurrent;
using Lyo.Espn.Fantasy.Football.Client.Models.Response;
using Lyo.Exceptions;

namespace Lyo.Espn.Fantasy.Football.Client;

/// <summary>Process-local player cards keyed by season and ESPN player id. Player reads write through it; a second lookup of the same ids does not call ESPN.</summary>
public sealed class PlayerCache
{
    private readonly ConcurrentDictionary<(int SeasonId, int PlayerId), PlayerInfoItemRes> _players = new();

    /// <summary>Stores cards for <paramref name="seasonId" />. Later reads replace earlier cards for the same id.</summary>
    public void Remember(int seasonId, IEnumerable<PlayerInfoItemRes>? players)
    {
        if (players == null)
            return;

        foreach (var player in players)
            _players[(seasonId, player.Id)] = player;
    }

    /// <summary>Returns the card stored for this season and player, when a read has already filled it.</summary>
    public bool TryGet(int seasonId, int playerId, out PlayerInfoItemRes? player) => _players.TryGetValue((seasonId, playerId), out player);

    /// <summary>Ids in <paramref name="playerIds" /> that are not stored for <paramref name="seasonId" />.</summary>
    public IReadOnlyList<int> Missing(int seasonId, IEnumerable<int> playerIds)
    {
        ArgumentHelpers.ThrowIfNull(playerIds);
        return playerIds.Where(id => !_players.ContainsKey((seasonId, id))).Distinct().ToArray();
    }
}
