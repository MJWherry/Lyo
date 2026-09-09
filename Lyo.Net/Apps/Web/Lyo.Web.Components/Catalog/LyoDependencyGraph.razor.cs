using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace Lyo.Web.Components.Catalog;

/// <summary>
/// Direct dependencies of one catalog package, grouped by kind. Loads the same JSON <see cref="PackageDoc" /> uses so the graph stays aligned with the docs.
/// </summary>
public partial class LyoDependencyGraph
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
    private ILogger<LyoDependencyGraph> Logger { get; set; } = null!;

    private CatalogPackageDoc? _doc;
    private bool _loading = true;
    private string? _error;
    private string? _loadedId;

    private IEnumerable<IGrouping<string, CatalogDependency>> Grouped
        => (_doc?.Dependencies ?? []).GroupBy(dependency => string.IsNullOrWhiteSpace(dependency.Kind) ? "other" : dependency.Kind);

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        if (string.Equals(_loadedId, PackageId, StringComparison.Ordinal) && _doc is not null)
            return;

        _loading = true;
        _error = null;
        _doc = null;
        try {
            var url = $"{CatalogBasePath.TrimEnd('/')}/packages/{Uri.EscapeDataString(PackageId)}.json";
            _doc = await Http.GetFromJsonAsync<CatalogPackageDoc>(url, CatalogJson.Options);
            _loadedId = PackageId;
        }
        catch (Exception ex) {
            Logger.LogWarning(ex, "Failed to load catalog dependencies for {PackageId}", PackageId);
            _error = $"Could not load dependencies for {PackageId}.";
        }
        finally {
            _loading = false;
        }
    }

    private static string ChipLabel(CatalogDependency dependency)
        => string.IsNullOrWhiteSpace(dependency.Version) ? dependency.Name : $"{dependency.Name} {dependency.Version}";
}
