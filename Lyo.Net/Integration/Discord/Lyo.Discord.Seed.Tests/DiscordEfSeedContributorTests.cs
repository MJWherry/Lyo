using Lyo.Discord.Postgres.Database;
using Lyo.Seed;

namespace Lyo.Discord.Seed.Tests;

public sealed class DiscordEfSeedContributorTests
{
    [Fact]
    public async Task DiscordEfSeedContributor_Count_PersistsGuildGraph()
    {
        var transport = await SeedAsync(new DiscordEfSeedContributor(), 2);
        var guilds = transport.Items.OfType<DiscordGuild>().ToList();
        var users = transport.Items.OfType<DiscordUser>().ToList();
        Assert.Equal(2, guilds.Count);
        Assert.All(
            guilds, guild => {
                Assert.Contains(users, user => user.Id == guild.OwnerId);
                Assert.Contains(transport.Items.OfType<DiscordMember>(), row => row.GuildId == guild.Id && row.UserId == guild.OwnerId);
                var channel = Assert.Single(transport.Items.OfType<DiscordChannel>(), row => row.GuildId == guild.Id);
                var emoji = Assert.Single(transport.Items.OfType<DiscordEmoji>(), row => row.GuildId == guild.Id);
                Assert.Contains(transport.Items.OfType<DiscordRole>(), row => row.GuildId == guild.Id && row.EmojiId == emoji.Id);
                var message = Assert.Single(transport.Items.OfType<DiscordMessage>(), row => row.GuildId == guild.Id && row.ChannelId == channel.Id);
                var interaction = Assert.Single(transport.Items.OfType<DiscordInteraction>(), row => row.GuildId == guild.Id && row.ChannelId == channel.Id);
                Assert.Contains(transport.Items.OfType<DiscordAttachment>(), row => row.MessageId == message.Id && row.InteractionId == interaction.Id);
                Assert.Contains(transport.Items.OfType<DiscordReaction>(), row => row.MessageId == message.Id && row.EmojiId == emoji.Id && row.ReactorId == guild.OwnerId);
            });
    }

    private static async Task<RecordingSeedTransport> SeedAsync(SeedContributor contributor, int count)
    {
        var transport = new RecordingSeedTransport();
        var result = await new SeedRunner().SeedAsync(contributor, transport, new() { Count = count, RandomSeed = 1 }, TestCt);
        Assert.True(result.Success, string.Join("; ", result.Errors));
        return transport;
    }

    private sealed class RecordingSeedTransport : ISeedTransport
    {
        public List<object> Items { get; } = [];

        public SeedTransportKind Kind => SeedTransportKind.Ef;

        public Task<bool> IsEmptyAsync<T>(CancellationToken ct = default)
            where T : class
            => Task.FromResult(!Items.OfType<T>().Any());

        public Task PersistAsync<T>(IReadOnlyList<T> items, CancellationToken ct = default)
            where T : class
        {
            foreach (var item in items)
                Items.Add(item);

            return Task.CompletedTask;
        }
    }
}