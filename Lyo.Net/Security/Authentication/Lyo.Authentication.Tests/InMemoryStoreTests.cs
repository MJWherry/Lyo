using Lyo.Authentication.Format;
using Lyo.Authentication.Models.Format;
using Lyo.Authentication.Models.Records;
using Lyo.Authentication.Services.Opaque;
using Lyo.Authentication.Services.Users;
using Lyo.Common.Core.Identifiers;
using Lyo.Exceptions.Models;

namespace Lyo.Authentication.Tests;

public class InMemoryStoreTests
{
    [Fact]
    public async Task InMemoryApiTokenStore_InsertGetTouchRevoke_RoundTrips()
    {
        var store = new InMemoryApiTokenStore();
        var (plaintext, id, hash) = ApiTokenCodec.Mint(ApiTokenKind.Pat, ApiTokenRing.Live);
        var record = new ApiTokenRecord(
            id, hash, ApiTokenKind.Pat, ApiTokenRing.Live, LyoGuid.CreateV7(), "test", ["people.read"], null, DateTime.UtcNow, null, null, null, null, null, null);

        await store.InsertAsync(record, null, TestCt);
        var fetched = await store.GetByIdAsync(id, null, TestCt);
        Assert.NotNull(fetched);
        Assert.Equal(id, fetched.Id);
        await store.TouchLastUsedAsync(id, DateTime.UtcNow, null, TestCt);
        fetched = await store.GetByIdAsync(id, null, TestCt);
        Assert.NotNull(fetched);
        Assert.NotNull(fetched.LastUsedAt);
        await store.RevokeAsync(id, DateTime.UtcNow, "test", null, TestCt);
        fetched = await store.GetByIdAsync(id, null, TestCt);
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
        await store.InsertAsync(record, null, TestCt);
        await Assert.ThrowsAnyAsync<ConflictException>(() => store.InsertAsync(record, null, TestCt));
    }

    [Fact]
    public async Task InMemoryApiTokenStore_ListForUser_FiltersByOwner()
    {
        var store = new InMemoryApiTokenStore();
        var u1 = LyoGuid.CreateV7();
        var u2 = LyoGuid.CreateV7();
        await store.InsertAsync(NewTokenRecord(u1), null, TestCt);
        await store.InsertAsync(NewTokenRecord(u1), null, TestCt);
        await store.InsertAsync(NewTokenRecord(u2), null, TestCt);
        var list = await store.ListForUserAsync(u1, false, null, TestCt);
        Assert.Equal(2, list.Count);
    }

    [Fact]
    public async Task InMemoryUserStore_CreateGetByIdAndEmail_RoundTrips()
    {
        var store = new InMemoryUserStore();
        var user = NewUser();
        await store.CreateAsync(user, null, TestCt);
        var byId = await store.GetByIdAsync(user.Id, null, TestCt);
        Assert.NotNull(byId);
        Assert.Equal(user.Id, byId.Id);
        var byEmail = await store.GetByEmailAsync(user.Email, null, TestCt);
        Assert.NotNull(byEmail);
        Assert.Equal(user.Id, byEmail.Id);
        Assert.Null(await store.GetByEmailAsync("ghost@example.com", null, TestCt));
    }

    [Fact]
    public async Task InMemoryUserStore_DuplicateEmail_Throws()
    {
        var store = new InMemoryUserStore();
        var a = NewUser();
        var b = NewUser() with { Email = a.Email };
        await store.CreateAsync(a, null, TestCt);
        await Assert.ThrowsAnyAsync<ConflictException>(() => store.CreateAsync(b, null, TestCt));
    }

    [Fact]
    public async Task InMemoryUserStore_SetDisabled_FlipsState()
    {
        var store = new InMemoryUserStore();
        var u = NewUser();
        await store.CreateAsync(u, null, TestCt);
        await store.SetDisabledAsync(u.Id, DateTime.UtcNow, "kicked", null, TestCt);
        var disabled = await store.GetByIdAsync(u.Id, null, TestCt);
        Assert.NotNull(disabled);
        Assert.True(disabled.IsDisabled);
        Assert.Equal("kicked", disabled.DisabledReason);
    }

    [Fact]
    public async Task InMemoryExternalIdentityStore_LinkFindUnlinkRelink_Works()
    {
        var store = new InMemoryExternalIdentityStore();
        var userId = LyoGuid.CreateV7();
        var link = await store.LinkAsync(userId, "google", "sub-1", "x@example.com", ["admin"], null, null, TestCt);
        Assert.NotEqual(Guid.Empty, link.Id);
        var found = await store.FindByProviderSubjectAsync("google", "sub-1", null, TestCt);
        Assert.NotNull(found);
        await store.UnlinkAsync(link.Id, DateTime.UtcNow, null, TestCt);
        Assert.Null(await store.FindByProviderSubjectAsync("google", "sub-1", null, TestCt));
        var relink = await store.LinkAsync(userId, "google", "sub-1", "x@example.com", [], null, null, TestCt);
        Assert.NotEqual(link.Id, relink.Id);
    }

    private static ApiTokenRecord NewTokenRecord(Guid? userId = null)
    {
        var (_, id, hash) = ApiTokenCodec.Mint(ApiTokenKind.Pat, ApiTokenRing.Live);
        return new(id, hash, ApiTokenKind.Pat, ApiTokenRing.Live, userId ?? LyoGuid.CreateV7(), "test", [], null, DateTime.UtcNow, null, null, null, null, null, null);
    }

    private static LyoUser NewUser()
        => new(
            LyoGuid.CreateV7(), "Test " + LyoGuid.CreateV7().ToString("N").Substring(0, 8), $"user-{LyoGuid.CreateV7():N}@example.com", true, null, null, [], null, null,
            DateTime.UtcNow, null, null, null, null);
}