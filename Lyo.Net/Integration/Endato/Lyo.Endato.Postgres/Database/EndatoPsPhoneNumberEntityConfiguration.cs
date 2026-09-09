using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Endato.Postgres.Database;

public sealed class EndatoPsPhoneNumberEntityConfiguration : IEntityTypeConfiguration<EndatoPsPhoneNumberEntity>
{
    public void Configure(EntityTypeBuilder<EndatoPsPhoneNumberEntity> builder)
    {
        builder.ToTable("endato_ps_phone_number");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.EndatoPersonId).IsRequired().HasColumnName("endato_person_id");
        builder.Property(e => e.Number).HasMaxLength(15).IsRequired().HasColumnName("number");
        builder.Property(e => e.Company).HasMaxLength(100).HasColumnName("company");
        builder.Property(e => e.Location).HasMaxLength(75).HasColumnName("location");
        builder.Property(e => e.Type).HasMaxLength(50).HasColumnName("type");
        builder.Property(e => e.IsConnected).IsRequired().HasColumnName("is_connected");
        builder.Property(e => e.IsPublic).IsRequired().HasColumnName("is_public");
        builder.Property(e => e.Coordinates).HasColumnType("point").HasColumnName("coordinates");
        builder.Property(e => e.OrderNumber).IsRequired().HasColumnName("order_number");
        builder.Property(e => e.FirstReportedDate).IsRequired().HasColumnName("first_reported_date");
        builder.Property(e => e.LastReportedDate).IsRequired().HasColumnName("last_reported_date");
        builder.Property(e => e.PublicFirstSeenDate).IsRequired().HasColumnName("public_first_seen_date");
        builder.Property(e => e.TotalFirstSeenDate).IsRequired().HasColumnName("total_first_seen_date");
        builder.HasOne(e => e.EndatoPerson).WithMany(p => p.PhoneNumbers).HasForeignKey(e => e.EndatoPersonId).OnDelete(DeleteBehavior.Cascade);
    }
}