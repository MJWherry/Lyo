using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Geolocation.Postgres.Database;

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
        builder.Property(e => e.StreetAddress).HasMaxLength(200).HasColumnName("street_address");
        builder.Property(e => e.StreetAddressLine2).HasMaxLength(200).HasColumnName("street_address_line2");
        builder.Property(e => e.Unit).HasMaxLength(8).HasColumnName("unit");
        builder.Property(e => e.UnitType).HasMaxLength(12).HasColumnName("unit_type");
        builder.Property(e => e.City).HasMaxLength(50).HasColumnName("city");
        builder.Property(e => e.SubLocality).HasMaxLength(50).HasColumnName("sub_locality");
        builder.Property(e => e.State).HasMaxLength(50).HasColumnName("state");
        builder.Property(e => e.Province).HasMaxLength(50).HasColumnName("province");
        builder.Property(e => e.Zipcode).HasMaxLength(5).HasColumnName("zipcode");
        builder.Property(e => e.Zipcode4).HasMaxLength(4).HasColumnName("zipcode4");
        builder.Property(e => e.PostalCode).HasMaxLength(20).HasColumnName("postal_code");
        builder.Property(e => e.CountryCode).HasMaxLength(3).IsRequired().HasColumnName("country_code");
        builder.Property(e => e.County).HasMaxLength(50).HasColumnName("county");
        builder.Property(e => e.SubAdministrativeArea).HasMaxLength(50).HasColumnName("sub_administrative_area");
        builder.Property(e => e.FullAddress).HasMaxLength(200).HasColumnName("full_address");
        builder.Property(e => e.Coordinates).HasColumnName("coordinates");
        builder.Property(e => e.IsDeliverable).HasColumnName("is_deliverable");
        builder.Property(e => e.IsMergedAddress).HasColumnName("is_merged_address");
        builder.Property(e => e.IsPublic).HasColumnName("is_public");
        builder.Property(e => e.PropertyIndicator).HasMaxLength(32).HasColumnName("property_indicator");
        builder.Property(e => e.BldgCode).HasMaxLength(32).HasColumnName("bldg_code");
        builder.Property(e => e.UtilityCode).HasMaxLength(32).HasColumnName("utility_code");
        builder.Property(e => e.UnitCount).HasColumnName("unit_count");
        builder.Property(e => e.FirstReportedDate).HasColumnType("date").HasColumnName("first_reported_date");
        builder.Property(e => e.LastReportedDate).HasColumnType("date").HasColumnName("last_reported_date");
        builder.Property(e => e.PublicFirstSeenDate).HasColumnType("date").HasColumnName("public_first_seen_date");
        builder.Property(e => e.GeocodeConfidence).HasColumnName("geocode_confidence");
        builder.Property(e => e.MetadataJson).HasColumnName("metadata");
        builder.Property(e => e.CreatedTimestamp).HasColumnName("created_timestamp");
        builder.Property(e => e.UpdatedTimestamp).HasColumnName("updated_timestamp");
    }
}