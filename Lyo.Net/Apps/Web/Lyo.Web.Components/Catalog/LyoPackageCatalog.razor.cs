using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace Lyo.Web.Components.Catalog;

/// <summary>
/// Browse shell over the pre-rendered <c>wwwroot/catalog</c> assets: search, area facets, package docs, and a dependency graph. <see cref="PackageDoc" /> already
/// draws one package; this is the missing index.
/// </summary>
public partial class LyoPackageCatalog
{
    /// <summary>Base path of the catalog static assets. Default is the RCL content root.</summary>
    [Parameter]
    public string CatalogBasePath { get; set; } = "_content/Lyo.Web.Components/catalog";

    [Inject]
    private HttpClient Http { get; set; } = null!;

    [Inject]
    private ILogger<LyoPackageCatalog> Logger { get; set; } = null!;

    private CatalogIndex? _index;
    private List<CatalogIndexPackage> _filtered = [];
    private CatalogIndexPackage? _selected;
    private string? _search;
    private string? _area;
    private bool _loading = true;
    private string? _error;

    private IReadOnlyList<string> Areas
        => _index?.Packages.Select(package => package.Area).Where(area => !string.IsNullOrWhiteSpace(area)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(area => area, StringComparer.OrdinalIgnoreCase).ToArray()
            ?? [];

    private string HeaderDescription => _index is null ? "Loading catalog…" : $"{_index.PackageCount} packages generated {(_index.GeneratedAt?.ToLocalTime().ToString("g") ?? "recently")}.";

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        try {
            _index = await Http.GetFromJsonAsync<CatalogIndex>($"{CatalogBasePath.TrimEnd('/')}/index.json", CatalogJson.Options);
            ApplyFilter();
        }
        catch (Exception ex) {
            Logger.LogWarning(ex, "Failed to load package catalog index");
            _error = "Could not load the package catalog.";
        }
        finally {
            _loading = false;
        }
    }

    private void SetArea(string? area)
    {
        _area = area;
        ApplyFilter();
    }

    private void Select(CatalogIndexPackage package) => _selected = package;

    private void ApplyFilter()
    {
        IEnumerable<CatalogIndexPackage> rows = _index?.Packages ?? [];
        if (!string.IsNullOrWhiteSpace(_area))
            rows = rows.Where(package => string.Equals(package.Area, _area, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(_search)) {
            var term = _search.Trim();
            rows = rows.Where(package =>
                package.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                || package.Id.Contains(term, StringComparison.OrdinalIgnoreCase)
                || package.Tagline.Contains(term, StringComparison.OrdinalIgnoreCase)
                || package.Topic.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        _filtered = rows.OrderBy(package => package.Name, StringComparer.OrdinalIgnoreCase).ToList();
        if (_selected is not null && _filtered.All(package => package.Id != _selected.Id))
            _selected = null;
    }
}
