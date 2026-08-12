using System.Text;
using Lyo.Authentication.Models.Format;
using Lyo.Authentication.Models.Records;
using Lyo.Authentication.Options;
using Lyo.Authentication.Services.Jwt;
using Lyo.Authentication.Services.Users;
using Lyo.Common.Extensions;
using Lyo.Common.Security;
using Lyo.KeyStore;
using Microsoft.Extensions.Logging.Abstractions;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace Lyo.Authentication.Tests;

public class Ed25519LyoJwtTests
{
    [Fact]
    public async Task IssueThenValidate_RoundTrips()
    {
        var (issuer, validator, users, _) = await Build();
        var user = await users.CreateAsync(NewUser(), null, TestContext.Current.CancellationToken);
        var issued = await issuer.IssueAsync(user, ["people.read"], "google", "g-sub-1", false, TestContext.Current.CancellationToken);
        Assert.False(issued.AccessToken.IsNullOrEmpty());
        Assert.NotEqual(default, issued.AccessTokenExpiresAt);
        var principal = await validator.ValidateAsync(issued.AccessToken, TestContext.Current.CancellationToken);
        Assert.NotNull(principal);
        Assert.True(principal.HasClaim(LyoJwtClaims.Scope, "people.read"));
        Assert.True(principal.HasClaim(LyoJwtClaims.LyoProvider, "google"));
        Assert.True(principal.HasClaim(LyoJwtClaims.LyoUser, user.Id.ToString("D")));
    }

    [Fact]
    public async Task Validate_RejectsForgedAlgorithm()
    {
        var (issuer, validator, users, _) = await Build();
        var user = await users.CreateAsync(NewUser(), null, TestContext.Current.CancellationToken);
        var issued = await issuer.IssueAsync(user, [], "local", null, false, TestContext.Current.CancellationToken);
        var parts = issued.AccessToken.Split('.');
        var forgedHeader = Base64Url.Encode(Encoding.UTF8.GetBytes("""{"alg":"none","kid":"lyo-sig:v1","typ":"JWT"}"""));
        var forged = $"{forgedHeader}.{parts[1]}.{parts[2]}";
        Assert.Null(await validator.ValidateAsync(forged, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Validate_RejectsWrongIssuer()
    {
        var keys = new LocalKeyStore();
        keys.AddKey("lyo-sig", "v1", CryptographicRandom.GetBytes(32));
        keys.SetCurrentVersion("lyo-sig", "v1");
        var users = new InMemoryUserStore();
        var issueOpts = MsOptions.Create(new LyoJwtOptions { Issuer = "https://evil", Audience = "lyo-api" });
        var validateOpts = MsOptions.Create(new LyoJwtOptions { Issuer = "https://auth.lyo", Audience = "lyo-api" });
        var issuer = new Ed25519LyoJwtIssuer(keys, issueOpts, NullLogger<Ed25519LyoJwtIssuer>.Instance);
        var validator = new Ed25519LyoJwtValidator(keys, users, validateOpts, MsOptions.Create(new AuthenticationOptions()), NullLogger<Ed25519LyoJwtValidator>.Instance);
        var user = await users.CreateAsync(NewUser(), null, TestContext.Current.CancellationToken);
        var issued = await issuer.IssueAsync(user, [], "local", null, false, TestContext.Current.CancellationToken);
        Assert.Null(await validator.ValidateAsync(issued.AccessToken, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Validate_RejectsExpired()
    {
        var keys = new LocalKeyStore();
        keys.AddKey("lyo-sig", "v1", CryptographicRandom.GetBytes(32));
        keys.SetCurrentVersion("lyo-sig", "v1");
        var users = new InMemoryUserStore();
        var jwtOpts = MsOptions.Create(new LyoJwtOptions { AccessTokenLifetime = TimeSpan.FromMilliseconds(1), ClockSkew = TimeSpan.Zero });
        var issuer = new Ed25519LyoJwtIssuer(keys, jwtOpts, NullLogger<Ed25519LyoJwtIssuer>.Instance);
        var validator = new Ed25519LyoJwtValidator(keys, users, jwtOpts, MsOptions.Create(new AuthenticationOptions()), NullLogger<Ed25519LyoJwtValidator>.Instance);
        var user = await users.CreateAsync(NewUser(), null, TestContext.Current.CancellationToken);
        var issued = await issuer.IssueAsync(user, [], "local", null, false, TestContext.Current.CancellationToken);
        await Task.Delay(1100, TestContext.Current.CancellationToken);
        Assert.Null(await validator.ValidateAsync(issued.AccessToken, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Validate_DisabledUser_ReturnsNull()
    {
        var (issuer, validator, users, _) = await Build();
        var user = await users.CreateAsync(NewUser(), null, TestContext.Current.CancellationToken);
        var issued = await issuer.IssueAsync(user, [], "local", null, false, TestContext.Current.CancellationToken);
        Assert.NotNull(await validator.ValidateAsync(issued.AccessToken, TestContext.Current.CancellationToken));
        await users.SetDisabledAsync(user.Id, DateTime.UtcNow, "no", null, TestContext.Current.CancellationToken);
        Assert.Null(await validator.ValidateAsync(issued.AccessToken, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task JwkSetBuilder_PublishesPublicKey()
    {
        var (_, _, _, keys) = await Build();
        var builder = new JwkSetBuilder(keys, MsOptions.Create(new LyoJwtOptions()));
        var jwks = await builder.BuildAsync(TestContext.Current.CancellationToken);
        var keyList = (IEnumerable<Dictionary<string, object>>)jwks["keys"];
        using var enumerator = keyList.GetEnumerator();
        Assert.True(enumerator.MoveNext());
        var first = enumerator.Current;
        Assert.Equal("OKP", first["kty"]);
        Assert.Equal("Ed25519", first["crv"]);
        Assert.Equal("EdDSA", first["alg"]);
        Assert.StartsWith("lyo-sig:", (string)first["kid"]);
        Assert.NotNull(first["x"]);
    }

    private static async Task<(Ed25519LyoJwtIssuer, Ed25519LyoJwtValidator, InMemoryUserStore, IKeyStore)> Build()
    {
        var keys = new LocalKeyStore();
        keys.AddKey("lyo-sig", "v1", CryptographicRandom.GetBytes(32));
        keys.SetCurrentVersion("lyo-sig", "v1");
        var users = new InMemoryUserStore();
        var jwtOpts = MsOptions.Create(new LyoJwtOptions());
        var authOpts = MsOptions.Create(new AuthenticationOptions());
        var issuer = new Ed25519LyoJwtIssuer(keys, jwtOpts, NullLogger<Ed25519LyoJwtIssuer>.Instance);
        var validator = new Ed25519LyoJwtValidator(keys, users, jwtOpts, authOpts, NullLogger<Ed25519LyoJwtValidator>.Instance);
        await Task.CompletedTask;
        return (issuer, validator, users, keys);
    }

    private static LyoUser NewUser() => new(Guid.NewGuid(), "Test", $"u-{Guid.NewGuid():N}@example.com", true, null, null, [], null, null, DateTime.UtcNow, null, null, null, null);
}