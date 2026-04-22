using Lyo.Api.Client;
using Lyo.FileStorage.Web.Components.Services;
using Lyo.IO.Temp;
using Lyo.Keystore;
using Lyo.Web.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace Lyo.FileStorage.Web.Components;

public partial class FileStorageWorkbench : ComponentBase
{
    private List<string> _availableKeyIds = [];
    private Dictionary<string, List<string>> _availableKeyVersions = new(StringComparer.Ordinal);

    [Inject]
    public FileStorageWorkbenchServiceResolver Resolver { get; set; } = default!;

    [Inject]
    public IApiClient ApiClient { get; set; } = default!;

    [Inject]
    public IIOTempService TempService { get; set; } = default!;

    [Inject]
    public IJsInterop Js { get; set; } = default!;

    [Inject]
    public IJSRuntime JsRuntime { get; set; } = default!;

    [Inject]
    public NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    public ISnackbar Snackbar { get; set; } = default!;

    [Inject]
    public IDialogService DialogService { get; set; } = default!;

    /// <summary>API route segment for file metadata QueryProject (e.g. <c>Workbench/FileStorage/FileMetadata</c>).</summary>
    [Parameter]
    public string FileMetadataQueryRoute { get; set; } = "Workbench/FileStorage/FileMetadata";

    /// <summary>App-relative path for the host download proxy (e.g. gateway route segment before file id).</summary>
    [Parameter]
    public string ProxyDownloadPath { get; set; } = "filestorage-download";

    [Parameter]
    public string Title { get; set; } = "File Storage Workbench";

    [Parameter]
    public string Description { get; set; } =
        "Inspect the configured file storage and keystore implementations, save and retrieve files, rotate keys, and optionally browse metadata/key inventories through an API-backed query service.";

    public IFileStorageService? FileStorage { get; private set; }

    public IKeyStore? KeyStore { get; private set; }

    public IKeyInventoryStore? InventoryStore { get; private set; }

    public IFileStorageWorkbenchQueryService? QueryService { get; private set; }

    public LocalKeyStore? LocalKeyStore { get; private set; }

    public IReadOnlyList<string> AvailableKeyIds => _availableKeyIds;

    public string? FileStorageImplementationName => FileStorage?.GetType().Name;

    public string? KeyStoreImplementationName => KeyStore?.GetType().Name;

    public string DescribeFileStorageResolution() => Resolver.DescribeFileStorageResolution();

    public string DescribeKeyStoreResolution() => Resolver.DescribeKeyStoreResolution();

    public void SetStatus(string message, Severity severity) => Snackbar.Add(message, severity);

    public IReadOnlyList<string> GetKnownVersionsForKey(string? keyId)
    {
        if (string.IsNullOrWhiteSpace(keyId))
            return [];

        return _availableKeyVersions.TryGetValue(keyId, out var versions) ? versions : [];
    }

    public void RememberKnownKey(string? keyId, params string?[] versions)
    {
        if (string.IsNullOrWhiteSpace(keyId))
            return;

        if (!_availableKeyIds.Contains(keyId, StringComparer.Ordinal)) {
            _availableKeyIds.Add(keyId);
            _availableKeyIds = _availableKeyIds.OrderBy(value => value).ToList();
        }

        if (!_availableKeyVersions.TryGetValue(keyId, out var knownVersions)) {
            knownVersions = [];
            _availableKeyVersions[keyId] = knownVersions;
        }

        foreach (var version in versions) {
            if (!string.IsNullOrWhiteSpace(version) && !knownVersions.Contains(version, StringComparer.Ordinal))
                knownVersions.Add(version);
        }

        knownVersions.Sort(StringComparer.Ordinal);
        _ = InvokeAsync(StateHasChanged);
    }

    protected override async Task OnInitializedAsync()
    {
        FileStorage = Resolver.TryGetFileStorageService();
        KeyStore = Resolver.TryGetKeyStore();
        InventoryStore = KeyStore as IKeyInventoryStore;
        QueryService = Resolver.TryGetQueryService();
        LocalKeyStore = KeyStore as LocalKeyStore;
        await RefreshKeyInventoryAsync();
    }

    public async Task RefreshKeyInventoryAsync()
    {
        if (InventoryStore == null) {
            _availableKeyIds = [];
            _availableKeyVersions = new(StringComparer.Ordinal);
            await InvokeAsync(StateHasChanged);
            return;
        }

        var keyIds = (await InventoryStore.GetAvailableKeyIdsAsync()).Distinct(StringComparer.Ordinal).OrderBy(keyId => keyId).ToList();
        var versionsByKey = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var keyId in keyIds) {
            var versions = (await InventoryStore.GetAvailableVersionsAsync(keyId)).Distinct(StringComparer.Ordinal).OrderBy(version => version).ToList();
            versionsByKey[keyId] = versions;
        }

        _availableKeyIds = keyIds;
        _availableKeyVersions = versionsByKey;
        await InvokeAsync(StateHasChanged);
    }
}