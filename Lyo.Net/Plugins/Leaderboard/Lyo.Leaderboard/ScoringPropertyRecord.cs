using Lyo.Leaderboard.Enums;

namespace Lyo.Leaderboard;

/// <summary>A form field on a board. The board formula reads its key. Choice options also carry a number.</summary>
public sealed class ScoringPropertyRecord
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; set; }

    /// <summary>Owning board.</summary>
    public Guid BoardId { get; set; }

    /// <summary>Stable key used in formulas and value maps (<c>[a-z][a-z0-9_]*</c>).</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Display label.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Optional help text.</summary>
    public string? Description { get; set; }

    /// <summary>Stored value type.</summary>
    public ScoringValueKind ValueKind { get; set; } = ScoringValueKind.Number;

    /// <summary>Optional unit label (e.g. <c>lb</c>, <c>in</c>).</summary>
    public string? Unit { get; set; }

    /// <summary>Whether a value is required on submit.</summary>
    public bool Required { get; set; }

    /// <summary>Display order, low first.</summary>
    public int SortOrder { get; set; }

    /// <summary>Optional numeric minimum for validation.</summary>
    public decimal? MinValue { get; set; }

    /// <summary>Optional numeric maximum for validation.</summary>
    public decimal? MaxValue { get; set; }

    /// <summary>Optional regex for text values.</summary>
    public string? ValidationRegex { get; set; }

    /// <summary>Choice options for <see cref="ScoringValueKind.Choice" />. Each option stores a key and a number.</summary>
    public List<ScoringChoice> Choices { get; set; } = [];

    /// <summary>Creation time (UTC).</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Last update time (UTC).</summary>
    public DateTime? UpdatedAt { get; set; }
}
