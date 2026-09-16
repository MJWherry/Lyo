using Lyo.Common.Core.Identifiers;
using Lyo.Common.Core.Pathing;
using Lyo.Exceptions.Models;

namespace Lyo.IO.FileSystem.Tests;

/// <summary>
/// Shared <see cref="IFileSystem" /> contract tests. Provider suites supply <see cref="CreateAsync" />. Every scenario runs twice, once over the blocking members and once
/// over the <c>*Async</c> members, driven by the <c>useAsync</c> theory parameter.
/// </summary>
public abstract class IFileSystemContractTests
{
    /// <summary>
    /// Builds a fresh file system for one test, or returns null when the backend needs infrastructure that is not available (for example Docker). Null skips the test.
    /// </summary>
    protected abstract ValueTask<IFileSystem?> CreateAsync();

    /// <summary>
    /// Releases the backend after one test. The default disposes it. Override when <see cref="CreateAsync" /> also allocated something the file system does not own, such
    /// as a remote client.
    /// </summary>
    protected virtual ValueTask DisposeBackendAsync(IFileSystem fileSystem)
    {
        fileSystem.Dispose();
        return default;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CreateDirectory_AndDirectoryExists_RoundTrip(bool useAsync)
    {
        await using var ctx = await ArrangeAsync(useAsync);
        var path = ctx.Combine("sub", "nested");
        await ctx.Api.CreateDirectoryAsync(path, TestCt);
        Assert.True(await ctx.Api.DirectoryExistsAsync(path, TestCt));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DirectoryExists_UnknownPath_ReturnsFalse(bool useAsync)
    {
        await using var ctx = await ArrangeAsync(useAsync);
        Assert.False(await ctx.Api.DirectoryExistsAsync(ctx.Combine("nope"), TestCt));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WriteAndRead_RoundTrip(bool useAsync)
    {
        await using var ctx = await ArrangeAsync(useAsync);
        var path = ctx.Combine("a.bin");
        var payload = "hello-vfs"u8.ToArray();
        await ctx.Api.WriteAllBytesAsync(path, payload, TestCt);
        Assert.True(await ctx.Api.FileExistsAsync(path, TestCt));
        Assert.Equal(payload.Length, await ctx.Api.GetLengthAsync(path, TestCt));
        Assert.Equal(payload, await ctx.Api.ReadAllBytesAsync(path, TestCt));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ListDirectory_ReturnsImmediateChildrenOnly(bool useAsync)
    {
        await using var ctx = await ArrangeAsync(useAsync);
        await ctx.Api.CreateDirectoryAsync(ctx.Combine("dir"), TestCt);
        await ctx.Api.WriteAllBytesAsync(ctx.Combine("dir", "file.txt"), "x"u8.ToArray(), TestCt);
        await ctx.Api.CreateDirectoryAsync(ctx.Combine("dir", "nested"), TestCt);
        await ctx.Api.WriteAllBytesAsync(ctx.Combine("dir", "nested", "deep.txt"), "y"u8.ToArray(), TestCt);
        var listed = await ctx.Api.ListDirectoryAsync(ctx.Combine("dir"), TestCt);
        Assert.Contains(listed, e => e is { IsDirectory: false, Name: "file.txt" });
        Assert.Contains(listed, e => e is { IsDirectory: true, Name: "nested" });
        Assert.DoesNotContain(listed, e => e.Name == "deep.txt");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ListDirectory_FilePath_ReturnsEmpty(bool useAsync)
    {
        await using var ctx = await ArrangeAsync(useAsync);
        var path = ctx.Combine("not-a-dir.bin");
        await ctx.Api.WriteAllBytesAsync(path, "x"u8.ToArray(), TestCt);
        Assert.Empty(await ctx.Api.ListDirectoryAsync(path, TestCt));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeleteFile_RemovesFile(bool useAsync)
    {
        await using var ctx = await ArrangeAsync(useAsync);
        var path = ctx.Combine("gone.txt");
        await ctx.Api.WriteAllBytesAsync(path, "z"u8.ToArray(), TestCt);
        await ctx.Api.DeleteFileAsync(path, TestCt);
        Assert.False(await ctx.Api.FileExistsAsync(path, TestCt));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeleteFile_MissingFile_IsNoOp(bool useAsync)
    {
        await using var ctx = await ArrangeAsync(useAsync);
        await ctx.Api.DeleteFileAsync(ctx.Combine("never-written.bin"), TestCt);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeleteDirectory_Recursive_RemovesTree(bool useAsync)
    {
        await using var ctx = await ArrangeAsync(useAsync);
        var dir = ctx.Combine("tree");
        await ctx.Api.CreateDirectoryAsync(dir, TestCt);
        await ctx.Api.WriteAllBytesAsync(ctx.Combine("tree", "child.bin"), "x"u8.ToArray(), TestCt);
        await ctx.Api.DeleteDirectoryAsync(dir, ct: TestCt);
        Assert.False(await ctx.Api.DirectoryExistsAsync(dir, TestCt));
        Assert.False(await ctx.Api.FileExistsAsync(ctx.Combine("tree", "child.bin"), TestCt));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeleteDirectory_NonRecursive_EmptySucceeds_NonEmptyThrows_MissingIsNoOp(bool useAsync)
    {
        await using var ctx = await ArrangeAsync(useAsync);
        var empty = ctx.Combine("empty");
        await ctx.Api.CreateDirectoryAsync(empty, TestCt);
        await ctx.Api.DeleteDirectoryAsync(empty, recursive: false, TestCt);
        Assert.False(await ctx.Api.DirectoryExistsAsync(empty, TestCt));

        var filled = ctx.Combine("filled");
        await ctx.Api.CreateDirectoryAsync(filled, TestCt);
        await ctx.Api.WriteAllBytesAsync(ctx.Combine("filled", "child.bin"), "x"u8.ToArray(), TestCt);
        await Assert.ThrowsAsync<IOException>(() => ctx.Api.DeleteDirectoryAsync(filled, recursive: false, TestCt));
        Assert.True(await ctx.Api.FileExistsAsync(ctx.Combine("filled", "child.bin"), TestCt));

        await ctx.Api.DeleteDirectoryAsync(ctx.Combine("missing"), recursive: false, TestCt);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeleteDirectory_OnFilePath_IsNoOp(bool useAsync)
    {
        await using var ctx = await ArrangeAsync(useAsync);
        var path = ctx.Combine("keep.bin");
        await ctx.Api.WriteAllBytesAsync(path, "z"u8.ToArray(), TestCt);
        await ctx.Api.DeleteDirectoryAsync(path, ct: TestCt);
        Assert.True(await ctx.Api.FileExistsAsync(path, TestCt));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CopyFile_AndMove_Work(bool useAsync)
    {
        await using var ctx = await ArrangeAsync(useAsync);
        var src = ctx.Combine("src.txt");
        var copied = ctx.Combine("copy.txt");
        var moved = ctx.Combine("moved.txt");
        await ctx.Api.WriteAllTextAsync(src, "payload", TestCt);
        await ctx.Api.CopyFileAsync(src, copied, ct: TestCt);
        Assert.True(await ctx.Api.FileExistsAsync(src, TestCt));
        Assert.True(await ctx.Api.FileExistsAsync(copied, TestCt));
        await ctx.Api.MoveAsync(copied, moved, ct: TestCt);
        Assert.False(await ctx.Api.FileExistsAsync(copied, TestCt));
        Assert.True(await ctx.Api.FileExistsAsync(moved, TestCt));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CopyFile_DestinationExists_OverwriteFalse_ThrowsIOException(bool useAsync)
    {
        await using var ctx = await ArrangeAsync(useAsync);
        var src = ctx.Combine("copy-src.bin");
        var dest = ctx.Combine("copy-dest.bin");
        await ctx.Api.WriteAllTextAsync(src, "source", TestCt);
        await ctx.Api.WriteAllTextAsync(dest, "original", TestCt);
        await Assert.ThrowsAsync<IOException>(() => ctx.Api.CopyFileAsync(src, dest, overwrite: false, TestCt));
        Assert.Equal("original"u8.ToArray(), await ctx.Api.ReadAllBytesAsync(dest, TestCt));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CopyFile_DestinationExists_OverwriteTrue_ReplacesContent(bool useAsync)
    {
        await using var ctx = await ArrangeAsync(useAsync);
        var src = ctx.Combine("ow-src.bin");
        var dest = ctx.Combine("ow-dest.bin");
        await ctx.Api.WriteAllTextAsync(src, "source", TestCt);
        await ctx.Api.WriteAllTextAsync(dest, "original", TestCt);
        await ctx.Api.CopyFileAsync(src, dest, overwrite: true, TestCt);
        Assert.Equal("source"u8.ToArray(), await ctx.Api.ReadAllBytesAsync(dest, TestCt));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Move_DestinationExists_OverwriteFalse_ThrowsIOException_AndSourceSurvives(bool useAsync)
    {
        await using var ctx = await ArrangeAsync(useAsync);
        var src = ctx.Combine("mv-src.bin");
        var dest = ctx.Combine("mv-dest.bin");
        await ctx.Api.WriteAllTextAsync(src, "source", TestCt);
        await ctx.Api.WriteAllTextAsync(dest, "original", TestCt);
        await Assert.ThrowsAsync<IOException>(() => ctx.Api.MoveAsync(src, dest, overwrite: false, TestCt));
        Assert.True(await ctx.Api.FileExistsAsync(src, TestCt));
        Assert.Equal("original"u8.ToArray(), await ctx.Api.ReadAllBytesAsync(dest, TestCt));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Move_DestinationExists_OverwriteTrue_ReplacesAndRemovesSource(bool useAsync)
    {
        await using var ctx = await ArrangeAsync(useAsync);
        var src = ctx.Combine("mvo-src.bin");
        var dest = ctx.Combine("mvo-dest.bin");
        await ctx.Api.WriteAllTextAsync(src, "source", TestCt);
        await ctx.Api.WriteAllTextAsync(dest, "original", TestCt);
        await ctx.Api.MoveAsync(src, dest, overwrite: true, TestCt);
        Assert.False(await ctx.Api.FileExistsAsync(src, TestCt));
        Assert.Equal("source"u8.ToArray(), await ctx.Api.ReadAllBytesAsync(dest, TestCt));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Move_Directory_MovesTree_OrThrowsNotSupported(bool useAsync)
    {
        await using var ctx = await ArrangeAsync(useAsync);
        var src = ctx.Combine("movable");
        var dest = ctx.Combine("moved-dir");
        await ctx.Api.CreateDirectoryAsync(src, TestCt);
        await ctx.Api.WriteAllTextAsync(ctx.Combine("movable", "inner.txt"), "inner", TestCt);

        if ((ctx.Api.Capabilities & FileSystemCapabilities.MoveDirectory) == 0) {
            await Assert.ThrowsAsync<NotSupportedException>(() => ctx.Api.MoveAsync(src, dest, ct: TestCt));
            return;
        }

        await ctx.Api.MoveAsync(src, dest, ct: TestCt);
        Assert.False(await ctx.Api.DirectoryExistsAsync(src, TestCt));
        Assert.True(await ctx.Api.DirectoryExistsAsync(dest, TestCt));
        Assert.Equal("inner"u8.ToArray(), await ctx.Api.ReadAllBytesAsync(ctx.Combine("moved-dir", "inner.txt"), TestCt));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OpenRead_MissingFile_ThrowsFileNotFoundException(bool useAsync)
    {
        await using var ctx = await ArrangeAsync(useAsync);
        await Assert.ThrowsAsync<FileNotFoundException>(() => ctx.Api.OpenReadAsync(ctx.Combine("absent.bin"), TestCt));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetLength_MissingFile_ThrowsFileNotFoundException(bool useAsync)
    {
        await using var ctx = await ArrangeAsync(useAsync);
        await Assert.ThrowsAsync<FileNotFoundException>(() => ctx.Api.GetLengthAsync(ctx.Combine("absent.bin"), TestCt));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetLength_DirectoryPath_ThrowsFileNotFoundException(bool useAsync)
    {
        await using var ctx = await ArrangeAsync(useAsync);
        var dir = ctx.Combine("a-directory");
        await ctx.Api.CreateDirectoryAsync(dir, TestCt);
        await Assert.ThrowsAsync<FileNotFoundException>(() => ctx.Api.GetLengthAsync(dir, TestCt));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Timestamps_MissingPath_Throw(bool useAsync)
    {
        await using var ctx = await ArrangeAsync(useAsync);
        var missing = ctx.Combine("nope.bin");
        await Assert.ThrowsAnyAsync<Exception>(() => ctx.Api.GetCreationTimeUtcAsync(missing, TestCt));
        await Assert.ThrowsAnyAsync<Exception>(() => ctx.Api.GetLastWriteTimeUtcAsync(missing, TestCt));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetEntry_ExistingFile_ReturnsLengthAndTimestamps(bool useAsync)
    {
        await using var ctx = await ArrangeAsync(useAsync);
        var path = ctx.Combine("entry.bin");
        var payload = "entry-payload"u8.ToArray();
        await ctx.Api.WriteAllBytesAsync(path, payload, TestCt);

        var entry = await ctx.Api.GetEntryAsync(path, TestCt);
        Assert.NotNull(entry);
        Assert.False(entry.IsDirectory);
        Assert.Equal("entry.bin", entry.Name);
        Assert.Equal(payload.Length, entry.Length);
        Assert.Equal(await ctx.Api.GetLastWriteTimeUtcAsync(path, TestCt), entry.LastWriteTimeUtc);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetEntry_MissingPath_ReturnsNull(bool useAsync)
    {
        await using var ctx = await ArrangeAsync(useAsync);
        Assert.Null(await ctx.Api.GetEntryAsync(ctx.Combine("no-entry.bin"), TestCt));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetEntry_Directory_ReturnsIsDirectoryTrue(bool useAsync)
    {
        await using var ctx = await ArrangeAsync(useAsync);
        var dir = ctx.Combine("entry-dir");
        await ctx.Api.CreateDirectoryAsync(dir, TestCt);
        var entry = await ctx.Api.GetEntryAsync(dir, TestCt);
        Assert.NotNull(entry);
        Assert.True(entry.IsDirectory);
        Assert.Equal("entry-dir", entry.Name);
    }

    [Fact]
    public async Task EnumerateDirectoryAsync_NonRecursive_YieldsSameSetAsListDirectory()
    {
        await using var ctx = await ArrangeAsync(useAsync: true);
        var dir = ctx.Combine("enum");
        await ctx.Api.CreateDirectoryAsync(dir, TestCt);
        await ctx.Api.WriteAllTextAsync(ctx.Combine("enum", "one.txt"), "1", TestCt);
        await ctx.Api.WriteAllTextAsync(ctx.Combine("enum", "two.txt"), "2", TestCt);
        await ctx.Api.CreateDirectoryAsync(ctx.Combine("enum", "sub"), TestCt);

        var listed = (await ctx.Api.ListDirectoryAsync(dir, TestCt)).Select(e => e.Path).OrderBy(p => p, StringComparer.Ordinal);
        var enumerated = new List<string>();
        await foreach (var entry in ctx.FileSystem.EnumerateDirectoryAsync(dir, recursive: false, TestCt))
            enumerated.Add(entry.Path);

        Assert.Equal(listed, enumerated.OrderBy(p => p, StringComparer.Ordinal));
    }

    [Fact]
    public async Task EnumerateDirectoryAsync_Recursive_YieldsDescendants()
    {
        await using var ctx = await ArrangeAsync(useAsync: true);
        var dir = ctx.Combine("deep");
        await ctx.Api.CreateDirectoryAsync(ctx.Combine("deep", "nested"), TestCt);
        await ctx.Api.WriteAllTextAsync(ctx.Combine("deep", "top.txt"), "t", TestCt);
        await ctx.Api.WriteAllTextAsync(ctx.Combine("deep", "nested", "deep.txt"), "d", TestCt);

        var names = new List<string>();
        await foreach (var entry in ctx.FileSystem.EnumerateDirectoryAsync(dir, recursive: true, TestCt))
            names.Add(entry.Name);

        Assert.Contains("top.txt", names);
        Assert.Contains("deep.txt", names);
        Assert.Contains("nested", names);
    }

    [Fact]
    public async Task EnumerateDirectoryAsync_Cancelled_ThrowsOperationCanceled()
    {
        await using var ctx = await ArrangeAsync(useAsync: true);
        var dir = ctx.Combine("cancel");
        await ctx.Api.CreateDirectoryAsync(dir, TestCt);
        for (var i = 0; i < 8; i++)
            await ctx.Api.WriteAllTextAsync(ctx.Combine("cancel", $"f{i}.txt"), "x", TestCt);

        using var cts = new CancellationTokenSource();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => {
                await foreach (var _ in ctx.FileSystem.EnumerateDirectoryAsync(dir, recursive: false, cts.Token))
                    await cts.CancelAsync();
            });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Capabilities_Append_MatchesOpenAppendBehaviour(bool useAsync)
    {
        await using var ctx = await ArrangeAsync(useAsync);
        var path = ctx.Combine("append.bin");
        await ctx.Api.WriteAllBytesAsync(path, "ab"u8.ToArray(), TestCt);

        if ((ctx.Api.Capabilities & FileSystemCapabilities.Append) == 0) {
            await Assert.ThrowsAsync<NotSupportedException>(() => ctx.Api.OpenAppendAsync(path, TestCt));
            return;
        }

        using (var append = await ctx.Api.OpenAppendAsync(path, TestCt))
            await append.WriteAsync("cd"u8.ToArray().AsMemory(), TestCt);

        Assert.Equal("abcd"u8.ToArray(), await ctx.Api.ReadAllBytesAsync(path, TestCt));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RelativePath_ResolvesAgainstRoot(bool useAsync)
    {
        await using var ctx = await ArrangeAsync(useAsync);
        await ctx.Api.WriteAllTextAsync("relative-sub/note.txt", "relative", TestCt);
        Assert.True(await ctx.Api.FileExistsAsync(ctx.Combine("relative-sub", "note.txt"), TestCt));
        Assert.Equal("relative"u8.ToArray(), await ctx.Api.ReadAllBytesAsync("relative-sub/note.txt", TestCt));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RelativePath_WithTraversal_ThrowsInvalidFormat(bool useAsync)
    {
        await using var ctx = await ArrangeAsync(useAsync);
        await Assert.ThrowsAsync<InvalidFormatException>(() => ctx.Api.FileExistsAsync("../escape.bin", TestCt));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Jail_PathEscapesRoot_Throws(bool useAsync)
    {
        await using var ctx = await ArrangeAsync(useAsync);
        var outside = ctx.FileSystem.PathStyle == PathStyle.Host
            ? Path.Combine(Path.GetTempPath(), "lyo-vfs-outside-" + LyoGuid.CreateV7().ToString("N"))
            : "/outside-" + LyoGuid.CreateV7().ToString("N");
        await Assert.ThrowsAsync<InvalidFormatException>(() => ctx.Api.DirectoryExistsAsync(outside, TestCt));
    }

    [Fact]
    public async Task Watch_WithoutCapability_ReturnsNull()
    {
        await using var ctx = await ArrangeAsync(useAsync: false);
        if ((ctx.Api.Capabilities & FileSystemCapabilities.Watch) != 0)
            return;

        Assert.Null(ctx.FileSystem.Watch(ctx.RootPath));
    }

    /// <summary>Creates the backend and wraps it. Skips the test when <see cref="CreateAsync" /> reports the backend is unavailable.</summary>
    private async ValueTask<ContractContext> ArrangeAsync(bool useAsync)
    {
        var fileSystem = await CreateAsync();
        if (fileSystem is null)
            Assert.Skip("Backend unavailable in this environment.");

        return new(fileSystem, useAsync, DisposeBackendAsync);
    }

    /// <summary>One backend instance plus the sync/async dispatcher for a single test.</summary>
    protected sealed class ContractContext : IAsyncDisposable
    {
        private readonly Func<IFileSystem, ValueTask> _release;

        /// <summary>Wraps <paramref name="fileSystem" /> and picks which half of the interface <see cref="Api" /> calls.</summary>
        public ContractContext(IFileSystem fileSystem, bool useAsync, Func<IFileSystem, ValueTask> release)
        {
            FileSystem = fileSystem;
            Api = new(fileSystem, useAsync);
            _release = release;
        }

        /// <summary>Backend under test.</summary>
        public IFileSystem FileSystem { get; }

        /// <summary>Sync/async dispatcher over <see cref="FileSystem" />.</summary>
        public FileSystemApi Api { get; }

        /// <summary>Root jail of the backend.</summary>
        public string RootPath => FileSystem.RootPath;

        /// <summary>Builds an absolute path under the backend root from <paramref name="parts" />.</summary>
        public string Combine(params string[] parts)
        {
            var segments = new string[parts.Length + 1];
            segments[0] = FileSystem.RootPath;
            Array.Copy(parts, 0, segments, 1, parts.Length);
            return PathHelpers.Combine(FileSystem.PathStyle, segments);
        }

        /// <inheritdoc />
        public ValueTask DisposeAsync() => _release(FileSystem);
    }
}
