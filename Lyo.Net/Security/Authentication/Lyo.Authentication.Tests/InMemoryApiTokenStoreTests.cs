using Lyo.Authentication.Models.Format;
using Lyo.Authentication.Models.Records;
using Lyo.Authentication.Services.Opaque;
using Lyo.Authentication.Services.Refresh;
using Lyo.Exceptions;
using Lyo.Testing;

namespace Lyo.Authentication.Tests;

public class InMemoryApiTokenStoreTests
{
    [Fact]
    public async Task DeleteAsync_RemovesRow()
    {
        var store = new InMemoryApiTokenStore();
        var token = NewToken("abcdefghjkm", Guid.NewGuid());
        await store.InsertAsync(token, null, TestContext.Current.CancellationToken);
        await store.DeleteAsync(token.Id, null, TestContext.Current.CancellationToken);
        Assert.Null(await store.GetByIdAsync(token.Id, null, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeleteAsync_Missing_Throws()
    {
        var store = new InMemoryApiTokenStore();
        await Assert.ThrowsAsync<NotFoundException>(() => store.DeleteAsync("missingid01", null, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RevokeRefreshTokensForUserAsync_RevokesLiveRefreshForProvider()
    {
        var store = new InMemoryApiTokenStore();
        var userId = Guid.NewGuid();
        var live = NewToken("abcdefghjkn", userId, [LyoRefreshTokenScopes.Refresh], ApiTokenKind.Internal, new Dictionary<string, object?> {
            [DefaultLyoRefreshTokenIssuer.ProviderMetadataKey] = "google"
        });
        var otherProvider = NewToken("abcdefghjkp", userId, [LyoRefreshTokenScopes.Refresh], ApiTokenKind.Internal, new Dictionary<string, object?> {
            [DefaultLyoRefreshTokenIssuer.ProviderMetadataKey] = "keycloak"
        });
        var pat = NewToken("abcdefghjkq", userId, ["people.read"], ApiTokenKind.Pat);
        await store.InsertAsync(live, null, TestContext.Current.CancellationToken);
        await store.InsertAsync(otherProvider, null, TestContext.Current.CancellationToken);
        await store.InsertAsync(pat, null, TestContext.Current.CancellationToken);

        await store.RevokeRefreshTokensForUserAsync(userId, "google", DateTime.UtcNow, "login_replaced", null, TestContext.Current.CancellationToken);

        var google = await store.GetByIdAsync(live.Id, null, TestContext.Current.CancellationToken);
        var keycloak = await store.GetByIdAsync(otherProvider.Id, null, TestContext.Current.CancellationToken);
        var remainingPat = await store.GetByIdAsync(pat.Id, null, TestContext.Current.CancellationToken);
        Assert.NotNull(google?.RevokedAt);
        Assert.Equal("login_replaced", google.RevokedReason);
        Assert.Null(keycloak?.RevokedAt);
        Assert.Null(remainingPat?.RevokedAt);
    }

    private static ApiTokenRecord NewToken(
        string id,
        Guid userId,
        IReadOnlyList<string>? scopes = null,
        string? kind = null,
        IReadOnlyDictionary<string, object?>? metadata = null)
        => new(
            id, TestData.Create(32, TestData.Seed + id.GetHashCode()), kind ?? ApiTokenKind.Pat, ApiTokenRing.Live, userId, "test", scopes ?? [], metadata, DateTime.UtcNow, null,
            null, null, null, null, null);
}
