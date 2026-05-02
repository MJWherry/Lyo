using System.Text;
using Lyo.Common.Records;
using Lyo.IO.Temp.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Lyo.IO.Temp.Tests;

public sealed class IOTempServiceTests : IDisposable
{
    private readonly IOTempService _service = new(TestOptions());

    public void Dispose() => _service.Dispose();

    private static IOTempServiceOptions TestOptions()
        => new() { TempRoot = Path.Combine(Path.GetTempPath(), "lyo-io-service-tests"), DirectoryName = Guid.NewGuid().ToString("N") };

    [Fact]
    public void AddIOTempService_registers_service()
    {
        var services = new ServiceCollection();
        services.AddIOTempService();
        using var sp = services.BuildServiceProvider();
        Assert.NotNull(sp.GetRequiredService<IIOTempService>());
    }

    [Fact]
    public void AddIOTempServiceWithAutoCleanup_registers_hosted_service()
    {
        var services = new ServiceCollection();
        services.AddIOTempServiceWithAutoCleanup(TimeSpan.FromHours(1), TimeSpan.FromMinutes(5));
        var provider = services.BuildServiceProvider();
        var hostedServices = provider.GetServices<IHostedService>().ToList();
        Assert.Contains(hostedServices, s => s is IOTempCleanupWorker);
        provider.Dispose();
    }

