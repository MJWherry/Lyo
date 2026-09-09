using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Lyo.Exceptions;

namespace Lyo.Testing.Containers;

/// <summary>
/// Starts a raw <see cref="ContainerBuilder" /> container for suites that have no Testcontainers module. Returns <c>null</c> instead of throwing when Docker is missing so
/// integration tests can skip cleanly.
/// </summary>
public static class ContainerTestHost
{
    /// <summary>
    /// Builds and starts a container from <paramref name="configure" />. On failure the half-built container is disposed and the method returns <c>null</c>. When
    /// <paramref name="readyDelay" /> is set, the call waits that long after start for servers whose wait strategy fires before they accept connections.
    /// </summary>
    public static async Task<IContainer?> TryStartAsync(
        string image, Func<ContainerBuilder, ContainerBuilder> configure, CancellationToken cancellationToken, TimeSpan? readyDelay = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(image);
        ArgumentHelpers.ThrowIfNull(configure);

        var container = configure(new ContainerBuilder(image)).Build();
        try {
            await container.StartAsync(cancellationToken);
        }
        catch (Exception) {
            await container.DisposeAsync();
            return null;
        }

        if (readyDelay is { } delay)
            await Task.Delay(delay, cancellationToken);

        return container;
    }
}
