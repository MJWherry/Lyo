using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Leaderboard.Postgres.Database;

public sealed class BoardEntityConfiguration : IEntityTypeConfiguration<BoardEntity>
{
    public void Configure(EntityTypeBuilder<BoardEntity> builder)
    {
        builder.ToTable("board");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.Slug).HasColumnName("slug").HasMaxLength(80).IsRequired();
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(e => e.Description).HasColumnName("description").HasMaxLength(4000);
        builder.Property(e => e.OwnerUserId).HasColumnName("owner_user_id").HasColumnType("uuid");
        builder.Property(e => e.TenantId).HasColumnName("tenant_id").HasColumnType("uuid");
        builder.Property(e => e.Visibility).HasColumnName("visibility").HasMaxLength(32).IsRequired();
        builder.Property(e => e.JoinPolicy).HasColumnName("join_policy").HasMaxLength(32).IsRequired();
        builder.Property(e => e.MembersCanSubmit).HasColumnName("members_can_submit");
        builder.Property(e => e.RequireVerification).HasColumnName("require_verification");
        builder.Property(e => e.MembersCanEditOwn).HasColumnName("members_can_edit_own");
        builder.Property(e => e.MembersCanDeleteOwn).HasColumnName("members_can_delete_own");
        builder.Property(e => e.AllowMultipleEntries).HasColumnName("allow_multiple_entries");
        builder.Property(e => e.AggregationKind).HasColumnName("aggregation_kind").HasMaxLength(32).IsRequired();
        builder.Property(e => e.AggregationN).HasColumnName("aggregation_n");
        builder.Property(e => e.SortDirection).HasColumnName("sort_direction").HasMaxLength(16).IsRequired();
        builder.Property(e => e.TieBreak).HasColumnName("tie_break").HasMaxLength(32).IsRequired();
        builder.Property(e => e.TieBreakPropertyKey).HasColumnName("tie_break_property_key").HasMaxLength(64);
        builder.Property(e => e.AlgorithmKey).HasColumnName("algorithm_key").HasMaxLength(200).IsRequired();
        builder.Property(e => e.StartsAt).HasColumnName("starts_at").HasColumnType("timestamp with time zone");
        builder.Property(e => e.EndsAt).HasColumnName("ends_at").HasColumnType("timestamp with time zone");
        builder.Property(e => e.ArchivedAt).HasColumnName("archived_at").HasColumnType("timestamp with time zone");
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at").HasColumnType("timestamp with time zone");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone");
        builder.HasIndex(e => e.Slug).IsUnique().HasDatabaseName("ux_leaderboard_board_slug");
        builder.HasIndex(e => e.OwnerUserId).HasDatabaseName("ix_leaderboard_board_owner");
    }
}
