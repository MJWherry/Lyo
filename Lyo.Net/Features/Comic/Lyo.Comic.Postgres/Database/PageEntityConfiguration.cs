using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Comic.Postgres.Database;

public sealed class PageEntityConfiguration : IEntityTypeConfiguration<PageEntity>
{
    public void Configure(EntityTypeBuilder<PageEntity> builder)
    {
        builder.ToTable("page");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.ChapterId).HasColumnName("chapter_id").HasColumnType("uuid");
        builder.Property(e => e.PageNumber).IsRequired().HasColumnName("page_number");
        builder.Property(e => e.ImageRef).HasColumnName("image_ref");
        builder.Property(e => e.Width).HasColumnName("width");
        builder.Property(e => e.Height).HasColumnName("height");
        builder.Property(e => e.CreatedTimestamp).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
        builder.Property(e => e.UpdatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
        builder.HasOne(e => e.Chapter).WithMany().HasForeignKey(e => e.ChapterId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(e => e.ChapterId).HasDatabaseName("ix_comic_page_chapter");
        builder.HasIndex(e => new { e.ChapterId, e.PageNumber }).IsUnique().HasDatabaseName("ix_comic_page_chapter_number");
    }
}