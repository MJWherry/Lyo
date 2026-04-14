using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.People.Postgres.Database;

public sealed class AddressEntityConfiguration : IEntityTypeConfiguration<AddressEntity>
{
    public void Configure(EntityTypeBuilder<AddressEntity> builder)
    {
        builder.ToTable("address");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.HouseNumber).HasMaxLength(12).HasColumnName("house_number");
        builder.Property(e => e.StreetPreDirection).HasMaxLength(12).HasColumnName("street_pre_direction");
        builder.Property(e => e.StreetName).HasMaxLength(50).HasColumnName("street_name");
        builder.Property(e => e.StreetPostDirection).HasMaxLength(12).HasColumnName("street_post_direction");
        builder.Property(e => e.StreetType).HasMaxLength(12).HasColumnName("street_type");
        builder.Property(e => e.Unit).HasMaxLength(8).HasColumnName("unit");
        builder.Property(e => e.UnitType).HasMaxLength(12).HasColumnName("unit_type");
        builder.Property(e => e.StreetAddress).HasMaxLength(200).HasColumnName("street_address");
        builder.Property(e => e.StreetAddressLine2).HasMaxLength(200).HasColumnName("street_address_line2");
        builder.Property(e => e.City).HasMaxLength(25).HasColumnName("city");
        builder.Property(e => e.State).HasMaxLength(2).HasColumnName("state");
        builder.Property(e => e.County).HasMaxLength(50).HasColumnName("county");
        builder.Property(e => e.Zipcode).HasMaxLength(5).HasColumnName("zipcode");
        builder.Property(e => e.Zipcode4).HasMaxLength(4).HasColumnName("zipcode4");
        builder.Property(e => e.PostalCode).HasMaxLength(20).HasColumnName("postal_code");
        builder.Property(e => e.CountryCode).HasMaxLength(3).IsRequired().HasColumnName("country_code");
        builder.Property(e => e.FullAddress).HasMaxLength(200).HasColumnName("full_address");
        builder.Property(e => e.Coordinates).HasColumnType("point").HasColumnName("coordinates");
        builder.Property(e => e.CreatedTimestamp).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
        builder.Property(e => e.UpdatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
        builder.HasIndex(e => e.CountryCode).HasDatabaseName("ix_address_country_code");
    }
}