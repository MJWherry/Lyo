using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.FileMetadataStore.Sqlite.Database;

public class FileDataEntityConfiguration : IEntityTypeConfiguration<FileDataEntity>
{
    public void Configure(EntityTypeBuilder<FileDataEntity> builder)
    {
        builder.ToTable("file_data");
        builder.HasKey(e => e.FileId);
        builder.Property(e => e.FileId).IsRequired().HasColumnName("file_id");
        builder.Property(e => e.Data).IsRequired().HasColumnName("data");
        builder.HasIndex(e => e.FileId).IsUnique().HasDatabaseName("ix_file_data_file_id");
    }
}