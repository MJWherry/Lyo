using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Law.Postgres.Database;

/// <summary>Maps <see cref="LawCodeEntity" /> to <c>law.code</c>.</summary>
public sealed class LawCodeEntityConfiguration : IEntityTypeConfiguration<LawCodeEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<LawCodeEntity> builder)
    {
        builder.ToTable("code");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.JurisdictionId).HasColumnName("jurisdiction_id").HasColumnType("uuid");
        builder.Property(e => e.Key).HasMaxLength(200).IsRequired().HasColumnName("key");
        builder.Property(e => e.Name).HasMaxLength(500).IsRequired().HasColumnName("name");
        builder.Property(e => e.CitationPrefix).HasMaxLength(200).HasColumnName("citation_prefix");
        builder.Property(e => e.SourceHost).HasMaxLength(200).HasColumnName("source_host");
        builder.Property(e => e.CreatedTimestamp).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
        builder.Property(e => e.UpdatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
        builder.HasOne(e => e.Jurisdiction).WithMany(e => e.Codes).HasForeignKey(e => e.JurisdictionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(e => new { e.JurisdictionId, e.Key }).IsUnique().HasDatabaseName("ux_law_code_key");
    }
}