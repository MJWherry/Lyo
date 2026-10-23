using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.FantasyFootball.Postgres.Database;

internal sealed class LeagueEntityConfiguration : IEntityTypeConfiguration<LeagueEntity>
{
    public void Configure(EntityTypeBuilder<LeagueEntity> builder)
    {
        builder.ToTable("league");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.EspnLeagueId).HasColumnName("espn_league_id");
        builder.Property(e => e.SeasonId).HasColumnName("season_id");
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(200);
        builder.Property(e => e.ScoringPeriodId).HasColumnName("scoring_period_id");
        builder.HasIndex(e => new { e.EspnLeagueId, e.SeasonId }).IsUnique().HasDatabaseName("ux_fantasy_football_league_espn");
    }
}

internal sealed class MemberEntityConfiguration : IEntityTypeConfiguration<MemberEntity>
{
    public void Configure(EntityTypeBuilder<MemberEntity> builder)
    {
        builder.ToTable("member");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.LeagueId).HasColumnName("league_id").HasColumnType("uuid");
        builder.Property(e => e.EspnMemberId).HasColumnName("espn_member_id").HasMaxLength(80).IsRequired();
        builder.Property(e => e.DisplayName).HasColumnName("display_name").HasMaxLength(200);
        builder.Property(e => e.FirstName).HasColumnName("first_name").HasMaxLength(200);
        builder.Property(e => e.LastName).HasColumnName("last_name").HasMaxLength(200);
        builder.HasOne(e => e.League).WithMany(e => e.Members).HasForeignKey(e => e.LeagueId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(e => new { e.LeagueId, e.EspnMemberId }).IsUnique().HasDatabaseName("ux_fantasy_football_member_espn");
    }
}

internal sealed class TeamEntityConfiguration : IEntityTypeConfiguration<TeamEntity>
{
    public void Configure(EntityTypeBuilder<TeamEntity> builder)
    {
        builder.ToTable("team");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.LeagueId).HasColumnName("league_id").HasColumnType("uuid");
        builder.Property(e => e.EspnTeamId).HasColumnName("espn_team_id");
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(200);
        builder.Property(e => e.Abbrev).HasColumnName("abbrev").HasMaxLength(16);
        builder.Property(e => e.Location).HasColumnName("location").HasMaxLength(200);
        builder.Property(e => e.Nickname).HasColumnName("nickname").HasMaxLength(200);
        builder.Property(e => e.PrimaryOwnerId).HasColumnName("primary_owner_id").HasMaxLength(80);
        builder.HasOne(e => e.League).WithMany(e => e.Teams).HasForeignKey(e => e.LeagueId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(e => new { e.LeagueId, e.EspnTeamId }).IsUnique().HasDatabaseName("ux_fantasy_football_team_espn");
    }
}

internal sealed class RosterEntryEntityConfiguration : IEntityTypeConfiguration<RosterEntryEntity>
{
    public void Configure(EntityTypeBuilder<RosterEntryEntity> builder)
    {
        builder.ToTable("roster_entry");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.TeamId).HasColumnName("team_id").HasColumnType("uuid");
        builder.Property(e => e.ScoringPeriodId).HasColumnName("scoring_period_id");
        builder.Property(e => e.PlayerId).HasColumnName("player_id");
        builder.Property(e => e.PlayerName).HasColumnName("player_name").HasMaxLength(200);
        builder.HasOne(e => e.Team).WithMany(e => e.Roster).HasForeignKey(e => e.TeamId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(e => new { e.TeamId, e.ScoringPeriodId, e.PlayerId }).IsUnique().HasDatabaseName("ux_fantasy_football_roster_player");
    }
}

internal sealed class ConversationEntityConfiguration : IEntityTypeConfiguration<ConversationEntity>
{
    public void Configure(EntityTypeBuilder<ConversationEntity> builder)
    {
        builder.ToTable("conversation");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.LeagueId).HasColumnName("league_id").HasColumnType("uuid");
        builder.Property(e => e.EspnTopicId).HasColumnName("espn_topic_id").HasMaxLength(80).IsRequired();
        builder.Property(e => e.Type).HasColumnName("type").HasMaxLength(64);
        builder.Property(e => e.UnreadCount).HasColumnName("unread_count");
        builder.Property(e => e.TotalMessageCount).HasColumnName("total_message_count");
        builder.Property(e => e.Subject).HasColumnName("subject").HasMaxLength(400);
        builder.HasOne(e => e.League).WithMany(e => e.Conversations).HasForeignKey(e => e.LeagueId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(e => new { e.LeagueId, e.EspnTopicId }).IsUnique().HasDatabaseName("ux_fantasy_football_conversation_topic");
    }
}

