using Lyo.Web.Automation.Abstractions;
using Lyo.Web.Automation.Selenium.Configuration;
using Lyo.Web.Automation.Selenium.Service;
using Lyo.Web.Automation.Selenium.WebDriver;
using Lyo.Testing;
using Lyo.Web.Automation.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Lyo.Web.Automation.Selenium.Tests;

public sealed class SeleniumWebAutomationTestEngineFixture : IWebAutomationTestEngineFactory, IDisposable
{
    private readonly string _root;
    private ILoggerFactory _loggerFactory;
    private ServiceProvider _provider;

    public SeleniumWebAutomationTestEngineFixture()
    {
        _root = Path.Combine(Path.GetTempPath(), "lyo-web-automation-tests", "selenium");
        Directory.CreateDirectory(_root);
        _loggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Warning));
        _provider = BuildProvider();
    }

    public string EngineName => "Selenium";

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
        var service = _provider.GetRequiredService<ISeleniumBrowserService>();
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
        services.AddSeleniumBrowserService(options => {
            options.Headless = true;
            options.ServiceRootDirectory = _root;
            options.EnableMetrics = false;
            options.SeleniumMaxWaitSeconds = 10;
            options.ImplicitWaitSeconds = 2;
            options.PageLoadTimeoutSeconds = 20;
            options.BrowserWindowWidth = 1280;
            options.BrowserWindowHeight = 900;
            options.BrowserKind = SeleniumBrowserKind.Chrome;
        });
        return services.BuildServiceProvider();
    }
}
