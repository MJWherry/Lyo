using Lyo.Authentication.Exceptions;
using Lyo.Authentication.Models.Format;
using Lyo.Authentication.Models.Records;
using Lyo.Authentication.Options;
using Lyo.Authentication.Services.Opaque;
using Lyo.Authentication.Services.Users;
using Microsoft.Extensions.Logging.Abstractions;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace Lyo.Authentication.Tests;

public class DefaultApiTokenIssuerValidatorTests
{
    public CancellationToken TCT = TestContext.Current.CancellationToken;

    [Fact]
    public async Task IssueThenValidate_RoundTrips()
    {
        var (issuer, validator, _, users) = BuildPair();
        var user = await users.CreateAsync(NewUser(), null, TCT);
        var issued = await issuer.IssueAsync(new(ApiTokenKind.Pat, "test", ["people.read"], user.Id), TCT);
        var principal = await validator.ValidateAsync(issued.Plaintext, TCT);
        Assert.NotNull(principal);
        Assert.Equal(user.Id, principal.OwnerUserId);
        Assert.Equal(ApiTokenKind.Pat, principal.Kind);
        Assert.Equal(ApiTokenRing.Live, principal.Ring);
        Assert.Contains("people.read", principal.Scopes);
    }

    [Fact]
    public async Task Validate_RingMismatch_ReturnsNull()
    {
        var (issuer, validator, _, users) = BuildPair();
        var user = await users.CreateAsync(NewUser(), null, TCT);
        var issued = await issuer.IssueAsync(new(ApiTokenKind.Pat, "test", [], user.Id, ApiTokenRing.Dev), TCT);
        Assert.Null(await validator.ValidateAsync(issued.Plaintext, TCT));
    }

    [Fact]
    public async Task Validate_RevokedToken_ReturnsNull()
    {
        var (issuer, validator, store, users) = BuildPair();
        var user = await users.CreateAsync(NewUser(), null, TCT);
        var issued = await issuer.IssueAsync(new(ApiTokenKind.Pat, "test", [], user.Id), TCT);
        await store.RevokeAsync(issued.Record.Id, DateTime.UtcNow, "test", null, TCT);
        Assert.Null(await validator.ValidateAsync(issued.Plaintext, TCT));
    }

    [Fact]
    public async Task Validate_ExpiredToken_ReturnsNull()
    {
        var (issuer, validator, _, users) = BuildPair();
        var user = await users.CreateAsync(NewUser(), null, TCT);
        var issued = await issuer.IssueAsync(new(ApiTokenKind.Pat, "test", [], user.Id, Lifetime: TimeSpan.FromMilliseconds(1)), TCT);
        await Task.Delay(100, TCT);
        Assert.Null(await validator.ValidateAsync(issued.Plaintext, TCT));
    }

    [Fact]
    public async Task Validate_DisabledUser_ReturnsNull()
    {
        var (issuer, validator, _, users) = BuildPair();
        var user = await users.CreateAsync(NewUser(), null, TCT);
        var issued = await issuer.IssueAsync(new(ApiTokenKind.Pat, "test", [], user.Id), TCT);
        Assert.NotNull(await validator.ValidateAsync(issued.Plaintext, TCT));
        await users.SetDisabledAsync(user.Id, DateTime.UtcNow, "no", null, TCT);
        Assert.Null(await validator.ValidateAsync(issued.Plaintext, TCT));
    }

    [Fact]
    public async Task Issue_ForDisabledUser_Throws()
    {
        var (issuer, _, _, users) = BuildPair();
        var user = await users.CreateAsync(NewUser(), null, TCT);
        await users.SetDisabledAsync(user.Id, DateTime.UtcNow, null, null, TCT);
        await Assert.ThrowsAsync<LyoUserDisabledException>(() => issuer.IssueAsync(new(ApiTokenKind.Pat, "test", [], user.Id), TCT));
    }

    [Fact]
    public async Task DynamicScopeIntersection_TrimsScopesAtValidation()
    {
        var (issuer, validator, _, users) = BuildPair(enableDynamicIntersection: true);
        var user = await users.CreateAsync(NewUser() with { Scopes = ["people.read", "people.write"] }, null, TCT);
        var issued = await issuer.IssueAsync(new(ApiTokenKind.Pat, "test", ["people.read", "people.write"], user.Id), TCT);
        var principal = await validator.ValidateAsync(issued.Plaintext, TCT);
        Assert.NotNull(principal);
        Assert.Equal(2, principal.Scopes.Count);
        await users.SetScopesAsync(user.Id, ["people.read"], null, TCT);
        principal = await validator.ValidateAsync(issued.Plaintext, TCT);
        Assert.NotNull(principal);
        Assert.Single(principal.Scopes);
        Assert.Contains("people.read", principal.Scopes);
    }

    private static (IApiTokenIssuer, IApiTokenValidator, IApiTokenStore, IUserStore) BuildPair(string ring = ApiTokenRing.Live, bool enableDynamicIntersection = false)
    {
        var store = new InMemoryApiTokenStore();
        var users = new InMemoryUserStore();
        var options = MsOptions.Create(new AuthenticationOptions { Ring = ring, EnableDynamicScopeIntersection = enableDynamicIntersection });
        var issuer = new DefaultApiTokenIssuer(store, users, options, NullLogger<DefaultApiTokenIssuer>.Instance);
        var validator = new DefaultApiTokenValidator(store, users, options, NullLogger<DefaultApiTokenValidator>.Instance);
        return (issuer, validator, store, users);
    }

    private static LyoUser NewUser() => new(Guid.NewGuid(), "Test", $"u-{Guid.NewGuid():N}@example.com", true, null, null, [], null, null, DateTime.UtcNow, null, null, null, null);
}