using System.Security.Cryptography;
using Lyo.Authentication.Models.Format;
using Lyo.Authentication.Models.Records;

namespace Lyo.Authentication.Postgres.Tests;

public sealed class PostgresApiTokenStoreTests
{
    private readonly AuthenticationPostgresFixture _fixture;

    public PostgresApiTokenStoreTests(AuthenticationPostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Insert_And_Get_RoundTrip()
    {
        var token = NewToken(NewId(), scopes: ["people.read", "people.write"]);
        await _fixture.TokenStore.InsertAsync(token, null, TestContext.Current.CancellationToken);
        var loaded = await _fixture.TokenStore.GetByIdAsync(token.Id, null, TestContext.Current.CancellationToken);
        Assert.NotNull(loaded);
        Assert.Equal(token.Id, loaded.Id);
        Assert.Equal(token.SecretHash, loaded.SecretHash);
        Assert.Equal(token.Kind, loaded.Kind);
        Assert.Equal(token.Ring, loaded.Ring);
        Assert.Equal(token.DisplayName, loaded.DisplayName);
        Assert.Equal(["people.read", "people.write"], loaded.Scopes);
    }

    [Fact]
    public async Task Insert_Duplicate_Throws()
    {
        var token = NewToken(NewId());
        await _fixture.TokenStore.InsertAsync(token, null, TestContext.Current.CancellationToken);
        await Assert.ThrowsAnyAsync<Exception>(() => _fixture.TokenStore.InsertAsync(token, null, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Revoke_SetsRevokedFields()
    {
        var token = NewToken(NewId());
        await _fixture.TokenStore.InsertAsync(token, null, TestContext.Current.CancellationToken);
        var revokedAt = DateTime.UtcNow;
        await _fixture.TokenStore.RevokeAsync(token.Id, revokedAt, "by-user", null, TestContext.Current.CancellationToken);
        var loaded = await _fixture.TokenStore.GetByIdAsync(token.Id, null, TestContext.Current.CancellationToken);
        Assert.NotNull(loaded);
        Assert.NotNull(loaded.RevokedAt);
        Assert.Equal("by-user", loaded.RevokedReason);
    }

    [Fact]
    public async Task TouchLastUsed_UpdatesTimestamp()
    {
        var token = NewToken(NewId());
        await _fixture.TokenStore.InsertAsync(token, null, TestContext.Current.CancellationToken);
        var now = DateTime.UtcNow;
        await _fixture.TokenStore.TouchLastUsedAsync(token.Id, now, null, TestContext.Current.CancellationToken);
        var loaded = await _fixture.TokenStore.GetByIdAsync(token.Id, null, TestContext.Current.CancellationToken);
        Assert.NotNull(loaded);
        Assert.NotNull(loaded.LastUsedAt);
    }

    [Fact]
    public async Task ListForUser_ReturnsOwnedOnly()
    {
        var owner = await CreateUserAsync();
        var other = await CreateUserAsync();
        var owned = NewToken(NewId(), owner.Id);
        var foreign = NewToken(NewId(), other.Id);
        await _fixture.TokenStore.InsertAsync(owned, null, TestContext.Current.CancellationToken);
        await _fixture.TokenStore.InsertAsync(foreign, null, TestContext.Current.CancellationToken);
        var list = await _fixture.TokenStore.ListForUserAsync(owner.Id, false, null, TestContext.Current.CancellationToken);
        Assert.Single(list);
        Assert.Equal(owned.Id, list[0].Id);
    }

    [Fact]
    public async Task ListForUser_HidesRevokedByDefault()
    {
        var owner = await CreateUserAsync();
        var active = NewToken(NewId(), owner.Id);
        var dead = NewToken(NewId(), owner.Id);
        await _fixture.TokenStore.InsertAsync(active, null, TestContext.Current.CancellationToken);
        await _fixture.TokenStore.InsertAsync(dead, null, TestContext.Current.CancellationToken);
        await _fixture.TokenStore.RevokeAsync(dead.Id, DateTime.UtcNow, "test", null, TestContext.Current.CancellationToken);
        var visible = await _fixture.TokenStore.ListForUserAsync(owner.Id, false, null, TestContext.Current.CancellationToken);
        Assert.DoesNotContain(visible, t => t.Id == dead.Id);
        var all = await _fixture.TokenStore.ListForUserAsync(owner.Id, true, null, TestContext.Current.CancellationToken);
        Assert.Contains(all, t => t.Id == dead.Id);
    }

    private async Task<LyoUser> CreateUserAsync()
    {
        var user = new LyoUser(Guid.NewGuid(), "Token Owner", $"owner-{Guid.NewGuid():N}@example.com", true, null, null, [], null, null, DateTime.UtcNow, null, null, null, null);
        return await _fixture.UserStore.CreateAsync(user, null, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task RotatedFrom_PreservesChain()
    {
        var first = NewToken(NewId());
        await _fixture.TokenStore.InsertAsync(first, null, TestContext.Current.CancellationToken);
        var second = NewToken(NewId(), rotatedFromId: first.Id);
        await _fixture.TokenStore.InsertAsync(second, null, TestContext.Current.CancellationToken);
        var loaded = await _fixture.TokenStore.GetByIdAsync(second.Id, null, TestContext.Current.CancellationToken);
        Assert.NotNull(loaded);
        Assert.Equal(first.Id, loaded.RotatedFromId);
    }

    [Fact]
    public async Task ConcurrentIssuance_AllSucceed_NoCollisions()
    {
        var ids = Enumerable.Range(0, 20).Select(_ => NewId()).ToArray();
        await Task.WhenAll(ids.Select(id => _fixture.TokenStore.InsertAsync(NewToken(id), null, TestContext.Current.CancellationToken)));
        foreach (var id in ids)
            Assert.NotNull(await _fixture.TokenStore.GetByIdAsync(id, null, TestContext.Current.CancellationToken));
    }

    private static string NewId()
    {
        const string alphabet = "0123456789abcdefghjkmnpqrstvwxyz";
        var bytes = new byte[11];
        RandomNumberGenerator.Fill(bytes);
        var chars = new char[11];
        for (var i = 0; i < 11; i++)
            chars[i] = alphabet[bytes[i] % alphabet.Length];

        return new(chars);
    }

    private static ApiTokenRecord NewToken(string id, Guid? userId = null, IReadOnlyList<string>? scopes = null, string? rotatedFromId = null)
        => new(
            id, RandomNumberGenerator.GetBytes(32), ApiTokenKind.Pat, ApiTokenRing.Live, userId, "test token", scopes ?? [], null, DateTime.UtcNow, null, null, null, null, null,
            rotatedFromId);
}