    [Fact]
    public void AddIOTempServiceWithAutoCleanup_with_config_action_registers_services()
    {
        var services = new ServiceCollection();
        services.AddIOTempServiceWithAutoCleanup(o => o.TempRoot = Path.Combine(Path.GetTempPath(), "lyo-di-test"));
        var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<IIOTempService>());
        provider.Dispose();
    }

    [Fact]
    public void Service_with_custom_root_creates_directory()
    {
        var options = TestOptions();
        using var service = new IOTempService(options);
        Assert.True(Directory.Exists(service.ServiceDirectory));
        Assert.StartsWith(options.RootDirectory, service.ServiceDirectory);
    }

    [Fact]
    public void CreateFile_creates_empty_file()
    {
        var path = _service.CreateFile();
        Assert.True(File.Exists(path));
        Assert.Equal(0, new FileInfo(path).Length);
    }

    [Fact]
    public void CreateFile_with_name_creates_named_file()
    {
        var path = _service.CreateFile("report.pdf");
        Assert.True(File.Exists(path));
        Assert.EndsWith("report.pdf", path);
    }

    [Fact]
    public void CreateFile_with_data_writes_content()
    {
        var data = Encoding.UTF8.GetBytes("Hello, World!");
        var path = _service.CreateFile(new ReadOnlyMemory<byte>(data));
        Assert.True(File.Exists(path));
        Assert.Equal("Hello, World!", File.ReadAllText(path));
    }

    [Fact]
    public void CreateFile_with_stream_writes_content()
    {
        var data = Encoding.UTF8.GetBytes("Stream content");
        using var stream = new MemoryStream(data);
        var path = _service.CreateFile(stream);
        Assert.True(File.Exists(path));
        Assert.Equal("Stream content", File.ReadAllText(path));
    }

    [Fact]
    public void CreateDirectory_creates_directory()
    {
        var path = _service.CreateDirectory("subdir");
        Assert.True(Directory.Exists(path));
        Assert.EndsWith("subdir", path);
    }

    [Fact]
    public void CreateSession_tracks_active_session_count()
    {
        Assert.Equal(0, _service.ActiveSessionCount);
        using (var session = _service.CreateSession()) {
            Assert.Equal(1, _service.ActiveSessionCount);
            Assert.True(Directory.Exists(session.SessionDirectory));
        }

        Assert.Equal(0, _service.ActiveSessionCount);
    }

    [Fact]
    public void GetOrCreateSession_returns_same_session_for_same_key()
    {
        var s1 = _service.GetOrCreateSession("key-a");
        var s2 = _service.GetOrCreateSession("key-a");
        Assert.Same(s1, s2);
    }

    [Fact]
    public void GetOrCreateSession_different_keys_return_different_sessions()
    {
        var s1 = _service.GetOrCreateSession("key-a");
        var s2 = _service.GetOrCreateSession("key-b");
        Assert.NotSame(s1, s2);
    }

    [Fact]
    public void GetOrCreateSession_with_options_creates_session()
    {
        var sessionOptions = new IOTempSessionOptions { MaxFileSizeBytes = 1024 };
        using var session = _service.GetOrCreateSession("opts-key", sessionOptions);
        Assert.NotNull(session);
        Assert.True(Directory.Exists(session.SessionDirectory));
    }

    [Fact]
    public void GetOrCreateSession_with_options_returns_same_session_on_second_call()
    {
        using var session1 = _service.GetOrCreateSession("same-key", new());
        using var session2 = _service.GetOrCreateSession("same-key", new());
        Assert.Same(session1, session2);
    }

    [Fact]
    public void ReleaseSession_disposes_and_removes_keyed_session()
    {
        var session = _service.GetOrCreateSession("to-release");
        var dir = session.SessionDirectory;
        _service.ReleaseSession("to-release");
        Assert.False(Directory.Exists(dir));
        var fresh = _service.GetOrCreateSession("to-release");
        Assert.NotSame(session, fresh);
    }

    [Fact]
    public void ReleaseSession_noop_on_unknown_key() => _service.ReleaseSession("does-not-exist");

    [Fact]
    public void GetStats_reflects_active_sessions_and_bytes()
    {
        using var session = _service.CreateSession();
        session.Generator.CreateRandomFile(1024);
        var stats = _service.GetStats();
        Assert.Equal(1, stats.ActiveSessionCount);
        Assert.Equal(1024, stats.TotalBytesUsed);
        Assert.Equal(_service.ServiceDirectory, stats.ServiceDirectory);
    }

    [Fact]
    public void GetStats_keyed_session_count_reflects_keyed_sessions()
    {
        _service.GetOrCreateSession("alpha");
        _service.GetOrCreateSession("beta");
        var stats = _service.GetStats();
        Assert.Equal(2, stats.KeyedSessionCount);
        Assert.Equal(2, stats.ActiveSessionCount);
    }

    [Fact]
    public void Dispose_removes_service_directory()
    {
        var service = new IOTempService(TestOptions());
        var serviceDir = service.ServiceDirectory;
        Assert.True(Directory.Exists(serviceDir));
        service.Dispose();
        Assert.False(Directory.Exists(serviceDir));
    }

    [Fact]
    public void Disposed_service_throws_on_CreateFile()
    {
        var service = new IOTempService(TestOptions());
        service.Dispose();
        Assert.Throws<ObjectDisposedException>(() => service.CreateFile());
    }

    [Fact]
    public async Task CleanupWorker_starts_and_stops_without_error()
    {
        var options = Options.Create(new IOTempCleanupOptions { InitialDelay = TimeSpan.FromHours(24), Interval = TimeSpan.FromHours(24) });
        using var worker = new IOTempCleanupWorker(_service, options);
        await worker.StartAsync(TestContext.Current.CancellationToken);
        await worker.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task CleanupWorker_default_options_used_when_none_provided()
    {
        using var worker = new IOTempCleanupWorker(_service);
        await worker.StartAsync(TestContext.Current.CancellationToken);
        await worker.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public void WithMaxFileSize_sets_service_option_in_bytes()
    {
        var opts = new IOTempServiceOptions().WithMaxFileSize(FileSizeUnitInfo.Megabyte, 50);
        Assert.Equal(FileSizeUnitInfo.Megabyte.ConvertToBytes(50), opts.MaxFileSizeBytes);
    }

    [Fact]
    public void WithMaxTotalSize_sets_service_option_in_bytes()
    {
        var opts = new IOTempServiceOptions().WithMaxTotalSize(FileSizeUnitInfo.Gigabyte, 2);
        Assert.Equal(FileSizeUnitInfo.Gigabyte.ConvertToBytes(2), opts.MaxTotalSizeBytes);
    }

    [Fact]
    public void WithMaxFileSize_and_WithMaxTotalSize_chain_on_service_options()
    {
        var opts = new IOTempServiceOptions().WithMaxFileSize(FileSizeUnitInfo.Megabyte, 10).WithMaxTotalSize(FileSizeUnitInfo.Gigabyte, 1);
        Assert.Equal(FileSizeUnitInfo.Megabyte.ConvertToBytes(10), opts.MaxFileSizeBytes);
        Assert.Equal(FileSizeUnitInfo.Gigabyte.ConvertToBytes(1), opts.MaxTotalSizeBytes);
    }

    [Fact]
    public void WithMaxFileSize_on_session_options_returns_independent_copy()
    {
        var original = new IOTempSessionOptions();
        var modified = original.WithMaxFileSize(FileSizeUnitInfo.Kilobyte, 512);
        Assert.NotSame(original, modified);
        Assert.Equal(FileSizeUnitInfo.Kilobyte.ConvertToBytes(512), modified.MaxFileSizeBytes);
    }

    [Fact]
    public void WithMaxFileCount_sets_service_option()
    {
        var opts = new IOTempServiceOptions().WithMaxFileCount(25);
        Assert.Equal(25, opts.MaxFileCount);
    }

    [Fact]
    public void WithFileLifetime_sets_service_option()
    {
        var lifetime = TimeSpan.FromHours(6);
        var opts = new IOTempServiceOptions().WithFileLifetime(lifetime);
        Assert.Equal(lifetime, opts.FileLifetime);
    }

    [Fact]
    public void WithMaxFileCount_on_session_options_returns_independent_copy()
    {
        var original = new IOTempSessionOptions();
        var modified = original.WithMaxFileCount(10);
        Assert.NotSame(original, modified);
        Assert.Equal(10, modified.MaxFileCount);
        Assert.Null(original.MaxFileCount);
    }

    [Fact]
    public void WithFileLifetime_on_session_options_returns_independent_copy()
    {
        var lifetime = TimeSpan.FromDays(1);
        var original = new IOTempSessionOptions();
        var modified = original.WithFileLifetime(lifetime);
        Assert.NotSame(original, modified);
        Assert.Equal(lifetime, modified.FileLifetime);
        Assert.Null(original.FileLifetime);
    }

    [Fact]
    public void MaxFileCount_throw_strategy_prevents_exceeding_limit()
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
    public void MaxFileCount_is_propagated_to_session_from_service()
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