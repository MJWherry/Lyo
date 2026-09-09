namespace Lyo.Espn.Fantasy.Football.Client.Models.Request;

/// <summary>Query options for transactions from the league.</summary>
public sealed record TransactionsQuery
{
    /// <summary>Scoring period for transaction reads. If omitted, ESPN returns its default window.</summary>
    public int? ScoringPeriodId { get; init; }

    /// <summary>transaction types to include. Present only when supplied.</summary>
    public IReadOnlyList<string> Types { get; init; } = [];
}