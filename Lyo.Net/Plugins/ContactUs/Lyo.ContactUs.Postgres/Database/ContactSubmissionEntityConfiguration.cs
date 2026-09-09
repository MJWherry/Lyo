using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.ContactUs.Postgres.Database;

public sealed class ContactSubmissionEntityConfiguration : IEntityTypeConfiguration<ContactSubmissionEntity>
{
    public void Configure(EntityTypeBuilder<ContactSubmissionEntity> builder)
    {
        builder.ToTable("contact_submissions");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).IsRequired().HasColumnName("id");
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired().HasColumnName("name");
        builder.Property(e => e.Email).HasMaxLength(320).IsRequired().HasColumnName("email");
        builder.Property(e => e.Subject).HasMaxLength(500).IsRequired().HasColumnName("subject");
        builder.Property(e => e.Message).HasMaxLength(10000).IsRequired().HasColumnName("message");
        builder.Property(e => e.Phone).HasMaxLength(50).HasColumnName("phone");
        builder.Property(e => e.Company).HasMaxLength(200).HasColumnName("company");
        builder.Property(e => e.CreatedTimestamp).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
        builder.HasIndex(e => e.CreatedTimestamp).HasDatabaseName("ix_contact_submissions_created_timestamp");
        builder.HasIndex(e => e.Email).HasDatabaseName("ix_contact_submissions_email");
    }
}