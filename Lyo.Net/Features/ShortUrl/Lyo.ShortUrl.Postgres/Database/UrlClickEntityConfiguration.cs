using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.ShortUrl.Postgres.Database;

public sealed class UrlClickEntityConfiguration : IEntityTypeConfiguration<UrlClickEntity>
{
    public void Configure(EntityTypeBuilder<UrlClickEntity> builder)
    {
        builder.ToTable("url_clicks");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd().HasColumnName("id");
        builder.Property(e => e.ShortUrlId).HasMaxLength(100).IsRequired().HasColumnName("short_url_id");
        builder.Property(e => e.ClickedAt).IsRequired().HasColumnName("clicked_at");
        builder.Property(e => e.IpAddress).HasMaxLength(45).HasColumnName("ip_address");
        builder.Property(e => e.UserAgent).HasMaxLength(500).HasColumnName("user_agent");
        builder.Property(e => e.Referrer).HasMaxLength(2048).HasColumnName("referrer");

        // Indexes for performance
        builder.HasIndex(e => e.ShortUrlId).HasDatabaseName("ix_url_clicks_short_url_id");
        builder.HasIndex(e => e.ClickedAt).HasDatabaseName("ix_url_clicks_clicked_at");
        builder.HasIndex(e => new { e.ShortUrlId, e.ClickedAt }).HasDatabaseName("ix_url_clicks_short_url_id_clicked_at");
    }
}