using Lyo.Testing;
using Lyo.Web.Automation.Abstractions;
using Lyo.Web.Automation.PuppeteerSharp.Service;
using Lyo.Web.Automation.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Web.Automation.PuppeteerSharp.Tests;

public sealed class PuppeteerSharpWebAutomationTestEngineFixture : IWebAutomationTestEngineFactory, IDisposable
{
    private readonly string _root;
    private ILoggerFactory _loggerFactory;
    private ServiceProvider _provider;

    /// <summary>Current xUnit cancellation token. Reads <see cref="TestContext.Current" /> when accessed, so each test gets that test's token.</summary>
    public CancellationToken TestCt => TestContext.Current.CancellationToken;

    public PuppeteerSharpWebAutomationTestEngineFixture()
    {
        _root = Path.Combine(Path.GetTempPath(), "lyo-web-automation-tests", "puppeteersharp");
        Directory.CreateDirectory(_root);
        _loggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Warning));
        _provider = BuildProvider();
    }

    public void Dispose()
    {
        _provider.Dispose();
        _loggerFactory.Dispose();
    }

    public string EngineName => "PuppeteerSharp";

    public Task<IWebAutomationSession> CreateSessionAsync(CancellationToken ct = default)
    {
        var service = _provider.GetRequiredService<IPuppeteerSharpBrowserService>();
        IWebAutomationSession session = service.CreateSession();
        return Task.FromResult(session);
    }

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

    private ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_loggerFactory);
        services.AddPuppeteerSharpBrowserService(options => {
            options.Headless = true;
            options.ServiceRootDirectory = _root;
            options.EnableMetrics = false;
            options.NavigationTimeoutMs = 20_000;
            options.LocatorDefaultTimeoutMs = 20_000;
            options.ViewportWidth = 1280;
            options.ViewportHeight = 900;
        });

        return services.BuildServiceProvider();
    }
}