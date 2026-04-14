using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Endato.Postgres.Database;

public sealed class EndatoCeAddressEntityConfiguration : IEntityTypeConfiguration<EndatoCeAddressEntity>
{
    public void Configure(EntityTypeBuilder<EndatoCeAddressEntity> builder)
    {
        builder.ToTable("endato_ce_address");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.EndatoCePersonId).IsRequired().HasColumnName("endato_ce_person_id");
        builder.Property(e => e.Street).HasMaxLength(75).IsRequired().HasColumnName("street");
        builder.Property(e => e.Unit).HasMaxLength(8).HasColumnName("unit");
        builder.Property(e => e.City).HasMaxLength(25).HasColumnName("city");
        builder.Property(e => e.State).HasMaxLength(2).HasColumnName("state");
        builder.Property(e => e.Zipcode).HasMaxLength(12).HasColumnName("zipcode");
        builder.Property(e => e.FirstReportedDate).IsRequired().HasColumnName("first_reported_date");
        builder.Property(e => e.LastReportedDate).IsRequired().HasColumnName("last_reported_date");
        builder.HasOne(e => e.EndatoCePerson).WithMany(p => p.Addresses).HasForeignKey(e => e.EndatoCePersonId).OnDelete(DeleteBehavior.Cascade);
    }
}