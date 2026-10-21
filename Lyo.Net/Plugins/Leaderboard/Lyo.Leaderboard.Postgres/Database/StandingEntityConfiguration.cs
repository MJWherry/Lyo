using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Leaderboard.Postgres.Database;

public sealed class StandingEntityConfiguration : IEntityTypeConfiguration<StandingEntity>
{
    public void Configure(EntityTypeBuilder<StandingEntity> builder)
    {
        builder.ToTable("standing");
        builder.HasKey(e => new { e.BoardId, e.MemberId });
        builder.Property(e => e.BoardId).HasColumnName("board_id").HasColumnType("uuid");
        builder.Property(e => e.MemberId).HasColumnName("member_id").HasColumnType("uuid");
        builder.Property(e => e.EntryScore).HasColumnName("entry_score").HasColumnType("numeric(18,4)");
        builder.Property(e => e.AdjustmentScore).HasColumnName("adjustment_score").HasColumnType("numeric(18,4)");
        builder.Property(e => e.TotalScore).HasColumnName("total_score").HasColumnType("numeric(18,4)");
        builder.Property(e => e.EntryCount).HasColumnName("entry_count");
        builder.Property(e => e.LastEntryAt).HasColumnName("last_entry_at").HasColumnType("timestamp with time zone");
        builder.Property(e => e.Rank).HasColumnName("rank");
        builder.Property(e => e.ComputedAt).HasColumnName("computed_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.HasOne(e => e.Board).WithMany(e => e.Standings).HasForeignKey(e => e.BoardId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Member).WithMany().HasForeignKey(e => e.MemberId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(e => new { e.BoardId, e.TotalScore }).HasDatabaseName("ix_leaderboard_standing_board_score");
    }
}