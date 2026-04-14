using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Endato.Postgres.Database;

public sealed class EndatoPsAddressEntityConfiguration : IEntityTypeConfiguration<EndatoPsAddressEntity>
{
    public void Configure(EntityTypeBuilder<EndatoPsAddressEntity> builder)
    {
        builder.ToTable("endato_ps_address");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.EndatoPersonId).IsRequired().HasColumnName("endato_person_id");
        builder.Property(e => e.IsDeliverable).IsRequired().HasColumnName("is_deliverable");
        builder.Property(e => e.IsMergedAddress).IsRequired().HasColumnName("is_merged_address");
        builder.Property(e => e.IsPublic).IsRequired().HasColumnName("is_public");
        builder.Property(e => e.AddressHash).HasMaxLength(25).IsRequired().HasColumnName("address_hash");
        builder.Property(e => e.HouseNumber).HasColumnName("house_number");
        builder.Property(e => e.StreetPreDirection).HasMaxLength(12).HasColumnName("street_pre_direction");
        builder.Property(e => e.StreetName).HasMaxLength(50).HasColumnName("street_name");
        builder.Property(e => e.StreetPostDirection).HasMaxLength(12).HasColumnName("street_post_direction");
        builder.Property(e => e.StreetType).HasMaxLength(12).HasColumnName("street_type");
        builder.Property(e => e.Unit).HasMaxLength(8).HasColumnName("unit");
        builder.Property(e => e.UnitType).HasMaxLength(12).HasColumnName("unit_type");
        builder.Property(e => e.City).HasMaxLength(25).HasColumnName("city");
        builder.Property(e => e.State).HasMaxLength(2).HasColumnName("state");
        builder.Property(e => e.County).HasMaxLength(50).HasColumnName("county");
        builder.Property(e => e.Zipcode).HasMaxLength(5).HasColumnName("zipcode");
        builder.Property(e => e.Zipcode4).HasMaxLength(4).HasColumnName("zipcode4");
        builder.Property(e => e.FullAddress).HasMaxLength(100).HasColumnName("full_address");
        builder.Property(e => e.Coordinates).HasColumnType("point").HasColumnName("coordinates");
        builder.Property(e => e.PhoneNumbers).HasColumnName("phone_numbers");
        builder.Property(e => e.OrderNumber).IsRequired().HasColumnName("order_number");
        builder.Property(e => e.FirstReportedDate).IsRequired().HasColumnName("first_reported_date");
        builder.Property(e => e.LastReportedDate).IsRequired().HasColumnName("last_reported_date");
        builder.Property(e => e.PublicFirstSeenDate).IsRequired().HasColumnName("public_first_seen_date");
        builder.Property(e => e.TotalFirstSeenDate).IsRequired().HasColumnName("total_first_seen_date");
        builder.HasOne(e => e.EndatoPerson).WithMany(p => p.Addresses).HasForeignKey(e => e.EndatoPersonId).OnDelete(DeleteBehavior.Cascade);
    }
}