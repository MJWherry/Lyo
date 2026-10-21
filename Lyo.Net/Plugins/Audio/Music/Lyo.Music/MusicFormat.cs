using System.Diagnostics;

namespace Lyo.Music;

/// <summary>Physical or digital format of a release (CD, vinyl, digital, cassette).</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class MusicFormat
{
    public Guid Id { get; set; }

    public string Slug { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"MusicFormat(Slug=\"{Slug}\")";
}