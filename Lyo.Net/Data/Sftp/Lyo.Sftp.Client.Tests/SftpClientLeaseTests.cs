namespace Lyo.Sftp.Client.Tests;

public class SftpClientLeaseTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task CancelWhileWaitingForLease_ThrowsAndDoesNotLeakPermit()
    {
        var host = await SftpTestHost.TryStartAsync(TestContext.Current.CancellationToken, maxPooledClients: 1);
        if (host is null) {
            Assert.Skip("Docker/Testcontainers unavailable");
            return;
        }

        await using var container = host.Value.Container;
        using var client = new SftpClient(host.Value.Options);
        await client.HealthPingAsync(TestContext.Current.CancellationToken);
        await client.UploadAsync("held.txt", "x"u8.ToArray(), TestContext.Current.CancellationToken);

        var held = await client.OpenReadAsync("held.txt", TestContext.Current.CancellationToken);
        try {
            using var cts = new CancellationTokenSource();
            var blocked = client.ExistsAsync("held.txt", cts.Token);
            await Task.Delay(400, TestContext.Current.CancellationToken);
            Assert.False(blocked.IsCompleted);
            cts.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => blocked);
        }
        finally {
            await held.DisposeAsync();
        }

        Assert.True(await client.ExistsAsync("held.txt", TestContext.Current.CancellationToken));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ConcurrentAsyncOps_RespectMaxPooledClients()
    {
        var host = await SftpTestHost.TryStartAsync(TestContext.Current.CancellationToken, maxPooledClients: 2);
        if (host is null) {
            Assert.Skip("Docker/Testcontainers unavailable");
            return;
        }

        await using var container = host.Value.Container;
        using var client = new SftpClient(host.Value.Options);
        await client.HealthPingAsync(TestContext.Current.CancellationToken);
        await client.UploadAsync("a.txt", "a"u8.ToArray(), TestContext.Current.CancellationToken);
        await client.UploadAsync("b.txt", "b"u8.ToArray(), TestContext.Current.CancellationToken);

        await using var s1 = await client.OpenReadAsync("a.txt", TestContext.Current.CancellationToken);
        await using var s2 = await client.OpenReadAsync("b.txt", TestContext.Current.CancellationToken);
        var third = client.ExistsAsync("a.txt", TestContext.Current.CancellationToken);
        await Task.Delay(500, TestContext.Current.CancellationToken);
        Assert.False(third.IsCompleted);

        await s1.DisposeAsync();
        Assert.True(await third);
        await s2.DisposeAsync();
    }
}
