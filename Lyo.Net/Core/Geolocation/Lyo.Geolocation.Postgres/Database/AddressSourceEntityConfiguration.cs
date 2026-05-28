using Lyo.EntityReference.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Geolocation.Postgres.Database;

public sealed class AddressSourceEntityConfiguration : IEntityTypeConfiguration<AddressSourceEntity>
{
    public void Configure(EntityTypeBuilder<AddressSourceEntity> builder)
    {
        builder.ToTable("address_source");
        builder.HasKey(e => e.Id);
        builder.ConfigureEntitySourceColumns("ix_address_source_source_entity");
        builder.Property(e => e.AddressId).HasColumnName("address_id").HasColumnType("uuid");
        builder.HasOne(e => e.Address)
            .WithMany(a => a.Sources)
            .HasForeignKey(e => e.AddressId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
