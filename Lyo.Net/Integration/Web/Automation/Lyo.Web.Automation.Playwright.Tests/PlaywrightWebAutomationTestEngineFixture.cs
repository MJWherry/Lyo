using Lyo.Web.Automation.Abstractions;
using Lyo.Web.Automation.Playwright.Configuration;
using Lyo.Web.Automation.Playwright.Service;
using Lyo.Testing;
using Lyo.Web.Automation.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Lyo.Web.Automation.Playwright.Tests;

public sealed class PlaywrightWebAutomationTestEngineFixture : IWebAutomationTestEngineFactory, IDisposable
{
    private readonly string _root;
    private ILoggerFactory _loggerFactory;
    private ServiceProvider _provider;

    public PlaywrightWebAutomationTestEngineFixture()
    {
        _root = Path.Combine(Path.GetTempPath(), "lyo-web-automation-tests", "playwright");
        Directory.CreateDirectory(_root);
        _loggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Warning));
        _provider = BuildProvider();
    }

    public string EngineName => "Playwright";

    public void UseTestOutputLogger(ITestOutputHelper output)
    {
        _loggerFactory.Dispose();
        _loggerFactory = LoggerFactory.Create(builder => {
            builder.AddProvider(new XunitLoggerProvider(output));
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        _provider.Dispose();
        _provider = BuildProvider();
    }

    public Task<IWebAutomationSession> CreateSessionAsync(CancellationToken ct = default)
    {
        var service = _provider.GetRequiredService<IPlaywrightBrowserService>();
        IWebAutomationSession session = service.CreateSession();
        return Task.FromResult(session);
    }

    public void Dispose()
    {
        _provider.Dispose();
        _loggerFactory.Dispose();
    }

    private ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_loggerFactory);
        services.AddPlaywrightBrowserService(options => {
            options.Headless = true;
            options.ServiceRootDirectory = _root;
            options.EnableMetrics = false;
            options.BrowserKind = PlaywrightBrowserKind.Chromium;
            options.NavigationTimeoutMs = 20_000;
            options.LocatorDefaultTimeoutMs = 20_000;
            options.ViewportWidth = 1280;
            options.ViewportHeight = 900;
        });

        return services.BuildServiceProvider();
    }
}
