using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Endato.Postgres.Database;

public sealed class EndatoCePhoneNumberEntityConfiguration : IEntityTypeConfiguration<EndatoCePhoneNumberEntity>
{
    public void Configure(EntityTypeBuilder<EndatoCePhoneNumberEntity> builder)
    {
        builder.ToTable("endato_ce_phone_number");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.EndatoCePersonId).IsRequired().HasColumnName("endato_ce_person_id");
        builder.Property(e => e.Number).HasMaxLength(18).IsRequired().HasColumnName("number");
        builder.Property(e => e.Type).HasMaxLength(15).IsRequired().HasColumnName("type");
        builder.Property(e => e.IsConnected).IsRequired().HasColumnName("is_connected");
        builder.Property(e => e.FirstReportedDate).IsRequired().HasColumnName("first_reported_date");
        builder.Property(e => e.LastReportedDate).IsRequired().HasColumnName("last_reported_date");
        builder.HasOne(e => e.EndatoCePerson).WithMany(p => p.PhoneNumbers).HasForeignKey(e => e.EndatoCePersonId).OnDelete(DeleteBehavior.Cascade);
    }
}