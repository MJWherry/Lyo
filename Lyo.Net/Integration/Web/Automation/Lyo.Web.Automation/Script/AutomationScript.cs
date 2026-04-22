using System.Diagnostics;
using Lyo.Exceptions;
using Lyo.Web.Automation.Abstractions;
using Lyo.Web.Automation.Core;
using Microsoft.Extensions.Logging;

namespace Lyo.Web.Automation.Script;

/// <summary>Named step in a delegate-based automation script (see <see cref="AutomationScriptBuilder" />).</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record AutomationScriptStep(string Name, Func<IWebAutomationSession, CancellationToken, Task> Execute, AutomationScriptRetryPolicy? Retry = null)
{
    /// <inheritdoc />
    public override string ToString()
    {
        var retry = Retry != null ? $" retry={Retry}" : "";
        return $"AutomationScriptStep \"{AutomationDisplayText.Ellipsis(Name, 80)}\"{retry}";
    }
}

/// <summary>Optional per-step retry policy.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class AutomationScriptRetryPolicy
{
    public int MaxAttempts { get; init; } = 3;

    public TimeSpan DelayBetweenAttempts { get; init; } = TimeSpan.FromSeconds(1);

    /// <inheritdoc />
    public override string ToString()
        => $"AutomationScriptRetryPolicy maxAttempts={MaxAttempts}, delay={DelayBetweenAttempts}";
}

/// <summary>Fluent configuration for <see cref="AutomationScriptRetryPolicy" /> (similar to query builders).</summary>
public sealed class AutomationScriptRetryBuilder
{
    public int MaxAttempts { get; set; } = 3;

    public TimeSpan DelayBetweenAttempts { get; set; } = TimeSpan.FromSeconds(1);

    public AutomationScriptRetryPolicy Build()
        => new() { MaxAttempts = MaxAttempts, DelayBetweenAttempts = DelayBetweenAttempts };
}

/// <summary>Fluent builder for <see cref="AutomationScriptStep" /> sequences.</summary>
public sealed class AutomationScriptBuilder
{
    private readonly List<AutomationScriptStep> _steps = [];

    public AutomationScriptBuilder Step(string name, Func<IWebAutomationSession, CancellationToken, Task> execute)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentHelpers.ThrowIfNull(execute, nameof(execute));
        _steps.Add(new(name, execute));
        return this;
    }

    public AutomationScriptBuilder Step(string name, Func<IWebAutomationSession, CancellationToken, Task> execute, Action<AutomationScriptRetryBuilder> configureRetry)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentHelpers.ThrowIfNull(execute, nameof(execute));
        ArgumentHelpers.ThrowIfNull(configureRetry, nameof(configureRetry));
        var b = new AutomationScriptRetryBuilder();
        configureRetry(b);
        _steps.Add(new(name, execute, b.Build()));
        return this;
    }

    public IReadOnlyList<AutomationScriptStep> Build() => _steps;

    public static AutomationScriptBuilder New() => new();
}

/// <summary>Runs <see cref="AutomationScriptStep" /> sequences with structured logging scopes and optional retries (calls <see cref="IWebAutomationSession.StartBrowserAsync" /> first).</summary>
public static class AutomationScriptRunner
{
    /// <summary>Executes each step in order using the session's cancellation token.</summary>
    public static async Task RunAsync(
        IWebAutomationSession session,
        IReadOnlyList<AutomationScriptStep> steps,
        ILogger? logger,
        CancellationToken ct,
        string? scriptName = null)
    {
        ArgumentHelpers.ThrowIfNull(session, nameof(session));
        ArgumentHelpers.ThrowIfNull(steps, nameof(steps));
        using (logger?.BeginScope(
                   new Dictionary<string, object?> {
                       ["session_id"] = session.SessionId,
                       ["automation_script"] = scriptName
                   })) {
            await session.StartBrowserAsync(ct).ConfigureAwait(false);
            foreach (var step in steps) {
                ct.ThrowIfCancellationRequested();
                using (logger?.BeginScope(new Dictionary<string, object?> { ["step"] = step.Name }))
                    await RunStepAsync(session, step, logger, ct).ConfigureAwait(false);
            }
        }
    }

    private static async Task RunStepAsync(IWebAutomationSession session, AutomationScriptStep step, ILogger? logger, CancellationToken ct)
    {
        var max = Math.Max(1, step.Retry?.MaxAttempts ?? 1);
        var delay = step.Retry?.DelayBetweenAttempts ?? TimeSpan.Zero;
        for (var attempt = 1; attempt <= max; attempt++) {
            ct.ThrowIfCancellationRequested();
            try {
                await step.Execute(session, ct).ConfigureAwait(false);
                return;
            }
            catch (Exception ex) {
                if (attempt >= max)
                    throw;

                logger?.LogWarning(ex, "Step {Step} failed (attempt {Attempt}/{Max})", step.Name, attempt, max);
                if (delay > TimeSpan.Zero)
                    await Task.Delay(delay, ct).ConfigureAwait(false);
            }
        }
    }
}
