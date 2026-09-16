using Bogus;
using Lyo.Discord.Postgres;
using Lyo.Discord.Postgres.Database;
using Lyo.Exceptions;
using Lyo.Seed;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Discord.Seed;

/// <summary>EF seed of one guild per count, with a user, member, channel, role, emoji, message, attachment, reaction, and interaction. Schema <c>discord</c>.</summary>
public sealed class DiscordEfSeedContributor : SeedContributor, IEfSeedSource
{
    /// <inheritdoc />
    public override string Name => "Discord";

    /// <inheritdoc />
    public override SeedTransportKind SupportedTransports => SeedTransportKind.Ef;

    /// <inheritdoc />
    public string Schema => PostgresDiscordOptions.Schema;

    /// <inheritdoc />
    public DbContext CreateContext(string connectionString)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(connectionString);
        var options = new DbContextOptionsBuilder<DiscordDbContext>()
            .UseNpgsql(connectionString, n => n.MigrationsHistoryTable("__EFMigrationsHistory", Schema))
            .Options;
        return new DiscordDbContext(options);
    }

    /// <inheritdoc />
    protected override void Configure(SeedGraph graph, SeedOptions options)
    {
        graph.OnClear(ClearAsync);
        var faker = options.RandomSeed is { } seed ? new Faker { Random = new(seed) } : new Faker();
        var now = DateTime.UtcNow;
        var userIds = Ids(options.Count, 1);
        var guildIds = Ids(options.Count, 2);
        var channelIds = Ids(options.Count, 3);
        var emojiIds = Ids(options.Count, 4);
        var roleIds = Ids(options.Count, 5);
        var messageIds = Ids(options.Count, 6);
        var interactionIds = Ids(options.Count, 7);
        var attachmentIds = Ids(options.Count, 8);
        graph.Entity(options.Count, i => new DiscordUser {
            Id = userIds[i],
            Username = Fit($"{faker.Internet.UserName()}{i}", 32),
            Discriminator = 0,
            IsBot = false,
            UserCreatedDate = now,
            CreatedTimestamp = now,
            UpdatedTimestamp = now
        });
        graph.Entity(options.Count, i => new DiscordGuild {
            Id = guildIds[i],
            OwnerId = userIds[i],
            Name = Fit(faker.Company.CompanyName(), 100),
            MemberCount = 1,
            GuildCreatedDate = now,
            JoinedDate = now,
            CreatedTimestamp = now,
            UpdatedTimestamp = now
        });
        graph.Entity(options.Count, i => new DiscordEmoji {
            Id = emojiIds[i],
            GuildId = guildIds[i],
            Name = "wave",
            IsAvailable = true,
            EmojiCreatedDate = now,
            CreatedTimestamp = now,
            UpdatedTimestamp = now
        });
        graph.Entity(options.Count, i => new DiscordRole {
            Id = roleIds[i],
            GuildId = guildIds[i],
            EmojiId = emojiIds[i],
            Name = "Moderator",
            Color = "#3366cc",
            RoleCreatedDate = now,
            CreatedTimestamp = now,
            UpdatedTimestamp = now
        });
        graph.Entity(options.Count, i => new DiscordChannel {
            Id = channelIds[i],
            GuildId = guildIds[i],
            Name = "general",
            ChannelType = "text",
            ChannelCreated = now,
            CreatedTimestamp = now,
            UpdatedTimestamp = now
        });
        graph.Entity(options.Count, i => new DiscordMember {
            UserId = userIds[i],
            GuildId = guildIds[i],
            JoinedAtUtc = now,
            Nickname = faker.Name.FirstName()
        });
        graph.Entity(options.Count, i => new DiscordMessage {
            Id = messageIds[i],
            AuthorId = userIds[i],
            ChannelId = channelIds[i],
            GuildId = guildIds[i],
            Content = faker.Lorem.Sentence(),
            MessageCreatedDate = now,
            CreatedTimestamp = now,
            UpdatedTimestamp = now
        });
        graph.Entity(options.Count, i => new DiscordInteraction {
            Id = interactionIds[i],
            AuthorId = userIds[i],
            ChannelId = channelIds[i],
            GuildId = guildIds[i],
            Content = faker.Hacker.Phrase(),
            InteractionCreatedDate = now
        });
        graph.Entity(options.Count, i => new DiscordAttachment {
            Id = attachmentIds[i],
            MessageId = messageIds[i],
            InteractionId = interactionIds[i],
            Filename = "notes.txt",
            FileSize = 4,
            MediaType = "text/plain",
            ProxyUrl = "https://cdn.example.com/notes.txt",
            Url = "https://cdn.example.com/notes.txt",
            AttachmentCreatedDate = now
        });
        graph.Entity(options.Count, i => new DiscordReaction {
            MessageId = messageIds[i],
            ReactorId = userIds[i],
            EmojiId = emojiIds[i],
            CreatedDate = now
        });
    }

    private static long[] Ids(int count, int band) => Enumerable.Range(0, count).Select(i => band * 1_000_000L + i + 1).ToArray();

    private static string Fit(string value, int max) => value.Length <= max ? value : value[..max];

    private static async Task ClearAsync(SeedSession session, CancellationToken ct)
    {
        if (session.Transport is not EfSeedTransport<DiscordDbContext> ef)
            throw new SeedException("Discord OnClear requires EfSeedTransport<DiscordDbContext>.");

        var db = ef.Context;
        await db.DiscordReactions.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.DiscordAttachments.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.DiscordMessages.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.DiscordInteractions.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.DiscordMembers.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.DiscordRoles.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.DiscordChannels.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.DiscordEmojis.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.DiscordGuilds.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.DiscordUsers.ExecuteDeleteAsync(ct).ConfigureAwait(false);
    }
}
