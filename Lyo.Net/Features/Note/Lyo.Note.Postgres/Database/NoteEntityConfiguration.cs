using Lyo.EntityReference.Postgres.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Note.Postgres.Database;

public sealed class NoteEntityConfiguration : EntityRefConfiguration<NoteEntity>
{
    public NoteEntityConfiguration()
        : base("note") { }

    /// <inheritdoc />
    public override void Configure(EntityTypeBuilder<NoteEntity> builder)
    {
        builder.ToTable("note");
        builder.HasKey(e => e.Id);
        MapColumns(builder);
        builder.Property(e => e.Content).IsRequired().HasMaxLength(8192).HasColumnName("content");
        builder.Property(e => e.UpdatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
        builder.HasIndex(e => new { e.TenantId, e.ForEntityType, e.ForEntityId }).HasDatabaseName("ix_note_tenant_for_entity");
        builder.HasIndex(e => new { e.TenantId, e.FromEntityType, e.FromEntityId }).HasDatabaseName("ix_note_tenant_from_entity");
        builder.HasIndex(e => new { e.TenantId, e.Context }).HasDatabaseName("ix_note_tenant_context");
        builder.HasIndex(e => e.CreatedAt).HasDatabaseName("ix_note_created_at");
        builder.HasIndex(e => e.ExpiresAt).HasDatabaseName("ix_note_expires_at").HasFilter("\"expires_at\" IS NOT NULL");
    }
}
