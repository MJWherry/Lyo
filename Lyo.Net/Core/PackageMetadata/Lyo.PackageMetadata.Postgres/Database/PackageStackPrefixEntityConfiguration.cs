using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.PackageMetadata.Postgres.Database;

public sealed class PackageStackPrefixEntityConfiguration : IEntityTypeConfiguration<PackageStackPrefixEntity>
{
    public void Configure(EntityTypeBuilder<PackageStackPrefixEntity> builder)
    {
        builder.ToTable("stack_prefix", PostgresPackageMetadataOptions.Schema);
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.PackageMetadataId).HasColumnName("package_metadata_id").HasColumnType("uuid");
        builder.Property(e => e.NormalizedPrefix).HasMaxLength(512).IsRequired().HasColumnName("normalized_prefix");
        builder.HasOne(e => e.Package).WithMany(p => p.StackPrefixes).HasForeignKey(e => e.PackageMetadataId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(e => e.NormalizedPrefix).IsUnique().HasDatabaseName("ix_stack_prefix_normalized_prefix_unique");
    }
}
