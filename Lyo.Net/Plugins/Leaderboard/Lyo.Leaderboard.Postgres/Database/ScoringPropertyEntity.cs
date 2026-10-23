using System.Diagnostics;

namespace Lyo.Leaderboard.Postgres.Database;

/// <summary>Persisted scoring property.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class ScoringPropertyEntity
{
    public Guid Id { get; set; }

    public Guid BoardId { get; set; }

    public string Key { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string ValueKind { get; set; } = "number";

    public string? Unit { get; set; }

    public bool Required { get; set; }

    public int SortOrder { get; set; }

    public decimal? MinValue { get; set; }

    public decimal? MaxValue { get; set; }

    public string? ValidationRegex { get; set; }

    public string? ChoicesJson { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public BoardEntity? Board { get; set; }

    /// <summary>Type name and id.</summary>
    public override string ToString() => $"{GetType().Name} {Id}";
}