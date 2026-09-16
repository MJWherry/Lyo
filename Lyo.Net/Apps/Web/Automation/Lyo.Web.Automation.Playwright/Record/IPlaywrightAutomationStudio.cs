using Lyo.Web.Automation.Models;
using Lyo.Web.Automation.Plan;
using Lyo.Web.Automation.Playwright.Browser;
using Microsoft.Extensions.Logging;

namespace Lyo.Web.Automation.Playwright.Record;

/// <summary>A browser session that records an <see cref="AutomationPlan" /> and can play one. Dispose or <see cref="StopAsync" /> ends it.</summary>
public interface IPlaywrightAutomationStudio : IAsyncDisposable
{
    /// <summary>The live browser. The page is only the site.</summary>
    PlaywrightBrowser Browser { get; }

    /// <summary>Completes when the browser closes.</summary>
    Task Completion { get; }

    /// <summary>The plan collected so far. Typing still in a field is one step, and reading this does not commit it.</summary>
    AutomationPlan Plan { get; }

    /// <summary>True while recorded clicks and typing are ignored.</summary>
    bool RecordingPaused { get; }

    /// <summary>True while clicks and typing in the browser are appended to the plan.</summary>
    bool IsRecording { get; }

    /// <summary>Raised after a recorded gesture changes the plan.</summary>
    event EventHandler? PlanChanged;

    /// <summary>Listens to clicks and typing on the open page and appends them to the plan. The listener is not installed until this is called.</summary>
    Task StartRecordingAsync(CancellationToken ct = default);

    /// <summary>Stops appending browser gestures. Steps already recorded stay.</summary>
    void StopRecording();

    /// <summary>Play notifications: step start, outcome, log line, screenshot path, and context.</summary>
    event EventHandler<AutomationStudioStepNotice>? StepNotice;

    /// <summary>Runner log. When set, play writes here in addition to <see cref="StepNotice" />.</summary>
    ILogger? PlayLogger { get; set; }

    /// <summary>Plan loaded from the startup <c>--plan</c> path, when that file parsed.</summary>
    AutomationPlan? LoadedPlan { get; }

    /// <summary>Ignores page clicks and typing until <see cref="ResumeRecording" />.</summary>
    void PauseRecording();

    /// <summary>Appends page clicks and typing again.</summary>
    void ResumeRecording();

    /// <summary>Inserts a step that does not come from a page gesture.</summary>
    void InsertStep(RecordedInsert insert);

    /// <summary>Moves a recorded step.</summary>
    void MoveStep(int from, int to);

    /// <summary>Removes a recorded step.</summary>
    void RemoveStep(int index);

    /// <summary>Writes the plan JSON into <paramref name="directory" /> and returns the file path.</summary>
    string SavePlan(string? directory = null);

    /// <summary>Runs <paramref name="plan" />. Earlier steps than <paramref name="startStepIndex" /> are skipped. When <paramref name="endStepIndex" /> is set, later steps are not started. Screenshots and context go under <paramref name="runDirectory" />.</summary>
    Task<AutomationPlanRunResult> PlayAsync(AutomationPlan plan, int startStepIndex, string runDirectory, CancellationToken ct = default, int? endStepIndex = null);

    /// <summary>Holds play before the next step.</summary>
    void PausePlay();

    /// <summary>Continues a held play at the next unstarted step.</summary>
    void ResumePlay();

    /// <summary>Cancels the active play, including a wait already inside Playwright.</summary>
    void CancelPlay();

    /// <summary>Starts recording browser requests.</summary>
    void StartNetworkCapture();

    /// <summary>Stops recording browser requests. Rows already captured stay.</summary>
    void StopNetworkCapture();

    /// <summary>Drops captured request rows. Listening stays as it was.</summary>
    void ClearNetwork();

    /// <summary>Captured requests. A blank <paramref name="urlPattern" /> returns every row.</summary>
    IReadOnlyList<AutomationStudioNetworkRow> NetworkRows(string? urlPattern);

    /// <summary>Headers and bodies for one captured request.</summary>
    AutomationStudioNetworkDetail? ReadNetwork(int id);

    /// <summary>Opens the request again in a browser tab.</summary>
    Task ReplayNetworkAsync(int id, CancellationToken ct = default);

    /// <summary>Adds a store-string-list step from captured URLs that match <paramref name="urlPattern" />.</summary>
    string? StoreCapturedUrls(string variableName, string? urlPattern);

    /// <summary>Highlights the element under the cursor when <paramref name="enabled" /> is true.</summary>
    Task SetGrabberAsync(bool enabled, CancellationToken ct = default);

    /// <summary>Describes the element under the cursor.</summary>
    Task<AutomationStudioElementInfo?> DescribeElementAsync(CancellationToken ct = default);

    /// <summary>Outlines <paramref name="locator" /> in the page and clears the previous outline.</summary>
    Task HighlightAsync(ElementLocator locator, CancellationToken ct = default);

    /// <summary>Adds the extract step for the element under the cursor. <paramref name="tool" /> is images, links, text, or attribute.</summary>
    Task<string?> InsertGrabbedAsync(string tool, string variableName, CancellationToken ct = default);

    /// <summary>Commits pending typing, cancels play, closes the browser, and returns the plan.</summary>
    Task<AutomationPlan> StopAsync(CancellationToken ct = default);
}

