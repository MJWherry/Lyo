using System.Net.Http.Json;
using Lyo.Exceptions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace Lyo.Web.Components.Catalog;

public partial class PackageDoc : IDisposable
{
    private readonly CatalogLoadGate _gate = new();

    private CatalogPackageDoc? _doc;
    private string? _error;
    private bool _loading = true;

    /// <summary>Package id (for example <c>Lyo.Cache</c>). Loads <c>_content/Lyo.Web.Components/catalog/packages/{id}.json</c>.</summary>
    [Parameter]
    [EditorRequired]
    public string PackageId { get; set; } = "";

    /// <summary>Optional base path override. Default uses RCL static assets: <c>_content/Lyo.Web.Components/catalog</c>.</summary>
    [Parameter]
    public string CatalogBasePath { get; set; } = "_content/Lyo.Web.Components/catalog";

    /// <inheritdoc />
    public void Dispose() => _gate.Dispose();

    /// <inheritdoc />
    protected override Task OnParametersSetAsync()
    {
        var key = CatalogLoadGate.Key(CatalogBasePath, PackageId);
        if (_gate.IsCurrentRequest(key))
            return Task.CompletedTask;

        return LoadAsync(key);
    }

    private async Task LoadAsync(string key)
    {
        var (requestId, ct) = _gate.Begin(key);
        var packageId = PackageId;
        _loading = true;
        _error = null;
        _doc = null;
        try {
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(packageId);
            var url = CatalogAssetUri.Resolve(Navigation, $"{CatalogBasePath.TrimEnd('/')}/packages/{Uri.EscapeDataString(packageId)}.json");
            var doc = await Http.GetFromJsonAsync<CatalogPackageDoc>(url, CatalogJson.Options, ct);
            if (!_gate.IsCurrent(requestId))
                return;

            _doc = doc;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (Exception ex) {
            if (!_gate.IsCurrent(requestId))
                return;

            Logger.LogWarning(ex, "Failed to load catalog package {PackageId}", packageId);
            _error = $"Could not load docs for {packageId}.";
        }
        finally {
            if (_gate.IsCurrent(requestId))
                _loading = false;
        }
    }
}