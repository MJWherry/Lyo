namespace Lyo.PackageMetadata.Postgres.Database;

/// <summary>EF entity: one row per registered package artifact.</summary>
public sealed class PackageMetadataEntity
{
    public Guid Id { get; set; }

    public PackageEcosystem Ecosystem { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Version { get; set; }

    public ArtifactDigestAlgorithm ArtifactDigestAlgorithm { get; set; }

    /// <summary>Lowercase hex digest when <see cref="ArtifactDigestAlgorithm" /> is not <see cref="ArtifactDigestAlgorithm.None" />.</summary>
    public string? ArtifactDigestHex { get; set; }

    public string? Title { get; set; }

    public string? Description { get; set; }

    public string? AuthorsJson { get; set; }

    public string? PackageTypesJson { get; set; }

    public string? TagsJson { get; set; }

    public string? ProjectUrl { get; set; }

    public string? RepositoryUrl { get; set; }

    public string? LicenseUrl { get; set; }

    public string? LicenseExpression { get; set; }

    public string? PackageDetailsUrl { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public ICollection<PackageStackPrefixEntity> StackPrefixes { get; set; } = new List<PackageStackPrefixEntity>();
}
