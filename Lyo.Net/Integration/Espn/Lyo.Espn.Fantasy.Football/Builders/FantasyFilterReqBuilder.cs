using System.Diagnostics;
using Lyo.Espn.Fantasy.Football.Models.Request;
using Lyo.Exceptions;

namespace Lyo.Espn.Fantasy.Football.Builders;

[DebuggerDisplay("{ToString(),nq}")]
public sealed class FantasyFilterReqBuilder
{
    private readonly Dictionary<string, TopicTypeFilterReq> _topicsByType = [];
    private PlayerFilterReq? _players;
    private TopicsFilterReq? _topics;
    private TransactionsFilterReq? _transactions;

    /// <summary>Sets the player ids for a player-card request.</summary>
    public FantasyFilterReqBuilder WithPlayerIds(IEnumerable<int> playerIds)
    {
        ArgumentHelpers.ThrowIfNullOrEmpty(playerIds, nameof(playerIds));
        var ids = playerIds.Distinct().ToArray();
        _players = (_players ?? new()) with { FilterIds = new() { Value = ids } };
        return this;
    }

    /// <summary>Sets the scoring period filter ESPN expects for player-card stats.</summary>
    public FantasyFilterReqBuilder WithTopScoringPeriod(int scoringPeriodId, IEnumerable<string>? additionalPeriodIds = null)
    {
        ArgumentHelpers.ThrowIf(scoringPeriodId <= 0, "Value must be greater than zero.", nameof(scoringPeriodId));
        var additionalValues = additionalPeriodIds?.Where(i => !string.IsNullOrWhiteSpace(i)).Distinct().ToArray();
        _players = (_players ?? new()) with {
            FilterStatsForTopScoringPeriodIds = new() { Value = scoringPeriodId, AdditionalValue = additionalValues is { Length: > 0 } ? additionalValues : null }
        };

        return this;
    }

    /// <summary>Sets the transaction types to include.</summary>
    public FantasyFilterReqBuilder WithTransactionTypes(IEnumerable<string> types)
    {
        ArgumentHelpers.ThrowIfNullOrEmpty(types, nameof(types));
        var values = types.Where(i => !string.IsNullOrWhiteSpace(i)).Distinct().ToArray();
        ArgumentHelpers.ThrowIfNullOrEmpty(values, nameof(types));
        _transactions = new() { FilterType = new() { Value = values } };
        return this;
    }

    /// <summary>Adds a topic type group to the league chat filter.</summary>
    public FantasyFilterReqBuilder AddTopicType(string topicType, int sortPriority = 1, bool sortAsc = false)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(topicType, nameof(topicType));
        _topicsByType[topicType] = new() { SortMessageDate = new() { SortPriority = sortPriority, SortAsc = sortAsc } };
        return this;
    }

    /// <summary>Adds multiple topic type groups to the league chat filter.</summary>
    public FantasyFilterReqBuilder WithTopicTypes(IEnumerable<string> topicTypes, int sortPriority = 1, bool sortAsc = false)
    {
        ArgumentHelpers.ThrowIfNullOrEmpty(topicTypes, nameof(topicTypes));
        foreach (var topicType in topicTypes.Where(i => !string.IsNullOrWhiteSpace(i)).Distinct())
            AddTopicType(topicType, sortPriority, sortAsc);

        ArgumentHelpers.ThrowIfNullOrEmpty(_topicsByType, nameof(topicTypes));
        return this;
    }

    /// <summary>Configures the recent activity topic filter.</summary>
    public FantasyFilterReqBuilder WithRecentActivity(int limit, int offset = 0, int limitPerMessageSet = 25, IEnumerable<int>? messageTypeIds = null)
    {
        ArgumentHelpers.ThrowIf(limit <= 0, "Value must be greater than zero.", nameof(limit));
        ArgumentHelpers.ThrowIf(offset < 0, "Value cannot be negative.", nameof(offset));
        ArgumentHelpers.ThrowIf(limitPerMessageSet <= 0, "Value must be greater than zero.", nameof(limitPerMessageSet));
        var messageTypeValues = messageTypeIds?.Distinct().ToArray();
        _topics = new() {
            FilterType = new() { Value = ["ACTIVITY_TRANSACTIONS"] },
            Limit = limit,
            LimitPerMessageSet = new() { Value = limitPerMessageSet },
            Offset = offset,
            SortMessageDate = new() { SortPriority = 1, SortAsc = false },
            SortFor = new() { SortPriority = 2, SortAsc = false },
            FilterIncludeMessageTypeIds = messageTypeValues is { Length: > 0 } ? new() { Value = messageTypeValues } : null
        };

        return this;
    }

    /// <summary>Builds the typed ESPN fantasy filter request.</summary>
    public FantasyFilterReq Build()
    {
        OperationHelpers.ThrowIf(
            _players == null && _transactions == null && _topics == null && _topicsByType.Count == 0, "At least one fantasy filter section must be specified.");

        return new() {
            Players = _players,
            Transactions = _transactions,
            Topics = _topics,
            TopicsByType = _topicsByType.Count == 0 ? null : new Dictionary<string, TopicTypeFilterReq>(_topicsByType)
        };
    }

    public static FantasyFilterReqBuilder Create() => new();

    public static FantasyFilterReqBuilder ForPlayers(IEnumerable<int> playerIds) => Create().WithPlayerIds(playerIds);

    public static FantasyFilterReqBuilder ForTransactions(IEnumerable<string> types) => Create().WithTransactionTypes(types);

    public static FantasyFilterReqBuilder ForTopicTypes(IEnumerable<string> topicTypes) => Create().WithTopicTypes(topicTypes);

    public static FantasyFilterReqBuilder ForRecentActivity(int limit, int offset = 0, int limitPerMessageSet = 25, IEnumerable<int>? messageTypeIds = null)
        => Create().WithRecentActivity(limit, offset, limitPerMessageSet, messageTypeIds);

    public override string ToString() => $"Players={_players != null} Transactions={_transactions != null} Topics={_topics != null} TopicTypes={_topicsByType.Count}";
}