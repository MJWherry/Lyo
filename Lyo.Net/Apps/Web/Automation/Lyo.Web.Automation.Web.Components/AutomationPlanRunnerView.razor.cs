using System.Text.Json;
using Lyo.Web.Automation.Client;
using Lyo.Web.Automation.Models;
using Lyo.Web.Automation.Plan;
using Microsoft.AspNetCore.Components;

namespace Lyo.Web.Automation.Web.Components;

/// <summary>Runs one plan and shows each finished step’s screenshot and context. It does not list plans.</summary>
public partial class AutomationPlanRunnerView : ComponentBase, IDisposable
{
    private readonly CancellationTokenSource _lifetime = new();
    private readonly List<AutomationRunStepRes> _steps = [];
    private bool _busy;
    private string? _error;
    private string? _image;
    private Guid? _runId;
    private int _selected = -1;
    private string _status = "";

    /// <summary>Client that posts the plan and reads step screenshots.</summary>
    [Parameter]
    [EditorRequired]
    public IAutomationPlanClient Client { get; set; } = null!;

    /// <summary>Plan to post, including one that was loaded from a file and never saved.</summary>
    [Parameter]
    public AutomationPlan? Plan { get; set; }

    private AutomationRunStepRes? _selectedStep => _selected >= 0 && _selected < _steps.Count ? _steps[_selected] : null;

    private AutomationRunStepRes? _latest => _steps.Count == 0 ? null : _steps[^1];

    /// <inheritdoc />
    public void Dispose() => _lifetime.Cancel();

    private async Task RunAsync()
    {
        if (Plan is null)
            return;

        _busy = true;
        _error = null;
        _steps.Clear();
        _selected = -1;
        _image = null;
        _status = AutomationRunStatus.Queued;
        try {
            var accepted = await Client.StartAsync(new() { Plan = Plan, Headless = true }, _lifetime.Token);
            _runId = accepted.RunId;
            var run = await PollAsync(accepted.RunId);
            _status = run.Status;
            _error = run.Error;
            if (_steps.Count > 0)
                await SelectAsync(0);
        }
        catch (Exception ex) when (ex is not OperationCanceledException) {
            _error = ex.Message;
            _status = AutomationRunStatus.Failed;
        }
        finally {
            _busy = false;
        }
    }

    private async Task<AutomationRunRes> PollAsync(Guid runId)
    {
        AutomationRunRes? last = null;
        for (var i = 0; i < 120; i++) {
            last = await Client.GetAsync(runId, _lifetime.Token) ?? throw new InvalidOperationException("Run disappeared.");
            _status = last.Status;
            ReplaceSteps(last.Steps);
            await InvokeAsync(StateHasChanged);
            if (last.Status is AutomationRunStatus.Completed or AutomationRunStatus.Failed or AutomationRunStatus.Cancelled)
                return last;

            await Task.Delay(400, _lifetime.Token);
        }

        throw new TimeoutException("The run did not finish.");
    }

    private void ReplaceSteps(IReadOnlyList<AutomationRunStepRes> steps)
    {
        _steps.Clear();
        _steps.AddRange(steps);
    }

    private async Task SelectAsync(int index)
    {
        _selected = index;
        _image = null;
        var step = _selectedStep;
        if (step?.ScreenshotFileId is null || _runId is null)
            return;

        try {
            var bytes = await Client.GetStepScreenshotAsync(_runId.Value, step.Index, _lifetime.Token);
            if (bytes.Length > 0)
                _image = "data:image/png;base64," + Convert.ToBase64String(bytes);
        }
        catch (Exception ex) when (ex is not OperationCanceledException) {
            _error = ex.Message;
        }
    }

    private static string ContextJson(AutomationPlanContextSnapshot? context) => context is null ? "" : JsonSerializer.Serialize(context, AutomationPlanDraft.JsonOptions);
}