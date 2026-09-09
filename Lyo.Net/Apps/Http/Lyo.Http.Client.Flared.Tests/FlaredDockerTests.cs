using DotNet.Testcontainers.Builders;
using Lyo.Testing.Containers;

namespace Lyo.Http.Client.Flared.Tests;

/// <summary>Optional Testcontainers FlareSolverr against a local fixture. Skipped unless Docker is available. Never hits live Cloudflare.</summary>
public sealed class FlaredDockerTests
{
    [Fact]
    public async Task FlareSolverrContainer_LocalEcho_NotLiveCloudflare()
    {
        if (string.Equals(Environment.GetEnvironmentVariable("NO_DOCKER"), "1", StringComparison.Ordinal))
            Assert.Skip("NO_DOCKER=1");

        var ct = TestContext.Current.CancellationToken;
        var container = await ContainerTestHost.TryStartAsync(
            "ghcr.io/flaresolverr/flaresolverr:latest",
            builder => builder
                .WithPortBinding(8191, true)
                .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(8191)),
            ct);
        if (container is null)
            Assert.Skip("Docker is not available.");

        try {
            var port = container.GetMappedPublicPort(8191);
            using var http = new HttpClient { BaseAddress = new($"http://127.0.0.1:{port}/"), Timeout = TimeSpan.FromSeconds(15) };
            using var response = await http.GetAsync("/", ct);
            Assert.True((int)response.StatusCode is >= 200 and < 500);
        }
        finally {
            await container.DisposeAsync();
        }
    }
}
