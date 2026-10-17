using Lyo.FileMetadataStore;
using Lyo.FileMetadataStore.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.FileMetadataStore.Sqlite.Database;

public sealed class FileTagEntityConfiguration : IEntityTypeConfiguration<FileTagEntity>
{
    public void Configure(EntityTypeBuilder<FileTagEntity> builder)
    {
        builder.ToTable("file_tag");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).IsRequired().HasColumnName("id");
        builder.Property(e => e.FileId).IsRequired().HasConversion(v => Guid.Parse(v), v => v.ToString()).HasColumnName("file_id");
        builder.Property(e => e.TagType).HasMaxLength(FileTagTypes.MaxTagTypeLength).IsRequired().HasColumnName("tag_type");
        builder.Property(e => e.Value).HasMaxLength(FileTagTypes.MaxValueLength).IsRequired().HasColumnName("value");
        builder.Property(e => e.Slug).HasMaxLength(FileTagTypes.MaxSlugLength).IsRequired().HasDefaultValue("").HasColumnName("slug");
        builder.Property(e => e.Automatic).IsRequired().HasDefaultValue(false).HasColumnName("automatic");
        builder.HasIndex(e => e.TagType).HasDatabaseName("ix_file_tag_tag_type");
        builder.HasIndex(e => new { e.FileId, e.TagType, e.Value }).IsUnique().HasDatabaseName("uq_file_tag_file_type_value");
        builder.HasOne<FileMetadataEntity>().WithMany().HasForeignKey(e => e.FileId).OnDelete(DeleteBehavior.Cascade);
    }
}
