using System.Collections.ObjectModel;
using System.Diagnostics;
using Lyo.Exceptions;
using Lyo.Web.Automation.Abstractions;
using Lyo.Web.Automation.Core;
using Lyo.Web.Automation.Models;
using Lyo.Web.Automation.Models.Enums;
using Microsoft.Extensions.Logging;

namespace Lyo.Web.Automation.Plan;

/// <summary>
/// Executes a serializable <see cref="AutomationPlan" /> using <see cref="IWebAutomationSession.Browser" /> (calls <see cref="IWebAutomationSession.StartBrowserAsync" />
/// first).
/// </summary>
public static class AutomationPlanRunner
{
    /// <summary>Runs each step in order; element and list refs are scoped to this run.</summary>
    public static async Task RunAsync(IWebAutomationSession session, AutomationPlan plan, ILogger? logger, CancellationToken ct)
        => await RunWithResultAsync(session, plan, null, logger, ct).ConfigureAwait(false);

    /// <summary>Runs the plan and returns final variables plus per-step <see cref="AutomationPlanExecutionContext" />.</summary>
    public static async Task<AutomationPlanRunResult> RunWithResultAsync(
        IWebAutomationSession session,
        AutomationPlan plan,
        AutomationPlanRuntimeOptions? runtime,
        ILogger? logger,
        CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfNull(session);
        ArgumentHelpers.ThrowIfNull(plan);
        ArgumentHelpers.ThrowIf(plan.Steps == null, "AutomationPlan.Steps cannot be null.", nameof(plan));
        var runId = AutomationGuid.CreateTimeOrdered();
        var state = new PlanExecutionState();
        var options = runtime ?? new AutomationPlanRuntimeOptions();
        var hooks = options.Hooks;
        var instr = options.Instrumentation;
        var dirOpts = options.PlanRunDirectory ?? (session.SessionDirectory is { } sessionDir
            ? new AutomationPlanRunDirectoryOptions { RootDirectory = sessionDir, NestRunUnderRoot = false }
            : null);

        AutomationPlanRunArtifacts? artifacts = null;
        if (dirOpts != null)
            artifacts = AutomationPlanRunArtifacts.TryCreate(dirOpts, runId);

        var swTotal = Stopwatch.StartNew();
        instr?.OnRunStarted(new(runId, plan));
        logger?.LogInformation("Automation plan run started {AutomationRunId} plan={AutomationPlanName} steps={StepCount}", runId, plan.Name, plan.Steps.Count);
        if (artifacts != null) {
            logger?.LogInformation("Automation plan run directory {AutomationPlanRunRoot} session={SessionId}", artifacts.RunRoot, session.SessionId);
            artifacts.LogLine($"RUN_STARTED plan={plan.Name} steps={plan.Steps.Count} session={session.SessionId} runRoot={artifacts.RunRoot}");
        }

        using (logger?.BeginScope(
            new Dictionary<string, object?> {
                ["automation_run_id"] = runId,
                ["plan_run_id"] = runId,
                ["session_id"] = session.SessionId,
                ["automation_plan"] = plan.Name,
                ["automation_plan_run_root"] = artifacts?.RunRoot
            })) {
            try {
                using var planCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                if (options.PlanTimeout is { } planTimeout)
                    planCts.CancelAfter(planTimeout);

                var runCt = planCts.Token;
                await session.StartBrowserAsync(runCt).ConfigureAwait(false);
                for (var i = 0; i < plan.Steps.Count; i++) {
                    runCt.ThrowIfCancellationRequested();
                    var step = plan.Steps[i];
                    var stepExecutionId = AutomationGuid.CreateTimeOrdered();
                    var planStepId = step.StepId ?? Guid.Empty;
                    var label = StepLabel(step);
                    var stepContext = new AutomationPlanStepContext {
                        RunId = runId,
                        StepExecutionId = stepExecutionId,
                        PlanStepId = planStepId,
                        StepIndex = i,
                        Plan = plan,
                        Step = step,
                        Session = session
                    };

                    using (logger?.BeginScope(
                        new Dictionary<string, object?> {
                            ["automation_step"] = label,
                            ["automation_step_index"] = i,
                            ["plan_step_id"] = planStepId == Guid.Empty ? null : planStepId,
                            ["automation_plan_step_id"] = planStepId == Guid.Empty ? null : planStepId,
                            ["automation_step_execution_id"] = stepExecutionId,
                            ["plan_step_execution_id"] = stepExecutionId
                        })) {
                        instr?.OnStepStarting(new(runId, stepExecutionId, planStepId, i, step));
                        logger?.LogInformation(
                            "Automation step starting {AutomationRunId} {AutomationStepExecutionId} {PlanStepId} {AutomationPlanStepId} index={StepIndex} step={AutomationStep}",
                            runId, stepExecutionId, planStepId == Guid.Empty ? null : planStepId, planStepId == Guid.Empty ? null : planStepId, i, label);

                        artifacts?.LogLine(
                            $"STEP_START index={i} stepExecutionId={stepExecutionId:N} planStepId={(planStepId == Guid.Empty ? "" : planStepId.ToString("N"))} label={label}");

                        if (hooks?.BeforeStepAsync is { } before)
                            await before(stepContext, runCt).ConfigureAwait(false);

                        if (artifacts != null && dirOpts!.WriteSnapshots && dirOpts.SnapshotBeforeEachStep)
                            await TryWritePlanStepSnapshotAsync(session.Browser, artifacts.SnapshotsDirectory, i, stepExecutionId, "before", logger, runCt).ConfigureAwait(false);

                        var sw = Stopwatch.StartNew();
                        Exception? failure = null;
                        try {
                            using var stepCts = CancellationTokenSource.CreateLinkedTokenSource(runCt);
                            var eff = EffectiveStepTimeout(step, options);
                            if (eff is { } t)
                                stepCts.CancelAfter(t);

                            var stepCt = stepCts.Token;
                            var stepLogScope = new AutomationPlanStepLogScope(runId, stepExecutionId, planStepId, i, label);
                            await RunStepAsync(session.Browser, state, step, options, logger, stepLogScope, stepCt).ConfigureAwait(false);
                        }
                        catch (Exception ex) {
                            failure = ex;
                        }
                        finally {
                            sw.Stop();
                        }

                        if (failure != null) {
                            if (artifacts != null && dirOpts!.WriteSnapshots && dirOpts.SnapshotOnStepFailure) {
                                await TryWritePlanStepSnapshotAsync(session.Browser, artifacts.SnapshotsDirectory, i, stepExecutionId, "failed", logger, runCt)
                                    .ConfigureAwait(false);
                            }

                            if (artifacts != null && dirOpts!.WriteVariables && dirOpts.VariablesOnStepFailure)
                                await TryWritePlanVariablesAsync(artifacts, dirOpts, state, runId, i, "failed", logger, runCt).ConfigureAwait(false);

                            var stepOutcome = ClassifyStepFailure(failure, runCt);
                            artifacts?.LogLine(
                                $"STEP_FAILED index={i} stepExecutionId={stepExecutionId:N} durationMs={sw.ElapsedMilliseconds} outcome={stepOutcome} error={failure.GetType().Name}: {failure.Message}");

                            var failCtx = new AutomationPlanStepFailureContext {
                                RunId = runId,
                                StepExecutionId = stepExecutionId,
                                PlanStepId = planStepId,
                                StepIndex = i,
                                Plan = plan,
                                Step = step,
                                Session = session,
                                Exception = failure
                            };

                            instr?.OnStepFailed(new(runId, stepExecutionId, planStepId, i, step, sw.Elapsed, stepOutcome, failure));
                            instr?.OnStepOutcome(new(runId, planStepId, stepExecutionId, i, stepOutcome, sw.Elapsed, step, failure));
                            logger?.LogWarning(
                                failure,
                                "Automation step failed {AutomationRunId} {AutomationStepExecutionId} {PlanStepId} index={StepIndex} step={AutomationStep} outcome={StepOutcome} durationMs={DurationMs}",
                                runId, stepExecutionId, planStepId == Guid.Empty ? null : planStepId, i, label, stepOutcome, sw.ElapsedMilliseconds);

                            if (hooks?.OnFailureAsync is { } onFail)
                                await onFail(failCtx, runCt).ConfigureAwait(false);

                            throw failure;
                        }

                        var stepResult = new AutomationPlanStepResult(sw.Elapsed);
                        if (artifacts != null && dirOpts!.WriteSnapshots && dirOpts.SnapshotAfterEachSuccessfulStep)
                            await TryWritePlanStepSnapshotAsync(session.Browser, artifacts.SnapshotsDirectory, i, stepExecutionId, "after", logger, runCt).ConfigureAwait(false);

                        if (artifacts != null && dirOpts!.WriteVariables && dirOpts.VariablesAfterEachSuccessfulStep)
                            await TryWritePlanVariablesAsync(artifacts, dirOpts, state, runId, i, "after", logger, runCt).ConfigureAwait(false);

                        instr?.OnStepCompleted(new(runId, stepExecutionId, planStepId, i, step, sw.Elapsed));
                        instr?.OnStepOutcome(new(runId, planStepId, stepExecutionId, i, AutomationPlanStepOutcome.Success, sw.Elapsed, step, null));
                        logger?.LogInformation(
                            "Automation step completed {AutomationRunId} {AutomationStepExecutionId} {PlanStepId} index={StepIndex} step={AutomationStep} outcome={StepOutcome} durationMs={DurationMs}",
                            runId, stepExecutionId, planStepId == Guid.Empty ? null : planStepId, i, label, AutomationPlanStepOutcome.Success, sw.ElapsedMilliseconds);

                        artifacts?.LogLine($"STEP_COMPLETE index={i} stepExecutionId={stepExecutionId:N} outcome=Success durationMs={sw.ElapsedMilliseconds}");
                        if (hooks?.AfterStepAsync is { } after)
                            await after(stepContext, stepResult, runCt).ConfigureAwait(false);

                        state.RecordFrame(step);
                    }
                }

                swTotal.Stop();
                instr?.OnRunCompleted(new(runId, plan, swTotal.Elapsed, AutomationPlanRunOutcome.Completed));
                logger?.LogInformation(
                    "Automation plan completed {AutomationRunId} outcome={RunOutcome} durationMs={DurationMs}", runId, AutomationPlanRunOutcome.Completed,
                    swTotal.ElapsedMilliseconds);

                if (artifacts != null && dirOpts!.WriteVariables && dirOpts.VariablesOnRunEnd)
                    await TryWritePlanVariablesAsync(artifacts, dirOpts, state, runId, null, "final", logger, ct).ConfigureAwait(false);

                artifacts?.LogLine($"RUN_COMPLETED outcome=Completed durationMs={swTotal.ElapsedMilliseconds}");
                return new(state.ToSnapshot(), state.ToContext());
            }
            catch (Exception ex) {
                swTotal.Stop();
                var runOutcome = ex is OperationCanceledException ? AutomationPlanRunOutcome.Cancelled : AutomationPlanRunOutcome.Faulted;
                instr?.OnRunCompleted(new(runId, plan, swTotal.Elapsed, runOutcome));
                if (artifacts != null && dirOpts != null && dirOpts.WriteVariables && dirOpts.VariablesOnRunEnd) {
                    try {
                        await TryWritePlanVariablesAsync(
                                artifacts, dirOpts, state, runId, null, runOutcome == AutomationPlanRunOutcome.Cancelled ? "cancelled" : "faulted", logger, ct)
                            .ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) {
                        // best-effort only
                    }
                }

                artifacts?.LogLine($"RUN_END outcome={runOutcome} durationMs={swTotal.ElapsedMilliseconds} error={ex.GetType().Name}: {ex.Message}");
                if (runOutcome == AutomationPlanRunOutcome.Cancelled) {
                    logger?.LogWarning(
                        ex, "Automation plan run ended {AutomationRunId} outcome={RunOutcome} durationMs={DurationMs}", runId, runOutcome, swTotal.ElapsedMilliseconds);
                }
                else {
                    logger?.LogError(
                        ex, "Automation plan run ended {AutomationRunId} outcome={RunOutcome} durationMs={DurationMs}", runId, runOutcome, swTotal.ElapsedMilliseconds);
                }

                throw;
            }
            finally {
                artifacts?.Dispose();
            }
        }
    }

