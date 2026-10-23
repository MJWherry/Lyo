using System.Diagnostics;

namespace Lyo.Leaderboard.Postgres.Database;

/// <summary>Persisted typed entry value.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class EntryValueEntity
{
    public Guid Id { get; set; }

    public Guid EntryId { get; set; }

    public Guid PropertyId { get; set; }

    public string? ChoiceKey { get; set; }

    public decimal? NumberValue { get; set; }

    public string? TextValue { get; set; }

    public bool? BoolValue { get; set; }

    public DateTime? DateTimeValue { get; set; }

    public EntryEntity? Entry { get; set; }

    public ScoringPropertyEntity? Property { get; set; }

    /// <summary>Type name and id.</summary>
    public override string ToString() => $"{GetType().Name} {Id}";
}