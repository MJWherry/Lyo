namespace Lyo.Music.Postgres.Database;

public sealed class MusicIdentifierEntity
{
    public Guid Id { get; set; }

    public Guid ReleaseId { get; set; }

    public string Scheme { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public string NormalizedValue { get; set; } = string.Empty;

    public bool IsPrimary { get; set; }

    public MusicReleaseEntity? Release { get; set; }
}