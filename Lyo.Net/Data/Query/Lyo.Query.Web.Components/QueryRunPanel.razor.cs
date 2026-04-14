using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Lyo.Api.Models.Common;
using Lyo.Api.Models.Common.Request;
using Lyo.Query.Models.Common.Request;
using Lyo.Web.Components.JsonEditor;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Lyo.Query.Web.Components;

public partial class QueryRunPanel : IAsyncDisposable
{
    private const string SplitterModuleUrl = "/_content/Lyo.Query.Web.Components/scripts/queryWorkbenchSplitter.js";

    private sealed class HostEndpointRow
    {
        public string Host { get; set; } = "";

        public string EndpointsCsv { get; set; } = "";
    }

    private string? _error;
    private bool _isResizing;
    private long? _lastResponseElapsedMs;
    private int? _lastResponseSizeBytes;
    private bool _loading;
    private string? _requestEditorParseError;
    private int _requestEditorRenderKey;
    private string _requestEditorSearchText = string.Empty;
    private JsonEditorViewMode _responseEditorDefaultViewMode = JsonEditorViewMode.Tree;
    private string? _responseEditorParseError;
    private int _responseEditorRenderKey;
    private string _responseEditorSearchText = string.Empty;
    private JsonEditorViewMode _responseEditorViewMode = JsonEditorViewMode.Tree;
    private JsonElement _resultJson;
    private string? _resultRawText;
    private ElementReference _splitContainerRef;
    private IJSObjectReference? _splitterModule;
    private List<HostEndpointRow> _hostRows = [];
    private int _lastRestoreKey = int.MinValue;

    [Inject]
    private IHttpClientFactory HttpClientFactory { get; set; } = null!;

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = null!;

    [Inject]
    private JsonSerializerOptions JsonOptions { get; set; } = null!;

    [Parameter]
    public QueryReq EntityRequest { get; set; } = new();

    [Parameter]
    public EventCallback<QueryReq> EntityRequestChanged { get; set; }

    [Parameter]
    public ProjectionQueryReq ProjectionRequest { get; set; } = new();

    [Parameter]
    public EventCallback<ProjectionQueryReq> ProjectionRequestChanged { get; set; }

    [Parameter]
    public QueryWorkbenchRunConfiguration Run { get; set; } = new();

    [Parameter]
    public EventCallback<QueryWorkbenchRunConfiguration> RunChanged { get; set; }

    /// <summary>Increment when restoring persisted state so host rows rehydrate without fighting in-progress edits.</summary>
    [Parameter]
    public int RunRestoreKey { get; set; }

    private string QuerySplitCssVariables => string.Create(CultureInfo.InvariantCulture, $"--qrb-split:{Run.LeftPanePercent:F2}%;");

    public async ValueTask DisposeAsync()
    {
        if (_splitterModule == null)
            return;

        try {
            await _splitterModule.DisposeAsync();
        }
        catch (JSDisconnectedException) {
            // Ignore disposal failures when the circuit is already gone.
        }
    }

