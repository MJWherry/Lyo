using System.Globalization;
using Lyo.Diagram.Models;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Lyo.Job.Web.Components;

/// <summary>
/// Draws the workflow from a <see cref="DiagramDocument" />: one rounded shape per step and an orthogonal connector per dependency. A tooltip says when each step runs. When
/// <see cref="StepStates" /> is supplied the same picture doubles as a run view. Drag the viewport to move the picture; scroll or the zoom controls to scale it.
/// </summary>
/// <remarks>
/// <see cref="JobWorkflowDiagram" /> builds the document from <see cref="JobWorkflowGraphLayout" />. The view draws that document as one SVG, including the arrowhead, so the
/// picture does not depend on an SVG marker URL (those break under the gateway's <c>&lt;base href&gt;</c>). The SVG stays at the layout's pixel size, and
/// <see cref="JobWorkflowGraphCamera" /> pans and zooms it, so the labels do not shrink to fit the column.
/// </remarks>
public partial class JobWorkflowGraphView : IAsyncDisposable
{
    private const double ContentMargin = 24;
    private const double DragSlop = 4;

    private readonly JobWorkflowGraphCamera _camera = new();
    private double _dragDistance;
    private double _lastX;
    private double _lastY;
    private IJSObjectReference? _module;
    private Task<IJSObjectReference?>? _moduleTask;
    private bool _panning;
    private JobWorkflowPicture _picture = JobWorkflowDiagram.Create("Workflow", []);
    private long _pointerId;
    private bool _pressed;
    private bool _suppressClick;
    private ElementReference _viewport;

    [Parameter]
    [EditorRequired]
    public IReadOnlyList<JobWorkflowStepDraft> Steps { get; set; } = [];

    /// <summary>Per-step state when viewing a run, keyed by step id. Empty while authoring.</summary>
    [Parameter]
    public IReadOnlyDictionary<Guid, JobWorkflowStepState> StepStates { get; set; } = new Dictionary<Guid, JobWorkflowStepState>();

    /// <summary>Definitions behind the steps, so a tooltip can name the job a step runs rather than only its step name.</summary>
    [Parameter]
    public IReadOnlyList<JobDefinitionRes> Definitions { get; set; } = [];

    /// <summary>Draft id of the step the editor is showing, highlighted in the graph. Empty while viewing a run.</summary>
    [Parameter]
    public Guid SelectedStepId { get; set; }

    /// <summary>Raised with the draft id when a node is activated. Unset makes the nodes inert, which is what the run view wants.</summary>
    [Parameter]
    public EventCallback<Guid> OnStepSelected { get; set; }

    private string ViewportClass => _panning ? "workflow-graph-viewport panning" : "workflow-graph-viewport";

    private string Transform => string.Create(CultureInfo.InvariantCulture, $"translate({_camera.PanX:0.###}px, {_camera.PanY:0.###}px) scale({_camera.Zoom:0.####})");

