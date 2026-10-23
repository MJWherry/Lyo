using System.Diagnostics;

namespace Lyo.Music.Postgres.Database;

[DebuggerDisplay("{ToString(),nq}")]
public sealed class MusicIdentifierEntity
{
    public Guid Id { get; set; }

    public Guid ReleaseId { get; set; }

    public string Scheme { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public string NormalizedValue { get; set; } = string.Empty;

    public bool IsPrimary { get; set; }

    public MusicReleaseEntity? Release { get; set; }

    /// <summary>Type name and id.</summary>
    public override string ToString() => $"{GetType().Name} {Id}";
}