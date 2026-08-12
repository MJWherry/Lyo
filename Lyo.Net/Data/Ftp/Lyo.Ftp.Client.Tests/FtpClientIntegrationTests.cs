namespace Lyo.Ftp.Client.Tests;

public class FtpClientIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task UploadDownloadAsync_RoundTrip()
    {
        var host = await FtpTestHost.TryStartAsync(TestContext.Current.CancellationToken);
        if (host is null) {
            Assert.Skip("Docker/Testcontainers unavailable");
            return;
        }

        await using var container = host.Value.Container;
        using var client = new FtpClient(host.Value.Options);
        await client.HealthPingAsync(TestContext.Current.CancellationToken);
        var remote = "roundtrip-async.txt";
        var payload = "hello-ftp-async"u8.ToArray();
        await client.UploadAsync(remote, payload, TestContext.Current.CancellationToken);
        Assert.True(await client.FileExistsAsync(remote, TestContext.Current.CancellationToken));
        Assert.Equal(payload, await client.DownloadBytesAsync(remote, TestContext.Current.CancellationToken));
        await client.DeleteFileAsync(remote, TestContext.Current.CancellationToken);
        Assert.False(await client.FileExistsAsync(remote, TestContext.Current.CancellationToken));
    }
}