    private string ZoomLabel => string.Create(CultureInfo.InvariantCulture, $"{_camera.Zoom * 100:0}%");

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_module is null)
            return;

        try {
            await _module.DisposeAsync();
        }
        catch (JSDisconnectedException) { }
    }

    protected override void OnInitialized() => _camera.Reset(ContentMargin, ContentMargin);

    protected override void OnParametersSet() => _picture = JobWorkflowDiagram.Create("Workflow", Steps);

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
            await EnsureModuleAsync();
    }

    private void OnPointerDown(PointerEventArgs e)
    {
        if (e.Button != 0)
            return;

        _pressed = true;
        _panning = false;
        _suppressClick = false;
        _dragDistance = 0;
        _pointerId = e.PointerId;
        _lastX = e.ClientX;
        _lastY = e.ClientY;
    }

    private void OnNodePointerDown(PointerEventArgs e)
    {
        if (e.Button != 0)
            return;

        _pressed = false;
        _panning = false;
        _suppressClick = false;
    }

    private void OnPointerMove(PointerEventArgs e)
    {
        if (!_pressed)
            return;

        var dx = e.ClientX - _lastX;
        var dy = e.ClientY - _lastY;
        _lastX = e.ClientX;
        _lastY = e.ClientY;
        _dragDistance += Math.Abs(dx) + Math.Abs(dy);
        if (_dragDistance <= DragSlop)
            return;

        _suppressClick = true;
        if (!_panning) {
            _panning = true;
            _ = CaptureAsync(_pointerId);
        }

        _camera.Pan(dx, dy);
    }

    private void OnPointerUp(PointerEventArgs _)
    {
        _pressed = false;
        _panning = false;
    }

    private async Task OnWheel(WheelEventArgs e)
    {
        var origin = await ViewportPointAsync(false, e.ClientX, e.ClientY);
        _camera.ZoomAt(WheelFactor(e), origin.X, origin.Y);
    }

    private async Task ZoomAroundCenterAsync(double factor)
    {
        var origin = await ViewportPointAsync(true, 0, 0);
        _camera.ZoomAt(factor, origin.X, origin.Y);
    }

    private void ResetView() => _camera.Reset(ContentMargin, ContentMargin);

    /// <summary>A mouse notch is about a hundred pixels; Firefox often reports the same gesture in lines. Both should move the zoom by a similar step.</summary>
    private static double WheelFactor(WheelEventArgs e)
    {
        var delta = e.DeltaY;
        if (e.DeltaMode == 1)
            delta *= 16;
        else if (e.DeltaMode == 2)
            delta *= 400;

        return Math.Pow(1.0015, -delta);
    }

    private async Task CaptureAsync(long pointerId)
    {
        await EnsureModuleAsync();
        if (_module is null)
            return;

        try {
            await _module.InvokeVoidAsync("capture", _viewport, pointerId);
        }
        catch (JSException) { }
    }

    private async Task<ViewportPoint> ViewportPointAsync(bool center, double clientX, double clientY)
    {
        await EnsureModuleAsync();
        if (_module is null)
            return new();

        try {
            return center ? await _module.InvokeAsync<ViewportPoint>("center", _viewport) : await _module.InvokeAsync<ViewportPoint>("origin", _viewport, clientX, clientY);
        }
        catch (JSException) {
            return new();
        }
    }

    private async Task EnsureModuleAsync()
    {
        if (_module is not null)
            return;

        _moduleTask ??= ImportModuleAsync();
        _module = await _moduleTask;
    }

    private async Task<IJSObjectReference?> ImportModuleAsync()
    {
        try {
            var path = Navigation.ToAbsoluteUri("_content/Lyo.Job.Web.Components/workflowGraphViewport.js");
            return await Js.InvokeAsync<IJSObjectReference>("import", path.ToString());
        }
        catch (JSException) {
            return null;
        }
        catch (InvalidOperationException) {
            return null;
        }
    }

    private static string ViewBox(DiagramPage page) => string.Create(CultureInfo.InvariantCulture, $"0 0 {page.PageWidth} {page.PageHeight}");

    private string NodeClass(DiagramCell node) => OnStepSelected.HasDelegate ? "workflow-graph-node-clickable" : "";

    private bool IsSelected(DiagramCell node) => TryStepId(node, out var id) && id == SelectedStepId;

    private Task SelectAsync(DiagramCell node)
    {
        if (_suppressClick) {
            _suppressClick = false;
            return Task.CompletedTask;
        }

        return TryStepId(node, out var id) ? OnStepSelected.InvokeAsync(id) : Task.CompletedTask;
    }

    /// <summary>A chip in the gap between the two steps, just off the stroke, so the condition reads as part of the arrow.</summary>
    private EdgeCaption Caption(DiagramCell edge)
    {
        var text = Fit(edge.Value, 36);
        var (x, y) = EdgeLabelAt(edge);
        var width = Math.Max(44, text.Length * 6.4 + 12);
        return new(x - width / 2, y - 16, width, text);
    }

    private static (double X, double Y) EdgeLabelAt(DiagramCell edge)
    {
        if (edge.Geometry?.SourcePoint is not { } source || edge.Geometry.TargetPoint is not { } target)
            return (0, 0);

        var x = (source.X + target.X) / 2;
        var y = (source.Y + target.Y) / 2;
        var vertical = Math.Abs(target.Y - source.Y) > Math.Abs(target.X - source.X);
        return vertical ? (x + 28, y) : (x, y - 14);
    }

    /// <summary>Keeps a label inside the node. The tooltip still carries the full text.</summary>
    private static string Fit(string? value, int max)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= max)
            return value ?? "";

        return string.Concat(value.AsSpan(0, max - 1), "…");
    }

    private Task OnNodeKeyDown(KeyboardEventArgs args, DiagramCell node) => args.Key is "Enter" or " " ? SelectAsync(node) : Task.CompletedTask;

    private static string Polyline(DiagramCell edge)
    {
        var geometry = edge.Geometry;
        if (geometry?.SourcePoint is null || geometry.TargetPoint is null)
            return "";

        var points = new List<DiagramPoint> { geometry.SourcePoint };
        points.AddRange(geometry.Points);
        points.Add(geometry.TargetPoint);
        return string.Join(' ', points.Select(point => string.Create(CultureInfo.InvariantCulture, $"{point.X},{point.Y}")));
    }

    /// <summary>A triangle at the target anchor, pointing into the step. Drawn as geometry so it does not need a marker URL.</summary>
    private static string Arrow(DiagramCell edge)
    {
        if (edge.Geometry?.TargetPoint is not { } tip)
            return "";

        var x = tip.X;
        var y = tip.Y;
        return string.Create(CultureInfo.InvariantCulture, $"{x},{y} {x - 8},{y - 4} {x - 8},{y + 4}");
    }

    private static double TextX(DiagramCell node) => node.Geometry!.X!.Value + node.Geometry.Width!.Value / 2;

    private static double TextY(DiagramCell node, double fraction) => node.Geometry!.Y!.Value + node.Geometry.Height!.Value * fraction;

    private string Stroke(DiagramCell node)
    {
        if (!TryStepId(node, out var id))
            return "var(--mud-palette-lines-default)";

        if (id == SelectedStepId)
            return "var(--mud-palette-primary)";

        if (!StepStates.TryGetValue(id, out var state))
            return "var(--mud-palette-lines-default)";

        return state switch {
            JobWorkflowStepState.Finished => "var(--mud-palette-success)",
            JobWorkflowStepState.Failed => "var(--mud-palette-error)",
            JobWorkflowStepState.Running => "var(--mud-palette-info)",
            JobWorkflowStepState.Skipped or JobWorkflowStepState.ConditionNotMet => "var(--mud-palette-warning)",
            var _ => "var(--mud-palette-lines-default)"
        };
    }

    private string Tooltip(DiagramCell node)
    {
        var step = StepOf(node);
        if (step is null)
            return node.Value ?? "";

        var definition = Definitions.FirstOrDefault(item => item.Id == step.JobDefinitionId)?.Name is { } name ? $"Runs {name}" : "No job definition picked";
        return $"{step.StepName}. {definition}. {RunsAfter(step)}. {RunsWhen(step)}";
    }

    private string RunsAfter(JobWorkflowStepDraft step)
    {
        if (step.DependsOn.Count == 0)
            return "Starts with the workflow";

        var names = step.DependsOn.Select(id => Steps.FirstOrDefault(item => item.Id == id)?.StepName).Where(name => !string.IsNullOrWhiteSpace(name));
        return $"After {string.Join(", ", names)}";
    }

    private static string RunsWhen(JobWorkflowStepDraft step) => step.ConditionText is { } text ? $"Only when {text}" : "Runs whenever its dependencies finish";

    /// <summary>The one line that fits on the node itself. The tooltip carries the dependency names and the condition text.</summary>
    private string GateSummary(DiagramCell node)
    {
        var step = StepOf(node);
        if (step is null)
            return "";

        if (step.ConditionText is not null)
            return $"if {step.ConditionText}";

        var count = step.DependsOn.Count;
        return count == 0 ? "Starts with the workflow" : $"After {count} {(count == 1 ? "step" : "steps")}";
    }

    private JobWorkflowStepDraft? StepOf(DiagramCell node) => TryStepId(node, out var id) ? Steps.FirstOrDefault(step => step.Id == id) : null;

    private static bool TryStepId(DiagramCell node, out Guid id) => Guid.TryParseExact(node.Id, "N", out id);

    private readonly record struct EdgeCaption(double X, double Y, double Width, string Text);

    private sealed class ViewportPoint
    {
        public double X { get; set; }

        public double Y { get; set; }
    }
}