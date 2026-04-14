namespace Lyo.Espn.Fantasy.Football.Models.Request;

/// <summary>Options for loading transactions from the league.</summary>
public sealed record TransactionsQuery
{
    /// <summary>The scoring period to load transactions for. If omitted, ESPN returns its default window.</summary>
    public int? ScoringPeriodId { get; init; }

    /// <summary>Optional transaction types to include.</summary>
    public IReadOnlyList<string> Types { get; init; } = [];
}