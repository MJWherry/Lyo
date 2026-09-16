using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Leaderboard.Postgres.Database;

public sealed class ScoringPropertyEntityConfiguration : IEntityTypeConfiguration<ScoringPropertyEntity>
{
    public void Configure(EntityTypeBuilder<ScoringPropertyEntity> builder)
    {
        builder.ToTable("scoring_property");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.BoardId).HasColumnName("board_id").HasColumnType("uuid");
        builder.Property(e => e.Key).HasColumnName("key").HasMaxLength(64).IsRequired();
        builder.Property(e => e.Label).HasColumnName("label").HasMaxLength(200).IsRequired();
        builder.Property(e => e.Description).HasColumnName("description").HasMaxLength(2000);
        builder.Property(e => e.ValueKind).HasColumnName("value_kind").HasMaxLength(32).IsRequired();
        builder.Property(e => e.Unit).HasColumnName("unit").HasMaxLength(32);
        builder.Property(e => e.Required).HasColumnName("required");
        builder.Property(e => e.SortOrder).HasColumnName("sort_order");
        builder.Property(e => e.MinValue).HasColumnName("min_value").HasColumnType("numeric(18,4)");
        builder.Property(e => e.MaxValue).HasColumnName("max_value").HasColumnType("numeric(18,4)");
        builder.Property(e => e.ValidationRegex).HasColumnName("validation_regex").HasMaxLength(500);
        builder.Property(e => e.ChoicesJson).HasColumnName("choices_json").HasColumnType("jsonb");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone");
        builder.HasOne(e => e.Board).WithMany(e => e.Properties).HasForeignKey(e => e.BoardId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(e => new { e.BoardId, e.Key }).IsUnique().HasDatabaseName("ux_leaderboard_property_board_key");
    }
}
