namespace Lyo.PackageMetadata.Postgres.Database;

/// <summary>EF entity: normalised namespace prefix (trailing dot) for stack-frame lookup.</summary>
public sealed class PackageStackPrefixEntity
{
    public Guid Id { get; set; }

    public Guid PackageMetadataId { get; set; }

    public PackageMetadataEntity Package { get; set; } = null!;

    /// <summary>Prefix with trailing <c>.</c>, e.g. <c>Npgsql.</c></summary>
    public string NormalizedPrefix { get; set; } = string.Empty;
}
