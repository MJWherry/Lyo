namespace Lyo.FantasyFootball;

/// <summary>Persistence for a fantasy league's chats and trades. ESPN ids are the upsert key.</summary>
public interface IFantasyFootballStore
{
    /// <summary>Inserts or updates the league, its members, its teams, and the roster for <see cref="FantasyLeagueRecord.ScoringPeriodId" />.</summary>
    Task<FantasyLeagueRecord> SaveLeagueAsync(FantasyLeagueRecord league, CancellationToken ct = default);

    /// <summary>Loads a league by ESPN league id and season. Null when it has not been saved.</summary>
    Task<FantasyLeagueRecord?> GetLeagueAsync(int espnLeagueId, int seasonId, CancellationToken ct = default);

    /// <summary>Lists chat topics for a league. Empty when the league has not been saved.</summary>
    Task<IReadOnlyList<FantasyConversationRecord>> ListConversationsAsync(int espnLeagueId, int seasonId, CancellationToken ct = default);

    /// <summary>Inserts or updates topics. Members are matched by SWID on the saved league.</summary>
    Task SaveConversationsAsync(int espnLeagueId, int seasonId, IReadOnlyList<FantasyConversationRecord> conversations, CancellationToken ct = default);

    /// <summary>Lists messages in one topic, oldest first.</summary>
    Task<IReadOnlyList<FantasyMessageRecord>> ListMessagesAsync(int espnLeagueId, int seasonId, string espnTopicId, CancellationToken ct = default);

    /// <summary>Inserts or updates messages in one topic. Creates the topic row when refresh has not stored it yet.</summary>
    Task SaveMessagesAsync(int espnLeagueId, int seasonId, string espnTopicId, IReadOnlyList<FantasyMessageRecord> messages, CancellationToken ct = default);

    /// <summary>Lists trades. <paramref name="pending" /> null returns every trade, true only open ones, false only the rest.</summary>
    Task<IReadOnlyList<FantasyTradeRecord>> ListTradesAsync(int espnLeagueId, int seasonId, bool? pending = null, CancellationToken ct = default);

    /// <summary>Inserts or updates trades and replaces each trade's items.</summary>
    Task SaveTradesAsync(int espnLeagueId, int seasonId, IReadOnlyList<FantasyTradeRecord> trades, CancellationToken ct = default);
}
