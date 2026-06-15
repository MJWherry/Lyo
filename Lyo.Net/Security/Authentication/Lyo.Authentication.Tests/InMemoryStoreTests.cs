using Lyo.Authentication.Format;
using Lyo.Authentication.Models.Format;
using Lyo.Authentication.Models.Records;
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
            id, hash, ApiTokenKind.Pat, ApiTokenRing.Live, Guid.NewGuid(), "test", ["people.read"], null, DateTime.UtcNow, null, null, null, null, null, null);

        await store.InsertAsync(record, null, TestContext.Current.CancellationToken);
        var fetched = await store.GetByIdAsync(id, null, TestContext.Current.CancellationToken);
        Assert.NotNull(fetched);
        Assert.Equal(id, fetched.Id);
        await store.TouchLastUsedAsync(id, DateTime.UtcNow, null, TestContext.Current.CancellationToken);
        fetched = await store.GetByIdAsync(id, null, TestContext.Current.CancellationToken);
        Assert.NotNull(fetched);
        Assert.NotNull(fetched.LastUsedAt);
        await store.RevokeAsync(id, DateTime.UtcNow, "test", null, TestContext.Current.CancellationToken);
        fetched = await store.GetByIdAsync(id, null, TestContext.Current.CancellationToken);
        Assert.NotNull(fetched);
        Assert.NotNull(fetched.RevokedAt);
        Assert.Equal("test", fetched.RevokedReason);
        Assert.NotNull(plaintext);
    }

    [Fact]
    public async Task InMemoryApiTokenStore_DuplicateId_Throws()
    {
        var store = new InMemoryApiTokenStore();
        var record = NewTokenRecord();
        await store.InsertAsync(record, null, TestContext.Current.CancellationToken);
        await Assert.ThrowsAnyAsync<InvalidOperationException>(() => store.InsertAsync(record, null, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task InMemoryApiTokenStore_ListForUser_FiltersByOwner()
    {
        var store = new InMemoryApiTokenStore();
        var u1 = Guid.NewGuid();
        var u2 = Guid.NewGuid();
        await store.InsertAsync(NewTokenRecord(u1), null, TestContext.Current.CancellationToken);
        await store.InsertAsync(NewTokenRecord(u1), null, TestContext.Current.CancellationToken);
        await store.InsertAsync(NewTokenRecord(u2), null, TestContext.Current.CancellationToken);
        var list = await store.ListForUserAsync(u1, false, null, TestContext.Current.CancellationToken);
        Assert.Equal(2, list.Count);
    }

    [Fact]
    public async Task InMemoryUserStore_CreateGetByIdAndEmail_RoundTrips()
    {
        var store = new InMemoryUserStore();
        var user = NewUser();
        await store.CreateAsync(user, null, TestContext.Current.CancellationToken);
        var byId = await store.GetByIdAsync(user.Id, null, TestContext.Current.CancellationToken);
        Assert.NotNull(byId);
        Assert.Equal(user.Id, byId.Id);
        var byEmail = await store.GetByEmailAsync(user.Email, null, TestContext.Current.CancellationToken);
        Assert.NotNull(byEmail);
        Assert.Equal(user.Id, byEmail.Id);
        Assert.Null(await store.GetByEmailAsync("ghost@example.com", null, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task InMemoryUserStore_DuplicateEmail_Throws()
    {
        var store = new InMemoryUserStore();
        var a = NewUser();
        var b = NewUser() with { Email = a.Email };
        await store.CreateAsync(a, null, TestContext.Current.CancellationToken);
        await Assert.ThrowsAnyAsync<InvalidOperationException>(() => store.CreateAsync(b, null, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task InMemoryUserStore_SetDisabled_FlipsState()
    {
        var store = new InMemoryUserStore();
        var u = NewUser();
        await store.CreateAsync(u, null, TestContext.Current.CancellationToken);
        await store.SetDisabledAsync(u.Id, DateTime.UtcNow, "kicked", null, TestContext.Current.CancellationToken);
        var disabled = await store.GetByIdAsync(u.Id, null, TestContext.Current.CancellationToken);
        Assert.NotNull(disabled);
        Assert.True(disabled.IsDisabled);
        Assert.Equal("kicked", disabled.DisabledReason);
    }

    [Fact]
    public async Task InMemoryExternalIdentityStore_LinkFindUnlinkRelink_Works()
    {
        var store = new InMemoryExternalIdentityStore();
        var userId = Guid.NewGuid();
        var link = await store.LinkAsync(userId, "google", "sub-1", "x@example.com", ["admin"], null, null, TestContext.Current.CancellationToken);
        Assert.NotEqual(Guid.Empty, link.Id);
        var found = await store.FindByProviderSubjectAsync("google", "sub-1", null, TestContext.Current.CancellationToken);
        Assert.NotNull(found);
        await store.UnlinkAsync(link.Id, DateTime.UtcNow, null, TestContext.Current.CancellationToken);
        Assert.Null(await store.FindByProviderSubjectAsync("google", "sub-1", null, TestContext.Current.CancellationToken));
        var relink = await store.LinkAsync(userId, "google", "sub-1", "x@example.com", [], null, null, TestContext.Current.CancellationToken);
        Assert.NotEqual(link.Id, relink.Id);
    }

    private static ApiTokenRecord NewTokenRecord(Guid? userId = null)
    {
        var (_, id, hash) = ApiTokenCodec.Mint(ApiTokenKind.Pat, ApiTokenRing.Live);
        return new(id, hash, ApiTokenKind.Pat, ApiTokenRing.Live, userId ?? Guid.NewGuid(), "test", [], null, DateTime.UtcNow, null, null, null, null, null, null);
    }

    private static LyoUser NewUser()
        => new(
            Guid.NewGuid(), "Test " + Guid.NewGuid().ToString("N").Substring(0, 8), $"user-{Guid.NewGuid():N}@example.com", true, null, null, [], null, null, DateTime.UtcNow, null,
            null, null, null);
}