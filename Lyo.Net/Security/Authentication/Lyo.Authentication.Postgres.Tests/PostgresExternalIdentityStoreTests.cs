using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lyo.Authentication.Models.Records;

namespace Lyo.Authentication.Postgres.Tests;

public sealed class PostgresExternalIdentityStoreTests
{
    private readonly AuthenticationPostgresFixture _fixture;

    public PostgresExternalIdentityStoreTests(AuthenticationPostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Link_CreatesActiveRow()
    {
        var user = await CreateUserAsync();
        var sub = $"sub-{Guid.NewGuid():N}";
        var link = await _fixture.IdentityStore.LinkAsync(user.Id, "google", sub, "alice@example.com", ["people.read"], null, tenantId: null, TestContext.Current.CancellationToken);
        Assert.True(link.IsActive);
        Assert.Equal(user.Id, link.UserId);
    }

    [Fact]
    public async Task FindByProviderSubject_FindsActiveLink()
    {
        var user = await CreateUserAsync();
        var sub = $"sub-{Guid.NewGuid():N}";
        await _fixture.IdentityStore.LinkAsync(user.Id, "google", sub, null, [], null, tenantId: null, TestContext.Current.CancellationToken);
        var found = await _fixture.IdentityStore.FindByProviderSubjectAsync("google", sub, tenantId: null, TestContext.Current.CancellationToken);
        Assert.NotNull(found);
        Assert.Equal(user.Id, found!.UserId);
    }

    [Fact]
    public async Task DifferentUserSameSubject_Throws()
    {
        var sub = $"sub-{Guid.NewGuid():N}";
        var a = await CreateUserAsync();
        var b = await CreateUserAsync();
        await _fixture.IdentityStore.LinkAsync(a.Id, "google", sub, null, [], null, tenantId: null, TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _fixture.IdentityStore.LinkAsync(b.Id, "google", sub, null, [], null, tenantId: null, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Relink_AfterUnlink_Succeeds()
    {
        var user = await CreateUserAsync();
        var sub = $"sub-{Guid.NewGuid():N}";
        var first = await _fixture.IdentityStore.LinkAsync(user.Id, "google", sub, null, [], null, tenantId: null, TestContext.Current.CancellationToken);
        await _fixture.IdentityStore.UnlinkAsync(first.Id, DateTime.UtcNow, tenantId: null, TestContext.Current.CancellationToken);
        var second = await _fixture.IdentityStore.LinkAsync(user.Id, "google", sub, null, [], null, tenantId: null, TestContext.Current.CancellationToken);
        Assert.NotEqual(first.Id, second.Id);
        Assert.True(second.IsActive);
    }

    [Fact]
    public async Task ListForUser_ExcludesUnlinked()
    {
        var user = await CreateUserAsync();
        var active = await _fixture.IdentityStore.LinkAsync(user.Id, "google", $"sub-{Guid.NewGuid():N}", null, [], null, tenantId: null, TestContext.Current.CancellationToken);
        var dead = await _fixture.IdentityStore.LinkAsync(user.Id, "keycloak:lyo", $"sub-{Guid.NewGuid():N}", null, [], null, tenantId: null, TestContext.Current.CancellationToken);
        await _fixture.IdentityStore.UnlinkAsync(dead.Id, DateTime.UtcNow, tenantId: null, TestContext.Current.CancellationToken);
        var list = await _fixture.IdentityStore.ListForUserAsync(user.Id, tenantId: null, TestContext.Current.CancellationToken);
        Assert.Contains(list, l => l.Id == active.Id);
        Assert.DoesNotContain(list, l => l.Id == dead.Id);
    }

    [Fact]
    public async Task Relink_RefreshesScopesAndRawClaims()
    {
        var user = await CreateUserAsync();
        var sub = $"sub-{Guid.NewGuid():N}";
        await _fixture.IdentityStore.LinkAsync(user.Id, "keycloak:lyo", sub, null, ["old"], null, tenantId: null, TestContext.Current.CancellationToken);
        var updated = await _fixture.IdentityStore.LinkAsync(user.Id, "keycloak:lyo", sub, "new@example.com", ["admin"], new Dictionary<string, object?> { ["name"] = "Alice" }, tenantId: null, TestContext.Current.CancellationToken);
        Assert.Equal(["admin"], updated.Scopes);
        Assert.Equal("new@example.com", updated.EmailAtLink);
    }

    private async Task<LyoUser> CreateUserAsync()
    {
        var user = new LyoUser(
            Id: Guid.NewGuid(),
            DisplayName: "Alice",
            Email: $"alice-{Guid.NewGuid():N}@example.com",
            EmailVerified: true,
            AvatarUrl: null,
            PreferredLanguageBcp47: null,
            Scopes: [],
            Metadata: null,
            PersonId: null,
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: null,
            LastLoginAt: null,
            DisabledAt: null,
            DisabledReason: null);
        return await _fixture.UserStore.CreateAsync(user, tenantId: null, TestContext.Current.CancellationToken);
    }
}
