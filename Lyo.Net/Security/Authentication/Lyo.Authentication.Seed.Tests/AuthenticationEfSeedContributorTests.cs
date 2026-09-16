using Lyo.Authentication.Format;
using Lyo.Authentication.Postgres.Database;
using Lyo.Authentication.Seed;
using Lyo.Seed;

namespace Lyo.Authentication.Seed.Tests;

public sealed class AuthenticationEfSeedContributorTests
{
    [Fact]
    public async Task AuthenticationEfSeedContributor_Count_PersistsLocalUserGraph()
    {
        var transport = await SeedAsync(new AuthenticationEfSeedContributor(), 2);
        var users = transport.Items.OfType<UserEntity>().ToList();
        var hash = ApiTokenCodec.ComputeSecretHash(AuthenticationEfSeedContributor.Password);
        Assert.Equal(2, users.Count);
        Assert.All(users, user => {
            var token = Assert.Single(transport.Items.OfType<TokenEntity>(), row => row.UserId == user.Id);
            Assert.Equal(hash, token.SecretHash);
            Assert.True(token.ExpiresTimestamp < DateTime.UtcNow);
            Assert.Contains(transport.Items.OfType<LinkedIdentityEntity>(), row => row.UserId == user.Id && row.Provider == "local");
            Assert.Contains(transport.Items.OfType<UserClaimEntity>(), row => row.UserId == user.Id);
            Assert.Contains(transport.Items.OfType<UserScopeEntity>(), row => row.UserId == user.Id);
            Assert.Contains(transport.Items.OfType<UserEventEntity>(), row => row.UserId == user.Id);
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
