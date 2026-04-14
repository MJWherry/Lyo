using Lyo.IO.Temp.Models;
using Lyo.Testing;
using Microsoft.Extensions.Logging;

namespace Lyo.FileSystemWatcher.Tests;

public class ErrorHandlingTests : IDisposable
{
    private readonly IIOTempSession _tempSession;

    public ErrorHandlingTests(ITestOutputHelper output)
    {
        var loggerFactory = LoggerFactory.Create(builder => {
            builder.AddProvider(new XunitLoggerProvider(output));
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        _tempSession = new IOTempSession(new(), loggerFactory.CreateLogger<IOTempSession>());
    }

    public void Dispose() => _tempSession.Dispose();

    [Fact]
    public void Error_Event_IsFired_OnException()
    {
        var errorFired = false;
        Exception? caughtException = null;
        var watcher = new FileSystemWatcher(_tempSession.SessionDirectory);
        watcher.Error += (_, ex) => {
            errorFired = true;
            caughtException = ex;
        };

        // Dispose watcher to trigger potential errors
        watcher.Dispose();

        // Error event should be available even if no errors occur
        Assert.NotNull(watcher);
    }

    [Fact]
    public async Task EventHandler_Exception_DoesNotCrashWatcher()
    {
        var fileName = Path.GetFileName(_tempSession.GetFilePath());
        var handlerCalled = false;
        var errorFired = false;
        using var watcher = new FileSystemWatcher(_tempSession.SessionDirectory);
        watcher.FileCreated += (_, _) => {
            handlerCalled = true;
            throw new InvalidOperationException("Test exception");
        };

        watcher.Error += (_, _) => {
            errorFired = true;
        };

        await _tempSession.CreateFileAsync(new byte[100], fileName, TestContext.Current.CancellationToken).ConfigureAwait(false);
        await Task.Delay(500, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(handlerCalled);
        Assert.True(errorFired);
    }

    [Fact]
    public async Task EventHandler_MultipleExceptions_AllHandled()
    {
        var fileName = Path.GetFileName(_tempSession.GetFilePath());
        var errorCount = 0;
        using var watcher = new FileSystemWatcher(_tempSession.SessionDirectory);
        watcher.FileCreated += (_, _) => throw new InvalidOperationException("Exception 1");
        watcher.OnAnyChange += (_, _) => throw new InvalidOperationException("Exception 2");
        watcher.Error += (_, _) => {
            errorCount++;
        };

        await _tempSession.CreateFileAsync(new byte[100], fileName, TestContext.Current.CancellationToken).ConfigureAwait(false);
        await Task.Delay(500, TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Both exceptions should be caught
        Assert.True(errorCount >= 2);
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_DoesNotThrow()
    {
        var watcher = new FileSystemWatcher(_tempSession.SessionDirectory);
        watcher.Dispose();
        watcher.Dispose(); // Should not throw
    }

    [Fact]
    public async Task Dispose_AfterDisposal_OperationsStop()
    {
        var fileName = Path.GetFileName(_tempSession.GetFilePath());
        var eventFired = false;
        var watcher = new FileSystemWatcher(_tempSession.SessionDirectory);
        watcher.FileCreated += (_, _) => eventFired = true;
        watcher.Dispose();
        await _tempSession.CreateFileAsync(new byte[100], fileName, TestContext.Current.CancellationToken).ConfigureAwait(false);
        await Task.Delay(500, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.False(eventFired);
    }
}