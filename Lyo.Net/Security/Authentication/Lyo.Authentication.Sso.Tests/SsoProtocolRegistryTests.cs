using Lyo.Authentication.Sso.Protocol;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;

namespace Lyo.Authentication.Sso.Tests;

public sealed class SsoProtocolRegistryTests
{
    [Fact]
    public void Get_TwoProtocols_RoutesToOwner()
    {
        var oidc = new NamedProtocol("oidc", SsoCallbackMethod.Get, "google");
        var saml = new NamedProtocol("saml", SsoCallbackMethod.Post, "okta");
        var registry = new SsoProtocolRegistry([oidc, saml]);
        Assert.Same(oidc, registry.Get("google"));
        Assert.Same(saml, registry.Get("okta"));
    }

    [Fact]
    public void Get_UnknownProvider_ThrowsNotFoundException()
    {
        var registry = new SsoProtocolRegistry([new NamedProtocol("oidc", SsoCallbackMethod.Get, "google")]);
        Assert.Throws<NotFoundException>(() => registry.Get("missing"));
    }

    [Fact]
    public void Register_DuplicateName_ThrowsConflictException()
    {
        var first = new NamedProtocol("oidc", SsoCallbackMethod.Get, "google");
        var second = new NamedProtocol("saml", SsoCallbackMethod.Post, "google");
        Assert.Throws<ConflictException>(() => new SsoProtocolRegistry([first, second]));
    }

    private sealed class NamedProtocol(string name, SsoCallbackMethod method, params string[] providers) : ISsoProtocol
    {
        public string Name => name;

        public SsoCallbackMethod CallbackMethod => method;

        public IReadOnlyCollection<string> ProviderNames => providers;

        public Task<SsoLoginStart> StartAsync(string providerName, string returnUrl, string mode, CancellationToken ct = default)
            => Task.FromResult(new SsoLoginStart("https://idp.example/start", "sealed"));

        public Task<ExternalLoginResult> CompleteAsync(SsoCallback callback, CancellationToken ct = default) => throw new NotSupportedException();
    }
}
