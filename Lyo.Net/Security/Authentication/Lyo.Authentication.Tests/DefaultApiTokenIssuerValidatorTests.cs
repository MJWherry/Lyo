using Lyo.Authentication.Exceptions;
using Lyo.Authentication.Models.Format;
using Lyo.Authentication.Models.Records;
using Lyo.Authentication.Options;
using Lyo.Authentication.Services.Opaque;
using Lyo.Authentication.Services.Users;
using Lyo.Common.Core.Identifiers;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Authentication.Tests;

public class DefaultApiTokenIssuerValidatorTests
{
    [Fact]
    public async Task IssueThenValidate_RoundTrips()
    {
        var (issuer, validator, _, users) = BuildPair();
        var user = await users.CreateAsync(NewUser(), null, TestCt);
        var issued = await issuer.IssueAsync(new(ApiTokenKind.Pat, "test", ["people.read"], user.Id), TestCt);
        var principal = await validator.ValidateAsync(issued.Plaintext, TestCt);
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
        var user = await users.CreateAsync(NewUser(), null, TestCt);
        var issued = await issuer.IssueAsync(new(ApiTokenKind.Pat, "test", [], user.Id, ApiTokenRing.Dev), TestCt);
        Assert.Null(await validator.ValidateAsync(issued.Plaintext, TestCt));
    }

    [Fact]
    public async Task Validate_RevokedToken_ReturnsNull()
    {
        var (issuer, validator, store, users) = BuildPair();
        var user = await users.CreateAsync(NewUser(), null, TestCt);
        var issued = await issuer.IssueAsync(new(ApiTokenKind.Pat, "test", [], user.Id), TestCt);
        await store.RevokeAsync(issued.Record.Id, DateTime.UtcNow, "test", null, TestCt);
        Assert.Null(await validator.ValidateAsync(issued.Plaintext, TestCt));
    }

    [Fact]
    public async Task Validate_ExpiredToken_ReturnsNull()
    {
        var (issuer, validator, _, users) = BuildPair();
        var user = await users.CreateAsync(NewUser(), null, TestCt);
        var issued = await issuer.IssueAsync(new(ApiTokenKind.Pat, "test", [], user.Id, Lifetime: TimeSpan.FromMilliseconds(1)), TestCt);
        await Task.Delay(100, TestCt);
        Assert.Null(await validator.ValidateAsync(issued.Plaintext, TestCt));
    }

    [Fact]
    public async Task Validate_DisabledUser_ReturnsNull()
    {
        var (issuer, validator, _, users) = BuildPair();
        var user = await users.CreateAsync(NewUser(), null, TestCt);
        var issued = await issuer.IssueAsync(new(ApiTokenKind.Pat, "test", [], user.Id), TestCt);
        Assert.NotNull(await validator.ValidateAsync(issued.Plaintext, TestCt));
        await users.SetDisabledAsync(user.Id, DateTime.UtcNow, "no", null, TestCt);
        Assert.Null(await validator.ValidateAsync(issued.Plaintext, TestCt));
    }

    [Fact]
    public async Task Issue_ForDisabledUser_Throws()
    {
        var (issuer, _, _, users) = BuildPair();
        var user = await users.CreateAsync(NewUser(), null, TestCt);
        await users.SetDisabledAsync(user.Id, DateTime.UtcNow, null, null, TestCt);
        await Assert.ThrowsAsync<LyoUserDisabledException>(() => issuer.IssueAsync(new(ApiTokenKind.Pat, "test", [], user.Id), TestCt));
    }

    [Fact]
    public async Task DynamicScopeIntersection_TrimsScopesAtValidation()
    {
        var (issuer, validator, _, users) = BuildPair(enableDynamicIntersection: true);
        var user = await users.CreateAsync(NewUser() with { Scopes = ["people.read", "people.write"] }, null, TestCt);
        var issued = await issuer.IssueAsync(new(ApiTokenKind.Pat, "test", ["people.read", "people.write"], user.Id), TestCt);
        var principal = await validator.ValidateAsync(issued.Plaintext, TestCt);
        Assert.NotNull(principal);
        Assert.Equal(2, principal.Scopes.Count);
        await users.SetScopesAsync(user.Id, ["people.read"], null, TestCt);
        principal = await validator.ValidateAsync(issued.Plaintext, TestCt);
        Assert.NotNull(principal);
        Assert.Single(principal.Scopes);
        Assert.Contains("people.read", principal.Scopes);
    }

    private static (IApiTokenIssuer, IApiTokenValidator, IApiTokenStore, IUserStore) BuildPair(string ring = ApiTokenRing.Live, bool enableDynamicIntersection = false)
    {
        var store = new InMemoryApiTokenStore();
        var users = new InMemoryUserStore();
        var options = new AuthenticationOptions { Ring = ring, EnableDynamicScopeIntersection = enableDynamicIntersection };
        var issuer = new DefaultApiTokenIssuer(store, users, options, NullLogger<DefaultApiTokenIssuer>.Instance);
        var validator = new DefaultApiTokenValidator(store, users, options, NullLogger<DefaultApiTokenValidator>.Instance);
        return (issuer, validator, store, users);
    }

    private static LyoUser NewUser()
        => new(LyoGuid.CreateV7(), "Test", $"u-{LyoGuid.CreateV7():N}@example.com", true, null, null, [], null, null, DateTime.UtcNow, null, null, null, null);
}