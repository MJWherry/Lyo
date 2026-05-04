namespace Lyo.PackageMetadata.Postgres.Database;

/// <summary>EF entity: one row per NuGet-style package.</summary>
public sealed class PackageMetadataEntity
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Version { get; set; }

    /// <summary>SHA-512 of package file bytes, 128 lowercase hex chars, or <see langword="null" /> when unknown.</summary>
    public string? PackageFileSha512Hex { get; set; }

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
