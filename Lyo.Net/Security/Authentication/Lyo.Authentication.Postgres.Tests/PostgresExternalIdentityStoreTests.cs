using Lyo.Authentication.Models.Records;
using Lyo.Common.Core.Identifiers;
using Lyo.Exceptions.Models;

namespace Lyo.Authentication.Postgres.Tests;

public sealed class PostgresExternalIdentityStoreTests
{
    private readonly AuthenticationPostgresFixture _fixture;

    public PostgresExternalIdentityStoreTests(AuthenticationPostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Link_CreatesActiveRow()
    {
        var user = await CreateUserAsync();
        var sub = $"sub-{LyoGuid.CreateV7():N}";
        var link = await _fixture.IdentityStore.LinkAsync(user.Id, "google", sub, "alice@example.com", ["people.read"], null, null, TestCt);
        Assert.True(link.IsActive);
        Assert.Equal(user.Id, link.UserId);
    }

    [Fact]
    public async Task FindByProviderSubject_FindsActiveLink()
    {
        var user = await CreateUserAsync();
        var sub = $"sub-{LyoGuid.CreateV7():N}";
        await _fixture.IdentityStore.LinkAsync(user.Id, "google", sub, null, [], null, null, TestCt);
        var found = await _fixture.IdentityStore.FindByProviderSubjectAsync("google", sub, null, TestCt);
        Assert.NotNull(found);
        Assert.Equal(user.Id, found.UserId);
    }

    [Fact]
    public async Task DifferentUserSameSubject_Throws()
    {
        var sub = $"sub-{LyoGuid.CreateV7():N}";
        var a = await CreateUserAsync();
        var b = await CreateUserAsync();
        await _fixture.IdentityStore.LinkAsync(a.Id, "google", sub, null, [], null, null, TestCt);
        await Assert.ThrowsAsync<ConflictException>(() => _fixture.IdentityStore.LinkAsync(b.Id, "google", sub, null, [], null, null, TestCt));
    }

    [Fact]
    public async Task Relink_AfterUnlink_Succeeds()
    {
        var user = await CreateUserAsync();
        var sub = $"sub-{LyoGuid.CreateV7():N}";
        var first = await _fixture.IdentityStore.LinkAsync(user.Id, "google", sub, null, [], null, null, TestCt);
        await _fixture.IdentityStore.UnlinkAsync(first.Id, DateTime.UtcNow, null, TestCt);
        var second = await _fixture.IdentityStore.LinkAsync(user.Id, "google", sub, null, [], null, null, TestCt);
        Assert.NotEqual(first.Id, second.Id);
        Assert.True(second.IsActive);
    }

    [Fact]
    public async Task ListForUser_ExcludesUnlinked()
    {
        var user = await CreateUserAsync();
        var active = await _fixture.IdentityStore.LinkAsync(user.Id, "google", $"sub-{LyoGuid.CreateV7():N}", null, [], null, null, TestCt);
        var dead = await _fixture.IdentityStore.LinkAsync(user.Id, "keycloak:lyo", $"sub-{LyoGuid.CreateV7():N}", null, [], null, null, TestCt);
        await _fixture.IdentityStore.UnlinkAsync(dead.Id, DateTime.UtcNow, null, TestCt);
        var list = await _fixture.IdentityStore.ListForUserAsync(user.Id, null, TestCt);
        Assert.Contains(list, l => l.Id == active.Id);
        Assert.DoesNotContain(list, l => l.Id == dead.Id);
    }

    [Fact]
    public async Task Relink_RefreshesScopesAndRawClaims()
    {
        var user = await CreateUserAsync();
        var sub = $"sub-{LyoGuid.CreateV7():N}";
        await _fixture.IdentityStore.LinkAsync(user.Id, "keycloak:lyo", sub, null, ["old"], null, null, TestCt);
        var updated = await _fixture.IdentityStore.LinkAsync(
            user.Id, "keycloak:lyo", sub, "new@example.com", ["admin"], new Dictionary<string, object?> { ["name"] = "Alice" }, null, TestCt);

        Assert.Equal(["admin"], updated.Scopes);
        Assert.Equal("new@example.com", updated.EmailAtLink);
    }

    private async Task<LyoUser> CreateUserAsync()
    {
        var user = new LyoUser(LyoGuid.CreateV7(), "Alice", $"alice-{LyoGuid.CreateV7():N}@example.com", true, null, null, [], null, null, DateTime.UtcNow, null, null, null, null);
        return await _fixture.UserStore.CreateAsync(user, null, TestCt);
    }
}