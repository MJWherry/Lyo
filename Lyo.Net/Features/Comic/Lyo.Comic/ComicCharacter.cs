using System.Diagnostics;

namespace Lyo.Comic;

/// <summary>Represents a character associated with a comic series, optionally linked to specific volumes they appear in.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class ComicCharacter
{
    /// <summary>Gets or sets the unique identifier of the character.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the series this character primarily belongs to.</summary>
    public Guid SeriesId { get; set; }

    /// <summary>Gets or sets the character's name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets a description or biography of the character.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets a reference to the character's image in file storage. Format is determined by the consuming application (e.g. a file storage key or URI).</summary>
    public string? ImageRef { get; set; }

    /// <summary>Gets or sets the character's role (e.g. "Protagonist", "Antagonist", "Supporting", "Minor").</summary>
    public string? Role { get; set; }

    /// <summary>Gets or sets when this record was created.</summary>
    public DateTime CreatedTimestamp { get; set; }

    /// <summary>Gets or sets when this record was last updated.</summary>
    public DateTime? UpdatedTimestamp { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"ComicCharacter(Id={Id}, SeriesId={SeriesId}, Name=\"{Name}\")";
}