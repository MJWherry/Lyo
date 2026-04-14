using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.People.Postgres.Database;

public sealed class EmailAddressEntityConfiguration : IEntityTypeConfiguration<EmailAddressEntity>
{
    public void Configure(EntityTypeBuilder<EmailAddressEntity> builder)
    {
        builder.ToTable("email_address");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.Email).HasMaxLength(256).IsRequired().HasColumnName("email");
        builder.Property(e => e.VerifiedAt).HasColumnType("timestamp with time zone").HasColumnName("verified_at");
        builder.Property(e => e.Label).HasMaxLength(100).HasColumnName("label");
        builder.Property(e => e.CreatedTimestamp).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
        builder.Property(e => e.UpdatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
        builder.HasIndex(e => e.Email).HasDatabaseName("ix_email_address_email");
    }
}