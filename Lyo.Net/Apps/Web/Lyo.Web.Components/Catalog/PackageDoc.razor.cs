using System.Net.Http.Json;
using Lyo.Exceptions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace Lyo.Web.Components.Catalog;

public partial class PackageDoc
{
    /// <summary>Package id (for example <c>Lyo.Cache</c>). Loads <c>_content/Lyo.Web.Components/catalog/packages/{id}.json</c>.</summary>
    [Parameter][EditorRequired]
    public string PackageId { get; set; } = "";

    /// <summary>Optional base path override. Default uses RCL static assets: <c>_content/Lyo.Web.Components/catalog</c>.</summary>
    [Parameter]
    public string CatalogBasePath { get; set; } = "_content/Lyo.Web.Components/catalog";

    private CatalogPackageDoc? _doc;
    private bool _loading = true;
    private string? _error;

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        _loading = true;
        _error = null;
        _doc = null;
        try {
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(PackageId);
            var url = $"{CatalogBasePath.TrimEnd('/')}/packages/{Uri.EscapeDataString(PackageId)}.json";
            _doc = await Http.GetFromJsonAsync<CatalogPackageDoc>(url, CatalogJson.Options);
        }
        catch (Exception ex) {
            Logger.LogWarning(ex, "Failed to load catalog package {PackageId}", PackageId);
            _error = $"Could not load docs for {PackageId}.";
        }
        finally {
            _loading = false;
        }
    }
}