    private static async Task TryWritePlanStepSnapshotAsync(
        IWebAutomationBrowser browser,
        string snapshotsDirectory,
        int stepIndex,
        Guid stepExecutionId,
        string phase,
        ILogger? logger,
        CancellationToken ct)
    {
        try {
            var bytes = await browser.TakeViewportSnapshotPngAsync(ct).ConfigureAwait(false);
            Directory.CreateDirectory(snapshotsDirectory);
            var safePhase = phase.Replace(Path.DirectorySeparatorChar, '_').Replace(Path.AltDirectorySeparatorChar, '_');
            var fileName = $"{stepIndex:000}_{stepExecutionId:N}_{safePhase}.png";
            var path = Path.Combine(snapshotsDirectory, fileName);
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous))
                await fs.WriteAsync(bytes, 0, bytes.Length, ct).ConfigureAwait(false);

            logger?.LogInformation("Automation step snapshot {AutomationSnapshotPath} phase={AutomationSnapshotPhase} stepIndex={StepIndex}", path, phase, stepIndex);
        }
        catch (OperationCanceledException) {
            throw;
        }
        catch (Exception ex) {
            logger?.LogWarning(ex, "Automation step snapshot failed phase={AutomationSnapshotPhase} stepIndex={StepIndex}", phase, stepIndex);
        }
    }

    private static async Task TryWritePlanVariablesAsync(
        AutomationPlanRunArtifacts artifacts,
        AutomationPlanRunDirectoryOptions dirOpts,
        PlanExecutionState state,
        Guid runId,
        int? stepIndex,
        string phase,
        ILogger? logger,
        CancellationToken ct)
    {
        var snap = state.ToSnapshot();
        var fileName = stepIndex is { } idx ? $"step_{idx:000}_{phase}.json" : dirOpts.FinalVariablesFileName;
        await artifacts.TryWriteVariablesAsync(fileName, runId, stepIndex, phase, snap.Strings, snap.StringLists, logger, ct).ConfigureAwait(false);
    }

    private static TimeSpan? EffectiveStepTimeout(AutomationStepDefinition step, AutomationPlanRuntimeOptions options) => step.StepTimeout ?? options.DefaultStepTimeout;

    private static AutomationPlanStepOutcome ClassifyStepFailure(Exception failure, CancellationToken runCt)
    {
        if (failure is not OperationCanceledException)
            return AutomationPlanStepOutcome.Failed;

        return runCt.IsCancellationRequested ? AutomationPlanStepOutcome.Cancelled : AutomationPlanStepOutcome.TimedOut;
    }

    private static AutomationPlanInterpolationBindings CreateBindings(PlanExecutionState state, IWebAutomationBrowser browser)
        => new() {
            Strings = state.Strings,
            StringLists = state.StringLists.ToDictionary(static kvp => kvp.Key, static kvp => (IReadOnlyList<string>)kvp.Value, StringComparer.Ordinal),
            Elements = state.Elements,
            Browser = browser
        };

    private static Task<string> ExpandPlanTemplateAsync(string template, AutomationPlanInterpolationBindings bindings, AutomationPlanRuntimeOptions? runtime, CancellationToken ct)
        => AutomationPlanInterpolation.ExpandAsync(template, bindings, runtime?.Formatter, ct);

    private static string StepLabel(AutomationStepDefinition step) => !string.IsNullOrWhiteSpace(step.Name) ? step.Name! : step.GetType().Name;

    /// <summary>Re-applies run/step correlation on loggers when child work may not inherit ambient scopes (e.g. thread pool).</summary>
    private static IDisposable? BeginStepCorrelationScope(ILogger? logger, AutomationPlanStepLogScope? scope)
    {
        if (logger == null || scope is not { } s)
            return null;

        return logger.BeginScope(
            new Dictionary<string, object?> {
                ["automation_run_id"] = s.RunId,
                ["plan_run_id"] = s.RunId,
                ["automation_step"] = s.StepLabel,
                ["automation_step_index"] = s.StepIndex,
                ["plan_step_id"] = s.PlanStepId == Guid.Empty ? null : s.PlanStepId,
                ["automation_plan_step_id"] = s.PlanStepId == Guid.Empty ? null : s.PlanStepId,
                ["automation_step_execution_id"] = s.StepExecutionId,
                ["plan_step_execution_id"] = s.StepExecutionId
            });
    }

    private static async Task RunStepAsync(
        IWebAutomationBrowser browser,
        PlanExecutionState state,
        AutomationStepDefinition step,
        AutomationPlanRuntimeOptions? runtime,
        ILogger? logger,
        AutomationPlanStepLogScope? stepLogScope,
        CancellationToken ct)
    {
        var bindings = CreateBindings(state, browser);
        switch (step) {
            case NavigateAutomationStep n:
                await browser.NavigateAsync(await ExpandPlanTemplateAsync(n.Url, bindings, runtime, ct).ConfigureAwait(false), ct).ConfigureAwait(false);
                return;
            case ReloadAutomationStep:
                await browser.ReloadAsync(ct).ConfigureAwait(false);
                return;
            case FindElementAutomationStep f:
                await FindAndStoreElementAsync(browser, state, f.RefName, f.Locator, ct).ConfigureAwait(false);
                return;
            case FindElementChainAutomationStep fc:
                await FindAndStoreChainAsync(browser, state, fc.RefName, fc.Chain, ct).ConfigureAwait(false);
                return;
            case FindElementsChainAutomationStep fe:
                await FindAndStoreElementListAsync(browser, state, fe.RefName, fe.Chain, ct).ConfigureAwait(false);
                return;
            case ElementActionAutomationStep a:
                var el = state.ResolveElement(a.ElementRefName);
                await ApplyElementActionAsync(state, browser, runtime, el, a.Action, ct).ConfigureAwait(false);
                return;
            case FindAndActAutomationStep c:
                await FindAndStoreElementAsync(browser, state, c.RefName, c.Locator, ct).ConfigureAwait(false);
                await ApplyElementActionAsync(state, browser, runtime, state.Elements[c.RefName], c.Action, ct).ConfigureAwait(false);
                return;
            case FindAndActChainAutomationStep cc:
                await FindAndStoreChainAsync(browser, state, cc.RefName, cc.Chain, ct).ConfigureAwait(false);
                await ApplyElementActionAsync(state, browser, runtime, state.Elements[cc.RefName], cc.Action, ct).ConfigureAwait(false);
                return;
            case DelayAutomationStep d:
                if (d.DelayMilliseconds > 0)
                    await Task.Delay(d.DelayMilliseconds, ct).ConfigureAwait(false);

                return;
            case ExtractElementDataAutomationStep ex:
                await ExtractElementDataAsync(state, ex, ct).ConfigureAwait(false);
                return;
            case ExtractElementsListDataAutomationStep exl:
                await ExtractElementsListDataAsync(state, exl, ct).ConfigureAwait(false);
                return;
            case WriteStringListToFileAutomationStep w:
                await WriteStringListToFileAsync(state, browser, runtime, w, logger, stepLogScope, ct).ConfigureAwait(false);
                return;
            case DownloadUrlsToDirectoryAutomationStep dl:
                await DownloadUrlsAsync(state, browser, dl, runtime, logger, stepLogScope, ct).ConfigureAwait(false);
                return;
            case StoreLiteralAutomationStep lit:
                state.Strings[lit.VariableName] = await ExpandPlanTemplateAsync(lit.Value, bindings, runtime, ct).ConfigureAwait(false);
                return;
            case StoreTemplateAutomationStep tpl:
                state.Strings[tpl.VariableName] = await ExpandPlanTemplateAsync(tpl.Template, bindings, runtime, ct).ConfigureAwait(false);
                return;
            case StorePageUrlAutomationStep u:
                state.Strings[u.VariableName] = await browser.GetCurrentUrlAsync(ct).ConfigureAwait(false);
                return;
            case StorePageTitleAutomationStep t:
                state.Strings[t.VariableName] = await browser.GetTitleAsync(ct).ConfigureAwait(false);
                return;
            default:
                OperationHelpers.ThrowIf(true, $"Unsupported automation step: {step.GetType().Name}");
                return;
        }
    }

    private static async Task FindAndStoreElementAsync(IWebAutomationBrowser browser, PlanExecutionState state, string refName, ElementLocator locator, CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(refName);
        state.ThrowIfRefNameTaken(refName);
        var element = await browser.PollForElementAsync(locator, ct).ConfigureAwait(false);
        state.Elements[refName] = element;
    }

    private static async Task FindAndStoreChainAsync(IWebAutomationBrowser browser, PlanExecutionState state, string refName, ElementLocatorChain chain, CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(refName);
        state.ThrowIfRefNameTaken(refName);
        var element = await browser.PollForElementAsync(chain, ct).ConfigureAwait(false);
        state.Elements[refName] = element;
    }

    private static async Task FindAndStoreElementListAsync(IWebAutomationBrowser browser, PlanExecutionState state, string refName, ElementLocatorChain chain, CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(refName);
        state.ThrowIfRefNameTaken(refName);
        var list = await browser.PollForElementsAsync(chain, ct).ConfigureAwait(false);
        state.ElementLists[refName] = list;
    }

    private static async Task ExtractElementDataAsync(PlanExecutionState state, ExtractElementDataAutomationStep step, CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(step.VariableName, nameof(step.VariableName));
        if (step.Kind == ElementDataExtractKind.Attribute)
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(step.AttributeName, nameof(step.AttributeName));

        var element = state.ResolveElement(step.ElementRefName);
        if (step.Kind == ElementDataExtractKind.Text)
            state.Strings[step.VariableName] = await element.GetTextAsync(ct).ConfigureAwait(false);
        else
            state.Strings[step.VariableName] = await element.GetAttributeAsync(step.AttributeName!, ct).ConfigureAwait(false) ?? string.Empty;
    }

    private static async Task ExtractElementsListDataAsync(PlanExecutionState state, ExtractElementsListDataAutomationStep step, CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(step.VariableName, nameof(step.VariableName));
        if (step.Kind == ElementDataExtractKind.Attribute)
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(step.AttributeName, nameof(step.AttributeName));

        var foundList = state.ElementLists.TryGetValue(step.ElementsListRefName, out var list);
        OperationHelpers.ThrowIf(!foundList, $"Unknown element list ref '{step.ElementsListRefName}'.");
        var result = new List<string>(list.Count);
        foreach (var element in list) {
            if (step.Kind == ElementDataExtractKind.Text)
                result.Add(await element.GetTextAsync(ct).ConfigureAwait(false));
            else {
                var v = await element.GetAttributeAsync(step.AttributeName!, ct).ConfigureAwait(false);
                result.Add(v ?? string.Empty);
            }
        }

        state.StringLists[step.VariableName] = result;
    }

    private static async Task WriteStringListToFileAsync(
        PlanExecutionState state,
        IWebAutomationBrowser browser,
        AutomationPlanRuntimeOptions? runtime,
        WriteStringListToFileAutomationStep step,
        ILogger? logger,
        AutomationPlanStepLogScope? stepLogScope,
        CancellationToken ct)
    {
        var foundLines = state.StringLists.TryGetValue(step.VariableName, out var lines);
        OperationHelpers.ThrowIf(!foundLines, $"Unknown string list variable '{step.VariableName}'.");
        using (BeginStepCorrelationScope(logger, stepLogScope)) {
            var bindings = CreateBindings(state, browser);
            var full = Path.GetFullPath(await ExpandPlanTemplateAsync(step.FilePath, bindings, runtime, ct).ConfigureAwait(false));
            var dir = Path.GetDirectoryName(full);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            var text = string.Join(Environment.NewLine, lines);
            await Task.Run(
                    () => {
                        using (BeginStepCorrelationScope(logger, stepLogScope)) {
                            ct.ThrowIfCancellationRequested();
                            if (step.Append && File.Exists(full))
                                File.AppendAllText(full, text + Environment.NewLine);
                            else
                                File.WriteAllText(full, text);
                        }
                    }, ct)
                .ConfigureAwait(false);
        }
    }

    private static async Task DownloadUrlsAsync(
        PlanExecutionState state,
        IWebAutomationBrowser browser,
        DownloadUrlsToDirectoryAutomationStep step,
        AutomationPlanRuntimeOptions? runtime,
        ILogger? logger,
        AutomationPlanStepLogScope? stepLogScope,
        CancellationToken ct)
    {
        using (BeginStepCorrelationScope(logger, stepLogScope)) {
            var http = runtime?.HttpClient;
            OperationHelpers.ThrowIfNull(http, "DownloadUrlsToDirectory requires AutomationPlanRuntimeOptions.HttpClient to be set.");
            IReadOnlyList<string> urlSource;
            if (step.UrlListFromCompletedStepIndex is { } stepIdx) {
                var foundFrame = state.TryGetStringListAtCompletedStep(stepIdx, step.UrlListVariableName, out var fromFrame);
                OperationHelpers.ThrowIf(!foundFrame, $"No string list variable '{step.UrlListVariableName}' at completed step index {stepIdx}, or step index out of range.");
                urlSource = fromFrame!;
            }
            else {
                var foundUrls = state.StringLists.TryGetValue(step.UrlListVariableName, out var urls);
                OperationHelpers.ThrowIf(!foundUrls, $"Unknown string list variable '{step.UrlListVariableName}'.");
                urlSource = urls!;
            }

            var bindings = CreateBindings(state, browser);
            var targetDir = await ExpandPlanTemplateAsync(step.TargetDirectory, bindings, runtime, ct).ConfigureAwait(false);
            Directory.CreateDirectory(targetDir);
            var prefix = step.FileNamePrefix != null
                ? await ExpandPlanTemplateAsync(step.FileNamePrefix, bindings, runtime, ct).ConfigureAwait(false)
                : runtime?.DownloadFileNamePrefix ?? "download";

            var index = 0;
            foreach (var url in urlSource) {
                ct.ThrowIfCancellationRequested();
                if (string.IsNullOrWhiteSpace(url))
                    continue;

                if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                    throw new InvalidOperationException($"Invalid HTTP(S) URL: {url}");

                index++;
                var ext = Path.GetExtension(uri.AbsolutePath);
                if (string.IsNullOrEmpty(ext) || ext.Length > 8)
                    ext = ".bin";

                var fileName = $"{prefix}_{index:000}{ext}";
                var fullPath = Path.Combine(targetDir, fileName);
                logger?.LogDebug("Downloading {Url} -> {Path}", url, fullPath);
                using var response = await http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                using var fs = File.Create(fullPath);
                await response.Content.CopyToAsync(fs).ConfigureAwait(false);
            }
        }
    }

    private static async Task ApplyElementActionAsync(
        PlanExecutionState state,
        IWebAutomationBrowser browser,
        AutomationPlanRuntimeOptions? runtime,
        IWebAutomationElement element,
        ElementAction action,
        CancellationToken ct)
    {
        var bindings = CreateBindings(state, browser);
        switch (action) {
            case ClickElementAction c:
                if (c.ScrollIntoView)
                    await element.ScrollIntoViewAsync(ct).ConfigureAwait(false);

                await element.ClickAsync(ct).ConfigureAwait(false);
                return;
            case InputTextElementAction i:
                ArgumentHelpers.ThrowIfNull(i.Text, nameof(i.Text));
                await element.SendKeysAsync(await ExpandPlanTemplateAsync(i.Text, bindings, runtime, ct).ConfigureAwait(false), i.ClearFirst, ct).ConfigureAwait(false);
                return;
            case SendKeysElementAction s:
                ArgumentHelpers.ThrowIfNull(s.Keys, nameof(s.Keys));
                await element.SendKeysRawAsync(await ExpandPlanTemplateAsync(s.Keys, bindings, runtime, ct).ConfigureAwait(false), ct).ConfigureAwait(false);
                return;
            case ClearElementAction:
                await element.ClearAsync(ct).ConfigureAwait(false);
                return;
            case SubmitElementAction:
                await element.SubmitAsync(ct).ConfigureAwait(false);
                return;
            case SelectByTextElementAction t:
                ArgumentHelpers.ThrowIfNull(t.Text, nameof(t.Text));
                await element.SelectByTextAsync(await ExpandPlanTemplateAsync(t.Text, bindings, runtime, ct).ConfigureAwait(false), ct).ConfigureAwait(false);
                return;
            case SelectByValueElementAction v:
                ArgumentHelpers.ThrowIfNull(v.Value, nameof(v.Value));
                await element.SelectByValueAsync(await ExpandPlanTemplateAsync(v.Value, bindings, runtime, ct).ConfigureAwait(false), ct).ConfigureAwait(false);
                return;
            case SelectByIndexElementAction x:
                await element.SelectByIndexAsync(x.Index, ct).ConfigureAwait(false);
                return;
            default:
                OperationHelpers.ThrowIf(true, $"Unsupported element action: {action.GetType().Name}");
                return;
        }
    }

    private sealed class PlanExecutionState
    {
        private readonly List<AutomationPlanStepFrame> _frames = new();

        public Dictionary<string, IWebAutomationElement> Elements { get; } = new(StringComparer.Ordinal);

        public Dictionary<string, IReadOnlyList<IWebAutomationElement>> ElementLists { get; } = new(StringComparer.Ordinal);

        public Dictionary<string, string> Strings { get; } = new(StringComparer.Ordinal);

        public Dictionary<string, List<string>> StringLists { get; } = new(StringComparer.Ordinal);

        public void ThrowIfRefNameTaken(string refName)
            => OperationHelpers.ThrowIf(
                Elements.ContainsKey(refName) || ElementLists.ContainsKey(refName), $"Duplicate ref name '{refName}'. Ref names must be unique within a plan.");

        public IWebAutomationElement ResolveElement(string refName)
        {
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(refName);
            OperationHelpers.ThrowIf(!Elements.TryGetValue(refName, out var el), $"Unknown element ref '{refName}'. Run a find step first.");
            return el!;
        }

        public AutomationPlanExecutionSnapshot ToSnapshot()
            => new(
                new Dictionary<string, string>(Strings, StringComparer.Ordinal),
                StringLists.ToDictionary(static kvp => kvp.Key, static kvp => (IReadOnlyList<string>)kvp.Value.ToArray(), StringComparer.Ordinal));

        public void RecordFrame(AutomationStepDefinition step) => _frames.Add(CloneFrame(step));

        public AutomationPlanExecutionContext ToContext() => new(CloneBindingsSnapshot(), new ReadOnlyCollection<AutomationPlanStepFrame>(_frames));

        private AutomationPlanBindings CloneBindingsSnapshot()
        {
            var elements = new Dictionary<string, IWebAutomationElement>(Elements, StringComparer.Ordinal);
            var elementLists = ElementLists.ToDictionary(static kvp => kvp.Key, static kvp => (IReadOnlyList<IWebAutomationElement>)kvp.Value.ToArray(), StringComparer.Ordinal);
            var strings = new Dictionary<string, string>(Strings, StringComparer.Ordinal);
            var stringLists = StringLists.ToDictionary(static kvp => kvp.Key, static kvp => (IReadOnlyList<string>)kvp.Value.ToArray(), StringComparer.Ordinal);
            return new(elements, elementLists, strings, stringLists);
        }

        private AutomationPlanStepFrame CloneFrame(AutomationStepDefinition step) => new(_frames.Count, step, CloneBindingsSnapshot());

        public bool TryGetStringListAtCompletedStep(int completedStepIndex, string variableName, out IReadOnlyList<string>? list)
        {
            if (completedStepIndex < 0 || completedStepIndex >= _frames.Count) {
                list = null;
                return false;
            }

            return _frames[completedStepIndex].TryGetStringList(variableName, out list);
        }
    }
}