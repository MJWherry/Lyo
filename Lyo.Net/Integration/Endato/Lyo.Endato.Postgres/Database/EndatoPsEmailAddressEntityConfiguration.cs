using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Endato.Postgres.Database;

public sealed class EndatoPsEmailAddressEntityConfiguration : IEntityTypeConfiguration<EndatoPsEmailAddressEntity>
{
    public void Configure(EntityTypeBuilder<EndatoPsEmailAddressEntity> builder)
    {
        builder.ToTable("endato_ps_email_address");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.EndatoPersonId).IsRequired().HasColumnName("endato_person_id");
        builder.Property(e => e.Address).HasMaxLength(100).IsRequired().HasColumnName("address");
        builder.Property(e => e.OrderNumber).IsRequired().HasColumnName("order_number");
        builder.Property(e => e.IsPremium).IsRequired().HasColumnName("is_premium");
        builder.Property(e => e.NonBusiness).IsRequired().HasColumnName("non_business");
        builder.HasOne(e => e.EndatoPerson).WithMany(p => p.EmailAddresses).HasForeignKey(e => e.EndatoPersonId).OnDelete(DeleteBehavior.Cascade);
    }
}