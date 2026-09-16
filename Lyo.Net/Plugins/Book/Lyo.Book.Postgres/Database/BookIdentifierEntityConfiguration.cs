using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Book.Postgres.Database;

public sealed class BookIdentifierEntityConfiguration : IEntityTypeConfiguration<BookIdentifierEntity>
{
    public void Configure(EntityTypeBuilder<BookIdentifierEntity> builder)
    {
        builder.ToTable("identifier");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.BookId).HasColumnName("book_id").HasColumnType("uuid");
        builder.Property(e => e.Scheme).IsRequired().HasMaxLength(50).HasColumnName("scheme");
        builder.Property(e => e.Value).IsRequired().HasMaxLength(256).HasColumnName("value");
        builder.Property(e => e.NormalizedValue).IsRequired().HasMaxLength(256).HasColumnName("normalized_value");
        builder.Property(e => e.IsPrimary).HasColumnName("is_primary");
        builder.Property(e => e.Notes).HasMaxLength(2000).HasColumnName("notes");
        builder.HasIndex(e => new { e.Scheme, e.NormalizedValue }).IsUnique().HasDatabaseName("ix_book_identifier_scheme_normalized");
        builder.HasIndex(e => e.BookId).IsUnique().HasFilter("is_primary").HasDatabaseName("ix_book_identifier_primary");
    }
}
