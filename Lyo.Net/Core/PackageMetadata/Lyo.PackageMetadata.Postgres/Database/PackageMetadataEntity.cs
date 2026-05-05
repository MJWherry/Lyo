using System.ComponentModel.DataAnnotations;

namespace Lyo.PackageMetadata.Postgres.Database;

/// <summary>EF entity: one row per registered package artifact.</summary>
public sealed class PackageMetadataEntity
{
    [Key]
    public Guid Id { get; set; }

    public PackageEcosystem Ecosystem { get; set; }

    [Required]
    [MaxLength(300)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(120)]
    public string? Version { get; set; }

    public ArtifactDigestAlgorithm ArtifactDigestAlgorithm { get; set; }

    /// <summary>Lowercase hex digest when <see cref="ArtifactDigestAlgorithm" /> is not <see cref="ArtifactDigestAlgorithm.None" />.</summary>
    [MaxLength(128)]
    public string? ArtifactDigestHex { get; set; }

    [MaxLength(500)]
    public string? Title { get; set; }

    [MaxLength(4000)]
    public string? Description { get; set; }

    [MaxLength(4096)]
    public string? AuthorsJson { get; set; }

    [MaxLength(4096)]
    public string? PackageTypesJson { get; set; }

    [MaxLength(4096)]
    public string? TagsJson { get; set; }

    [MaxLength(512)]
    public string? ProjectUrl { get; set; }

    [MaxLength(512)]
    public string? RepositoryUrl { get; set; }

    [MaxLength(512)]
    public string? LicenseUrl { get; set; }

    [MaxLength(500)]
    public string? LicenseExpression { get; set; }

    /// <summary>JSON tree for <see cref="LicenseExpression" />: <c>and</c>, <c>or</c>, <c>with</c>, <c>license</c>, <c>exception</c>.</summary>
    [MaxLength(16384)]
    public string? LicenseExpressionSyntaxJson { get; set; }

    [MaxLength(512)]
    public string? PackageDetailsUrl { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public ICollection<PackageStackPrefixEntity> StackPrefixes { get; set; } = new List<PackageStackPrefixEntity>();
}
