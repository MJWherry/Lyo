using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Diagram.Postgres.Database;

/// <summary>Fluent EF mapping for <see cref="DiagramEntity" />.</summary>
public sealed class DiagramEntityConfiguration : IEntityTypeConfiguration<DiagramEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DiagramEntity> builder)
    {
        builder.ToTable("diagram");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(entity => entity.Name).HasMaxLength(200).IsRequired().HasColumnName("name");
        builder.Property(entity => entity.DocumentJson).HasColumnName("document").HasColumnType("jsonb").IsRequired();
        builder.Property(entity => entity.CreatedTimestamp).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
        builder.Property(entity => entity.UpdatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
        builder.HasIndex(entity => entity.Name).HasDatabaseName("ix_diagram_name");
    }
}