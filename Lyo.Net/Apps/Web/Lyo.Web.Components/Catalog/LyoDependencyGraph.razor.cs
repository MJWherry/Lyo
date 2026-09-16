using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace Lyo.Web.Components.Catalog;

/// <summary>
/// Direct dependencies of one catalog package, grouped by kind. Loads the same JSON <see cref="PackageDoc" /> uses so the graph stays aligned with the docs.
/// </summary>
public partial class LyoDependencyGraph : IDisposable
{
    /// <summary>Package id whose dependencies to display.</summary>
    [Parameter]
    [EditorRequired]
    public string PackageId { get; set; } = "";

    /// <summary>Root path of the catalog static assets.</summary>
    [Parameter]
    public string CatalogBasePath { get; set; } = "_content/Lyo.Web.Components/catalog";

    /// <summary>Optional index, reserved so a later view can colour nodes by area without another fetch.</summary>
    [Parameter]
    public CatalogIndex? Index { get; set; }

    [Inject]
    private HttpClient Http { get; set; } = null!;

    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    [Inject]
    private ILogger<LyoDependencyGraph> Logger { get; set; } = null!;

    private readonly CatalogLoadGate _gate = new();

    private CatalogPackageDoc? _doc;
    private bool _loading = true;
    private string? _error;

    private IEnumerable<IGrouping<string, CatalogDependency>> Grouped
        => (_doc?.Dependencies ?? []).GroupBy(dependency => string.IsNullOrWhiteSpace(dependency.Kind) ? "other" : dependency.Kind);

    /// <inheritdoc />
    protected override Task OnParametersSetAsync()
    {
        var key = CatalogLoadGate.Key(CatalogBasePath, PackageId);
        if (_gate.IsCurrentRequest(key))
            return Task.CompletedTask;

        return LoadAsync(key);
    }

    /// <inheritdoc />
    public void Dispose() => _gate.Dispose();

    private async Task LoadAsync(string key)
    {
        var (requestId, ct) = _gate.Begin(key);
        var packageId = PackageId;
        _loading = true;
        _error = null;
        _doc = null;
        try {
            var url = CatalogAssetUri.Resolve(Navigation, $"{CatalogBasePath.TrimEnd('/')}/packages/{Uri.EscapeDataString(packageId)}.json");
            var doc = await Http.GetFromJsonAsync<CatalogPackageDoc>(url, CatalogJson.Options, ct);
            if (!_gate.IsCurrent(requestId))
                return;

            _doc = doc;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) {
            return;
        }
        catch (Exception ex) {
            if (!_gate.IsCurrent(requestId))
                return;

            Logger.LogWarning(ex, "Failed to load catalog dependencies for {PackageId}", packageId);
            _error = $"Could not load dependencies for {packageId}.";
        }
        finally {
            if (_gate.IsCurrent(requestId))
                _loading = false;
        }
    }

    private static string ChipLabel(CatalogDependency dependency)
        => string.IsNullOrWhiteSpace(dependency.Version) ? dependency.Name : $"{dependency.Name} {dependency.Version}";
}
