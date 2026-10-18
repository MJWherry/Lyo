using Lyo.Authentication;
using Lyo.Authentication.Saml;
using Lyo.Authentication.Sso.Completion;
using Lyo.Authentication.Sso.Protocol;
using Lyo.KeyStore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Authentication.Okta.Tests;

public class OktaSamlProfileTests
{
    [Fact]
    public void AddOktaProvider_RegistersOkta_AndMetadataUrl()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLocalKeyStore();
        services.AddLyoAuthentication();
        services.AddInMemoryAuthenticationStores();
        services.AddOktaProvider(options => Apply(options, "exkabc123"));
        using var provider = services.BuildServiceProvider();

        var okta = provider.GetServices<ISamlIdentityProvider>().Single(item => item.Name == "okta");
        Assert.Equal("https://lyo.okta.com/app/exkabc123/sso/saml/metadata", okta.MetadataUrl);
        var protocol = provider.GetRequiredService<SsoProtocolRegistry>().Get("okta");
        Assert.Equal("saml", protocol.Name);
        Assert.Equal(SsoCallbackMethod.Post, protocol.CallbackMethod);
    }

    [Fact]
    public void AddOktaProviderFromConfiguration_BindsExternalLoginPolicy()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["OktaAuth:Domain"] = "lyo.okta.com",
            ["OktaAuth:AppId"] = "exkabc123",
            ["OktaAuth:EntityId"] = "https://sp.example/saml",
            ["OktaAuth:AcsUrl"] = "https://sp.example/auth/callback/okta",
            ["LyoExternalLogin:Policy"] = "RequireExistingUser"
        }).Build();
        var services = new ServiceCollection();
        services.AddOktaProviderFromConfiguration(configuration);
        using var provider = services.BuildServiceProvider();
        Assert.Equal(ExternalLoginPolicy.RequireExistingUser, provider.GetRequiredService<ExternalLoginOptions>().Policy);
    }

    [Fact]
    public void AddOktaProvider_RejectsApplicationId()
    {
        var options = new OktaOptions();
        Apply(options, "0oaabc123");
        Assert.Throws<ArgumentException>(() => options.Validate());
    }

    [Fact]
    public void Validate_HttpAcsOnPublicHost_Rejects()
    {
        var options = new OktaOptions();
        Apply(options, "exkabc123");
        options.AcsUrl = "http://sp.example/acs";
        Assert.Throws<ArgumentException>(() => options.Validate());
    }

    [Fact]
    public void Validate_HttpAcsOnLocalhost_Allows()
    {
        var options = new OktaOptions();
        Apply(options, "exkabc123");
        options.AcsUrl = "http://localhost/acs";
        options.Validate();
    }

    [Fact]
    public void Validate_DomainWithScheme_Rejects()
    {
        var options = new OktaOptions();
        Apply(options, "exkabc123");
        options.Domain = "https://lyo.okta.com";
        Assert.Throws<ArgumentException>(() => options.Validate());
    }

    private static void Apply(OktaOptions options, string appId)
    {
        options.Domain = "lyo.okta.com";
        options.AppId = appId;
        options.EntityId = "https://sp.example/saml";
        options.AcsUrl = "https://sp.example/auth/callback/okta";
    }
}