    protected override void OnParametersSet()
    {
        if (_lastRestoreKey != RunRestoreKey) {
            _lastRestoreKey = RunRestoreKey;
            _hostRows = HostRowsFrom(Run.HostEndpoints);
            _requestEditorRenderKey++;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
            _splitterModule = await JsRuntime.InvokeAsync<IJSObjectReference>("import", SplitterModuleUrl);
    }

    private static List<HostEndpointRow> HostRowsFrom(Dictionary<string, List<string>> dict)
        => dict.Count == 0
            ? []
            : dict.Select(kvp => new HostEndpointRow { Host = kvp.Key, EndpointsCsv = string.Join(", ", kvp.Value) }).ToList();

    private async Task NotifyRunChangedAsync(QueryWorkbenchRunConfiguration next)
    {
        Run = next;
        await RunChanged.InvokeAsync(next);
    }

    private string GetRequestUri()
    {
        var baseUrl = string.IsNullOrWhiteSpace(Run.SelectedHost) ? "" : QueryWorkbenchHostNormalization.NormalizeBaseUrl(Run.SelectedHost).TrimEnd('/');
        var route = (Run.Route ?? "").Trim();
        if (string.IsNullOrEmpty(baseUrl) || string.IsNullOrEmpty(route))
            return "(not configured)";

        return $"{baseUrl}/{route}/{GetEndpointSegment()}";
    }

    private async Task RunQuery()
    {
        _error = null;
        _resultJson = default;
        _resultRawText = null;
        _loading = true;
        _lastResponseElapsedMs = null;
        _lastResponseSizeBytes = null;
        try {
            var stopwatch = Stopwatch.StartNew();
            var baseUrl = string.IsNullOrWhiteSpace(Run.SelectedHost) ? "" : QueryWorkbenchHostNormalization.NormalizeBaseUrl(Run.SelectedHost).TrimEnd('/');
            var route = (Run.Route ?? "").Trim();
            if (string.IsNullOrEmpty(baseUrl) || string.IsNullOrEmpty(route)) {
                _error = "Add an API host and route (expand “API targets” below, or configure the gateway workbench defaults).";
                _loading = false;
                return;
            }

            var pathRoute = route + "/" + GetEndpointSegment();
            var uri = $"{baseUrl}/{pathRoute}";
            var httpClient = HttpClientFactory.CreateClient();
            var json = JsonSerializer.Serialize(
                Run.RunMode == QueryWorkbenchRunMode.Query ? (object)EntityRequest : ProjectionRequest,
                JsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(uri, content);
            var responseJson = await response.Content.ReadAsStringAsync();
            stopwatch.Stop();
            _lastResponseElapsedMs = stopwatch.ElapsedMilliseconds;
            _lastResponseSizeBytes = Encoding.UTF8.GetByteCount(responseJson ?? "");
            if (!response.IsSuccessStatusCode) {
                _error = $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}";
                if (!string.IsNullOrWhiteSpace(responseJson) && responseJson.TrimStart().StartsWith('{')) {
                    try {
                        using var doc = JsonDocument.Parse(responseJson);
                        var root = doc.RootElement;
                        if (root.TryGetProperty("detail", out var detailProp))
                            _error += $" — {detailProp.GetString()}";
                        else if (root.TryGetProperty("Detail", out var detailProp2))
                            _error += $" — {detailProp2.GetString()}";

                        _resultJson = root.Clone();
                    }
                    catch {
                        _error += " — Response was not valid JSON.";
                        _resultRawText = responseJson;
                    }
                }
                else if (!string.IsNullOrWhiteSpace(responseJson)) {
                    _error += $" — {TruncateForDisplay(responseJson, 200)}";
                    _resultRawText = responseJson;
                }
            }
            else if (!string.IsNullOrWhiteSpace(responseJson)) {
                try {
                    using var doc = JsonDocument.Parse(responseJson);
                    _resultJson = doc.RootElement.Clone();
                }
                catch (JsonException ex) {
                    _error = $"Response was not valid JSON: {ex.Message}";
                    _resultRawText = responseJson;
                }
            }
        }
        catch (Exception ex) {
            _error = SafeError(ex.Message);
            if (ex.InnerException != null)
                _error += " — " + SafeError(ex.InnerException.Message);
        }
        finally {
            _loading = false;
        }
    }

    private async Task OnQueryRunModeChanged(QueryWorkbenchRunMode mode)
    {
        if (Run.RunMode == mode)
            return;

        await NotifyRunChangedAsync(Run with { RunMode = mode });
    }

    private void ClearResponse()
    {
        _resultJson = default;
        _resultRawText = null;
        _lastResponseElapsedMs = null;
        _lastResponseSizeBytes = null;
        _responseEditorParseError = null;
        _responseEditorSearchText = string.Empty;
    }

    private async Task OnEntityJsonChanged(QueryReq? request)
    {
        if (request != null)
            await EntityRequestChanged.InvokeAsync(request);
    }

    private async Task OnProjectionJsonChanged(ProjectionQueryReq? request)
    {
        if (request != null) {
            await ProjectionRequestChanged.InvokeAsync(request);
            if ((request.Select?.Count ?? 0) > 0 && Run.RunMode == QueryWorkbenchRunMode.Query)
                await NotifyRunChangedAsync(Run with { RunMode = QueryWorkbenchRunMode.QueryProject });
        }
    }

    private Task OnRequestEditorViewModeChanged(JsonEditorViewMode mode)
    {
        _requestEditorRenderKey++;
        return NotifyRunChangedAsync(Run with { RequestEditorViewMode = mode });
    }

    private Task OnRequestEditorParseErrorChanged(string? error)
    {
        _requestEditorParseError = error;
        return Task.CompletedTask;
    }

    private Task OnRequestEditorSearchTextChanged(string value)
    {
        _requestEditorSearchText = value;
        return Task.CompletedTask;
    }

    private Task OnResponseEditorDefaultViewModeChanged(JsonEditorViewMode mode)
    {
        _responseEditorDefaultViewMode = mode;
        _responseEditorViewMode = mode;
        _responseEditorRenderKey++;
        return Task.CompletedTask;
    }

    private Task OnResponseEditorViewModeChanged(JsonEditorViewMode mode)
    {
        _responseEditorViewMode = mode;
        return Task.CompletedTask;
    }

    private Task OnResponseEditorParseErrorChanged(string? error)
    {
        _responseEditorParseError = error;
        return Task.CompletedTask;
    }

    private Task OnResponseEditorSearchTextChanged(string value)
    {
        _responseEditorSearchText = value;
        return Task.CompletedTask;
    }

    private static string SafeError(string? s)
    {
        if (string.IsNullOrEmpty(s))
            return "";

        return new(s.Where(c => (c >= 32 && c != 0x7F) || c == '\t' || c == '\n' || c == '\r').ToArray());
    }

    private static string TruncateForDisplay(string s, int maxLen)
    {
        if (string.IsNullOrEmpty(s))
            return "";

        var trimmed = s.Trim();
        if (trimmed.Length <= maxLen)
            return trimmed;

        return trimmed[..maxLen] + "...";
    }

    private void StartSplitResize(MouseEventArgs _) => _isResizing = true;

    private void StopSplitResize(MouseEventArgs _) => _isResizing = false;

    private async Task OnSplitMouseMove(MouseEventArgs e)
    {
        if (!_isResizing)
            return;

        _splitterModule ??= await JsRuntime.InvokeAsync<IJSObjectReference>("import", SplitterModuleUrl);
        var percent = await _splitterModule.InvokeAsync<double>("getSplitPercentAdaptive", _splitContainerRef, e.ClientX, e.ClientY);
        var clamped = Math.Clamp(percent, 25d, 75d);
        await NotifyRunChangedAsync(Run with { LeftPanePercent = clamped });
    }

    private int GetRequestSizeBytes()
    {
        try {
            var payload = Run.RunMode == QueryWorkbenchRunMode.Query ? (object)EntityRequest : ProjectionRequest;
            return Encoding.UTF8.GetByteCount(JsonSerializer.Serialize(payload, JsonOptions));
        }
        catch {
            return 0;
        }
    }

    private static string GetSizeLabel(int bytes)
    {
        if (bytes < 1024)
            return $"{bytes} B";

        if (bytes < 1024 * 1024)
            return $"{bytes / 1024d:F1} KB";

        return $"{bytes / (1024d * 1024d):F2} MB";
    }

    private QueryRequestScoreBreakdown GetCurrentScoreBreakdown()
    {
        try {
            return Run.RunMode == QueryWorkbenchRunMode.Query
                ? QueryRequestScorer.ScoreDetailed(EntityRequest)
                : QueryRequestScorer.ScoreDetailed(ProjectionRequest);
        }
        catch {
            return QueryRequestScoreBreakdown.Empty();
        }
    }

    private string GetEndpointSegment() => Run.RunMode == QueryWorkbenchRunMode.QueryProject ? "QueryProject" : "Query";

    private async Task OnSelectedHostChanged(string? host)
    {
        host = string.IsNullOrWhiteSpace(host) ? null : QueryWorkbenchHostNormalization.NormalizeBaseUrl(host);
        if (host == null || Run.HostEndpoints.Count == 0) {
            await NotifyRunChangedAsync(Run with { SelectedHost = host });
            return;
        }

        var match = Run.HostEndpoints.Keys.FirstOrDefault(k => string.Equals(QueryWorkbenchHostNormalization.NormalizeBaseUrl(k), host, StringComparison.OrdinalIgnoreCase));
        if (match == null) {
            await NotifyRunChangedAsync(Run with { SelectedHost = host });
            return;
        }

        var routes = Run.HostEndpoints[match];
        var route = Run.Route;
        if (routes is not { Count: > 0 } || !routes.Contains(route, StringComparer.OrdinalIgnoreCase))
            route = routes[0];

        await NotifyRunChangedAsync(Run with { SelectedHost = match, Route = route });
    }

    private Task OnRouteChanged(string route) => NotifyRunChangedAsync(Run with { Route = route.Trim() });

    private IEnumerable<string> RoutesForSelectedHost()
    {
        if (Run.HostEndpoints.Count == 0 || string.IsNullOrWhiteSpace(Run.SelectedHost))
            return [];

        foreach (var kvp in Run.HostEndpoints) {
            if (string.Equals(QueryWorkbenchHostNormalization.NormalizeBaseUrl(kvp.Key), QueryWorkbenchHostNormalization.NormalizeBaseUrl(Run.SelectedHost!), StringComparison.OrdinalIgnoreCase))
                return kvp.Value is { Count: > 0 } ? kvp.Value : [];
        }

        return [];
    }

    private async Task AddHostRow()
    {
        _hostRows.Add(new HostEndpointRow());
        await CommitHostRowsAsync();
    }

    private async Task RemoveHostRow(HostEndpointRow row)
    {
        _hostRows.Remove(row);
        await CommitHostRowsAsync();
    }

    private void OnHostRowFocusOut()
        => _ = CommitHostRowsAsync();

    private async Task CommitHostRowsAsync()
    {
        var dict = BuildDictFromRows(_hostRows);
        string? newSelected = Run.SelectedHost != null ? QueryWorkbenchHostNormalization.NormalizeBaseUrl(Run.SelectedHost) : null;
        if (dict.Count > 0) {
            if (newSelected == null
                || !dict.Keys.Any(k => string.Equals(QueryWorkbenchHostNormalization.NormalizeBaseUrl(k), newSelected, StringComparison.OrdinalIgnoreCase)))
                newSelected = QueryWorkbenchHostNormalization.NormalizeBaseUrl(dict.Keys.First());
        }
        else {
            newSelected = null;
        }

        var route = Run.Route;
        if (newSelected != null && dict.Count > 0) {
            var hostEntry = dict.First(kvp => string.Equals(QueryWorkbenchHostNormalization.NormalizeBaseUrl(kvp.Key), newSelected, StringComparison.OrdinalIgnoreCase));
            var routes = hostEntry.Value;
            if (routes is { Count: > 0 } && !routes.Contains(route, StringComparer.OrdinalIgnoreCase))
                route = routes[0];
        }

        await NotifyRunChangedAsync(Run with { HostEndpoints = dict, SelectedHost = newSelected, Route = route });
    }

    private static Dictionary<string, List<string>> BuildDictFromRows(IEnumerable<HostEndpointRow> rows)
    {
        var d = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows) {
            var host = QueryWorkbenchHostNormalization.NormalizeBaseUrl(row.Host);
            if (string.IsNullOrWhiteSpace(host))
                continue;

            var parts = (row.EndpointsCsv ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(static s => s.Length > 0)
                .ToList();

            if (parts.Count == 0)
                continue;

            d[host] = parts;
        }

        return d;
    }

}
