using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Note.Postgres.Database;

public sealed class NoteEntityConfiguration : IEntityTypeConfiguration<NoteEntity>
{
    public void Configure(EntityTypeBuilder<NoteEntity> builder)
    {
        builder.ToTable("note");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.ForEntityType).HasMaxLength(200).IsRequired().HasColumnName("for_entity_type");
        builder.Property(e => e.ForEntityId).HasMaxLength(200).IsRequired().HasColumnName("for_entity_id");
        builder.Property(e => e.FromEntityType).HasMaxLength(200).IsRequired().HasColumnName("from_entity_type");
        builder.Property(e => e.FromEntityId).HasMaxLength(200).IsRequired().HasColumnName("from_entity_id");
        builder.Property(e => e.Content).IsRequired().HasColumnName("content");
        builder.Property(e => e.CreatedTimestamp).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
        builder.Property(e => e.UpdatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
        builder.HasIndex(e => new { e.ForEntityType, e.ForEntityId }).HasDatabaseName("ix_note_for_entity");
        builder.HasIndex(e => new { e.FromEntityType, e.FromEntityId }).HasDatabaseName("ix_note_from_entity");
    }
}