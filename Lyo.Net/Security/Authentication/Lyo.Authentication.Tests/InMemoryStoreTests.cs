using System;
using System.Threading.Tasks;
using Lyo.Authentication.Format;
using Lyo.Authentication.Records;
using Lyo.Authentication.Services.Opaque;
using Lyo.Authentication.Services.Users;

namespace Lyo.Authentication.Tests;

public class InMemoryStoreTests
{
    [Fact]
    public async Task InMemoryApiTokenStore_InsertGetTouchRevoke_RoundTrips()
    {
        var store = new InMemoryApiTokenStore();
        var (plaintext, id, hash) = ApiTokenCodec.Mint(ApiTokenKind.Pat, ApiTokenRing.Live);
        var record = new ApiTokenRecord(
            id, hash, ApiTokenKind.Pat, ApiTokenRing.Live,
            UserId: Guid.NewGuid(), DisplayName: "test", Scopes: new[] { "people.read" }, Metadata: null,
            CreatedAt: DateTime.UtcNow, UpdatedAt: null, ExpiresAt: null, LastUsedAt: null,
            RevokedAt: null, RevokedReason: null, RotatedFromId: null);

        await store.InsertAsync(record);
        var fetched = await store.GetByIdAsync(id);
        Assert.NotNull(fetched);
        Assert.Equal(id, fetched!.Id);
        await store.TouchLastUsedAsync(id, DateTime.UtcNow);
        fetched = await store.GetByIdAsync(id);
        Assert.NotNull(fetched!.LastUsedAt);
        await store.RevokeAsync(id, DateTime.UtcNow, "test");
        fetched = await store.GetByIdAsync(id);
        Assert.NotNull(fetched!.RevokedAt);
        Assert.Equal("test", fetched.RevokedReason);
        Assert.NotNull(plaintext);
    }

    [Fact]
    public async Task InMemoryApiTokenStore_DuplicateId_Throws()
    {
        var store = new InMemoryApiTokenStore();
        var record = NewTokenRecord();
        await store.InsertAsync(record);
        await Assert.ThrowsAnyAsync<InvalidOperationException>(() => store.InsertAsync(record));
    }

    [Fact]
    public async Task InMemoryApiTokenStore_ListForUser_FiltersByOwner()
    {
        var store = new InMemoryApiTokenStore();
        var u1 = Guid.NewGuid();
        var u2 = Guid.NewGuid();
        await store.InsertAsync(NewTokenRecord(u1));
        await store.InsertAsync(NewTokenRecord(u1));
        await store.InsertAsync(NewTokenRecord(u2));
        var list = await store.ListForUserAsync(u1, includeRevoked: false);
        Assert.Equal(2, list.Count);
    }

    [Fact]
    public async Task InMemoryUserStore_CreateGetByIdAndEmail_RoundTrips()
    {
        var store = new InMemoryUserStore();
        var user = NewUser();
        await store.CreateAsync(user);
        Assert.Equal(user.Id, (await store.GetByIdAsync(user.Id))!.Id);
        Assert.Equal(user.Id, (await store.GetByEmailAsync(user.Email))!.Id);
        Assert.Null(await store.GetByEmailAsync("ghost@example.com"));
    }

    [Fact]
    public async Task InMemoryUserStore_DuplicateEmail_Throws()
    {
        var store = new InMemoryUserStore();
        var a = NewUser();
        var b = NewUser() with { Email = a.Email };
        await store.CreateAsync(a);
        await Assert.ThrowsAnyAsync<InvalidOperationException>(() => store.CreateAsync(b));
    }

    [Fact]
    public async Task InMemoryUserStore_SetDisabled_FlipsState()
    {
        var store = new InMemoryUserStore();
        var u = NewUser();
        await store.CreateAsync(u);
        await store.SetDisabledAsync(u.Id, DateTime.UtcNow, "kicked");
        var disabled = await store.GetByIdAsync(u.Id);
        Assert.True(disabled!.IsDisabled);
        Assert.Equal("kicked", disabled.DisabledReason);
    }

    [Fact]
    public async Task InMemoryExternalIdentityStore_LinkFindUnlinkRelink_Works()
    {
        var store = new InMemoryExternalIdentityStore();
        var userId = Guid.NewGuid();
        var link = await store.LinkAsync(userId, "google", "sub-1", "x@example.com", new[] { "admin" }, null);
        Assert.NotEqual(Guid.Empty, link.Id);
        var found = await store.FindByProviderSubjectAsync("google", "sub-1");
        Assert.NotNull(found);
        await store.UnlinkAsync(link.Id, DateTime.UtcNow);
        Assert.Null(await store.FindByProviderSubjectAsync("google", "sub-1"));
        var relink = await store.LinkAsync(userId, "google", "sub-1", "x@example.com", System.Array.Empty<string>(), null);
        Assert.NotEqual(link.Id, relink.Id);
    }

    private static ApiTokenRecord NewTokenRecord(Guid? userId = null)
    {
        var (_, id, hash) = ApiTokenCodec.Mint(ApiTokenKind.Pat, ApiTokenRing.Live);
        return new(
            id, hash, ApiTokenKind.Pat, ApiTokenRing.Live,
            UserId: userId ?? Guid.NewGuid(), DisplayName: "test", Scopes: System.Array.Empty<string>(), Metadata: null,
            CreatedAt: DateTime.UtcNow, UpdatedAt: null, ExpiresAt: null, LastUsedAt: null,
            RevokedAt: null, RevokedReason: null, RotatedFromId: null);
    }

    private static LyoUser NewUser() => new(
        Id: Guid.NewGuid(),
        DisplayName: "Test " + Guid.NewGuid().ToString("N").Substring(0, 8),
        Email: $"user-{Guid.NewGuid():N}@example.com",
        EmailVerified: true,
        AvatarUrl: null,
        PreferredLanguageBcp47: null,
        Scopes: System.Array.Empty<string>(),
        Metadata: null,
        PersonId: null,
        CreatedAt: DateTime.UtcNow,
        UpdatedAt: null,
        LastLoginAt: null,
        DisabledAt: null,
        DisabledReason: null);
}
