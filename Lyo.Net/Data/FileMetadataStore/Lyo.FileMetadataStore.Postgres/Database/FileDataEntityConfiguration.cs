using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.FileMetadataStore.Postgres.Database;

public class FileDataEntityConfiguration : IEntityTypeConfiguration<FileDataEntity>
{
    public void Configure(EntityTypeBuilder<FileDataEntity> builder)
    {
        builder.ToTable("file_data");
        builder.HasKey(e => e.FileId);
        builder.Property(e => e.FileId).IsRequired().HasColumnType("uuid").HasColumnName("file_id");
        builder.Property(e => e.Data).IsRequired().HasColumnType("bytea").HasColumnName("data");

        // Create index for better query performance
        builder.HasIndex(e => e.FileId).IsUnique().HasDatabaseName("ix_file_data_file_id");
    }
}