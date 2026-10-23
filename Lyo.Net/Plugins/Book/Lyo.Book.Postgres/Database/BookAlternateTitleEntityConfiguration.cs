using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Book.Postgres.Database;

public sealed class BookAlternateTitleEntityConfiguration : IEntityTypeConfiguration<BookAlternateTitleEntity>
{
    public void Configure(EntityTypeBuilder<BookAlternateTitleEntity> builder)
    {
        builder.ToTable("alternate_title");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.BookId).HasColumnName("book_id").HasColumnType("uuid");
        builder.Property(e => e.Title).IsRequired().HasMaxLength(500).HasColumnName("title");
        builder.Property(e => e.Language).HasMaxLength(16).HasColumnName("language");
        builder.Property(e => e.Kind).HasConversion<string>().HasColumnType("text").HasColumnName("kind");
        builder.Property(e => e.IsPrimary).HasColumnName("is_primary");
        builder.Property(e => e.SortOrder).HasColumnName("sort_order");
        builder.HasIndex(e => e.BookId).IsUnique().HasFilter("is_primary").HasDatabaseName("ix_book_alternate_title_primary");
    }
}