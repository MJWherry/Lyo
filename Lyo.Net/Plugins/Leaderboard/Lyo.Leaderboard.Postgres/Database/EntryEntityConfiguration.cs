using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Leaderboard.Postgres.Database;

public sealed class EntryEntityConfiguration : IEntityTypeConfiguration<EntryEntity>
{
    public void Configure(EntityTypeBuilder<EntryEntity> builder)
    {
        builder.ToTable("entry");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.BoardId).HasColumnName("board_id").HasColumnType("uuid");
        builder.Property(e => e.MemberId).HasColumnName("member_id").HasColumnType("uuid");
        builder.Property(e => e.SubmittedByUserId).HasColumnName("submitted_by_user_id").HasColumnType("uuid");
        builder.Property(e => e.Title).HasColumnName("title").HasMaxLength(200);
        builder.Property(e => e.Notes).HasColumnName("notes").HasMaxLength(4000);
        builder.Property(e => e.OccurredAt).HasColumnName("occurred_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
        builder.Property(e => e.VerifiedByUserId).HasColumnName("verified_by_user_id").HasColumnType("uuid");
        builder.Property(e => e.VerifiedAt).HasColumnName("verified_at").HasColumnType("timestamp with time zone");
        builder.Property(e => e.RejectionReason).HasColumnName("rejection_reason").HasMaxLength(1000);
        builder.Property(e => e.ComputedScore).HasColumnName("computed_score").HasColumnType("numeric(18,4)");
        builder.Property(e => e.ScoreBreakdownJson).HasColumnName("score_breakdown_json").HasColumnType("jsonb");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone");
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at").HasColumnType("timestamp with time zone");
        builder.HasOne(e => e.Board).WithMany(e => e.Entries).HasForeignKey(e => e.BoardId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Member).WithMany().HasForeignKey(e => e.MemberId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(e => new { e.BoardId, e.MemberId, e.Status }).HasDatabaseName("ix_leaderboard_entry_board_member_status");
        builder.HasIndex(e => new { e.BoardId, e.Status, e.ComputedScore }).HasDatabaseName("ix_leaderboard_entry_board_status_score");
    }
}