using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Book.Postgres.Database;

public sealed class BookContributorEntityConfiguration : IEntityTypeConfiguration<BookContributorEntity>
{
    public void Configure(EntityTypeBuilder<BookContributorEntity> builder)
    {
        builder.ToTable("contributor");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.BookId).HasColumnName("book_id").HasColumnType("uuid");
        builder.Property(e => e.DisplayName).IsRequired().HasMaxLength(500).HasColumnName("display_name");
        builder.Property(e => e.SortName).HasMaxLength(500).HasColumnName("sort_name");
        builder.Property(e => e.Role).HasConversion<string>().HasColumnType("text").HasColumnName("role");
        builder.Property(e => e.Ordinal).HasColumnName("ordinal");
        builder.Property(e => e.Language).HasMaxLength(16).HasColumnName("language");
        builder.Property(e => e.PersonRef).HasMaxLength(256).HasColumnName("person_ref");
        builder.Property(e => e.Notes).HasMaxLength(2000).HasColumnName("notes");
        builder.HasIndex(e => new { e.BookId, e.Role, e.Ordinal }).IsUnique().HasDatabaseName("ix_book_contributor_book_role_ordinal");
        builder.HasIndex(e => e.SortName).HasDatabaseName("ix_book_contributor_sort_name");
    }
}