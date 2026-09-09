using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Endato.Client.Tests;

public sealed class EndatoClientDiTests
{
    [Fact]
    public void AddEndatoClient_ResolvesTypedClient()
    {
        var services = new ServiceCollection();
        services.AddEndatoClient(new EndatoClientOptions {
            BaseUrl = "https://example.test/",
            ApName = "name",
            ApPassword = "secret"
        });
        using var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetRequiredService<EndatoClient>());
    }
}
