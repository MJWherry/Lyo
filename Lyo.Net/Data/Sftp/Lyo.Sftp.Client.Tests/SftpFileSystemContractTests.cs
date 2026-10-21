using DotNet.Testcontainers.Containers;
using Lyo.Common.Core.Identifiers;
using Lyo.IO.FileSystem;
using Lyo.IO.FileSystem.Tests;

namespace Lyo.Sftp.Client.Tests;

/// <summary>
/// Runs the shared <see cref="IFileSystem" /> contract against <see cref="SftpFileSystem" />. One container is shared by the whole class and each test gets its own remote
/// root, so the cases stay isolated without paying for a container apiece.
/// </summary>
[Trait("Category", "Integration")]
public sealed class SftpFileSystemContractTests(SftpContainerFixture fixture) : IFileSystemContractTests, IClassFixture<SftpContainerFixture>
{
    private SftpClient? _client;

    protected override async ValueTask<IFileSystem?> CreateAsync()
    {
        if (fixture.Options is not { } template)
            return null;

        _client = new(fixture.WithRoot(template, $"/upload/c-{LyoGuid.CreateV7():N}"));
        await _client.CreateDirectoryAsync(_client.RootRemoteDirectory, TestContext.Current.CancellationToken);
        return new SftpFileSystem(_client);
    }

    protected override ValueTask DisposeBackendAsync(IFileSystem fileSystem)
    {
        fileSystem.Dispose();
        _client?.Dispose();
        _client = null;
        return default;
    }
}

/// <summary>Starts one SFTP container for the contract suite and hands out per-test client options.</summary>
public sealed class SftpContainerFixture : IAsyncLifetime
{
    private IContainer? _container;

    /// <summary>Template options for the running container, or null when Docker is unavailable.</summary>
    public SftpClientOptions? Options { get; private set; }

    public async ValueTask InitializeAsync()
    {
        if (await SftpTestHost.TryStartAsync(TestContext.Current.CancellationToken) is not { } host)
            return;

        (_container, Options) = host;
    }

    public async ValueTask DisposeAsync()
    {
        if (_container is not null)
            await _container.DisposeAsync();
    }

    /// <summary>Copies <paramref name="template" /> with a different remote root so each test gets its own jail on the shared server.</summary>
    public SftpClientOptions WithRoot(SftpClientOptions template, string root)
        => new() {
            Host = template.Host,
            Port = template.Port,
            Username = template.Username,
            Password = template.Password,
            HostKeyPolicy = template.HostKeyPolicy,
            RootRemoteDirectory = root,
            MaxPooledClients = 2,
            EnableMetrics = false
        };
}