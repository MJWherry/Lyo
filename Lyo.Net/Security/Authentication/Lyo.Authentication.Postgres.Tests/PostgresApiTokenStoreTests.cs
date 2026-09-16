using Lyo.Authentication.Models.Format;
using Lyo.Authentication.Models.Records;
using Lyo.Testing;
using Lyo.Common.Core.Identifiers;

namespace Lyo.Authentication.Postgres.Tests;

public sealed class PostgresApiTokenStoreTests
{
    private readonly AuthenticationPostgresFixture _fixture;

    public PostgresApiTokenStoreTests(AuthenticationPostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Insert_And_Get_RoundTrip()
    {
        var token = NewToken(NewId(), scopes: ["people.read", "people.write"]);
        await _fixture.TokenStore.InsertAsync(token, null, TestCt);
        var loaded = await _fixture.TokenStore.GetByIdAsync(token.Id, null, TestCt);
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
        await _fixture.TokenStore.InsertAsync(token, null, TestCt);
        await Assert.ThrowsAnyAsync<Exception>(() => _fixture.TokenStore.InsertAsync(token, null, TestCt));
    }

    [Fact]
    public async Task Revoke_SetsRevokedFields()
    {
        var token = NewToken(NewId());
        await _fixture.TokenStore.InsertAsync(token, null, TestCt);
        var revokedAt = DateTime.UtcNow;
        await _fixture.TokenStore.RevokeAsync(token.Id, revokedAt, "by-user", null, TestCt);
        var loaded = await _fixture.TokenStore.GetByIdAsync(token.Id, null, TestCt);
        Assert.NotNull(loaded);
        Assert.NotNull(loaded.RevokedAt);
        Assert.Equal("by-user", loaded.RevokedReason);
    }

    [Fact]
    public async Task TouchLastUsed_UpdatesTimestamp()
    {
        var token = NewToken(NewId());
        await _fixture.TokenStore.InsertAsync(token, null, TestCt);
        var now = DateTime.UtcNow;
        await _fixture.TokenStore.TouchLastUsedAsync(token.Id, now, null, TestCt);
        var loaded = await _fixture.TokenStore.GetByIdAsync(token.Id, null, TestCt);
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
        await _fixture.TokenStore.InsertAsync(owned, null, TestCt);
        await _fixture.TokenStore.InsertAsync(foreign, null, TestCt);
        var list = await _fixture.TokenStore.ListForUserAsync(owner.Id, false, null, TestCt);
        Assert.Single(list);
        Assert.Equal(owned.Id, list[0].Id);
    }

    [Fact]
    public async Task ListForUser_HidesRevokedByDefault()
    {
        var owner = await CreateUserAsync();
        var active = NewToken(NewId(), owner.Id);
        var dead = NewToken(NewId(), owner.Id);
        await _fixture.TokenStore.InsertAsync(active, null, TestCt);
        await _fixture.TokenStore.InsertAsync(dead, null, TestCt);
        await _fixture.TokenStore.RevokeAsync(dead.Id, DateTime.UtcNow, "test", null, TestCt);
        var visible = await _fixture.TokenStore.ListForUserAsync(owner.Id, false, null, TestCt);
        Assert.DoesNotContain(visible, t => t.Id == dead.Id);
        var all = await _fixture.TokenStore.ListForUserAsync(owner.Id, true, null, TestCt);
        Assert.Contains(all, t => t.Id == dead.Id);
    }

    [Fact]
    public async Task DeleteAsync_RemovesRow()
    {
        var token = NewToken(NewId());
        await _fixture.TokenStore.InsertAsync(token, null, TestCt);
        await _fixture.TokenStore.DeleteAsync(token.Id, null, TestCt);
        Assert.Null(await _fixture.TokenStore.GetByIdAsync(token.Id, null, TestCt));
    }

    [Fact]
    public async Task RevokeRefreshTokensForUserAsync_RevokesLiveRefresh()
    {
        var owner = await CreateUserAsync();
        var refresh = NewToken(NewId(), owner.Id, ["lyo:refresh"], kind: "internal", metadata: new Dictionary<string, object?> { ["lyo_provider"] = "google" });
        var pat = NewToken(NewId(), owner.Id, ["people.read"]);
        await _fixture.TokenStore.InsertAsync(refresh, null, TestCt);
        await _fixture.TokenStore.InsertAsync(pat, null, TestCt);
        await _fixture.TokenStore.RevokeRefreshTokensForUserAsync(owner.Id, "google", DateTime.UtcNow, "login_replaced", null, TestCt);
        var loadedRefresh = await _fixture.TokenStore.GetByIdAsync(refresh.Id, null, TestCt);
        var loadedPat = await _fixture.TokenStore.GetByIdAsync(pat.Id, null, TestCt);
        Assert.NotNull(loadedRefresh?.RevokedAt);
        Assert.Null(loadedPat?.RevokedAt);
    }

    private async Task<LyoUser> CreateUserAsync()
    {
        var user = new LyoUser(LyoGuid.CreateV7(), "Token Owner", $"owner-{LyoGuid.CreateV7():N}@example.com", true, null, null, [], null, null, DateTime.UtcNow, null, null, null, null);
        return await _fixture.UserStore.CreateAsync(user, null, TestCt);
    }

    [Fact]
    public async Task RotatedFrom_PreservesChain()
    {
        var first = NewToken(NewId());
        await _fixture.TokenStore.InsertAsync(first, null, TestCt);
        var second = NewToken(NewId(), rotatedFromId: first.Id);
        await _fixture.TokenStore.InsertAsync(second, null, TestCt);
        var loaded = await _fixture.TokenStore.GetByIdAsync(second.Id, null, TestCt);
        Assert.NotNull(loaded);
        Assert.Equal(first.Id, loaded.RotatedFromId);
    }

    [Fact]
    public async Task ConcurrentIssuance_AllSucceed_NoCollisions()
    {
        var ids = Enumerable.Range(0, 20).Select(_ => NewId()).ToArray();
        await Task.WhenAll(ids.Select(id => _fixture.TokenStore.InsertAsync(NewToken(id), null, TestCt)));
        foreach (var id in ids)
            Assert.NotNull(await _fixture.TokenStore.GetByIdAsync(id, null, TestCt));
    }

    private static int _idSeq;

    private static string NewId()
    {
        const string alphabet = "0123456789abcdefghjkmnpqrstvwxyz";
        var n = Interlocked.Increment(ref _idSeq);
        var bytes = new byte[11];
        TestData.Fill(bytes, TestData.Seed + n);
        var chars = new char[11];
        for (var i = 0; i < 11; i++)
            chars[i] = alphabet[bytes[i] % alphabet.Length];

        return new(chars);
    }

    private static ApiTokenRecord NewToken(
        string id,
        Guid? userId = null,
        IReadOnlyList<string>? scopes = null,
        string? rotatedFromId = null,
        string? kind = null,
        IReadOnlyDictionary<string, object?>? metadata = null)
        => new(
            id, TestData.Create(32, TestData.Seed + id.GetHashCode()), kind ?? ApiTokenKind.Pat, ApiTokenRing.Live, userId, "test token", scopes ?? [], metadata,
            DateTime.UtcNow, null, null, null, null, null, rotatedFromId);
}