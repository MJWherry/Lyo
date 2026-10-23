using Lyo.FileMetadataStore.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.FileMetadataStore.Postgres.Database;

/// <summary>Maps prior file revisions. This is file history, not an encryption key version.</summary>
public sealed class FileRevisionEntityConfiguration : IEntityTypeConfiguration<FileRevisionEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<FileRevisionEntity> builder)
    {
        builder.ToTable("file_revision");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.ParentId).HasColumnName("parent_id");
        builder.Property(e => e.StoredKey).HasMaxLength(500).IsRequired().HasColumnName("stored_key");
        builder.Property(e => e.CreatedAt).HasColumnType("timestamp with time zone").HasColumnName("created_at");
        builder.HasIndex(e => e.ParentId).HasDatabaseName("ix_file_revision_parent_id");
    }
}