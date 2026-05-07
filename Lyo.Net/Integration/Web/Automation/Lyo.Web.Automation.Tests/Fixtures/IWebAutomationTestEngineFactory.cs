using Lyo.Web.Automation.Abstractions;

namespace Lyo.Web.Automation.Tests.Fixtures;

/// <summary>Creates automation sessions for a concrete engine (Selenium, Playwright, etc.) so shared contract tests can run against each implementation.</summary>
public interface IWebAutomationTestEngineFactory
{
    string EngineName { get; }

    Task<IWebAutomationSession> CreateSessionAsync(CancellationToken ct = default);
}