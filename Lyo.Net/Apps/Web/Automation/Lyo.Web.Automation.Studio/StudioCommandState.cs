namespace Lyo.Web.Automation.Studio;

/// <summary>Facts the studio window knows about the selected session. <see cref="StudioCommandState.From" /> turns these into button enablement.</summary>
public sealed class StudioCommandInput
{
    /// <summary>A session tab is selected.</summary>
    public bool HasSession { get; init; }

    /// <summary>That session's browser window is still open.</summary>
    public bool BrowserOpen { get; init; }

    /// <summary>A plan run is in progress, including while it is paused.</summary>
    public bool Playing { get; init; }

    /// <summary>The in-progress run is held before the next step.</summary>
    public bool Paused { get; init; }

    /// <summary>Steps in the plan being edited.</summary>
    public int StepCount { get; init; }

    /// <summary>Selected step on Record, or -1 when nothing is selected.</summary>
    public int SelectedStep { get; init; } = -1;

    /// <summary>Selected step on Run, or -1 when nothing is selected.</summary>
    public int RunSelectedStep { get; init; } = -1;

    /// <summary>Captured request rows, ignoring the filter.</summary>
    public int NetworkCount { get; init; }

    /// <summary>A captured row is selected.</summary>
    public bool NetworkRowSelected { get; init; }

    /// <summary>The capture variable box has text.</summary>
    public bool CaptureVariableFilled { get; init; }

    /// <summary>At least one captured row matches the filter.</summary>
    public bool HasCapturedMatch { get; init; }
}

/// <summary>Which studio commands are available for one <see cref="StudioCommandInput" />.</summary>
public sealed class StudioCommandState
{
    /// <summary>Navigate the open browser. This does not change the plan.</summary>
    public bool Go { get; init; }

    /// <summary>Reload the open page.</summary>
    public bool Reload { get; init; }

    /// <summary>Search the open page for an element.</summary>
    public bool Find { get; init; }

    /// <summary>Click the element that Find resolved.</summary>
    public bool Click { get; init; }

    /// <summary>Type into the element that Find resolved.</summary>
    public bool Type { get; init; }

    /// <summary>Clear the element that Find resolved.</summary>
    public bool ClearElement { get; init; }

    /// <summary>Start or stop request capture on the open browser.</summary>
    public bool Listen { get; init; }

    /// <summary>Open the selected captured request again.</summary>
    public bool Replay { get; init; }

    /// <summary>Arm the cursor grabber on the open page.</summary>
    public bool Grabber { get; init; }

    /// <summary>Apply a viewport size to the open page.</summary>
    public bool ViewportApply { get; init; }

    /// <summary>Open, switch, or close a tab on the open browser.</summary>
    public bool TabActions { get; init; }

    /// <summary>Run the plan from the first step.</summary>
    public bool Run { get; init; }

    /// <summary>Run from the selected step through the end.</summary>
    public bool RunFromHere { get; init; }

    /// <summary>Run only the selected step.</summary>
    public bool RunThisStep { get; init; }

    /// <summary>Hold the run before the next step.</summary>
    public bool Pause { get; init; }

    /// <summary>Continue a held run.</summary>
    public bool Resume { get; init; }

    /// <summary>Cancel the run.</summary>
    public bool Stop { get; init; }

    /// <summary>Move the selected step up.</summary>
    public bool Up { get; init; }

    /// <summary>Move the selected step down.</summary>
    public bool Down { get; init; }

    /// <summary>Remove the selected step.</summary>
    public bool Delete { get; init; }

    /// <summary>Write the plan JSON.</summary>
    public bool Save { get; init; }

    /// <summary>Drop captured request rows.</summary>
    public bool ClearNetwork { get; init; }

    /// <summary>Add a store-string-list step from the filtered capture.</summary>
    public bool StoreCaptured { get; init; }

    /// <summary>Add a capture-network step.</summary>
    public bool AddCapture { get; init; }

    /// <summary>Append a step. The browser does not have to still be open.</summary>
    public bool AddStep { get; init; }

    /// <summary>Enables commands from <paramref name="input" />.</summary>
    public static StudioCommandState From(StudioCommandInput input)
    {
        var live = input.HasSession && input.BrowserOpen && !input.Playing;
        var selected = input.SelectedStep >= 0 && input.SelectedStep < input.StepCount;
        var runSelected = input.RunSelectedStep >= 0 && input.RunSelectedStep < input.StepCount;
        var canRun = live && input.StepCount > 0;
        return new() {
            Go = live,
            Reload = live,
            Find = live,
            Click = live,
            Type = live,
            ClearElement = live,
            Listen = live,
            Replay = live && input.NetworkRowSelected,
            Grabber = live,
            ViewportApply = live,
            TabActions = live,
            Run = canRun,
            RunFromHere = canRun && runSelected,
            RunThisStep = canRun && runSelected,
            Pause = input.Playing && !input.Paused,
            Resume = input.Paused,
            Stop = input.Playing || input.Paused,
            Up = selected && input.SelectedStep > 0,
            Down = selected && input.SelectedStep < input.StepCount - 1,
            Delete = selected,
            Save = input.HasSession && input.StepCount > 0,
            ClearNetwork = input.NetworkCount > 0,
            StoreCaptured = input.HasSession && input.HasCapturedMatch,
            AddCapture = input.HasSession && input.CaptureVariableFilled,
            AddStep = input.HasSession
        };
    }
}
