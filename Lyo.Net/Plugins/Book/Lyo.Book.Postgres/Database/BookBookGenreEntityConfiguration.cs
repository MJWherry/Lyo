using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Book.Postgres.Database;

public sealed class BookBookGenreEntityConfiguration : IEntityTypeConfiguration<BookBookGenreEntity>
{
    public void Configure(EntityTypeBuilder<BookBookGenreEntity> builder)
    {
        builder.ToTable("book_genre");
        builder.HasKey(e => new { e.BookId, e.GenreId });
        builder.Property(e => e.BookId).HasColumnName("book_id").HasColumnType("uuid");
        builder.Property(e => e.GenreId).HasColumnName("genre_id").HasColumnType("uuid");
        builder.HasOne(e => e.Genre).WithMany(g => g.BookLinks).HasForeignKey(e => e.GenreId).OnDelete(DeleteBehavior.Restrict);
    }
}