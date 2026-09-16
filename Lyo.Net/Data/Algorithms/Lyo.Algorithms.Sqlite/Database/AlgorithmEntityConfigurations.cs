using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Algorithms.Sqlite.Database;

internal sealed class AlgorithmDefinitionEntityConfiguration : IEntityTypeConfiguration<AlgorithmDefinitionEntity>
{
    public void Configure(EntityTypeBuilder<AlgorithmDefinitionEntity> builder)
    {
        builder.ToTable("algorithm_definition");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.Key).HasColumnName("key").HasMaxLength(200).IsRequired();
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(200);
        builder.Property(e => e.Description).HasColumnName("description").HasMaxLength(2000);
        builder.Property(e => e.Kind).HasColumnName("kind").HasMaxLength(32).IsRequired();
        builder.Property(e => e.CurrentRevision).HasColumnName("current_revision");
        builder.Property(e => e.CreatedTimestamp).HasColumnName("created_timestamp");
        builder.Property(e => e.UpdatedTimestamp).HasColumnName("updated_timestamp");
        builder.HasIndex(e => e.Key).IsUnique();
        builder.HasMany(e => e.Revisions).WithOne(r => r.Definition!).HasForeignKey(r => r.DefinitionId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(e => e.Labels).WithOne(l => l.Definition!).HasForeignKey(l => l.DefinitionId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class AlgorithmRevisionEntityConfiguration : IEntityTypeConfiguration<AlgorithmRevisionEntity>
{
    public void Configure(EntityTypeBuilder<AlgorithmRevisionEntity> builder)
    {
        builder.ToTable("algorithm_definition_revision");
        builder.HasKey(e => new { e.DefinitionId, e.Revision });
        builder.Property(e => e.DefinitionId).HasColumnName("definition_id");
        builder.Property(e => e.Revision).HasColumnName("revision");
        builder.Property(e => e.Kind).HasColumnName("kind").HasMaxLength(32).IsRequired();
        builder.Property(e => e.Source).HasColumnName("source").IsRequired();
        builder.Property(e => e.Payload).HasColumnName("payload");
        builder.Property(e => e.CreatedTimestamp).HasColumnName("created_timestamp");
    }
}

internal sealed class AlgorithmLabelEntityConfiguration : IEntityTypeConfiguration<AlgorithmLabelEntity>
{
    public void Configure(EntityTypeBuilder<AlgorithmLabelEntity> builder)
    {
        builder.ToTable("algorithm_definition_label");
        builder.HasKey(e => new { e.DefinitionId, e.Label });
        builder.Property(e => e.DefinitionId).HasColumnName("definition_id");
        builder.Property(e => e.Label).HasColumnName("label").HasMaxLength(200);
        builder.Property(e => e.Revision).HasColumnName("revision");
        builder.Property(e => e.IsMutable).HasColumnName("is_mutable");
    }
}
