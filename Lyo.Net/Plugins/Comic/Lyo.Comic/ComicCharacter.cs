using System.Diagnostics;

namespace Lyo.Comic;

/// <summary>Model for a character associated with a comic series, optionally linked to specific volumes they appear in.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class ComicCharacter
{
    /// <summary>Unique identifier of the character for this record.</summary>
    public Guid Id { get; set; }

    /// <summary>Value of the series this character primarily belongs to.</summary>
    public Guid SeriesId { get; set; }

    /// <summary>Holds the character's name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Holds a description or biography of the character.</summary>
    public string? Description { get; set; }

    /// <summary>Optional field: a reference to the character's image in file storage. Format is determined by the consuming application (e.g. a file storage key or URI).</summary>
    public string? ImageRef { get; set; }

    /// <summary>Character's role (e.g. "Protagonist", "Antagonist", "Supporting", "Minor").</summary>
    public string? Role { get; set; }

    /// <summary>Clock time for when this record was created.</summary>
    public DateTime CreatedTimestamp { get; set; }

    /// <summary>Recorded time of when this record was last updated.</summary>
    public DateTime? UpdatedTimestamp { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"ComicCharacter(Id={Id}, SeriesId={SeriesId}, Name=\"{Name}\")";
}