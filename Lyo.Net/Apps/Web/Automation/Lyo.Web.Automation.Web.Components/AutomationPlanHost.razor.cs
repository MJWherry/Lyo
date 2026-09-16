using Lyo.Api.Client;
using Lyo.Web.Automation.Client;
using Lyo.Web.Automation.Models;
using Lyo.Web.Components.DataGrid;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Lyo.Web.Automation.Web.Components;

/// <summary>
/// Selection bar plus Manage, Editor, and Runner. Manage lists stored plans. Editor and Runner share the selection, including a file that was never saved.
/// </summary>
public partial class AutomationPlanHost : ComponentBase, IDisposable
{
    private readonly CancellationTokenSource _lifetime = new();
    private AutomationPlan? _plan;
    private string _source = "none";
    private string? _error;
    private bool _busy;
    private int _tab;

    /// <summary>Client for plan reads, saves, and runs.</summary>
    [Parameter]
    [EditorRequired]
    public IAutomationPlanClient Client { get; set; } = null!;

    /// <summary>Client for the plan grid.</summary>
    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    /// <inheritdoc />
    public void Dispose() => _lifetime.Cancel();

    private void OnEdited(AutomationPlan plan) => _plan = plan;

    private async Task LoadFileAsync(InputFileChangeEventArgs args)
    {
        _busy = true;
        _error = null;
        try {
            await using var stream = args.File.OpenReadStream(1024 * 1024);
            using var reader = new StreamReader(stream);
            var json = await reader.ReadToEndAsync(_lifetime.Token);
            _plan = AutomationPlanDraft.Parse(json);
            _source = "file";
            _tab = 1;
        }
        catch (Exception ex) when (ex is not OperationCanceledException) {
            _error = ex.Message;
        }
        finally {
            _busy = false;
        }
    }

    private async Task OpenRowAsync(object? row, bool run)
    {
        var idText = ProjectedValueHelper.GetDisplayValue(row, "Id");
        if (!Guid.TryParse(idText, out var id)) {
            _error = "That row has no plan id.";
            return;
        }

        _busy = true;
        _error = null;
        try {
            var plan = await Client.GetPlanByIdAsync(id, _lifetime.Token);
            if (plan is null) {
                _error = "That plan is not on the server.";
                return;
            }

            _plan = plan;
            _source = "database";
            _tab = run ? 2 : 1;
        }
        catch (Exception ex) when (ex is not OperationCanceledException) {
            _error = ex.Message;
        }
        finally {
            _busy = false;
        }
    }

    private async Task SaveAsync()
    {
        if (_plan is null || string.IsNullOrWhiteSpace(_plan.Name)) {
            _error = "Name the plan before saving.";
            return;
        }

        _busy = true;
        _error = null;
        try {
            var name = _plan.Name;
            await Client.CreatePlanAsync(name, _lifetime.Token);
            _plan = await Client.SavePlanAsync(name, _plan, _lifetime.Token);
            _source = "database";
        }
        catch (Exception ex) when (ex is not OperationCanceledException) {
            _error = ex.Message;
        }
        finally {
            _busy = false;
        }
    }
}
