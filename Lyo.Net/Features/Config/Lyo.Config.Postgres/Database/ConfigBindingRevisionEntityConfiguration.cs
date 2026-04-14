using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Config.Postgres.Database;

public sealed class ConfigBindingRevisionEntityConfiguration : IEntityTypeConfiguration<ConfigBindingRevisionEntity>
{
    public void Configure(EntityTypeBuilder<ConfigBindingRevisionEntity> builder)
    {
        builder.ToTable("config_binding_revision");
        builder.HasKey(e => new { e.BindingId, e.Revision });
        builder.Property(e => e.BindingId).IsRequired().HasColumnName("binding_id").HasColumnType("uuid");
        builder.Property(e => e.Revision).IsRequired().HasColumnName("revision");
        builder.Property(e => e.ValueJson).HasColumnName("value_json").HasColumnType("jsonb");
        builder.Property(e => e.CreatedTimestamp).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
        builder.HasOne(e => e.Binding).WithMany().HasForeignKey(e => e.BindingId).OnDelete(DeleteBehavior.Cascade);
    }
}