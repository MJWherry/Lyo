using Lyo.IO.Temp.Models;
using Lyo.Testing;
using Microsoft.Extensions.Logging;

namespace Lyo.FileSystemWatcher.Tests;

public class OptionsTests : IDisposable
{
    private readonly IIOTempSession _tempSession;

    public OptionsTests(ITestOutputHelper output)
    {
        var loggerFactory = LoggerFactory.Create(builder => {
            builder.AddProvider(new XunitLoggerProvider(output));
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        _tempSession = new IOTempSession(new(), loggerFactory.CreateLogger<IOTempSession>());
    }

    public void Dispose() => _tempSession.Dispose();

    [Fact]
    public void Options_DefaultValues_AreCorrect()
    {
        var options = new FileSystemWatcherOptions();
        Assert.False(options.IncludeSubdirectories);
        Assert.Equal(250, options.DebounceTimerDelay);
        Assert.True(options.EnableFileHashing);
        Assert.Equal(StringComparison.OrdinalIgnoreCase, options.PathComparison);
        Assert.False(options.EnableMetrics);
    }

    [Fact]
    public async Task Options_IncludeSubdirectories_Works()
    {
        var options = new FileSystemWatcherOptions { IncludeSubdirectories = true };
        var subDir = Path.Combine(_tempSession.SessionDirectory, "subdir");
        Directory.CreateDirectory(subDir);
        var fired = false;
        using var watcher = new FileSystemWatcher(_tempSession.SessionDirectory, options);
        watcher.FileCreated += (_, e) => {
            if (e.NewPath?.Contains("subdir") == true)
                fired = true;
        };

        await _tempSession.CreateFileAsync(new byte[100], Path.Combine("subdir", "test.txt"), TestContext.Current.CancellationToken).ConfigureAwait(false);
        await Task.Delay(500, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(fired);
    }

    [Fact]
    public void Options_DebounceTimerDelay_CanBeSet()
    {
        var options = new FileSystemWatcherOptions { DebounceTimerDelay = 500 };
        using var watcher = new FileSystemWatcher(_tempSession.SessionDirectory, options);
        Assert.Equal(500, watcher.DebounceTimerDelay);
    }

    [Fact]
    public void Options_DebounceTimerDelay_Negative_Throws()
    {
        var options = new FileSystemWatcherOptions { DebounceTimerDelay = -1 };
        Assert.Throws<ArgumentException>(() => new FileSystemWatcher(_tempSession.SessionDirectory, options));
    }

    [Fact]
    public async Task Options_EnableFileHashing_False_Works()
    {
        var options = new FileSystemWatcherOptions { EnableFileHashing = false };
        var fileName = Path.GetFileName(_tempSession.GetFilePath());
        var fired = false;
        using var watcher = new FileSystemWatcher(_tempSession.SessionDirectory, options);
        watcher.FileCreated += (_, e) => {
            if (e.NewPath?.EndsWith(fileName) == true)
                fired = true;
        };

        await _tempSession.CreateFileAsync(new byte[100], fileName, TestContext.Current.CancellationToken).ConfigureAwait(false);
        await Task.Delay(500, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(fired);
    }

    [Fact]
    public async Task Options_PathComparison_Ordinal_Works()
    {
        var options = new FileSystemWatcherOptions { PathComparison = StringComparison.Ordinal };
        var fileName = Path.GetFileName(_tempSession.GetFilePath());
        var fired = false;
        using var watcher = new FileSystemWatcher(_tempSession.SessionDirectory, options);
        watcher.FileCreated += (_, e) => {
            if (e.NewPath?.EndsWith(fileName) == true)
                fired = true;
        };

        await _tempSession.CreateFileAsync(new byte[100], fileName, TestContext.Current.CancellationToken).ConfigureAwait(false);
        await Task.Delay(500, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(fired);
    }

    [Fact]
    public void Constructor_WithOptions_NullPath_Throws()
    {
        var options = new FileSystemWatcherOptions();
        Assert.Throws<ArgumentNullException>(() => new FileSystemWatcher(null!, options));
    }

    [Fact]
    public void Constructor_WithOptions_EmptyPath_Throws()
    {
        var options = new FileSystemWatcherOptions();
        Assert.Throws<ArgumentException>(() => new FileSystemWatcher("", options));
    }

    [Fact]
    public void Constructor_WithOptions_NonExistentPath_Throws()
    {
        var options = new FileSystemWatcherOptions();
        var nonExistentPath = Path.Combine(_tempSession.SessionDirectory, "nonexistent");
        Assert.Throws<DirectoryNotFoundException>(() => new FileSystemWatcher(nonExistentPath, options));
    }

    [Fact]
    public void DebounceTimerDelay_Setter_ValidValue_Works()
    {
        using var watcher = new FileSystemWatcher(_tempSession.SessionDirectory);
        watcher.DebounceTimerDelay = 1000;
        Assert.Equal(1000, watcher.DebounceTimerDelay);
    }

    [Fact]
    public void DebounceTimerDelay_Setter_Negative_Throws()
    {
        using var watcher = new FileSystemWatcher(_tempSession.SessionDirectory);
        Assert.Throws<ArgumentException>(() => watcher.DebounceTimerDelay = -1);
    }
}