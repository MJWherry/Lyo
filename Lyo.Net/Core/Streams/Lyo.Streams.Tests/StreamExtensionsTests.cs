using System.Text;

namespace Lyo.Streams.Tests;

public sealed class StreamExtensionsTests
{
    [Fact]
    public async Task CopyToAsync_copies_content()
    {
        var src = new MemoryStream("copy me"u8.ToArray());
        var dest = new MemoryStream();
        await src.CopyToAsync(dest, TestContext.Current.CancellationToken);
        Assert.Equal("copy me", Encoding.UTF8.GetString(dest.ToArray()));
    }

    [Fact]
    public async Task CopyToAsync_with_progress_copies_content()
    {
        var src = new MemoryStream("progress"u8.ToArray());
        var dest = new MemoryStream();
        var progress = new Progress<long>(_ => { });
        await src.CopyToAsync(dest, progress: progress, ct: TestContext.Current.CancellationToken);
        Assert.Equal("progress", Encoding.UTF8.GetString(dest.ToArray()));
    }

    [Fact]
    public async Task CopyToAsync_throws_on_null_destination()
    {
        var src = new MemoryStream();
        await Assert.ThrowsAsync<ArgumentNullException>(() => src.CopyToAsync(null!, TestContext.Current.CancellationToken));
    }
}