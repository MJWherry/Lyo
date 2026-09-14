using System.Text;
using Lyo.Common.Metadata.Records;
using Lyo.IO.Temp.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
namespace Lyo.IO.Temp.Tests;

public sealed class IOTempServiceTests : IDisposable
{
    private readonly IOTempService _service = new(TestOptions());

    public void Dispose() => _service.Dispose();

    private static IOTempServiceOptions TestOptions()
        => new() { TempRoot = Path.Combine(Path.GetTempPath(), "lyo-io-service-tests"), DirectoryName = Guid.NewGuid().ToString("N") };

    [Fact]
    public void AddIOTempService_Registers_Service()
    {
        var services = new ServiceCollection();
        services.AddIOTempService();
        using var sp = services.BuildServiceProvider();
        Assert.NotNull(sp.GetRequiredService<IIOTempService>());
    }

    [Fact]
    public void AddIOTempServiceWithAutoCleanup_Registers_HostedService()
    {
        var services = new ServiceCollection();
        services.AddIOTempServiceWithAutoCleanup(TimeSpan.FromHours(1), TimeSpan.FromMinutes(5));
        var provider = services.BuildServiceProvider();
        var hostedServices = provider.GetServices<IHostedService>().ToList();
        Assert.Contains(hostedServices, s => s is IOTempCleanupWorker);
        provider.Dispose();
    }

