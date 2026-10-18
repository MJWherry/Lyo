using Lyo.Authentication.Sso.Completion;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Authentication.Saml.Tests;

public class SamlConfigurationTests
{
    [Fact]
    public void AddLyoSaml_FromConfiguration_BindsExternalLoginPolicy()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["LyoExternalLogin:Policy"] = "RequireExistingUser" }).Build();
        var services = new ServiceCollection();
        services.AddLyoSaml(configuration);
        using var provider = services.BuildServiceProvider();
        Assert.Equal(ExternalLoginPolicy.RequireExistingUser, provider.GetRequiredService<ExternalLoginOptions>().Policy);
    }
}
