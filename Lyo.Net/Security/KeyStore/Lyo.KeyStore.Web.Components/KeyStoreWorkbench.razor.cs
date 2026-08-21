using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;

namespace Lyo.KeyStore.Web.Components;

/// <summary>In-process keystore inspect / add / rotate / set-current. Does not display key bytes.</summary>
public partial class KeyStoreWorkbench : ComponentBase
{
    private readonly List<string> _keyIds = [];
    private readonly List<string> _versions = [];
    private bool _busy;
    private string? _currentVersion;
    private string _editKeyId = "";
    private string _editKeyString = "";
    private string _editVersion = "";
    private IKeyInventoryStore? _inventory;
    private string _inspectVersion = "";
    private KeyMetadata? _metadata;
    private string _selectedKeyId = "";

    [Inject]
    public IServiceProvider Services { get; set; } = null!;

    [Inject]
    public ISnackbar Snackbar { get; set; } = null!;

    private IKeyStore? KeyStore { get; set; }

    protected override async Task OnInitializedAsync()
    {
        KeyStore = Services.GetService<IKeyStore>();
        _inventory = KeyStore as IKeyInventoryStore;
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        _keyIds.Clear();
        if (_inventory == null || KeyStore == null)
            return;

        _busy = true;
        try {
            _keyIds.AddRange(await _inventory.GetAvailableKeyIdsAsync());
            if (!string.IsNullOrEmpty(_selectedKeyId))
                await LoadSelectedAsync();
        }
        catch (Exception ex) {
            Snackbar.Add(ex.Message, Severity.Error);
        }
        finally {
            _busy = false;
        }
    }

    private Task SelectKeyAsync(string keyId)
    {
        _selectedKeyId = keyId;
        _editKeyId = keyId;
        return LoadSelectedAsync();
    }

    private async Task LoadSelectedAsync()
    {
        if (KeyStore == null || string.IsNullOrEmpty(_selectedKeyId))
            return;

        _versions.Clear();
        _currentVersion = await KeyStore.GetCurrentVersionAsync(_selectedKeyId);
        if (_inventory != null)
            _versions.AddRange(await _inventory.GetAvailableVersionsAsync(_selectedKeyId));

        _inspectVersion = _currentVersion ?? _versions.FirstOrDefault() ?? "";
        await LoadMetadataAsync();
    }

    private Task OnInspectVersionChanged(string? value)
    {
        _inspectVersion = value ?? "";
        return LoadMetadataAsync();
    }

    private async Task LoadMetadataAsync()
    {
        _metadata = null;
        if (KeyStore == null || string.IsNullOrEmpty(_selectedKeyId) || string.IsNullOrWhiteSpace(_inspectVersion))
            return;

        _metadata = await KeyStore.GetKeyMetadataAsync(_selectedKeyId, _inspectVersion);
    }

    private async Task AddAsync()
    {
        if (KeyStore == null)
            return;

        if (string.IsNullOrWhiteSpace(_editKeyId) || string.IsNullOrWhiteSpace(_editVersion) || string.IsNullOrWhiteSpace(_editKeyString)) {
            Snackbar.Add("Key id, version, and key string are required to add.", Severity.Warning);
            return;
        }

        _busy = true;
        try {
            await KeyStore.AddKeyFromStringAsync(_editKeyId.Trim(), _editVersion.Trim(), _editKeyString);
            _editKeyString = "";
            _selectedKeyId = _editKeyId.Trim();
            Snackbar.Add($"Added {_selectedKeyId} version {_editVersion.Trim()}.", Severity.Success);
            await RefreshAsync();
        }
        catch (Exception ex) {
            Snackbar.Add(ex.Message, Severity.Error);
        }
        finally {
            _busy = false;
        }
    }

    private async Task RotateAsync()
    {
        if (KeyStore == null)
            return;

        if (string.IsNullOrWhiteSpace(_editKeyId) || string.IsNullOrWhiteSpace(_editKeyString)) {
            Snackbar.Add("Key id and key string are required to rotate.", Severity.Warning);
            return;
        }

        _busy = true;
        try {
            var version = await KeyStore.UpdateKeyFromStringAsync(_editKeyId.Trim(), _editKeyString);
            _editKeyString = "";
            _selectedKeyId = _editKeyId.Trim();
            Snackbar.Add($"Rotated {_selectedKeyId} to version {version}.", Severity.Success);
            await RefreshAsync();
        }
        catch (Exception ex) {
            Snackbar.Add(ex.Message, Severity.Error);
        }
        finally {
            _busy = false;
        }
    }

    private async Task SetCurrentAsync()
    {
        if (KeyStore == null || string.IsNullOrEmpty(_selectedKeyId) || string.IsNullOrWhiteSpace(_inspectVersion))
            return;

        _busy = true;
        try {
            await KeyStore.SetCurrentVersionAsync(_selectedKeyId, _inspectVersion);
            Snackbar.Add($"Current version for {_selectedKeyId} is {_inspectVersion}.", Severity.Success);
            await LoadSelectedAsync();
        }
        catch (Exception ex) {
            Snackbar.Add(ex.Message, Severity.Error);
        }
        finally {
            _busy = false;
        }
    }
}
