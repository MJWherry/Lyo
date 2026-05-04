using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.PackageMetadata.Postgres.Database;

public sealed class PackageMetadataEntityConfiguration : IEntityTypeConfiguration<PackageMetadataEntity>
{
    public void Configure(EntityTypeBuilder<PackageMetadataEntity> builder)
    {
        builder.ToTable("package", PostgresPackageMetadataOptions.Schema);
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.Ecosystem).IsRequired().HasColumnName("ecosystem");
        builder.Property(e => e.Name).HasMaxLength(300).IsRequired().HasColumnName("name");
        builder.Property(e => e.Version).HasMaxLength(120).HasColumnName("version");
        builder.Property(e => e.ArtifactDigestAlgorithm).IsRequired().HasColumnName("artifact_digest_algorithm");
        builder.Property(e => e.ArtifactDigestHex).HasMaxLength(128).HasColumnName("artifact_digest_hex");
        builder.Property(e => e.Title).HasMaxLength(500).HasColumnName("title");
        builder.Property(e => e.Description).HasColumnType("text").HasColumnName("description");
        builder.Property(e => e.AuthorsJson).HasColumnType("text").HasColumnName("authors_json");
        builder.Property(e => e.PackageTypesJson).HasColumnType("text").HasColumnName("package_types_json");
        builder.Property(e => e.TagsJson).HasColumnType("text").HasColumnName("tags_json");
        builder.Property(e => e.ProjectUrl).HasMaxLength(2000).HasColumnName("project_url");
        builder.Property(e => e.RepositoryUrl).HasMaxLength(2000).HasColumnName("repository_url");
        builder.Property(e => e.LicenseUrl).HasMaxLength(2000).HasColumnName("license_url");
        builder.Property(e => e.LicenseExpression).HasMaxLength(500).HasColumnName("license_expression");
        builder.Property(e => e.PackageDetailsUrl).HasMaxLength(2000).HasColumnName("package_details_url");
        builder.Property(e => e.CreatedAt).IsRequired().HasColumnName("created_at");
        builder.Property(e => e.UpdatedAt).IsRequired().HasColumnName("updated_at");
        builder.HasIndex(e => e.Name).HasDatabaseName("ix_package_name");
        builder.HasIndex(e => new { e.Name, e.Version }).HasDatabaseName("ix_package_name_version");
    }
}
