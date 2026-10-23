using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Book.Postgres.Database;

public sealed class BookGenreEntityConfiguration : IEntityTypeConfiguration<BookGenreEntity>
{
    public void Configure(EntityTypeBuilder<BookGenreEntity> builder)
    {
        builder.ToTable("genre");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.ParentGenreId).HasColumnName("parent_genre_id").HasColumnType("uuid");
        builder.Property(e => e.Slug).IsRequired().HasMaxLength(100).HasColumnName("slug");
        builder.Property(e => e.Name).IsRequired().HasMaxLength(200).HasColumnName("name");
        builder.Property(e => e.Description).HasMaxLength(2000).HasColumnName("description");
        builder.Property(e => e.SortOrder).HasColumnName("sort_order");
        builder.HasIndex(e => e.Slug).IsUnique().HasDatabaseName("ix_book_genre_slug");
        builder.HasOne(e => e.Parent).WithMany(e => e.Children).HasForeignKey(e => e.ParentGenreId).OnDelete(DeleteBehavior.Restrict);
    }
}