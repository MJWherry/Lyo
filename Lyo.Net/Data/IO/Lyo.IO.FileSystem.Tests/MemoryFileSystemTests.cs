using Lyo.Common.Core.Pathing;

namespace Lyo.IO.FileSystem.Tests;

public sealed class MemoryFileSystemTests : IFileSystemContractTests
{
    protected override ValueTask<IFileSystem?> CreateAsync() => new(new MemoryFileSystem());

    [Fact]
    public void PathStyle_IsPosix()
    {
        using var fs = new MemoryFileSystem();
        Assert.Equal(PathStyle.Posix, fs.PathStyle);
    }

    [Fact]
    public void RootPath_IsUniquePerInstance()
    {
        using var a = new MemoryFileSystem();
        using var b = new MemoryFileSystem();
        Assert.NotEqual(a.RootPath, b.RootPath);
    }

    [Fact]
    public async Task OpenAppend_ConcurrentWriters_PreserveAllBytes()
    {
        using var fs = new MemoryFileSystem();
        var path = PathHelpers.Combine(PathStyle.Posix, fs.RootPath, "concurrent-append.bin");
        fs.WriteAllBytes(path, []);
        const int writers = 2;
        const int perWriter = 1000;
        using var gate = new Barrier(writers);
        var tasks = new Task[writers];
        for (var i = 0; i < writers; i++) {
            var payload = (byte)(i + 1);
            tasks[i] = Task.Run(
                () => {
                    var bytes = new byte[perWriter];
                    Array.Fill(bytes, payload);
                    gate.SignalAndWait(TestCt);
                    using var stream = fs.OpenAppend(path);
                    stream.Write(bytes, 0, bytes.Length);
                }, TestCt);
        }

        await Task.WhenAll(tasks);
        Assert.Equal(writers * perWriter, fs.GetLength(path));
    }

    [Fact]
    public async Task CreateDirectory_ConcurrentCallers_ProduceOneNodePerSegment()
    {
        using var fs = new MemoryFileSystem();
        var target = PathHelpers.Combine(PathStyle.Posix, fs.RootPath, "a", "b", "c");
        const int callers = 8;
        using var gate = new Barrier(callers);
        var tasks = new Task[callers];
        for (var i = 0; i < callers; i++) {
            tasks[i] = Task.Run(
                () => {
                    gate.SignalAndWait(TestCt);
                    fs.CreateDirectory(target);
                }, TestCt);
        }

        await Task.WhenAll(tasks);
        Assert.True(fs.DirectoryExists(target));
        Assert.Single(fs.ListDirectory(PathHelpers.Combine(PathStyle.Posix, fs.RootPath, "a")));
        Assert.Single(fs.ListDirectory(PathHelpers.Combine(PathStyle.Posix, fs.RootPath, "a", "b")));
    }
}