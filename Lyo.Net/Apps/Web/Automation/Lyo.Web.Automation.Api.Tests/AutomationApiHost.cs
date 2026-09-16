using System.Net.Http.Json;
using System.Text.Json;
using Lyo.Common.Core.Identifiers;
using Lyo.Json;
using Lyo.Web.Automation;
using Lyo.Web.Automation.Abstractions;
using Lyo.Web.Automation.Api;
using Lyo.Web.Automation.Models;
using Lyo.Web.Automation.Models.Enums;
using Lyo.Web.Automation.Plan;
using Lyo.Web.Automation.Playwright.Service;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Web.Automation.Api.Tests;

/// <summary>Test server for <c>BuildAutomationGroup</c> with a scripted runner, or the Playwright registration for the one browser test.</summary>
public sealed class AutomationApiHost : IAsyncDisposable
{
    public static readonly JsonSerializerOptions Json = LyoJsonSerializerOptions.Create();

    private readonly WebApplication _app;
    private readonly string _artifactRoot;

    public HttpClient Http { get; }

    private AutomationApiHost(WebApplication app, string artifactRoot, HttpClient http)
    {
        _app = app;
        _artifactRoot = artifactRoot;
        Http = http;
    }

    /// <summary>Host whose runner writes <paramref name="transcript" /> and optionally waits until cancelled.</summary>
    public static Task<AutomationApiHost> StartScriptedAsync(bool hold, string transcript = "RUN_STARTED plan=demo\n")
        => StartAsync(new ScriptedPlanRunner(hold, transcript), new TempSessionFactory());

    /// <summary>Host whose runner reports one step, then waits until cancelled. <paramref name="storage" /> serves that step's PNG.</summary>
    public static Task<AutomationApiHost> StartReportingAsync(IAutomationPlanFileStorage storage)
    {
        var root = NewRoot();
        var builder = NewBuilder(root);
        builder.Services.AddSingleton<IAutomationPlanRunner>(new ReportingPlanRunner());
        builder.Services.AddSingleton<IAutomationBrowserSessionFactory>(new TempSessionFactory());
        builder.Services.AddSingleton<IAutomationPlanFileStorage>(storage);
        return BootAsync(builder, root);
    }

    /// <summary>Host that launches headless Playwright and the real plan runner.</summary>
    public static Task<AutomationApiHost> StartPlaywrightAsync()
    {
        var root = NewRoot();
        var builder = NewBuilder(root);
        builder.Services.AddPlaywrightBrowserService(options => {
            options.Headless = true;
            options.ServiceRootDirectory = Path.Combine(root, "sessions");
            options.EnableMetrics = false;
            options.BrowserKind = AutomationBrowserKind.Chromium;
            options.NavigationTimeoutMs = 20_000;
            options.LocatorDefaultTimeoutMs = 20_000;
        });
        builder.Services.AddPlaywrightBrowserSessionFactory();
        builder.Services.AddWebAutomationPlanRunner();
        return BootAsync(builder, root);
    }

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync();
        await _app.DisposeAsync();
        if (Directory.Exists(_artifactRoot))
            Directory.Delete(_artifactRoot, true);
    }

    private static Task<AutomationApiHost> StartAsync(IAutomationPlanRunner runner, IAutomationBrowserSessionFactory factory)
    {
        var root = NewRoot();
        var builder = NewBuilder(root);
        builder.Services.AddSingleton(runner);
        builder.Services.AddSingleton(factory);
        return BootAsync(builder, root);
    }

    private static async Task<AutomationApiHost> BootAsync(WebApplicationBuilder builder, string root)
    {
        var app = builder.Build();
        app.BuildAutomationGroup();
        await app.StartAsync();
        return new AutomationApiHost(app, root, app.GetTestServer().CreateClient());
    }

    private static WebApplicationBuilder NewBuilder(string root)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.ConfigureHttpJsonOptions(options => LyoJsonSerializerOptions.ApplyTo(options.SerializerOptions));
        builder.Services.AddAutomationApi(options => {
            options.MaxConcurrentRuns = 1;
            options.ArtifactRoot = root;
            options.RunTtl = TimeSpan.FromMinutes(30);
        });
        return builder;
    }

    private static string NewRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "lyo-automation-api-tests", LyoGuid.CreateV7().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}

