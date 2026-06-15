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
    [Fact]
    public async Task IssueThenValidate_RoundTrips()
    {
        var (issuer, validator, _, users) = BuildPair();
        var user = await users.CreateAsync(NewUser(), null, TestContext.Current.CancellationToken);
        var issued = await issuer.IssueAsync(new(ApiTokenKind.Pat, "test", ["people.read"], user.Id), TestContext.Current.CancellationToken);
        var principal = await validator.ValidateAsync(issued.Plaintext, TestContext.Current.CancellationToken);
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
        var user = await users.CreateAsync(NewUser(), null, TestContext.Current.CancellationToken);
        var issued = await issuer.IssueAsync(new(ApiTokenKind.Pat, "test", [], user.Id, ApiTokenRing.Dev), TestContext.Current.CancellationToken);
        Assert.Null(await validator.ValidateAsync(issued.Plaintext, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Validate_RevokedToken_ReturnsNull()
    {
        var (issuer, validator, store, users) = BuildPair();
        var user = await users.CreateAsync(NewUser(), null, TestContext.Current.CancellationToken);
        var issued = await issuer.IssueAsync(new(ApiTokenKind.Pat, "test", [], user.Id), TestContext.Current.CancellationToken);
        await store.RevokeAsync(issued.Record.Id, DateTime.UtcNow, "test", null, TestContext.Current.CancellationToken);
        Assert.Null(await validator.ValidateAsync(issued.Plaintext, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Validate_ExpiredToken_ReturnsNull()
    {
        var (issuer, validator, _, users) = BuildPair();
        var user = await users.CreateAsync(NewUser(), null, TestContext.Current.CancellationToken);
        var issued = await issuer.IssueAsync(new(ApiTokenKind.Pat, "test", [], user.Id, Lifetime: TimeSpan.FromMilliseconds(1)), TestContext.Current.CancellationToken);
        await Task.Delay(100, TestContext.Current.CancellationToken);
        Assert.Null(await validator.ValidateAsync(issued.Plaintext, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Validate_DisabledUser_ReturnsNull()
    {
        var (issuer, validator, _, users) = BuildPair();
        var user = await users.CreateAsync(NewUser(), null, TestContext.Current.CancellationToken);
        var issued = await issuer.IssueAsync(new(ApiTokenKind.Pat, "test", [], user.Id), TestContext.Current.CancellationToken);
        Assert.NotNull(await validator.ValidateAsync(issued.Plaintext, TestContext.Current.CancellationToken));
        await users.SetDisabledAsync(user.Id, DateTime.UtcNow, "no", null, TestContext.Current.CancellationToken);
        Assert.Null(await validator.ValidateAsync(issued.Plaintext, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Issue_ForDisabledUser_Throws()
    {
        var (issuer, _, _, users) = BuildPair();
        var user = await users.CreateAsync(NewUser(), null, TestContext.Current.CancellationToken);
        await users.SetDisabledAsync(user.Id, DateTime.UtcNow, null, null, TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<LyoUserDisabledException>(() => issuer.IssueAsync(new(ApiTokenKind.Pat, "test", [], user.Id), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DynamicScopeIntersection_TrimsScopesAtValidation()
    {
        var (issuer, validator, _, users) = BuildPair(enableDynamicIntersection: true);
        var user = await users.CreateAsync(NewUser() with { Scopes = ["people.read", "people.write"] }, null, TestContext.Current.CancellationToken);
        var issued = await issuer.IssueAsync(new(ApiTokenKind.Pat, "test", ["people.read", "people.write"], user.Id), TestContext.Current.CancellationToken);
        var principal = await validator.ValidateAsync(issued.Plaintext, TestContext.Current.CancellationToken);
        Assert.NotNull(principal);
        Assert.Equal(2, principal.Scopes.Count);
        await users.SetScopesAsync(user.Id, ["people.read"], null, TestContext.Current.CancellationToken);
        principal = await validator.ValidateAsync(issued.Plaintext, TestContext.Current.CancellationToken);
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