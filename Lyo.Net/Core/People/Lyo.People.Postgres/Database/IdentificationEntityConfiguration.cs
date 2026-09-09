using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.People.Postgres.Database;

public sealed class IdentificationEntityConfiguration : IEntityTypeConfiguration<IdentificationEntity>
{
    public void Configure(EntityTypeBuilder<IdentificationEntity> builder)
    {
        builder.ToTable("identification");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.PersonId).HasColumnName("person_id");
        builder.Property(e => e.Type).HasMaxLength(30).IsRequired().HasColumnName("type");
        builder.Property(e => e.Number).HasMaxLength(100).IsRequired().HasColumnName("number");
        builder.Property(e => e.IssuingCountry).HasMaxLength(3).HasColumnName("issuing_country");
        builder.Property(e => e.IssuingAuthority).HasMaxLength(200).HasColumnName("issuing_authority");
        builder.Property(e => e.IssueDate).HasColumnType("date").HasColumnName("issue_date");
        builder.Property(e => e.ExpiryDate).HasColumnType("date").HasColumnName("expiry_date");
        builder.Property(e => e.IsVerified).HasColumnName("is_verified");
        builder.Property(e => e.PhotoUrl).HasMaxLength(500).HasColumnName("photo_url");
        builder.Property(e => e.CreatedTimestamp).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
        builder.Property(e => e.UpdatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
        builder.HasIndex(e => e.PersonId).HasDatabaseName("ix_identification_person_id");
        builder.HasIndex(e => new { e.Type, e.Number }).HasDatabaseName("ix_identification_type_number");
        builder.HasOne(e => e.Person).WithMany().HasForeignKey(e => e.PersonId).OnDelete(DeleteBehavior.Cascade);
    }
}