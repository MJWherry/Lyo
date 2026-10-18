using Lyo.Authentication;
using Lyo.Authentication.Saml;
using Lyo.Authentication.Saml.Tests;
using Lyo.Authentication.Sso.Protocol;
using Lyo.KeyStore;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Authentication.Entra.Tests;

public class EntraSamlProfileTests
{
    [Fact]
    public void AddEntraProvider_RegistersEntra_AndFederationMetadataUrl()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLocalKeyStore();
        services.AddLyoAuthentication();
        services.AddInMemoryAuthenticationStores();
        services.AddEntraProvider(Apply);
        using var provider = services.BuildServiceProvider();

        var entra = provider.GetServices<ISamlIdentityProvider>().Single(item => item.Name == "entra");
        Assert.Contains("?appid=app-1", entra.MetadataUrl, StringComparison.Ordinal);
        Assert.Equal("https://login.microsoftonline.com/tenant-1/federationmetadata/2007-06/federationmetadata.xml?appid=app-1", entra.MetadataUrl);
        var protocol = provider.GetRequiredService<SsoProtocolRegistry>().Get("entra");
        Assert.Equal("saml", protocol.Name);
    }

    [Fact]
    public void Entra_Subject_UsesObjectId()
    {
        var options = new EntraOptions();
        Apply(options);
        var spec = new SamlResponseSpec
        {
            NameId = "user@contoso.com",
            Issuer = options.ExpectedIssuer,
            Audience = options.EntityId!,
            Destination = options.AcsUrl!,
            Recipient = options.AcsUrl!,
            Now = DateTime.UtcNow
        };
        spec.Attributes.Add((EntraOptions.ObjectIdAttribute, ["11111111-1111-1111-1111-111111111111"]));
        var metadata = new SamlIdpMetadata(options.ExpectedIssuer, "https://login.microsoftonline.com/sso", [SamlTestCertificate.Signing.RawData]);
        var ticket = new SamlLoginTicket("ticket", "entra", spec.RequestId, "/done", "browser", spec.Now.AddMinutes(10));
        var assertion = new SamlResponseValidator().Validate(spec.ToBase64(), ticket, new EntraSamlProvider(options), metadata, spec.Now, new InMemorySamlAssertionReplayCache());

        Assert.Equal("11111111-1111-1111-1111-111111111111", assertion.Subject);
        Assert.EndsWith("/", options.ExpectedIssuer);
    }

    [Fact]
    public void Validate_HttpAcsOnPublicHost_Rejects()
    {
        var options = new EntraOptions();
        Apply(options);
        options.AcsUrl = "http://sp.example/acs";
        Assert.Throws<ArgumentException>(() => options.Validate());
    }

    private static void Apply(EntraOptions options)
    {
        options.TenantId = "tenant-1";
        options.ApplicationId = "app-1";
        options.EntityId = "https://sp.example/saml";
        options.AcsUrl = "https://sp.example/auth/callback/entra";
    }
}
