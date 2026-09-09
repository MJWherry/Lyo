using System.ComponentModel.DataAnnotations;

namespace Lyo.PackageMetadata.Postgres.Database;

/// <summary>EF entity for a normalised namespace prefix (trailing dot) used in stack-frame lookup.</summary>
public sealed class PackageStackPrefixEntity
{
    [Key]
    public Guid Id { get; set; }

    public Guid PackageMetadataId { get; set; }

    public PackageMetadataEntity Package { get; set; } = null!;

    /// <summary>Prefix with a trailing <c>.</c>, for example <c>Npgsql.</c></summary>
    [Required]
    [MaxLength(512)]
    public string NormalizedPrefix { get; set; } = string.Empty;
}