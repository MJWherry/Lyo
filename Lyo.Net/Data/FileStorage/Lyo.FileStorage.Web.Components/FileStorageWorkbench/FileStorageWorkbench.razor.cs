using System.Linq;
using Lyo.Api.Client;
using Lyo.Web.Components;
using Lyo.FileStorage;
using Lyo.FileStorage.Web.Components.Services;
using Lyo.IO.Temp;
using Lyo.Keystore;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace Lyo.FileStorage.Web.Components;

public partial class FileStorageWorkbench : ComponentBase
{
    [Inject] public FileStorageWorkbenchServiceResolver Resolver { get; set; } = default!;
    [Inject] public IApiClient ApiClient { get; set; } = default!;
    [Inject] public IIOTempService TempService { get; set; } = default!;
    [Inject] public IJsInterop Js { get; set; } = default!;
    [Inject] public IJSRuntime JsRuntime { get; set; } = default!;
    [Inject] public NavigationManager NavigationManager { get; set; } = default!;
    [Inject] public ISnackbar Snackbar { get; set; } = default!;
    [Inject] public IDialogService DialogService { get; set; } = default!;

    /// <summary>API route segment for file metadata QueryProject (e.g. <c>Workbench/FileStorage/FileMetadata</c>).</summary>
    [Parameter]
    public string FileMetadataQueryRoute { get; set; } = "Workbench/FileStorage/FileMetadata";

    /// <summary>App-relative path for the host download proxy (e.g. gateway route segment before file id).</summary>
    [Parameter]
    public string ProxyDownloadPath { get; set; } = "filestorage-download";

    [Parameter] public string Title { get; set; } = "File Storage Workbench";

    [Parameter] public string Description { get; set; } =
        "Inspect the configured file storage and keystore implementations, save and retrieve files, rotate keys, and optionally browse metadata/key inventories through an API-backed query service.";

    private IFileStorageService? _fileStorage;
    private IKeyStore? _keyStore;
    private IKeyInventoryStore? _inventoryStore;
    private IFileStorageWorkbenchQueryService? _queryService;
    private LocalKeyStore? _localKeyStore;

    private List<string> _availableKeyIds = [];
    private Dictionary<string, List<string>> _availableKeyVersions = new(StringComparer.Ordinal);

    public IFileStorageService? FileStorage => _fileStorage;

    public IKeyStore? KeyStore => _keyStore;

    public IKeyInventoryStore? InventoryStore => _inventoryStore;

    public IFileStorageWorkbenchQueryService? QueryService => _queryService;

    public LocalKeyStore? LocalKeyStore => _localKeyStore;

    public IReadOnlyList<string> AvailableKeyIds => _availableKeyIds;

    public string? FileStorageImplementationName => _fileStorage?.GetType().Name;

    public string? KeyStoreImplementationName => _keyStore?.GetType().Name;

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
        _fileStorage = Resolver.TryGetFileStorageService();
        _keyStore = Resolver.TryGetKeyStore();
        _inventoryStore = _keyStore as IKeyInventoryStore;
        _queryService = Resolver.TryGetQueryService();
        _localKeyStore = _keyStore as LocalKeyStore;
        await RefreshKeyInventoryAsync();
    }

    public async Task RefreshKeyInventoryAsync()
    {
        if (_inventoryStore == null) {
            _availableKeyIds = [];
            _availableKeyVersions = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            await InvokeAsync(StateHasChanged);
            return;
        }

        var keyIds = (await _inventoryStore.GetAvailableKeyIdsAsync()).Distinct(StringComparer.Ordinal).OrderBy(keyId => keyId).ToList();
        var versionsByKey = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var keyId in keyIds) {
            var versions = (await _inventoryStore.GetAvailableVersionsAsync(keyId)).Distinct(StringComparer.Ordinal).OrderBy(version => version).ToList();
            versionsByKey[keyId] = versions;
        }

        _availableKeyIds = keyIds;
        _availableKeyVersions = versionsByKey;
        await InvokeAsync(StateHasChanged);
    }
}
