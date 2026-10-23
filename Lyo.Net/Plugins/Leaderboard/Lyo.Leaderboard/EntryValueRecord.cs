namespace Lyo.Leaderboard;

/// <summary>One typed value on an entry, keyed by scoring property.</summary>
public sealed class EntryValueRecord
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; set; }

    /// <summary>Owning entry.</summary>
    public Guid EntryId { get; set; }

    /// <summary>Scoring property this value fills.</summary>
    public Guid PropertyId { get; set; }

    /// <summary>Property key snapshot for scoring (not persisted separately when the property is loaded).</summary>
    public string? PropertyKey { get; set; }

    /// <summary>Selected choice key.</summary>
    public string? ChoiceKey { get; set; }

    /// <summary>Numeric / integer / duration (seconds) value.</summary>
    public decimal? NumberValue { get; set; }

    /// <summary>Text value.</summary>
    public string? TextValue { get; set; }

    /// <summary>Boolean value.</summary>
    public bool? BoolValue { get; set; }

    /// <summary>Date-time value (UTC).</summary>
    public DateTime? DateTimeValue { get; set; }
}