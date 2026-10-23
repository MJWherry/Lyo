using Lyo.Common.Core.Identifiers;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.IO.FileSystem.Tests;

public sealed class ExtensionsTests
{
    [Fact]
    public void AddLocalFileSystem_WhenOptionsAlreadyRegistered_UsesSuppliedRoot()
    {
        var wanted = TempRoot("wanted");
        var services = new ServiceCollection();
        services.AddSingleton(new LocalFileSystemOptions { RootPath = TempRoot("other") });
        services.AddLocalFileSystem(o => o.RootPath = wanted);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var fileSystem = scope.ServiceProvider.GetRequiredService<IFileSystem>();
        Assert.Equal(new LocalFileSystem(wanted).RootPath, fileSystem.RootPath);
    }

    [Fact]
    public void AddMemoryFileSystem_ResolvesMemoryBackend()
    {
        var services = new ServiceCollection();
        services.AddMemoryFileSystem();
        using var provider = services.BuildServiceProvider();
        Assert.IsType<MemoryFileSystem>(provider.GetRequiredService<IFileSystem>());
    }

    [Fact]
    public void AddFileTreeSource_ResolvesTreeSourceOverRegisteredFileSystem()
    {
        var services = new ServiceCollection();
        services.AddMemoryFileSystem();
        services.AddFileTreeSource();
        using var provider = services.BuildServiceProvider();
        Assert.IsType<FileSystemTreeSource>(provider.GetRequiredService<IFileTreeSource>());
    }

    private static string TempRoot(string label) => Path.Combine(Path.GetTempPath(), $"lyo-vfs-di-{label}-{LyoGuid.CreateV7():N}");
}