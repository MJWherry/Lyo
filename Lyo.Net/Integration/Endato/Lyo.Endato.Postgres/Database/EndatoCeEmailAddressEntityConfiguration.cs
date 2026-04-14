using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Endato.Postgres.Database;

public sealed class EndatoCeEmailAddressEntityConfiguration : IEntityTypeConfiguration<EndatoCeEmailAddressEntity>
{
    public void Configure(EntityTypeBuilder<EndatoCeEmailAddressEntity> builder)
    {
        builder.ToTable("endato_ce_email_address");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.EndatoCePersonId).IsRequired().HasColumnName("endato_ce_person_id");
        builder.Property(e => e.Email).HasMaxLength(100).IsRequired().HasColumnName("email");
        builder.Property(e => e.IsValidated).IsRequired().HasColumnName("is_validated");
        builder.Property(e => e.IsBusiness).IsRequired().HasColumnName("is_business");
        builder.HasOne(e => e.EndatoCePerson).WithMany(p => p.EmailAddresses).HasForeignKey(e => e.EndatoCePersonId).OnDelete(DeleteBehavior.Cascade);
    }
}