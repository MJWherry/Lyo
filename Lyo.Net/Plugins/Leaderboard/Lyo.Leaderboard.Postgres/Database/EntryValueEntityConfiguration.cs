using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Leaderboard.Postgres.Database;

public sealed class EntryValueEntityConfiguration : IEntityTypeConfiguration<EntryValueEntity>
{
    public void Configure(EntityTypeBuilder<EntryValueEntity> builder)
    {
        builder.ToTable("entry_value");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.EntryId).HasColumnName("entry_id").HasColumnType("uuid");
        builder.Property(e => e.PropertyId).HasColumnName("property_id").HasColumnType("uuid");
        builder.Property(e => e.ChoiceKey).HasColumnName("choice_key").HasMaxLength(64);
        builder.Property(e => e.NumberValue).HasColumnName("number_value").HasColumnType("numeric(18,4)");
        builder.Property(e => e.TextValue).HasColumnName("text_value").HasMaxLength(2000);
        builder.Property(e => e.BoolValue).HasColumnName("bool_value");
        builder.Property(e => e.DateTimeValue).HasColumnName("datetime_value").HasColumnType("timestamp with time zone");
        builder.HasOne(e => e.Entry).WithMany(e => e.Values).HasForeignKey(e => e.EntryId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Property).WithMany().HasForeignKey(e => e.PropertyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(e => new { e.EntryId, e.PropertyId }).IsUnique().HasDatabaseName("ux_leaderboard_entry_value_entry_property");
        builder.HasIndex(e => new { e.PropertyId, e.NumberValue }).HasDatabaseName("ix_leaderboard_entry_value_property_number");
    }
}
