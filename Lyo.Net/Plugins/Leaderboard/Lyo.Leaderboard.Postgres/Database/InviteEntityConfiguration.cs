using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Leaderboard.Postgres.Database;

public sealed class InviteEntityConfiguration : IEntityTypeConfiguration<InviteEntity>
{
    public void Configure(EntityTypeBuilder<InviteEntity> builder)
    {
        builder.ToTable("invite");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.BoardId).HasColumnName("board_id").HasColumnType("uuid");
        builder.Property(e => e.TokenHash).HasColumnName("token_hash").HasMaxLength(64).IsRequired();
        builder.Property(e => e.ShareKind).HasColumnName("share_kind").HasMaxLength(16).IsRequired();
        builder.Property(e => e.Email).HasColumnName("email").HasMaxLength(320);
        builder.Property(e => e.Phone).HasColumnName("phone").HasMaxLength(32);
        builder.Property(e => e.InvitedUserId).HasColumnName("invited_user_id").HasColumnType("uuid");
        builder.Property(e => e.ShortUrlId).HasColumnName("short_url_id").HasMaxLength(100);
        builder.Property(e => e.Role).HasColumnName("role").HasMaxLength(32).IsRequired();
        builder.Property(e => e.MaxUses).HasColumnName("max_uses");
        builder.Property(e => e.UseCount).HasColumnName("use_count");
        builder.Property(e => e.ExpiresAt).HasColumnName("expires_at").HasColumnType("timestamp with time zone");
        builder.Property(e => e.RevokedAt).HasColumnName("revoked_at").HasColumnType("timestamp with time zone");
        builder.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id").HasColumnType("uuid");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.HasOne(e => e.Board).WithMany(e => e.Invites).HasForeignKey(e => e.BoardId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(e => e.TokenHash).IsUnique().HasDatabaseName("ux_leaderboard_invite_token_hash");
    }
}
