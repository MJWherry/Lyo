using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Leaderboard.Postgres.Database;

public sealed class MemberEntityConfiguration : IEntityTypeConfiguration<MemberEntity>
{
    public void Configure(EntityTypeBuilder<MemberEntity> builder)
    {
        builder.ToTable("member");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.BoardId).HasColumnName("board_id").HasColumnType("uuid");
        builder.Property(e => e.UserId).HasColumnName("user_id").HasColumnType("uuid");
        builder.Property(e => e.DisplayName).HasColumnName("display_name").HasMaxLength(200).IsRequired();
        builder.Property(e => e.Role).HasColumnName("role").HasMaxLength(32).IsRequired();
        builder.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
        builder.Property(e => e.InvitedByUserId).HasColumnName("invited_by_user_id").HasColumnType("uuid");
        builder.Property(e => e.JoinedAt).HasColumnName("joined_at").HasColumnType("timestamp with time zone");
        builder.Property(e => e.RemovedAt).HasColumnName("removed_at").HasColumnType("timestamp with time zone");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone");
        builder.HasOne(e => e.Board).WithMany(e => e.Members).HasForeignKey(e => e.BoardId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(e => new { e.BoardId, e.UserId }).IsUnique().HasDatabaseName("ux_leaderboard_member_board_user").HasFilter("\"user_id\" IS NOT NULL");
        builder.HasIndex(e => new { e.BoardId, e.Status }).HasDatabaseName("ix_leaderboard_member_board_status");
    }
}
