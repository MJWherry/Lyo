using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.People.Postgres.Database;

public sealed class PhoneNumberEntityConfiguration : IEntityTypeConfiguration<PhoneNumberEntity>
{
    public void Configure(EntityTypeBuilder<PhoneNumberEntity> builder)
    {
        builder.ToTable("phone_number");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.Number).HasMaxLength(20).IsRequired().HasColumnName("number");
        builder.Property(e => e.CountryCode).HasMaxLength(3).HasColumnName("country_code");
        builder.Property(e => e.CountryCodeString).HasMaxLength(10).HasColumnName("country_code_string");
        builder.Property(e => e.TechnologyType).HasMaxLength(20).HasColumnName("technology_type");
        builder.Property(e => e.VerifiedAt).HasColumnType("timestamp with time zone").HasColumnName("verified_at");
        builder.Property(e => e.Label).HasMaxLength(100).HasColumnName("label");
        builder.Property(e => e.CreatedTimestamp).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
        builder.Property(e => e.UpdatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
        builder.HasIndex(e => e.Number).HasDatabaseName("ix_phone_number_number");
    }
}