internal sealed class ConversationMemberEntityConfiguration : IEntityTypeConfiguration<ConversationMemberEntity>
{
    public void Configure(EntityTypeBuilder<ConversationMemberEntity> builder)
    {
        builder.ToTable("conversation_member");
        builder.HasKey(e => new { e.ConversationId, e.MemberId });
        builder.Property(e => e.ConversationId).HasColumnName("conversation_id").HasColumnType("uuid");
        builder.Property(e => e.MemberId).HasColumnName("member_id").HasColumnType("uuid");
        builder.HasOne(e => e.Conversation).WithMany(e => e.Members).HasForeignKey(e => e.ConversationId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Member).WithMany().HasForeignKey(e => e.MemberId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class MessageEntityConfiguration : IEntityTypeConfiguration<MessageEntity>
{
    public void Configure(EntityTypeBuilder<MessageEntity> builder)
    {
        builder.ToTable("message");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.ConversationId).HasColumnName("conversation_id").HasColumnType("uuid");
        builder.Property(e => e.EspnMessageId).HasColumnName("espn_message_id").HasMaxLength(80).IsRequired();
        builder.Property(e => e.AuthorEspnId).HasColumnName("author_espn_id").HasMaxLength(80);
        builder.Property(e => e.AuthorName).HasColumnName("author_name").HasMaxLength(200);
        builder.Property(e => e.Kind).HasColumnName("kind").HasMaxLength(32).IsRequired();
        builder.Property(e => e.Content).HasColumnName("content");
        builder.Property(e => e.SentAt).HasColumnName("sent_at").HasColumnType("timestamp with time zone");
        builder.Property(e => e.EspnTransactionId).HasColumnName("espn_transaction_id").HasMaxLength(80);
        builder.HasOne(e => e.Conversation).WithMany(e => e.Messages).HasForeignKey(e => e.ConversationId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(e => new { e.ConversationId, e.EspnMessageId }).IsUnique().HasDatabaseName("ux_fantasy_football_message_espn");
        builder.HasIndex(e => new { e.ConversationId, e.SentAt }).HasDatabaseName("ix_fantasy_football_message_sent");
    }
}

internal sealed class MessagePlayerEntityConfiguration : IEntityTypeConfiguration<MessagePlayerEntity>
{
    public void Configure(EntityTypeBuilder<MessagePlayerEntity> builder)
    {
        builder.ToTable("message_player");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.MessageId).HasColumnName("message_id").HasColumnType("uuid");
        builder.Property(e => e.Side).HasColumnName("side");
        builder.Property(e => e.EspnTeamId).HasColumnName("espn_team_id");
        builder.Property(e => e.PlayerId).HasColumnName("player_id");
        builder.Property(e => e.PlayerName).HasColumnName("player_name").HasMaxLength(200);
        builder.Property(e => e.Direction).HasColumnName("direction").HasMaxLength(16).IsRequired();
        builder.HasOne(e => e.Message).WithMany(e => e.Players).HasForeignKey(e => e.MessageId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class TradeEntityConfiguration : IEntityTypeConfiguration<TradeEntity>
{
    public void Configure(EntityTypeBuilder<TradeEntity> builder)
    {
        builder.ToTable("trade");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.LeagueId).HasColumnName("league_id").HasColumnType("uuid");
        builder.Property(e => e.EspnTransactionId).HasColumnName("espn_transaction_id").HasMaxLength(80).IsRequired();
        builder.Property(e => e.Type).HasColumnName("type").HasMaxLength(64);
        builder.Property(e => e.Status).HasColumnName("status").HasMaxLength(64);
        builder.Property(e => e.ScoringPeriodId).HasColumnName("scoring_period_id");
        builder.Property(e => e.IsPending).HasColumnName("is_pending");
        builder.Property(e => e.Comment).HasColumnName("comment");
        builder.HasOne(e => e.League).WithMany(e => e.Trades).HasForeignKey(e => e.LeagueId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(e => new { e.LeagueId, e.EspnTransactionId }).IsUnique().HasDatabaseName("ux_fantasy_football_trade_espn");
    }
}

internal sealed class TradeItemEntityConfiguration : IEntityTypeConfiguration<TradeItemEntity>
{
    public void Configure(EntityTypeBuilder<TradeItemEntity> builder)
    {
        builder.ToTable("trade_item");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.TradeId).HasColumnName("trade_id").HasColumnType("uuid");
        builder.Property(e => e.FromEspnTeamId).HasColumnName("from_espn_team_id");
        builder.Property(e => e.ToEspnTeamId).HasColumnName("to_espn_team_id");
        builder.Property(e => e.PlayerId).HasColumnName("player_id");
        builder.Property(e => e.PlayerName).HasColumnName("player_name").HasMaxLength(200);
        builder.HasOne(e => e.Trade).WithMany(e => e.Items).HasForeignKey(e => e.TradeId).OnDelete(DeleteBehavior.Cascade);
    }
}