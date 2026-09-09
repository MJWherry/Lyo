namespace Lyo.Espn.Fantasy.Football.Client.Models.Request;

/// <summary>Strongly typed request body for ESPN's x-fantasy-filter header.</summary>
public sealed record FantasyFilterReq
{
    /// <summary>Per-player filter options.</summary>
    public PlayerFilterReq? Players { get; init; }

    /// <summary>Per-transaction filter options.</summary>
    public TransactionsFilterReq? Transactions { get; init; }

    /// <summary>Per-topic filter options.</summary>
    public TopicsFilterReq? Topics { get; init; }

    /// <summary>Topic groups keyed by ESPN topic type id.</summary>
    public IReadOnlyDictionary<string, TopicTypeFilterReq>? TopicsByType { get; init; }
}

/// <summary>Model for a filter value block with a single value.</summary>
/// <typeparam name="T">CLR type of the value.</typeparam>
public sealed record FilterValueReq<T>
{
    /// <summary>Value applied by the filter.</summary>
    public T? Value { get; init; }
}

/// <summary>Model for a filter value block with an additional value collection.</summary>
/// <typeparam name="TValue">CLR type of the primary value.</typeparam>
/// <typeparam name="TAdditional">CLR type of the extra value.</typeparam>
public sealed record FilterValueReq<TValue, TAdditional>
{
    /// <summary>Primary filter value.</summary>
    public TValue? Value { get; init; }

    /// <summary>Additional values ESPN expects of the filter.</summary>
    public IReadOnlyList<TAdditional>? AdditionalValue { get; init; }
}

/// <summary>Model for an ESPN sort definition.</summary>
public sealed record SortReq
{
    /// <summary>Sort priority; lower values win first.</summary>
    public int SortPriority { get; init; }

    /// <summary>True when the sort should be ascending.</summary>
    public bool SortAsc { get; init; }
}

/// <summary>ESPN filter settings for players.</summary>
public sealed record PlayerFilterReq
{
    /// <summary>Player ids included in the request.</summary>
    public FilterValueReq<IReadOnlyList<int>>? FilterIds { get; init; }

    /// <summary>Player card stats the scoring period filter ESPN expects.</summary>
    public FilterValueReq<int, string>? FilterStatsForTopScoringPeriodIds { get; init; }
}

/// <summary>ESPN filter settings for transactions.</summary>
public sealed record TransactionsFilterReq
{
    /// <summary>Transaction types included.</summary>
    public FilterValueReq<IReadOnlyList<string>>? FilterType { get; init; }
}

/// <summary>Filter settings for communication topics.</summary>
public sealed record TopicsFilterReq
{
    /// <summary>Topic types included.</summary>
    public FilterValueReq<IReadOnlyList<string>>? FilterType { get; init; }

    /// <summary>Upper bound on topics returned.</summary>
    public int? Limit { get; init; }

    /// <summary>Cap on messages ESPN considers in each set.</summary>
    public FilterValueReq<int>? LimitPerMessageSet { get; init; }

    /// <summary>How many topics to skip.</summary>
    public int? Offset { get; init; }

    /// <summary>Topic message dates primary sort definition.</summary>
    public SortReq? SortMessageDate { get; init; }

    /// <summary>Secondary sort.</summary>
    public SortReq? SortFor { get; init; }

    /// <summary>ESPN message type ids to include.</summary>
    public FilterValueReq<IReadOnlyList<int>>? FilterIncludeMessageTypeIds { get; init; }
}

/// <summary>A single topic type group sort settings.</summary>
public sealed record TopicTypeFilterReq
{
    /// <summary>The topic group sort definition.</summary>
    public SortReq? SortMessageDate { get; init; }
}