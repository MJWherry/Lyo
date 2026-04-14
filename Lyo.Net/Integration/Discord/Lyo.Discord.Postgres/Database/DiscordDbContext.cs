using Microsoft.EntityFrameworkCore;

namespace Lyo.Discord.Postgres.Database;

public class DiscordDbContext : DbContext
{
    public DbSet<DiscordUser> DiscordUsers => Set<DiscordUser>();

    public DbSet<DiscordGuild> DiscordGuilds => Set<DiscordGuild>();

    public DbSet<DiscordChannel> DiscordChannels => Set<DiscordChannel>();

    public DbSet<DiscordEmoji> DiscordEmojis => Set<DiscordEmoji>();

    public DbSet<DiscordRole> DiscordRoles => Set<DiscordRole>();

    public DbSet<DiscordInteraction> DiscordInteractions => Set<DiscordInteraction>();

    public DbSet<DiscordMessage> DiscordMessages => Set<DiscordMessage>();

    public DbSet<DiscordAttachment> DiscordAttachments => Set<DiscordAttachment>();

    public DbSet<DiscordReaction> DiscordReactions => Set<DiscordReaction>();

    public DbSet<DiscordMember> DiscordMembers => Set<DiscordMember>();

    public DiscordDbContext(DbContextOptions<DiscordDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Must match migration snapshot / designer (EF Core 10+ validates model on Migrate).
        modelBuilder.HasDefaultSchema(PostgresDiscordOptions.Schema).HasAnnotation("ProductVersion", "10.0.0").HasAnnotation("Relational:MaxIdentifierLength", 63);
        modelBuilder.UseIdentityByDefaultColumns();
        modelBuilder.Entity<DiscordUser>(e => {
            e.ToTable("discord_user");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(x => x.Username).HasMaxLength(35).HasColumnName("username");
            e.Property(x => x.Discriminator).HasColumnName("discriminator");
            e.Property(x => x.Email).HasMaxLength(50).HasColumnName("email");
            e.Property(x => x.Locale).HasMaxLength(12).HasColumnName("locale");
            e.Property(x => x.IsVerified).HasColumnName("is_verified");
            e.Property(x => x.IsBot).HasColumnName("is_bot");
            e.Property(x => x.IsSystem).HasColumnName("is_system");
            e.Property(x => x.IsMfaEnabled).HasColumnName("is_mfa_enabled");
            e.Property(x => x.PremiumLevel).HasMaxLength(12).HasColumnName("premium_level");
            e.Property(x => x.UserCreatedDate).HasColumnType("timestamp with time zone").HasColumnName("user_created_date");
            e.Property(x => x.CreatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
            e.Property(x => x.UpdatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
        });

        modelBuilder.Entity<DiscordGuild>(e => {
            e.ToTable("discord_guild");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(x => x.OwnerId).HasColumnName("owner_id");
            e.Property(x => x.Name).HasMaxLength(50).HasColumnName("name");
            e.Property(x => x.Description).HasMaxLength(100).HasColumnName("description");
            e.Property(x => x.MemberCount).HasColumnName("member_count");
            e.Property(x => x.CurrentSubscriptionCount).HasColumnName("current_subscription_count").ValueGeneratedOnAdd().HasDefaultValue(0);
            e.Property(x => x.IsLarge).HasColumnName("is_large");
            e.Property(x => x.IsNSFW).HasColumnName("is_nsfw");
            e.Property(x => x.IsUnavailable).HasColumnName("is_unavailable");
            e.Property(x => x.GuildCreatedDate).HasColumnType("timestamp with time zone").HasColumnName("guild_created_date");
            e.Property(x => x.JoinedDate).HasColumnType("timestamp with time zone").HasColumnName("joined_date");
            e.Property(x => x.CreatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
            e.Property(x => x.UpdatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
            e.HasOne<DiscordUser>().WithMany().HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_discord_guild_owner_discord_user");
        });

        modelBuilder.Entity<DiscordChannel>(e => {
            e.ToTable("discord_channel");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(x => x.GuildId).HasColumnName("guild_id");
            e.Property(x => x.Name).HasMaxLength(50).HasColumnName("name");
            e.Property(x => x.Topic).HasMaxLength(1024).HasColumnName("topic");
            e.Property(x => x.ChannelType).HasMaxLength(10).HasColumnName("type");
            e.Property(x => x.IsCategory).HasColumnName("is_category");
            e.Property(x => x.IsNSFW).HasColumnName("is_nsfw");
            e.Property(x => x.IsPrivate).HasColumnName("is_private");
            e.Property(x => x.IsThread).HasColumnName("is_thread");
            e.Property(x => x.Position).HasColumnName("position");
            e.Property(x => x.ParentId).HasColumnName("parent_id");
            e.Property(x => x.ChannelCreated).HasColumnType("timestamp with time zone").HasColumnName("channel_created");
            e.Property(x => x.CreatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
            e.Property(x => x.UpdatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
            e.HasOne<DiscordGuild>().WithMany().HasForeignKey(x => x.GuildId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_discord_channel_guild");
            e.HasIndex(x => x.GuildId).HasDatabaseName("ix_discord_channel_guild_id");
        });

        modelBuilder.Entity<DiscordEmoji>(e => {
            e.ToTable("discord_emoji");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(x => x.GuildId).HasColumnName("guild_id");
            e.Property(x => x.Name).HasMaxLength(50).HasColumnName("name");
            e.Property(x => x.Url).HasMaxLength(200).HasColumnName("url");
            e.Property(x => x.IsAnimated).HasColumnName("is_animated");
            e.Property(x => x.IsAvailable).HasColumnName("is_available");
            e.Property(x => x.IsManaged).HasColumnName("is_managed");
            e.Property(x => x.RequiresColons).HasColumnName("requires_colons");
            e.Property(x => x.EmojiCreatedDate).HasColumnType("timestamp with time zone").HasColumnName("emoji_created_date");
            e.Property(x => x.CreatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
            e.Property(x => x.UpdatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
            e.HasOne<DiscordGuild>().WithMany().HasForeignKey(x => x.GuildId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_discord_emoji_guild");
            e.HasIndex(x => x.GuildId).HasDatabaseName("ix_discord_emoji_guild_id");
        });

        modelBuilder.Entity<DiscordRole>(e => {
            e.ToTable("discord_role");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(x => x.GuildId).HasColumnName("guild_id");
            e.Property(x => x.EmojiId).HasColumnName("emoji_id");
            e.Property(x => x.Name).HasMaxLength(50).HasColumnName("name");
            e.Property(x => x.Icon).HasMaxLength(40).HasColumnName("icon");
            e.Property(x => x.Color).HasMaxLength(7).HasColumnName("color");
            e.Property(x => x.IsHoisted).HasColumnName("is_hoisted");
            e.Property(x => x.IsManaged).HasColumnName("is_managed");
            e.Property(x => x.IsMentionable).HasColumnName("is_mentionable");
            e.Property(x => x.Position).HasColumnName("position");
            e.Property(x => x.RoleCreatedDate).HasColumnType("timestamp with time zone").HasColumnName("role_created_date");
            e.Property(x => x.CreatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
            e.Property(x => x.UpdatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
            e.HasOne<DiscordGuild>().WithMany().HasForeignKey(x => x.GuildId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_discord_role_guild");
            e.HasOne<DiscordEmoji>().WithMany().HasForeignKey(x => x.EmojiId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_discord_role_emoji");
            e.HasIndex(x => x.GuildId).HasDatabaseName("ix_discord_role_guild_id");
        });

        modelBuilder.Entity<DiscordInteraction>(e => {
            e.ToTable("discord_interaction");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(x => x.AuthorId).HasColumnName("author_id");
            e.Property(x => x.ChannelId).HasColumnName("channel_id");
            e.Property(x => x.GuildId).HasColumnName("guild_id");
            e.Property(x => x.Content).HasMaxLength(300).HasColumnName("content");
            e.Property(x => x.InteractionCreatedDate).HasColumnType("timestamp with time zone").HasColumnName("interaction_created_date");
            e.HasOne<DiscordGuild>().WithMany().HasForeignKey(x => x.GuildId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_discord_interaction_guild");
            e.HasOne<DiscordChannel>().WithMany().HasForeignKey(x => x.ChannelId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_discord_interaction_channel");
            e.HasOne<DiscordUser>().WithMany().HasForeignKey(x => x.AuthorId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_discord_interaction_author");
            e.HasIndex(x => x.AuthorId);
            e.HasIndex(x => x.ChannelId).HasDatabaseName("ix_discord_interaction_channel_id");
            e.HasIndex(x => x.GuildId).HasDatabaseName("ix_discord_interaction_guild_id");
        });

        modelBuilder.Entity<DiscordMessage>(e => {
            e.ToTable("discord_message");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(x => x.AuthorId).HasColumnName("author_id");
            e.Property(x => x.ChannelId).HasColumnName("channel_id");
            e.Property(x => x.GuildId).HasColumnName("guild_id");
            e.Property(x => x.Content).HasColumnName("content");
            e.Property(x => x.IsEdited).HasColumnName("is_edited").ValueGeneratedOnAdd().HasDefaultValue(false);
            e.Property(x => x.IsDeleted).HasColumnName("is_deleted").ValueGeneratedOnAdd().HasDefaultValue(false);
            e.Property(x => x.MessageCreatedDate).HasColumnType("timestamp with time zone").HasColumnName("message_created_date");
            e.Property(x => x.CreatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
            e.Property(x => x.UpdatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
            e.HasOne<DiscordGuild>().WithMany().HasForeignKey(x => x.GuildId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_discord_message_guild");
            e.HasOne<DiscordChannel>().WithMany().HasForeignKey(x => x.ChannelId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_discord_message_channel");
            e.HasOne<DiscordUser>().WithMany().HasForeignKey(x => x.AuthorId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_discord_message_author");
            e.HasIndex(x => x.AuthorId);
            e.HasIndex(x => x.ChannelId).HasDatabaseName("ix_discord_message_channel_id");
            e.HasIndex(x => x.GuildId).HasDatabaseName("ix_discord_message_guild_id");
        });

        modelBuilder.Entity<DiscordAttachment>(e => {
            e.ToTable("discord_attachment");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(x => x.InteractionId).HasColumnName("interaction_id");
            e.Property(x => x.MessageId).HasColumnName("message_id");
            e.Property(x => x.Filename).HasMaxLength(100).HasColumnName("filename");
            e.Property(x => x.FileSize).HasColumnName("file_size");
            e.Property(x => x.MediaType).HasMaxLength(40).HasColumnName("media_type");
            e.Property(x => x.ProxyUrl).HasMaxLength(300).HasColumnName("proxy_url");
            e.Property(x => x.Url).HasMaxLength(300).HasColumnName("url");
            e.Property(x => x.AttachmentCreatedDate).HasColumnType("timestamp with time zone").HasColumnName("attachment_created_date");
            e.HasOne<DiscordMessage>().WithMany().HasForeignKey(x => x.MessageId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_discord_attachment_message");
            e.HasOne<DiscordInteraction>().WithMany().HasForeignKey(x => x.InteractionId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_discord_attachment_interaction");
            e.HasIndex(x => x.MessageId).HasDatabaseName("ix_discord_attachment_message_id");
            e.HasIndex(x => x.InteractionId).HasDatabaseName("ix_discord_attachment_interaction_id");
        });

        modelBuilder.Entity<DiscordReaction>(e => {
            e.ToTable("discord_reactions");
            e.HasKey(x => new { x.MessageId, x.ReactorId, x.EmojiId });
            e.Property(x => x.MessageId).HasColumnName("message_id");
            e.Property(x => x.ReactorId).HasColumnName("reactor_id");
            e.Property(x => x.EmojiId).HasColumnName("emoji_id");
            e.Property(x => x.CreatedDate).HasColumnType("timestamp with time zone").HasColumnName("created_date");
            e.HasOne<DiscordMessage>().WithMany().HasForeignKey(x => x.MessageId).OnDelete(DeleteBehavior.Cascade).HasConstraintName("fk_discord_reactions_message");
            e.HasOne<DiscordUser>().WithMany().HasForeignKey(x => x.ReactorId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_discord_reactions_reactor");
            e.HasOne<DiscordEmoji>().WithMany().HasForeignKey(x => x.EmojiId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_discord_reactions_emoji");
        });

        modelBuilder.Entity<DiscordMember>(e => {
            e.ToTable("discord_member");
            e.HasKey(x => new { x.UserId, x.GuildId });
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.GuildId).HasColumnName("guild_id");
            e.Property(x => x.JoinedAtUtc).HasColumnType("timestamp with time zone").HasColumnName("joined_at_utc");
            e.Property(x => x.Nickname).HasMaxLength(32).HasColumnName("nickname");
            e.Property(x => x.ExtraJson).HasColumnName("extra_json").HasColumnType("jsonb");
            e.HasOne<DiscordUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade).HasConstraintName("fk_discord_member_user");
            e.HasOne<DiscordGuild>().WithMany().HasForeignKey(x => x.GuildId).OnDelete(DeleteBehavior.Cascade).HasConstraintName("fk_discord_member_guild");
            e.HasIndex(x => x.GuildId).HasDatabaseName("ix_discord_member_guild_id");
        });
    }
}