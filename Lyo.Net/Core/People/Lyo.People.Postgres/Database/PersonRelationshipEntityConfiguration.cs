using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.People.Postgres.Database;

public sealed class PersonRelationshipEntityConfiguration : IEntityTypeConfiguration<PersonRelationshipEntity>
{
    public void Configure(EntityTypeBuilder<PersonRelationshipEntity> builder)
    {
        builder.ToTable("person_relationship");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.PersonId).HasColumnName("person_id");
        builder.Property(e => e.RelatedPersonId).HasColumnName("related_person_id");
        builder.Property(e => e.Type).HasMaxLength(30).IsRequired().HasColumnName("type");
        builder.Property(e => e.StartDate).HasColumnType("date").HasColumnName("start_date");
        builder.Property(e => e.EndDate).HasColumnType("date").HasColumnName("end_date");
        builder.Property(e => e.IsActive).HasColumnName("is_active");
        builder.Property(e => e.Notes).HasMaxLength(500).HasColumnName("notes");
        builder.Property(e => e.CreatedTimestamp).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
        builder.Property(e => e.UpdatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
        builder.HasIndex(e => e.PersonId).HasDatabaseName("ix_person_relationship_person_id");
        builder.HasIndex(e => e.RelatedPersonId).HasDatabaseName("ix_person_relationship_related_person_id");
        builder.HasOne(e => e.Person).WithMany().HasForeignKey(e => e.PersonId).OnDelete(DeleteBehavior.Cascade);
    }
}