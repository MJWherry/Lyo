using Lyo.Authentication.Models.Records;
using Lyo.Authentication.Postgres.Stores;
using Lyo.EntityReference.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Authentication.Postgres.Tests;

public sealed class PostgresUserStoreTests
{
    private readonly AuthenticationPostgresFixture _fixture;

    public PostgresUserStoreTests(AuthenticationPostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Create_And_Get_RoundTrip()
    {
        var user = NewUser($"alice-{Guid.NewGuid():N}@example.com");
        await _fixture.UserStore.CreateAsync(user, null, TestContext.Current.CancellationToken);
        var loaded = await _fixture.UserStore.GetByIdAsync(user.Id, null, TestContext.Current.CancellationToken);
        Assert.NotNull(loaded);
        Assert.Equal(user.Email, loaded.Email);
        Assert.Equal(user.DisplayName, loaded.DisplayName);
    }

    [Fact]
    public async Task GetByEmail_IsCaseInsensitive()
    {
        var user = NewUser($"Mixed-{Guid.NewGuid():N}@EXAMPLE.COM");
        await _fixture.UserStore.CreateAsync(user, null, TestContext.Current.CancellationToken);
        var loaded = await _fixture.UserStore.GetByEmailAsync(user.Email.ToLowerInvariant(), null, TestContext.Current.CancellationToken);
        Assert.NotNull(loaded);
        Assert.Equal(user.Id, loaded.Id);
    }

    [Fact]
    public async Task Create_DuplicateEmail_Throws()
    {
        var email = $"dup-{Guid.NewGuid():N}@example.com";
        var first = NewUser(email);
        await _fixture.UserStore.CreateAsync(first, null, TestContext.Current.CancellationToken);
        var second = NewUser(email);
        await Assert.ThrowsAnyAsync<Exception>(() => _fixture.UserStore.CreateAsync(second, null, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SetScopes_PersistsNewSnapshot()
    {
        var user = NewUser($"scopes-{Guid.NewGuid():N}@example.com");
        await _fixture.UserStore.CreateAsync(user, null, TestContext.Current.CancellationToken);
        await _fixture.UserStore.SetScopesAsync(user.Id, ["admin", "people.read"], null, TestContext.Current.CancellationToken);
        var loaded = await _fixture.UserStore.GetByIdAsync(user.Id, null, TestContext.Current.CancellationToken);
        Assert.NotNull(loaded);
        Assert.Equal(["admin", "people.read"], loaded.Scopes);
    }

    [Fact]
    public async Task SetDisabled_FlipsKillSwitch()
    {
        var user = NewUser($"disable-{Guid.NewGuid():N}@example.com");
        await _fixture.UserStore.CreateAsync(user, null, TestContext.Current.CancellationToken);
        var when = DateTime.UtcNow;
        await _fixture.UserStore.SetDisabledAsync(user.Id, when, "policy", null, TestContext.Current.CancellationToken);
        var loaded = await _fixture.UserStore.GetByIdAsync(user.Id, null, TestContext.Current.CancellationToken);
        Assert.NotNull(loaded);
        Assert.True(loaded.IsDisabled);
        Assert.Equal("policy", loaded.DisabledReason);
    }

    [Fact]
    public async Task UpdateLastLogin_Touches()
    {
        var user = NewUser($"login-{Guid.NewGuid():N}@example.com");
        await _fixture.UserStore.CreateAsync(user, null, TestContext.Current.CancellationToken);
        var now = DateTime.UtcNow;
        await _fixture.UserStore.UpdateLastLoginAsync(user.Id, now, null, TestContext.Current.CancellationToken);
        var loaded = await _fixture.UserStore.GetByIdAsync(user.Id, null, TestContext.Current.CancellationToken);
        Assert.NotNull(loaded);
        Assert.NotNull(loaded.LastLoginAt);
    }

    [Fact]
    public async Task MultiTenantStrict_ThrowsWhenCallerOmitsTenant()
    {
        var strictStore = new PostgresUserStore(
            _fixture.ContextFactory, NullLogger<PostgresUserStore>.Instance, Microsoft.Extensions.Options.Options.Create(new EntityRefOptions()),
            Microsoft.Extensions.Options.Options.Create(new PostgresUserOptions { Tenancy = new() { Mode = TenancyMode.MultiTenantStrict } }));

        var user = NewUser($"strict-{Guid.NewGuid():N}@example.com");
        await Assert.ThrowsAsync<ArgumentNullException>(() => strictStore.CreateAsync(user, null, TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentNullException>(() => strictStore.GetByIdAsync(user.Id, null, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SystemOnly_StoresAndRetrievesWithNullTenant()
    {
        var systemStore = new PostgresUserStore(
            _fixture.ContextFactory, NullLogger<PostgresUserStore>.Instance, Microsoft.Extensions.Options.Options.Create(new EntityRefOptions()),
            Microsoft.Extensions.Options.Options.Create(new PostgresUserOptions { Tenancy = new() { Mode = TenancyMode.SystemOnly } }));

        var user = NewUser($"system-{Guid.NewGuid():N}@example.com");
        await systemStore.CreateAsync(user, Guid.NewGuid(), TestContext.Current.CancellationToken);
        var loaded = await systemStore.GetByIdAsync(user.Id, Guid.NewGuid(), TestContext.Current.CancellationToken);
        Assert.NotNull(loaded);
        Assert.Equal(user.Email, loaded.Email);
    }

    private static LyoUser NewUser(string email) => new(Guid.NewGuid(), "Alice", email, true, null, "en-US", ["people.read"], null, null, DateTime.UtcNow, null, null, null, null);
}