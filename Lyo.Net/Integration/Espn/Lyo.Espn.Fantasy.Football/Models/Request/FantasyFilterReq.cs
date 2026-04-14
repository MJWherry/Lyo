namespace Lyo.Espn.Fantasy.Football.Models.Request;

/// <summary>Typed request body for ESPN's x-fantasy-filter header.</summary>
public sealed record FantasyFilterReq
{
    /// <summary>Player-specific filter options.</summary>
    public PlayerFilterReq? Players { get; init; }

    /// <summary>Transaction-specific filter options.</summary>
    public TransactionsFilterReq? Transactions { get; init; }

    /// <summary>Topic-specific filter options.</summary>
    public TopicsFilterReq? Topics { get; init; }

    /// <summary>Topic groups keyed by ESPN topic type.</summary>
    public IReadOnlyDictionary<string, TopicTypeFilterReq>? TopicsByType { get; init; }
}

/// <summary>Represents a filter value block with a single value.</summary>
/// <typeparam name="T">The value type.</typeparam>
public sealed record FilterValueReq<T>
{
    /// <summary>The filter value.</summary>
    public T? Value { get; init; }
}

/// <summary>Represents a filter value block with an additional value collection.</summary>
/// <typeparam name="TValue">The primary value type.</typeparam>
/// <typeparam name="TAdditional">The additional value type.</typeparam>
public sealed record FilterValueReq<TValue, TAdditional>
{
    /// <summary>The primary filter value.</summary>
    public TValue? Value { get; init; }

    /// <summary>Additional values ESPN expects for the filter.</summary>
    public IReadOnlyList<TAdditional>? AdditionalValue { get; init; }
}

/// <summary>Represents an ESPN sort definition.</summary>
public sealed record SortReq
{
    /// <summary>The sort priority. Lower values are applied first.</summary>
    public int SortPriority { get; init; }

    /// <summary>Whether the sort should be ascending.</summary>
    public bool SortAsc { get; init; }
}

/// <summary>Player-specific ESPN filter settings.</summary>
public sealed record PlayerFilterReq
{
    /// <summary>The player ids to include.</summary>
    public FilterValueReq<IReadOnlyList<int>>? FilterIds { get; init; }

    /// <summary>The scoring period filter ESPN expects for player card stats.</summary>
    public FilterValueReq<int, string>? FilterStatsForTopScoringPeriodIds { get; init; }
}

/// <summary>Transaction-specific ESPN filter settings.</summary>
public sealed record TransactionsFilterReq
{
    /// <summary>The transaction types to include.</summary>
    public FilterValueReq<IReadOnlyList<string>>? FilterType { get; init; }
}

/// <summary>Communication topic filter settings.</summary>
public sealed record TopicsFilterReq
{
    /// <summary>The topic types to include.</summary>
    public FilterValueReq<IReadOnlyList<string>>? FilterType { get; init; }

    /// <summary>Maximum number of topics to return.</summary>
    public int? Limit { get; init; }

    /// <summary>Maximum number of messages ESPN should consider per message set.</summary>
    public FilterValueReq<int>? LimitPerMessageSet { get; init; }

    /// <summary>Number of topics to skip.</summary>
    public int? Offset { get; init; }

    /// <summary>Primary sort definition for topic message dates.</summary>
    public SortReq? SortMessageDate { get; init; }

    /// <summary>Secondary sort definition.</summary>
    public SortReq? SortFor { get; init; }

    /// <summary>Optional ESPN message type ids to include.</summary>
    public FilterValueReq<IReadOnlyList<int>>? FilterIncludeMessageTypeIds { get; init; }
}

/// <summary>Sort settings for a single topic type group.</summary>
public sealed record TopicTypeFilterReq
{
    /// <summary>Sort definition for the topic group.</summary>
    public SortReq? SortMessageDate { get; init; }
}