internal sealed class ReportingPlanRunner : IAutomationPlanRunner
{
    public Task RunAsync(IWebAutomationSession session, AutomationPlan plan, Microsoft.Extensions.Logging.ILogger? logger, CancellationToken ct)
        => RunWithResultAsync(session, plan, null, logger, ct);

    public async Task<AutomationPlanRunResult> RunWithResultAsync(
        IWebAutomationSession session,
        AutomationPlan plan,
        AutomationPlanRuntimeOptions? runtime,
        Microsoft.Extensions.Logging.ILogger? logger,
        CancellationToken ct)
    {
        var snapshot = new AutomationPlanContextSnapshot { Strings = new() { ["greeting"] = "hello" } };
        runtime?.Instrumentation?.OnStepOutcome(
            new AutomationPlanStepOutcomeRecord(
                Guid.Empty, Guid.Empty, Guid.Empty, 0, AutomationPlanStepOutcome.Success, TimeSpan.FromMilliseconds(12),
                new StoreLiteralAutomationStep("greeting", "hello"), null, "snap-1", snapshot));
        await Task.Delay(Timeout.Infinite, ct).ConfigureAwait(false);
        var strings = new Dictionary<string, string> { ["greeting"] = "hello" };
        var bindings = new AutomationPlanBindings(
            new Dictionary<string, IWebAutomationElement>(), new Dictionary<string, IReadOnlyList<IWebAutomationElement>>(), strings,
            new Dictionary<string, IReadOnlyList<string>>(), new Dictionary<string, object?>());
        return new AutomationPlanRunResult(new AutomationPlanExecutionSnapshot(strings, new Dictionary<string, IReadOnlyList<string>>()), new AutomationPlanExecutionContext(bindings, []));
    }
}

internal sealed class ScriptedPlanRunner(bool hold, string transcript) : IAutomationPlanRunner
{
    public Task RunAsync(IWebAutomationSession session, AutomationPlan plan, Microsoft.Extensions.Logging.ILogger? logger, CancellationToken ct)
        => RunWithResultAsync(session, plan, null, logger, ct);

    public async Task<AutomationPlanRunResult> RunWithResultAsync(
        IWebAutomationSession session,
        AutomationPlan plan,
        AutomationPlanRuntimeOptions? runtime,
        Microsoft.Extensions.Logging.ILogger? logger,
        CancellationToken ct)
    {
        if (session.SessionDirectory is { } directory) {
            var logs = Path.Combine(directory, "logs");
            Directory.CreateDirectory(logs);
            await File.WriteAllTextAsync(Path.Combine(logs, "run.log"), transcript, ct).ConfigureAwait(false);
        }

        if (hold)
            await Task.Delay(Timeout.Infinite, ct).ConfigureAwait(false);

        var strings = new Dictionary<string, string> { ["marker"] = "ok" };
        var lists = new Dictionary<string, IReadOnlyList<string>>();
        var bindings = new AutomationPlanBindings(
            new Dictionary<string, IWebAutomationElement>(),
            new Dictionary<string, IReadOnlyList<IWebAutomationElement>>(),
            strings,
            lists,
            new Dictionary<string, object?>());
        var context = new AutomationPlanExecutionContext(bindings, []);
        return new AutomationPlanRunResult(new AutomationPlanExecutionSnapshot(strings, lists), context);
    }
}

internal sealed class TempSessionFactory : IAutomationBrowserSessionFactory
{
    public IWebAutomationSession CreateSession(AutomationBrowserKind kind, bool headless)
    {
        var directory = Path.Combine(Path.GetTempPath(), "lyo-automation-api-tests", "session-" + LyoGuid.CreateV7().ToString("N"));
        Directory.CreateDirectory(directory);
        return new TempSession(directory);
    }

    private sealed class TempSession(string directory) : IWebAutomationSession
    {
        public Guid SessionId { get; } = LyoGuid.CreateV7();

        public string? SessionDirectory { get; } = directory;

        public IWebAutomationBrowser Browser => throw new NotSupportedException();

        public Task StartBrowserAsync(CancellationToken ct = default) => Task.CompletedTask;

        public void Dispose()
        {
            if (SessionDirectory != null && Directory.Exists(SessionDirectory))
                Directory.Delete(SessionDirectory, true);
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
