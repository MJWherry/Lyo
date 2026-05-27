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

        await store.InsertAsync(record, tenantId: null);
        var fetched = await store.GetByIdAsync(id, tenantId: null);
        Assert.NotNull(fetched);
        Assert.Equal(id, fetched!.Id);
        await store.TouchLastUsedAsync(id, DateTime.UtcNow, tenantId: null);
        fetched = await store.GetByIdAsync(id, tenantId: null);
        Assert.NotNull(fetched!.LastUsedAt);
        await store.RevokeAsync(id, DateTime.UtcNow, "test", tenantId: null);
        fetched = await store.GetByIdAsync(id, tenantId: null);
        Assert.NotNull(fetched!.RevokedAt);
        Assert.Equal("test", fetched.RevokedReason);
        Assert.NotNull(plaintext);
    }

    [Fact]
    public async Task InMemoryApiTokenStore_DuplicateId_Throws()
    {
        var store = new InMemoryApiTokenStore();
        var record = NewTokenRecord();
        await store.InsertAsync(record, tenantId: null);
        await Assert.ThrowsAnyAsync<InvalidOperationException>(() => store.InsertAsync(record, tenantId: null));
    }

    [Fact]
    public async Task InMemoryApiTokenStore_ListForUser_FiltersByOwner()
    {
        var store = new InMemoryApiTokenStore();
        var u1 = Guid.NewGuid();
        var u2 = Guid.NewGuid();
        await store.InsertAsync(NewTokenRecord(u1), tenantId: null);
        await store.InsertAsync(NewTokenRecord(u1), tenantId: null);
        await store.InsertAsync(NewTokenRecord(u2), tenantId: null);
        var list = await store.ListForUserAsync(u1, includeRevoked: false, tenantId: null);
        Assert.Equal(2, list.Count);
    }

    [Fact]
    public async Task InMemoryUserStore_CreateGetByIdAndEmail_RoundTrips()
    {
        var store = new InMemoryUserStore();
        var user = NewUser();
        await store.CreateAsync(user, tenantId: null);
        Assert.Equal(user.Id, (await store.GetByIdAsync(user.Id, tenantId: null))!.Id);
        Assert.Equal(user.Id, (await store.GetByEmailAsync(user.Email, tenantId: null))!.Id);
        Assert.Null(await store.GetByEmailAsync("ghost@example.com", tenantId: null));
    }

    [Fact]
    public async Task InMemoryUserStore_DuplicateEmail_Throws()
    {
        var store = new InMemoryUserStore();
        var a = NewUser();
        var b = NewUser() with { Email = a.Email };
        await store.CreateAsync(a, tenantId: null);
        await Assert.ThrowsAnyAsync<InvalidOperationException>(() => store.CreateAsync(b, tenantId: null));
    }

    [Fact]
    public async Task InMemoryUserStore_SetDisabled_FlipsState()
    {
        var store = new InMemoryUserStore();
        var u = NewUser();
        await store.CreateAsync(u, tenantId: null);
        await store.SetDisabledAsync(u.Id, DateTime.UtcNow, "kicked", tenantId: null);
        var disabled = await store.GetByIdAsync(u.Id, tenantId: null);
        Assert.True(disabled!.IsDisabled);
        Assert.Equal("kicked", disabled.DisabledReason);
    }

    [Fact]
    public async Task InMemoryExternalIdentityStore_LinkFindUnlinkRelink_Works()
    {
        var store = new InMemoryExternalIdentityStore();
        var userId = Guid.NewGuid();
        var link = await store.LinkAsync(userId, "google", "sub-1", "x@example.com", new[] { "admin" }, null, tenantId: null);
        Assert.NotEqual(Guid.Empty, link.Id);
        var found = await store.FindByProviderSubjectAsync("google", "sub-1", tenantId: null);
        Assert.NotNull(found);
        await store.UnlinkAsync(link.Id, DateTime.UtcNow, tenantId: null);
        Assert.Null(await store.FindByProviderSubjectAsync("google", "sub-1", tenantId: null));
        var relink = await store.LinkAsync(userId, "google", "sub-1", "x@example.com", System.Array.Empty<string>(), null, tenantId: null);
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
