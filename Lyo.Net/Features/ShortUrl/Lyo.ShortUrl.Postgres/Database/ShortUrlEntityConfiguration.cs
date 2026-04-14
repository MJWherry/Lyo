using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.ShortUrl.Postgres.Database;

public sealed class ShortUrlEntityConfiguration : IEntityTypeConfiguration<ShortUrlEntity>
{
    public void Configure(EntityTypeBuilder<ShortUrlEntity> builder)
    {
        builder.ToTable("short_urls");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasMaxLength(100).IsRequired().HasColumnName("id");
        builder.Property(e => e.LongUrl).HasMaxLength(2048).IsRequired().HasColumnName("long_url");
        builder.Property(e => e.CustomAlias).HasMaxLength(100).HasColumnName("custom_alias");
        builder.Property(e => e.CreatedTimestamp).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
        builder.Property(e => e.UpdatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
        builder.Property(e => e.ExpirationDate).HasColumnName("expiration_date");
        builder.Property(e => e.LastAccessedDate).HasColumnName("last_accessed_date");
        builder.Property(e => e.ClickCount).HasDefaultValue(0).HasColumnName("click_count");
        builder.Property(e => e.IsActive).HasDefaultValue(true).HasColumnName("is_active");

        // Indexes for performance
        builder.HasIndex(e => e.CustomAlias).IsUnique().HasFilter("custom_alias IS NOT NULL").HasDatabaseName("ix_short_urls_custom_alias");
        builder.HasIndex(e => e.CreatedTimestamp).HasDatabaseName("ix_short_urls_created_timestamp");
        builder.HasIndex(e => e.ExpirationDate).HasDatabaseName("ix_short_urls_expiration_date");
        builder.HasIndex(e => e.IsActive).HasDatabaseName("ix_short_urls_is_active");
        builder.HasIndex(e => new { e.IsActive, e.ExpirationDate }).HasDatabaseName("ix_short_urls_is_active_expiration_date");

        // Relationship with clicks
        builder.HasMany<UrlClickEntity>(e => e.Clicks).WithOne(c => c.ShortUrl).HasForeignKey(c => c.ShortUrlId).OnDelete(DeleteBehavior.Cascade);
    }
}