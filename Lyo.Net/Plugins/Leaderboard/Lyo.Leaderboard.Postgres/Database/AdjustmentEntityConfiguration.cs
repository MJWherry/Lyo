using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Leaderboard.Postgres.Database;

public sealed class AdjustmentEntityConfiguration : IEntityTypeConfiguration<AdjustmentEntity>
{
    public void Configure(EntityTypeBuilder<AdjustmentEntity> builder)
    {
        builder.ToTable("adjustment");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.BoardId).HasColumnName("board_id").HasColumnType("uuid");
        builder.Property(e => e.MemberId).HasColumnName("member_id").HasColumnType("uuid");
        builder.Property(e => e.EntryId).HasColumnName("entry_id").HasColumnType("uuid");
        builder.Property(e => e.Delta).HasColumnName("delta").HasColumnType("numeric(18,4)");
        builder.Property(e => e.Reason).HasColumnName("reason").HasMaxLength(1000).IsRequired();
        builder.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id").HasColumnType("uuid");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at").HasColumnType("timestamp with time zone");
        builder.HasOne(e => e.Board).WithMany(e => e.Adjustments).HasForeignKey(e => e.BoardId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Member).WithMany().HasForeignKey(e => e.MemberId).OnDelete(DeleteBehavior.Restrict);
    }
}
