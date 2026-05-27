using System;
using System.Threading.Tasks;
using Lyo.Authentication.Records;

namespace Lyo.Authentication.Postgres.Tests;

public sealed class PostgresUserStoreTests
{
    private readonly AuthenticationPostgresFixture _fixture;

    public PostgresUserStoreTests(AuthenticationPostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Create_And_Get_RoundTrip()
    {
        var user = NewUser($"alice-{Guid.NewGuid():N}@example.com");
        await _fixture.UserStore.CreateAsync(user, TestContext.Current.CancellationToken);
        var loaded = await _fixture.UserStore.GetByIdAsync(user.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(loaded);
        Assert.Equal(user.Email, loaded!.Email);
        Assert.Equal(user.DisplayName, loaded.DisplayName);
    }

    [Fact]
    public async Task GetByEmail_IsCaseInsensitive()
    {
        var user = NewUser($"Mixed-{Guid.NewGuid():N}@EXAMPLE.COM");
        await _fixture.UserStore.CreateAsync(user, TestContext.Current.CancellationToken);
        var loaded = await _fixture.UserStore.GetByEmailAsync(user.Email.ToLowerInvariant(), TestContext.Current.CancellationToken);
        Assert.NotNull(loaded);
        Assert.Equal(user.Id, loaded!.Id);
    }

    [Fact]
    public async Task Create_DuplicateEmail_Throws()
    {
        var email = $"dup-{Guid.NewGuid():N}@example.com";
        var first = NewUser(email);
        await _fixture.UserStore.CreateAsync(first, TestContext.Current.CancellationToken);
        var second = NewUser(email);
        await Assert.ThrowsAnyAsync<Exception>(() => _fixture.UserStore.CreateAsync(second, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SetScopes_PersistsNewSnapshot()
    {
        var user = NewUser($"scopes-{Guid.NewGuid():N}@example.com");
        await _fixture.UserStore.CreateAsync(user, TestContext.Current.CancellationToken);
        await _fixture.UserStore.SetScopesAsync(user.Id, ["admin", "people.read"], TestContext.Current.CancellationToken);
        var loaded = await _fixture.UserStore.GetByIdAsync(user.Id, TestContext.Current.CancellationToken);
        Assert.Equal(["admin", "people.read"], loaded!.Scopes);
    }

    [Fact]
    public async Task SetDisabled_FlipsKillSwitch()
    {
        var user = NewUser($"disable-{Guid.NewGuid():N}@example.com");
        await _fixture.UserStore.CreateAsync(user, TestContext.Current.CancellationToken);
        var when = DateTime.UtcNow;
        await _fixture.UserStore.SetDisabledAsync(user.Id, when, "policy", TestContext.Current.CancellationToken);
        var loaded = await _fixture.UserStore.GetByIdAsync(user.Id, TestContext.Current.CancellationToken);
        Assert.True(loaded!.IsDisabled);
        Assert.Equal("policy", loaded.DisabledReason);
    }

    [Fact]
    public async Task UpdateLastLogin_Touches()
    {
        var user = NewUser($"login-{Guid.NewGuid():N}@example.com");
        await _fixture.UserStore.CreateAsync(user, TestContext.Current.CancellationToken);
        var now = DateTime.UtcNow;
        await _fixture.UserStore.UpdateLastLoginAsync(user.Id, now, TestContext.Current.CancellationToken);
        var loaded = await _fixture.UserStore.GetByIdAsync(user.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(loaded!.LastLoginAt);
    }

    private static LyoUser NewUser(string email) =>
        new(
            Id: Guid.NewGuid(),
            DisplayName: "Alice",
            Email: email,
            EmailVerified: true,
            AvatarUrl: null,
            PreferredLanguageBcp47: "en-US",
            Scopes: ["people.read"],
            Metadata: null,
            PersonId: null,
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: null,
            LastLoginAt: null,
            DisabledAt: null,
            DisabledReason: null);
}
