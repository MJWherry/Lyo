using System.Text;
using Lyo.IO.Temp.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.IO.Temp.Tests;

public sealed class IOTempTests
{
    private static string GetTestRoot() => Path.Combine(Path.GetTempPath(), "lyo-io-temp-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void Service_can_be_created()
    {
        var services = new ServiceCollection();
        services.AddIOTempService();
        using var sp = services.BuildServiceProvider();
        var service = sp.GetRequiredService<IIOTempService>();
        Assert.NotNull(service);
    }

    [Fact]
    public void Service_with_custom_root_creates_directory()
    {
        var root = GetTestRoot();
        try {
            var options = new IOTempServiceOptions { RootDirectory = root };
            using var service = new IOTempService(options);
            Assert.True(Directory.Exists(service.ServiceDirectory));
            Assert.StartsWith(root, service.ServiceDirectory);
        }
        finally {
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }

    [Fact]
    public void CreateFile_creates_empty_file()
    {
        var root = GetTestRoot();
        try {
            var options = new IOTempServiceOptions { RootDirectory = root };
            using var service = new IOTempService(options);
            var path = service.CreateFile();
            Assert.True(File.Exists(path));
            Assert.Equal(0, new FileInfo(path).Length);
        }
        finally {
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }

    [Fact]
    public void CreateFile_with_name_creates_named_file()
    {
        var root = GetTestRoot();
        try {
            var options = new IOTempServiceOptions { RootDirectory = root };
            using var service = new IOTempService(options);
            var path = service.CreateFile("report.pdf");
            Assert.True(File.Exists(path));
            Assert.EndsWith("report.pdf", path);
        }
        finally {
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }

    [Fact]
    public void CreateFile_with_data_writes_content()
    {
        var root = GetTestRoot();
        try {
            var options = new IOTempServiceOptions { RootDirectory = root };
            using var service = new IOTempService(options);
            var data = Encoding.UTF8.GetBytes("Hello, World!");
            var path = service.CreateFile(new ReadOnlyMemory<byte>(data));
            Assert.True(File.Exists(path));
            Assert.Equal("Hello, World!", File.ReadAllText(path));
        }
        finally {
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }

    [Fact]
    public void CreateFile_with_stream_writes_content()
    {
        var root = GetTestRoot();
        try {
            var options = new IOTempServiceOptions { RootDirectory = root };
            using var service = new IOTempService(options);
            var data = Encoding.UTF8.GetBytes("Stream content");
            using var stream = new MemoryStream(data);
            var path = service.CreateFile(stream);
            Assert.True(File.Exists(path));
            Assert.Equal("Stream content", File.ReadAllText(path));
        }
        finally {
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }

    [Fact]
    public void CreateDirectory_creates_directory()
    {
        var root = GetTestRoot();
        try {
            var options = new IOTempServiceOptions { RootDirectory = root };
            using var service = new IOTempService(options);
            var path = service.CreateDirectory("subdir");
            Assert.True(Directory.Exists(path));
            Assert.EndsWith("subdir", path);
        }
        finally {
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }

    [Fact]
    public void CreateSession_increments_ActiveSessionCount()
    {
        var root = GetTestRoot();
        try {
            var options = new IOTempServiceOptions { RootDirectory = root };
            using var service = new IOTempService(options);
            Assert.Equal(0, service.ActiveSessionCount);
            using (var session = service.CreateSession()) {
                Assert.Equal(1, service.ActiveSessionCount);
                Assert.True(Directory.Exists(session.SessionDirectory));
            }

            Assert.Equal(0, service.ActiveSessionCount);
        }
        finally {
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }

    [Fact]
    public void Session_Dispose_cleans_up_directory()
    {
        var root = GetTestRoot();
        try {
            var options = new IOTempServiceOptions { RootDirectory = root };
            using var service = new IOTempService(options);
            string sessionDir;
            using (var session = service.CreateSession()) {
                sessionDir = session.SessionDirectory;
                session.TouchFile("test.txt");
                Assert.True(Directory.Exists(sessionDir));
            }

            Assert.False(Directory.Exists(sessionDir));
        }
        finally {
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }

    [Fact]
    public void Session_GetFilePath_returns_path_without_creating_file()
    {
        var root = GetTestRoot();
        try {
            var options = new IOTempServiceOptions { RootDirectory = root };
            using var service = new IOTempService(options);
            using var session = service.CreateSession();
            var path = session.GetFilePath("planned.txt");
            Assert.False(File.Exists(path));
            Assert.EndsWith("planned.txt", path);
        }
        finally {
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }

    [Fact]
    public void Session_TouchFile_creates_empty_file()
    {
        var root = GetTestRoot();
        try {
            var options = new IOTempServiceOptions { RootDirectory = root };
            using var service = new IOTempService(options);
            using var session = service.CreateSession();
            var path = session.TouchFile("empty.tmp");
            Assert.True(File.Exists(path));
            Assert.Equal(0, new FileInfo(path).Length);
            Assert.Single(session.Files);
            Assert.Contains(path, session.Files);
        }
        finally {
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }

    [Fact]
    public void Session_CreateFile_text_writes_content()
    {
        var root = GetTestRoot();
        try {
            var options = new IOTempServiceOptions { RootDirectory = root };
            using var service = new IOTempService(options);
            using var session = service.CreateSession();
            var path = session.CreateFile("Hello from session");
            Assert.True(File.Exists(path));
            Assert.Equal("Hello from session", File.ReadAllText(path));
        }
        finally {
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }

    [Fact]
    public void Session_CreateFile_bytes_writes_content()
    {
        var root = GetTestRoot();
        try {
            var options = new IOTempServiceOptions { RootDirectory = root };
            using var service = new IOTempService(options);
            using var session = service.CreateSession();
            var data = "Binary data"u8.ToArray();
            var path = session.CreateFile(new ReadOnlyMemory<byte>(data), "data.bin");
            Assert.True(File.Exists(path));
            Assert.Equal("Binary data", Encoding.UTF8.GetString(File.ReadAllBytes(path)));
        }
        finally {
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }

    [Fact]
    public void Session_CreateDirectory_creates_directory()
    {
        var root = GetTestRoot();
        try {
            var options = new IOTempServiceOptions { RootDirectory = root };
            using var service = new IOTempService(options);
            using var session = service.CreateSession();
            var path = session.CreateDirectory("assets");
            Assert.True(Directory.Exists(path));
            Assert.Single(session.Directories);
            Assert.Contains(path, session.Directories);
        }
        finally {
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task Session_CreateFileAsync_writes_content()
    {
        var root = GetTestRoot();
        try {
            var options = new IOTempServiceOptions { RootDirectory = root };
            using var service = new IOTempService(options);
            await using var session = service.CreateSession();
            var path = await session.CreateFileAsync("Async content", TestContext.Current.CancellationToken).ConfigureAwait(false);
            Assert.True(File.Exists(path));
            Assert.Equal("Async content", await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken).ConfigureAwait(false));
        }
        finally {
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }

    [Fact]
    public void Service_Dispose_removes_service_directory()
    {
        var root = GetTestRoot();
        string? serviceDir = null;
        try {
            var options = new IOTempServiceOptions { RootDirectory = root };
            var service = new IOTempService(options);
            serviceDir = service.ServiceDirectory;
            Assert.True(Directory.Exists(serviceDir));
            service.Dispose();
            Assert.False(Directory.Exists(serviceDir));
        }
        finally {
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }

    [Fact]
    public void Disposed_service_throws_on_CreateFile()
    {
        var root = GetTestRoot();
        try {
            var options = new IOTempServiceOptions { RootDirectory = root };
            var service = new IOTempService(options);
            service.Dispose();
            Assert.Throws<ObjectDisposedException>(() => service.CreateFile());
        }
        finally {
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }

    [Fact]
    public void Disposed_session_throws_on_TouchFile()
    {
        var root = GetTestRoot();
        try {
            var options = new IOTempServiceOptions { RootDirectory = root };
            using var service = new IOTempService(options);
            var session = service.CreateSession();
            session.Dispose();
            Assert.Throws<ObjectDisposedException>(() => session.TouchFile());
        }
        finally {
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }
}