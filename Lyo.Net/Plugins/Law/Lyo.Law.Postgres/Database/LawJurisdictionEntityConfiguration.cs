using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Law.Postgres.Database;

/// <summary>Maps <see cref="LawJurisdictionEntity" /> to <c>law.jurisdiction</c>.</summary>
public sealed class LawJurisdictionEntityConfiguration : IEntityTypeConfiguration<LawJurisdictionEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<LawJurisdictionEntity> builder)
    {
        builder.ToTable("jurisdiction");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.Key).HasMaxLength(32).IsRequired().HasColumnName("key");
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired().HasColumnName("name");
        builder.Property(e => e.CreatedTimestamp).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
        builder.Property(e => e.UpdatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
        builder.HasIndex(e => e.Key).IsUnique().HasDatabaseName("ux_law_jurisdiction_key");
    }
}