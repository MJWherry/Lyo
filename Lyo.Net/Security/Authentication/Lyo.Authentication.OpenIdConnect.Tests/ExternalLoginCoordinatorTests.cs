using Lyo.Authentication.Models.Records;
using Lyo.Authentication.OpenIdConnect.Client;
using Lyo.Authentication.OpenIdConnect.Coordinator;
using Lyo.Authentication.OpenIdConnect.Discovery;
using Lyo.Authentication.OpenIdConnect.Pkce;
using Lyo.Authentication.OpenIdConnect.Provider;
using Lyo.Authentication.Services.Users;
using Lyo.Common.Extensions;
using Lyo.Keystore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Lyo.Authentication.OpenIdConnect.Tests;

public sealed class ExternalLoginCoordinatorTests
{
    [Fact]
    public async Task HandleCallbackAsync_JustInTime_ProvisionsLinkedUser_AndMintsJwt()
    {
        await using var fx = await CoordinatorFixture.CreateAsync(ExternalLoginPolicy.JustInTime);
        var (sealedState, nonce, state) = await fx.BuildRedirectAsync("fake");
        var code = fx.IssueCode("user-1", "alice@example.com", nonce);
        var result = await fx.Coordinator.HandleCallbackAsync("fake", code, sealedState, state, TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Equal("/home", result.ReturnUrl);
        Assert.False(result.Issued.AccessToken.IsNullOrEmpty());
        Assert.False(result.Issued.RefreshToken.IsNullOrEmpty());
        var user = await fx.UserStore.GetByEmailAsync("alice@example.com", null, TestContext.Current.CancellationToken);
        Assert.NotNull(user);
        var links = await fx.IdentityStore.ListForUserAsync(user.Id, null, TestContext.Current.CancellationToken);
        Assert.Single(links);
        Assert.Equal("user-1", links[0].Subject);
    }

    [Fact]
    public async Task HandleCallbackAsync_RequireExistingUser_RejectsWhenMissing()
    {
        await using var fx = await CoordinatorFixture.CreateAsync(ExternalLoginPolicy.RequireExistingUser);
        var (sealedState, nonce, state) = await fx.BuildRedirectAsync("fake");
        var code = fx.IssueCode("user-2", "bob@example.com", nonce);
        var ex = await Assert.ThrowsAsync<ExternalLoginRejectedException>(() => fx.Coordinator.HandleCallbackAsync(
            "fake", code, sealedState, state, TestContext.Current.CancellationToken));

        Assert.Equal("UserNotProvisioned", ex.Reason);
    }

    [Fact]
    public async Task HandleCallbackAsync_RequireExistingUser_LinksExistingLyoUser()
    {
        await using var fx = await CoordinatorFixture.CreateAsync(ExternalLoginPolicy.RequireExistingUser);
        var preExisting = new LyoUser(Guid.NewGuid(), "Carol", "carol@example.com", true, null, null, ["people.read"], null, null, DateTime.UtcNow, null, null, null, null);
        await fx.UserStore.CreateAsync(preExisting, null, TestContext.Current.CancellationToken);
        var (sealedState, nonce, state) = await fx.BuildRedirectAsync("fake");
        var code = fx.IssueCode("user-3", "carol@example.com", nonce);
        var result = await fx.Coordinator.HandleCallbackAsync("fake", code, sealedState, state, TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.False(result.Issued.AccessToken.IsNullOrEmpty());
        var links = await fx.IdentityStore.ListForUserAsync(preExisting.Id, null, TestContext.Current.CancellationToken);
        Assert.Single(links);
    }

    [Fact]
    public async Task HandleCallbackAsync_RejectsUnverifiedEmail()
    {
        await using var fx = await CoordinatorFixture.CreateAsync(ExternalLoginPolicy.JustInTime);
        var (sealedState, nonce, state) = await fx.BuildRedirectAsync("fake");
        var claims = CoordinatorFixture.IssueClaims("user-4", "dave@example.com", nonce);
        claims["email_verified"] = false;
        var code = fx.IssueCodeWithClaims(claims);
        var ex = await Assert.ThrowsAsync<ExternalLoginRejectedException>(() => fx.Coordinator.HandleCallbackAsync(
            "fake", code, sealedState, state, TestContext.Current.CancellationToken));

        Assert.Equal("EmailNotVerified", ex.Reason);
    }

    [Fact]
    public async Task HandleCallbackAsync_RejectsTamperedState()
    {
        await using var fx = await CoordinatorFixture.CreateAsync(ExternalLoginPolicy.JustInTime);
        var (sealedState, nonce, state) = await fx.BuildRedirectAsync("fake");
        var code = fx.IssueCode("user-5", "eve@example.com", nonce);
        var ex = await Assert.ThrowsAsync<ExternalLoginRejectedException>(() => fx.Coordinator.HandleCallbackAsync(
            "fake", code, sealedState[..^2] + "AA", state, TestContext.Current.CancellationToken));

        Assert.Equal("OidcStateInvalid", ex.Reason);
    }

    [Fact]
    public async Task HandleCallbackAsync_RejectsProviderMismatch()
    {
        await using var fx = await CoordinatorFixture.CreateAsync(ExternalLoginPolicy.JustInTime, secondProvider: true);
        var (sealedState, nonce, state) = await fx.BuildRedirectAsync("fake");
        var code = fx.IssueCode("user-6", "frank@example.com", nonce);
        var ex = await Assert.ThrowsAsync<ExternalLoginRejectedException>(() => fx.Coordinator.HandleCallbackAsync(
            "fake2", code, sealedState, state, TestContext.Current.CancellationToken));

        Assert.Equal("OidcStateInvalid", ex.Reason);
    }

    [Fact]
    public async Task HandleCallbackAsync_JitFromAllowedClaim_RejectsOutsideAllowedSet()
    {
        await using var fx = await CoordinatorFixture.CreateAsync(ExternalLoginPolicy.JitFromAllowedClaim, "hd", ["lyolabs.io"]);
        var (sealedState, nonce, state) = await fx.BuildRedirectAsync("fake");
        var claims = CoordinatorFixture.IssueClaims("user-7", "x@nope.io", nonce);
        claims["hd"] = "nope.io";
        var code = fx.IssueCodeWithClaims(claims);
        var ex = await Assert.ThrowsAsync<ExternalLoginRejectedException>(() => fx.Coordinator.HandleCallbackAsync(
            "fake", code, sealedState, state, TestContext.Current.CancellationToken));

        Assert.Equal("UserNotProvisioned", ex.Reason);
    }

    [Fact]
    public async Task HandleCallbackAsync_JitFromAllowedClaim_AcceptsWhenInsideAllowedSet()
    {
        await using var fx = await CoordinatorFixture.CreateAsync(ExternalLoginPolicy.JitFromAllowedClaim, "hd", ["lyolabs.io"]);
        var (sealedState, nonce, state) = await fx.BuildRedirectAsync("fake");
        var claims = CoordinatorFixture.IssueClaims("user-8", "ok@lyolabs.io", nonce);
        claims["hd"] = "lyolabs.io";
        var code = fx.IssueCodeWithClaims(claims);
        var result = await fx.Coordinator.HandleCallbackAsync("fake", code, sealedState, state, TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.False(result.Issued.AccessToken.IsNullOrEmpty());
    }

    [Fact]
    public async Task HandleCallbackAsync_RejectsCallbackStateNotMatchingCookie()
    {
        await using var fx = await CoordinatorFixture.CreateAsync(ExternalLoginPolicy.JustInTime);
        var (sealedState, nonce, _) = await fx.BuildRedirectAsync("fake");
        var code = fx.IssueCode("user-state-mismatch", "spoof@example.com", nonce);
        var ex = await Assert.ThrowsAsync<ExternalLoginRejectedException>(() => fx.Coordinator.HandleCallbackAsync(
            "fake", code, sealedState, "not-the-right-state", TestContext.Current.CancellationToken));

        Assert.Equal("OidcStateInvalid", ex.Reason);
    }

    [Fact]
    public async Task HandleCallbackAsync_RejectsDisabledUser()
    {
        await using var fx = await CoordinatorFixture.CreateAsync(ExternalLoginPolicy.JustInTime);
        var (sealedState1, nonce1, state1) = await fx.BuildRedirectAsync("fake");
        var code1 = fx.IssueCode("user-9", "gina@example.com", nonce1);
        await fx.Coordinator.HandleCallbackAsync("fake", code1, sealedState1, state1, TestContext.Current.CancellationToken);
        var user = await fx.UserStore.GetByEmailAsync("gina@example.com", null, TestContext.Current.CancellationToken);
        Assert.NotNull(user);
        await fx.UserStore.SetDisabledAsync(user.Id, DateTime.UtcNow, "compromised", null, TestContext.Current.CancellationToken);
        var (sealedState2, nonce2, state2) = await fx.BuildRedirectAsync("fake");
        var code2 = fx.IssueCode("user-9", "gina@example.com", nonce2);
        var ex = await Assert.ThrowsAsync<ExternalLoginRejectedException>(() => fx.Coordinator.HandleCallbackAsync(
            "fake", code2, sealedState2, state2, TestContext.Current.CancellationToken));

        Assert.Equal("UserDisabled", ex.Reason);
    }

    private sealed class CoordinatorFixture : IAsyncDisposable
    {
        private IHost _host = null!;
        private FakeOidcIdentityProvider _idp = null!;
        private StateNonceProtector _protector = null!;

        public IExternalLoginCoordinator Coordinator { get; private set; } = null!;

        public IUserStore UserStore { get; private set; } = null!;

        public IExternalIdentityStore IdentityStore { get; private set; } = null!;

        public async ValueTask DisposeAsync()
        {
            if (_host is not null) {
                await _host.StopAsync(TestContext.Current.CancellationToken);
                _host.Dispose();
            }
        }

        public async Task<(string sealedState, string nonce, string state)> BuildRedirectAsync(string providerName)
        {
            var redirect = await Coordinator.BuildLoginRedirectAsync(providerName, "/home", ct: TestContext.Current.CancellationToken);
            var unsealed = _protector.Unseal(redirect.SealedState) ?? throw new InvalidOperationException("could not unseal state in test");
            return (redirect.SealedState, unsealed.Nonce, unsealed.State);
        }

        public string IssueCode(string sub, string email, string nonce) => IssueCodeWithClaims(IssueClaims(sub, email, nonce));

        public string IssueCodeWithClaims(Dictionary<string, object?> claims) => _idp.IssueCode(claims);

        public static Dictionary<string, object?> IssueClaims(string subject, string email, string nonce)
            => new() {
                ["sub"] = subject,
                ["iss"] = FakeOidcIdentityProvider.Issuer,
                ["aud"] = "test-client",
                ["email"] = email,
                ["email_verified"] = true,
                ["name"] = email,
                ["exp"] = DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds(),
                ["iat"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                ["nonce"] = nonce
            };

        public static async Task<CoordinatorFixture> CreateAsync(
            ExternalLoginPolicy policy,
            string? allowedClaimName = null,
            IReadOnlyList<string>? allowedClaimValues = null,
            bool secondProvider = false)
        {
            var fx = new CoordinatorFixture { _idp = new() };
            var hostBuilder = Host.CreateApplicationBuilder();
            hostBuilder.Services.AddLogging();
            hostBuilder.Services.AddLocalKeyStore();
            hostBuilder.Services.AddLyoAuthentication();
            hostBuilder.Services.AddInMemoryAuthenticationStores();
            hostBuilder.Services.AddLyoOpenIdConnect();
            hostBuilder.Services.Configure<ExternalLoginOptions>(o => {
                o.Policy = policy;
                o.AllowedClaimName = allowedClaimName;
                if (allowedClaimValues is not null) {
                    foreach (var v in allowedClaimValues)
                        o.AllowedClaimValues.Add(v);
                }
            });

            hostBuilder.Services.AddSingleton<IOpenIdConnectProvider>(new FakeOidcProviderProfile());
            if (secondProvider)
                hostBuilder.Services.AddSingleton<IOpenIdConnectProvider>(new FakeOidcProviderProfile { Name = "fake2" });

            hostBuilder.Services.AddHttpClient<OidcDiscoveryCache>().ConfigurePrimaryHttpMessageHandler(() => fx._idp.CreateHandler());
            hostBuilder.Services.AddHttpClient<OidcJwksResolver>().ConfigurePrimaryHttpMessageHandler(() => fx._idp.CreateHandler());
            hostBuilder.Services.AddHttpClient<OidcTokenExchangeClient>().ConfigurePrimaryHttpMessageHandler(() => fx._idp.CreateHandler());
            fx._host = hostBuilder.Build();
            await fx._host.StartAsync(TestContext.Current.CancellationToken);
            fx.Coordinator = fx._host.Services.GetRequiredService<IExternalLoginCoordinator>();
            fx.UserStore = fx._host.Services.GetRequiredService<IUserStore>();
            fx.IdentityStore = fx._host.Services.GetRequiredService<IExternalIdentityStore>();
            fx._protector = fx._host.Services.GetRequiredService<StateNonceProtector>();
            return fx;
        }
    }
}