    [Fact]
    public void AddIOTempServiceWithAutoCleanup_WithConfigAction_RegistersServices()
    {
        var services = new ServiceCollection();
        services.AddIOTempServiceWithAutoCleanup(o => o.TempRoot = Path.Combine(Path.GetTempPath(), "lyo-di-test"));
        var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<IIOTempService>());
        provider.Dispose();
    }

    [Fact]
    public void Service_WithCustomRoot_CreatesDirectory()
    {
        var options = TestOptions();
        using var service = new IOTempService(options);
        Assert.True(Directory.Exists(service.ServiceDirectory));
        Assert.StartsWith(options.RootDirectory, service.ServiceDirectory);
    }

    [Fact]
    public void CreateFile_Creates_EmptyFile()
    {
        var path = _service.CreateFile();
        Assert.True(File.Exists(path));
        Assert.Equal(0, new FileInfo(path).Length);
    }

    [Fact]
    public void CreateFile_WithName_CreatesNamedFile()
    {
        var path = _service.CreateFile("report.pdf");
        Assert.True(File.Exists(path));
        Assert.EndsWith("report.pdf", path);
    }

    [Fact]
    public void CreateFile_WithData_WritesContent()
    {
        var data = Encoding.UTF8.GetBytes("Hello, World!");
        var path = _service.CreateFile(new ReadOnlyMemory<byte>(data));
        Assert.True(File.Exists(path));
        Assert.Equal("Hello, World!", File.ReadAllText(path));
    }

    [Fact]
    public void CreateFile_WithStream_WritesContent()
    {
        var data = Encoding.UTF8.GetBytes("Stream content");
        using var stream = new MemoryStream(data);
        var path = _service.CreateFile(stream);
        Assert.True(File.Exists(path));
        Assert.Equal("Stream content", File.ReadAllText(path));
    }

    [Fact]
    public void CreateDirectory_Creates_Directory()
    {
        var path = _service.CreateDirectory("subdir");
        Assert.True(Directory.Exists(path));
        Assert.EndsWith("subdir", path);
    }

    [Fact]
    public void SessionCreated_Fires_OnCreateSession()
    {
        string? raised = null;
        _service.SessionCreated += p => raised = p;
        using var session = _service.CreateSession();
        Assert.Equal(session.SessionDirectory, raised);
    }

    [Fact]
    public void SessionCreated_Fires_OnceOnGetOrCreateSession()
    {
        var raised = new List<string>();
        _service.SessionCreated += raised.Add;
        var s1 = _service.GetOrCreateSession("event-key");
        var s2 = _service.GetOrCreateSession("event-key");
        Assert.Same(s1, s2);
        Assert.Equal([s1.SessionDirectory], raised);
    }

    [Fact]
    public void SessionDisposed_Fires_OnSessionDispose()
    {
        string? raised = null;
        _service.SessionDisposed += p => raised = p;
        string dir;
        using (var session = _service.CreateSession())
            dir = session.SessionDirectory;

        Assert.Equal(dir, raised);
    }

    [Fact]
    public void FileCreated_Fires_OnOneOffCreateFile()
    {
        string? raised = null;
        _service.FileCreated += p => raised = p;
        var path = _service.CreateFile();
        Assert.Equal(path, raised);
    }

    [Fact]
    public void DirectoryCreated_Fires_OnOneOffCreateDirectory()
    {
        string? raised = null;
        _service.DirectoryCreated += p => raised = p;
        var path = _service.CreateDirectory();
        Assert.Equal(path, raised);
    }

    [Fact]
    public void FileDeleted_Fires_OnCleanup()
    {
        var path = _service.CreateFile();
        var deleted = new List<string>();
        _service.FileDeleted += deleted.Add;
        _service.Cleanup();
        Assert.Equal([path], deleted);
        Assert.False(File.Exists(path));
    }

    [Fact]
    public void DirectoryDeleted_Fires_OnCleanup()
    {
        var path = _service.CreateDirectory();
        var deleted = new List<string>();
        _service.DirectoryDeleted += deleted.Add;
        _service.Cleanup();
        Assert.Equal([path], deleted);
        Assert.False(Directory.Exists(path));
    }

    [Fact]
    public void CreateSession_Tracks_ActiveSessionCount()
    {
        Assert.Equal(0, _service.ActiveSessionCount);
        using (var session = _service.CreateSession()) {
            Assert.Equal(1, _service.ActiveSessionCount);
            Assert.True(Directory.Exists(session.SessionDirectory));
        }

        Assert.Equal(0, _service.ActiveSessionCount);
    }

    [Fact]
    public void GetOrCreateSession_Returns_SameSessionForSameKey()
    {
        var s1 = _service.GetOrCreateSession("key-a");
        var s2 = _service.GetOrCreateSession("key-a");
        Assert.Same(s1, s2);
    }

    [Fact]
    public void GetOrCreateSession_DifferentKeysReturnDifferent_Sessions()
    {
        var s1 = _service.GetOrCreateSession("key-a");
        var s2 = _service.GetOrCreateSession("key-b");
        Assert.NotSame(s1, s2);
    }

    [Fact]
    public void GetOrCreateSession_WithOptions_CreatesSession()
    {
        var sessionOptions = new IOTempSessionOptions { MaxFileSizeBytes = 1024 };
        using var session = _service.GetOrCreateSession("opts-key", sessionOptions);
        Assert.NotNull(session);
        Assert.True(Directory.Exists(session.SessionDirectory));
    }

    [Fact]
    public void GetOrCreateSession_WithOptions_ReturnsSameSessionOnSecondCall()
    {
        using var session1 = _service.GetOrCreateSession("same-key", new());
        using var session2 = _service.GetOrCreateSession("same-key", new());
        Assert.Same(session1, session2);
    }

    [Fact]
    public void ReleaseSession_DisposesAnd_RemovesKeyedSession()
    {
        var session = _service.GetOrCreateSession("to-release");
        var dir = session.SessionDirectory;
        _service.ReleaseSession("to-release");
        Assert.False(Directory.Exists(dir));
        var fresh = _service.GetOrCreateSession("to-release");
        Assert.NotSame(session, fresh);
    }

    [Fact]
    public void ReleaseSession_Noop_OnUnknownKey() => _service.ReleaseSession("does-not-exist");

    [Fact]
    public void GetStats_Reflects_ActiveSessionsAndBytes()
    {
        using var session = _service.CreateSession();
        session.Generator.CreateRandomFile(1024);
        var stats = _service.GetStats();
        Assert.Equal(1, stats.ActiveSessionCount);
        Assert.Equal(1024, stats.TotalBytesUsed);
        Assert.Equal(_service.ServiceDirectory, stats.ServiceDirectory);
    }

    [Fact]
    public void GetStats_KeyedSessionCount_ReflectsKeyedSessions()
    {
        _service.GetOrCreateSession("alpha");
        _service.GetOrCreateSession("beta");
        var stats = _service.GetStats();
        Assert.Equal(2, stats.KeyedSessionCount);
        Assert.Equal(2, stats.ActiveSessionCount);
    }

    [Fact]
    public void Dispose_Removes_ServiceDirectory()
    {
        var service = new IOTempService(TestOptions());
        var serviceDir = service.ServiceDirectory;
        Assert.True(Directory.Exists(serviceDir));
        service.Dispose();
        Assert.False(Directory.Exists(serviceDir));
    }

    [Fact]
    public void Disposed_Service_ThrowsOnCreateFile()
    {
        var service = new IOTempService(TestOptions());
        service.Dispose();
        Assert.Throws<ObjectDisposedException>(() => service.CreateFile());
    }

    [Fact]
    public async Task CleanupWorker_Starts_AndStopsWithoutError()
    {
        var options = new IOTempCleanupOptions { InitialDelay = TimeSpan.FromHours(24), Interval = TimeSpan.FromHours(24) };
        using var worker = new IOTempCleanupWorker(_service, options);
        await worker.StartAsync(TestContext.Current.CancellationToken);
        await worker.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task CleanupWorker_DefaultOptionsUsedWhenNone_Provided()
    {
        using var worker = new IOTempCleanupWorker(_service);
        await worker.StartAsync(TestContext.Current.CancellationToken);
        await worker.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public void WithMaxFileSize_Sets_ServiceOptionInBytes()
    {
        var opts = new IOTempServiceOptions().WithMaxFileSize(FileSizeUnitInfo.Megabyte, 50);
        Assert.Equal(FileSizeUnitInfo.Megabyte.ConvertToBytes(50), opts.MaxFileSizeBytes);
    }

    [Fact]
    public void WithMaxTotalSize_Sets_ServiceOptionInBytes()
    {
        var opts = new IOTempServiceOptions().WithMaxTotalSize(FileSizeUnitInfo.Gigabyte, 2);
        Assert.Equal(FileSizeUnitInfo.Gigabyte.ConvertToBytes(2), opts.MaxTotalSizeBytes);
    }

    [Fact]
    public void WithMaxFileSize_AndWithMaxTotalSizeChainOnService_Options()
    {
        var opts = new IOTempServiceOptions().WithMaxFileSize(FileSizeUnitInfo.Megabyte, 10).WithMaxTotalSize(FileSizeUnitInfo.Gigabyte, 1);
        Assert.Equal(FileSizeUnitInfo.Megabyte.ConvertToBytes(10), opts.MaxFileSizeBytes);
        Assert.Equal(FileSizeUnitInfo.Gigabyte.ConvertToBytes(1), opts.MaxTotalSizeBytes);
    }

    [Fact]
    public void WithMaxFileSize_OnSessionOptions_ReturnsIndependentCopy()
    {
        var original = new IOTempSessionOptions();
        var modified = original.WithMaxFileSize(FileSizeUnitInfo.Kilobyte, 512);
        Assert.NotSame(original, modified);
        Assert.Equal(FileSizeUnitInfo.Kilobyte.ConvertToBytes(512), modified.MaxFileSizeBytes);
    }

    [Fact]
    public void WithMaxFileCount_Sets_ServiceOption()
    {
        var opts = new IOTempServiceOptions().WithMaxFileCount(25);
        Assert.Equal(25, opts.MaxFileCount);
    }

    [Fact]
    public void WithFileLifetime_Sets_ServiceOption()
    {
        var lifetime = TimeSpan.FromHours(6);
        var opts = new IOTempServiceOptions().WithFileLifetime(lifetime);
        Assert.Equal(lifetime, opts.FileLifetime);
    }

    [Fact]
    public void WithMaxFileCount_OnSessionOptions_ReturnsIndependentCopy()
    {
        var original = new IOTempSessionOptions();
        var modified = original.WithMaxFileCount(10);
        Assert.NotSame(original, modified);
        Assert.Equal(10, modified.MaxFileCount);
        Assert.Null(original.MaxFileCount);
    }

    [Fact]
    public void WithFileLifetime_OnSessionOptions_ReturnsIndependentCopy()
    {
        var lifetime = TimeSpan.FromDays(1);
        var original = new IOTempSessionOptions();
        var modified = original.WithFileLifetime(lifetime);
        Assert.NotSame(original, modified);
        Assert.Equal(lifetime, modified.FileLifetime);
        Assert.Null(original.FileLifetime);
    }

    [Fact]
    public void MaxFileCount_ThrowStrategy_PreventsExceedingLimit()
    {
        var serviceOpts = new IOTempServiceOptions {
            TempRoot = Path.Combine(Path.GetTempPath(), "lyo-io-service-maxcount-tests"), DirectoryName = Guid.NewGuid().ToString("N"), MaxFileCount = 2
        };

        using var service = new IOTempService(serviceOpts);
        using var session = service.CreateSession();
        session.Generator.CreateRandomFile(1);
        session.Generator.CreateRandomFile(1);
        Assert.Throws<InvalidOperationException>(() => session.Generator.CreateRandomFile(1));
    }

    [Fact]
    public void MaxFileCount_Is_PropagatedToSessionFromService()
    {
        var serviceOpts = new IOTempServiceOptions {
            TempRoot = Path.Combine(Path.GetTempPath(), "lyo-io-service-maxcount-prop-tests"), DirectoryName = Guid.NewGuid().ToString("N"), MaxFileCount = 1
        };

        using var service = new IOTempService(serviceOpts);
        using var session = service.CreateSession();
        session.CreateFile("first");
        Assert.Throws<InvalidOperationException>(() => session.CreateFile("second"));
    }
}