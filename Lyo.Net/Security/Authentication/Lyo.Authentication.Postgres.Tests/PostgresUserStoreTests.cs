using Lyo.Authentication.Models.Records;
using Lyo.Authentication.Postgres.Stores;
using Lyo.EntityReference.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Lyo.Common.Core.Identifiers;

namespace Lyo.Authentication.Postgres.Tests;

public sealed class PostgresUserStoreTests
{
    private readonly AuthenticationPostgresFixture _fixture;

    public PostgresUserStoreTests(AuthenticationPostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Create_And_Get_RoundTrip()
    {
        var user = NewUser($"alice-{LyoGuid.CreateV7():N}@example.com");
        await _fixture.UserStore.CreateAsync(user, null, TestCt);
        var loaded = await _fixture.UserStore.GetByIdAsync(user.Id, null, TestCt);
        Assert.NotNull(loaded);
        Assert.Equal(user.Email, loaded.Email);
        Assert.Equal(user.DisplayName, loaded.DisplayName);
    }

    [Fact]
    public async Task GetByEmail_IsCaseInsensitive()
    {
        var user = NewUser($"Mixed-{LyoGuid.CreateV7():N}@EXAMPLE.COM");
        await _fixture.UserStore.CreateAsync(user, null, TestCt);
        var loaded = await _fixture.UserStore.GetByEmailAsync(user.Email.ToLowerInvariant(), null, TestCt);
        Assert.NotNull(loaded);
        Assert.Equal(user.Id, loaded.Id);
    }

    [Fact]
    public async Task Create_DuplicateEmail_Throws()
    {
        var email = $"dup-{LyoGuid.CreateV7():N}@example.com";
        var first = NewUser(email);
        await _fixture.UserStore.CreateAsync(first, null, TestCt);
        var second = NewUser(email);
        await Assert.ThrowsAnyAsync<Exception>(() => _fixture.UserStore.CreateAsync(second, null, TestCt));
    }

    [Fact]
    public async Task SetScopes_PersistsNewSnapshot()
    {
        var user = NewUser($"scopes-{LyoGuid.CreateV7():N}@example.com");
        await _fixture.UserStore.CreateAsync(user, null, TestCt);
        await _fixture.UserStore.SetScopesAsync(user.Id, ["admin", "people.read"], null, TestCt);
        var loaded = await _fixture.UserStore.GetByIdAsync(user.Id, null, TestCt);
        Assert.NotNull(loaded);
        Assert.Equal(["admin", "people.read"], loaded.Scopes);
    }

    [Fact]
    public async Task SetDisabled_FlipsKillSwitch()
    {
        var user = NewUser($"disable-{LyoGuid.CreateV7():N}@example.com");
        await _fixture.UserStore.CreateAsync(user, null, TestCt);
        var when = DateTime.UtcNow;
        await _fixture.UserStore.SetDisabledAsync(user.Id, when, "policy", null, TestCt);
        var loaded = await _fixture.UserStore.GetByIdAsync(user.Id, null, TestCt);
        Assert.NotNull(loaded);
        Assert.True(loaded.IsDisabled);
        Assert.Equal("policy", loaded.DisabledReason);
    }

    [Fact]
    public async Task UpdateLastLogin_Touches()
    {
        var user = NewUser($"login-{LyoGuid.CreateV7():N}@example.com");
        await _fixture.UserStore.CreateAsync(user, null, TestCt);
        var now = DateTime.UtcNow;
        await _fixture.UserStore.UpdateLastLoginAsync(user.Id, now, null, TestCt);
        var loaded = await _fixture.UserStore.GetByIdAsync(user.Id, null, TestCt);
        Assert.NotNull(loaded);
        Assert.NotNull(loaded.LastLoginAt);
    }

    [Fact]
    public async Task MultiTenantStrict_ThrowsWhenCallerOmitsTenant()
    {
        var strictStore = new PostgresUserStore(
            _fixture.ContextFactory, NullLogger<PostgresUserStore>.Instance, new EntityRefOptions(),
            new PostgresUserOptions { Tenancy = new() { Mode = TenancyMode.MultiTenantStrict } });

        var user = NewUser($"strict-{LyoGuid.CreateV7():N}@example.com");
        await Assert.ThrowsAsync<ArgumentNullException>(() => strictStore.CreateAsync(user, null, TestCt));
        await Assert.ThrowsAsync<ArgumentNullException>(() => strictStore.GetByIdAsync(user.Id, null, TestCt));
    }

    [Fact]
    public async Task SystemOnly_StoresAndRetrievesWithNullTenant()
    {
        var systemStore = new PostgresUserStore(
            _fixture.ContextFactory, NullLogger<PostgresUserStore>.Instance, new EntityRefOptions(),
            new PostgresUserOptions { Tenancy = new() { Mode = TenancyMode.SystemOnly } });

        var user = NewUser($"system-{LyoGuid.CreateV7():N}@example.com");
        await systemStore.CreateAsync(user, LyoGuid.CreateV7(), TestCt);
        var loaded = await systemStore.GetByIdAsync(user.Id, LyoGuid.CreateV7(), TestCt);
        Assert.NotNull(loaded);
        Assert.Equal(user.Email, loaded.Email);
    }

    private static LyoUser NewUser(string email) => new(LyoGuid.CreateV7(), "Alice", email, true, null, "en-US", ["people.read"], null, null, DateTime.UtcNow, null, null, null, null);
}