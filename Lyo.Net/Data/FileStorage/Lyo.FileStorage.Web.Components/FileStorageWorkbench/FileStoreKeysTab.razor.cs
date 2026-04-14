using System.Linq;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Lyo.FileMetadataStore.Models;
using Lyo.FileStorage.Web.Components.Services;
using Lyo.Keystore;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace Lyo.FileStorage.Web.Components;

public partial class FileStoreKeysTab : ComponentBase
{
    [CascadingParameter]
    public FileStorageWorkbench Workbench { get; set; } = default!;

    private bool _keyBusy;
    private bool _browseKeysBusy;
    private string _keyIdInput = string.Empty;
    private string _keyVersionInput = string.Empty;
    private string _keySecret = string.Empty;
    private bool _showSecret;
    private bool? _inspectedKeyExists;
    private string? _currentKeyVersion;
    private string? _resolvedKeyVersion;
    private string? _resolvedKeyFingerprint;
    private byte[]? _resolvedSalt;
    private KeyMetadata? _inspectedKeyMetadata;
    private string _keyAlgorithm = string.Empty;
    private string _keyExpiresAtText = string.Empty;
    private string _keyAdditionalDataJson = "{}";
    private string _keySearchText = string.Empty;
    private int _keySearchTake = 25;
    private List<FileStorageWorkbenchKeyRecord> _keySearchResults = [];
    private List<string> _localKeyVersions = [];

    private async Task InspectKeyAsync()
    {
        var keyStore = Workbench.KeyStore;
        if (keyStore == null) {
            Workbench.SetStatus("No keystore service is registered for the workbench.", Severity.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(_keyIdInput)) {
            Workbench.SetStatus("Key id is required.", Severity.Warning);
            return;
        }

        _keyBusy = true;
        try {
            var requestedVersion = NullIfWhiteSpace(_keyVersionInput);
            _inspectedKeyExists = await keyStore.HasKeyAsync(_keyIdInput, requestedVersion).ConfigureAwait(false);
            _currentKeyVersion = await keyStore.GetCurrentVersionAsync(_keyIdInput).ConfigureAwait(false);
            _resolvedKeyVersion = requestedVersion ?? _currentKeyVersion;
            _resolvedSalt = null;
            _resolvedKeyFingerprint = null;
            _inspectedKeyMetadata = null;
            _localKeyVersions = Workbench.LocalKeyStore?.GetAvailableVersions(_keyIdInput).ToList() ?? [];
            if (!_inspectedKeyExists.GetValueOrDefault()) {
                LoadKeyMetadataEditor(null);
                Workbench.SetStatus($"Key '{_keyIdInput}' was not found.", Severity.Warning);
                return;
            }

            if (!string.IsNullOrWhiteSpace(_resolvedKeyVersion)) {
                _inspectedKeyMetadata = await keyStore.GetKeyMetadataAsync(_keyIdInput, _resolvedKeyVersion).ConfigureAwait(false);
                _resolvedSalt = await keyStore.GetSaltForVersionAsync(_keyIdInput, _resolvedKeyVersion).ConfigureAwait(false);
                var keyBytes = await keyStore.GetKeyAsync(_keyIdInput, _resolvedKeyVersion).ConfigureAwait(false);
                _resolvedKeyFingerprint = BuildKeyFingerprint(keyBytes);
            }

            LoadKeyMetadataEditor(_inspectedKeyMetadata);
            Workbench.RememberKnownKey(_keyIdInput, _currentKeyVersion, _resolvedKeyVersion);
            Workbench.SetStatus($"Loaded key '{_keyIdInput}'.", Severity.Success);
        }
        catch (Exception ex) {
            Workbench.SetStatus(ex.Message, Severity.Error);
        }
        finally {
            _keyBusy = false;
        }
    }

    private async Task AddKeyVersionAsync()
    {
        var keyStore = Workbench.KeyStore;
        if (keyStore == null) {
            Workbench.SetStatus("No keystore service is registered for the workbench.", Severity.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(_keyIdInput) || string.IsNullOrWhiteSpace(_keyVersionInput) || string.IsNullOrWhiteSpace(_keySecret)) {
            Workbench.SetStatus("Key id, version, and secret are required to add a key version.", Severity.Warning);
            return;
        }

        _keyBusy = true;
        try {
            await keyStore.AddKeyFromStringAsync(_keyIdInput, _keyVersionInput, _keySecret).ConfigureAwait(false);
            await Workbench.RefreshKeyInventoryAsync().ConfigureAwait(false);
            await InspectKeyAsync().ConfigureAwait(false);
            Workbench.SetStatus($"Added key version '{_keyVersionInput}'.", Severity.Success);
        }
        catch (Exception ex) {
            Workbench.SetStatus(ex.Message, Severity.Error);
        }
        finally {
            _keyBusy = false;
        }
    }

    private async Task RotateKeyAsync()
    {
        var keyStore = Workbench.KeyStore;
        if (keyStore == null) {
            Workbench.SetStatus("No keystore service is registered for the workbench.", Severity.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(_keyIdInput) || string.IsNullOrWhiteSpace(_keySecret)) {
            Workbench.SetStatus("Key id and secret are required to rotate a key.", Severity.Warning);
            return;
        }

        _keyBusy = true;
        try {
            var newVersion = await keyStore.UpdateKeyFromStringAsync(_keyIdInput, _keySecret).ConfigureAwait(false);
            _keyVersionInput = newVersion;
            await Workbench.RefreshKeyInventoryAsync().ConfigureAwait(false);
            await InspectKeyAsync().ConfigureAwait(false);
            Workbench.SetStatus($"Rotated '{_keyIdInput}' to version '{newVersion}'.", Severity.Success);
        }
        catch (Exception ex) {
            Workbench.SetStatus(ex.Message, Severity.Error);
        }
        finally {
            _keyBusy = false;
        }
    }

    private async Task SetCurrentVersionAsync()
    {
        var keyStore = Workbench.KeyStore;
        if (keyStore == null) {
            Workbench.SetStatus("No keystore service is registered for the workbench.", Severity.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(_keyIdInput) || string.IsNullOrWhiteSpace(_keyVersionInput)) {
            Workbench.SetStatus("Key id and version are required to set the current version.", Severity.Warning);
            return;
        }

        _keyBusy = true;
        try {
            await keyStore.SetCurrentVersionAsync(_keyIdInput, _keyVersionInput).ConfigureAwait(false);
            await Workbench.RefreshKeyInventoryAsync().ConfigureAwait(false);
            await InspectKeyAsync().ConfigureAwait(false);
            Workbench.SetStatus($"Set current version for '{_keyIdInput}' to '{_keyVersionInput}'.", Severity.Success);
        }
        catch (Exception ex) {
            Workbench.SetStatus(ex.Message, Severity.Error);
        }
        finally {
            _keyBusy = false;
        }
    }

    private async Task SaveKeyMetadataAsync()
    {
        var keyStore = Workbench.KeyStore;
        if (keyStore == null) {
            Workbench.SetStatus("No keystore service is registered for the workbench.", Severity.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(_keyIdInput)) {
            Workbench.SetStatus("Key id is required.", Severity.Warning);
            return;
        }

        var version = NullIfWhiteSpace(_keyVersionInput) ?? _resolvedKeyVersion ?? _currentKeyVersion;
        if (string.IsNullOrWhiteSpace(version)) {
            Workbench.SetStatus("A key version is required to save metadata.", Severity.Warning);
            return;
        }

        Dictionary<string, string>? additionalData = null;
        if (!string.IsNullOrWhiteSpace(_keyAdditionalDataJson)) {
            try {
                additionalData = JsonSerializer.Deserialize<Dictionary<string, string>>(_keyAdditionalDataJson);
            }
            catch (JsonException ex) {
                Workbench.SetStatus($"Additional data JSON is invalid: {ex.Message}", Severity.Warning);
                return;
            }
        }

        DateTime? expiresAt = null;
        var parsedExpiresAt = default(DateTime);
        if (!string.IsNullOrWhiteSpace(_keyExpiresAtText) && !DateTime.TryParse(_keyExpiresAtText, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out parsedExpiresAt)) {
            Workbench.SetStatus("Expires at must be blank or a valid UTC date/time.", Severity.Warning);
            return;
        }

        if (!string.IsNullOrWhiteSpace(_keyExpiresAtText))
            expiresAt = parsedExpiresAt;

        _keyBusy = true;
        try {
            var currentMetadata = await keyStore.GetKeyMetadataAsync(_keyIdInput, version).ConfigureAwait(false);
            await keyStore.SetKeyMetadataAsync(
                _keyIdInput, version, new() {
                    CreatedAt = currentMetadata?.CreatedAt ?? DateTime.UtcNow,
                    Algorithm = NullIfWhiteSpace(_keyAlgorithm),
                    ExpiresAt = expiresAt,
                    AdditionalData = additionalData
                }).ConfigureAwait(false);

            _keyVersionInput = version;
            await InspectKeyAsync().ConfigureAwait(false);
            Workbench.SetStatus($"Saved metadata for '{_keyIdInput}' version '{version}'.", Severity.Success);
        }
        catch (Exception ex) {
            Workbench.SetStatus(ex.Message, Severity.Error);
        }
        finally {
            _keyBusy = false;
        }
    }

    private async Task RemoveLocalKeyVersionAsync()
    {
        var local = Workbench.LocalKeyStore;
        if (local == null) {
            Workbench.SetStatus("Remove version is only available for LocalKeyStore.", Severity.Info);
            return;
        }

        if (string.IsNullOrWhiteSpace(_keyIdInput) || string.IsNullOrWhiteSpace(_keyVersionInput)) {
            Workbench.SetStatus("Key id and version are required.", Severity.Warning);
            return;
        }

        _keyBusy = true;
        try {
            var removed = local.RemoveKey(_keyIdInput, _keyVersionInput);
            if (!removed) {
                Workbench.SetStatus($"Version '{_keyVersionInput}' was not removed.", Severity.Warning);
                return;
            }

            _keyVersionInput = string.Empty;
            await Workbench.RefreshKeyInventoryAsync().ConfigureAwait(false);
            await InspectKeyAsync().ConfigureAwait(false);
            Workbench.SetStatus("Removed local key version.", Severity.Success);
        }
        catch (Exception ex) {
            Workbench.SetStatus(ex.Message, Severity.Error);
        }
        finally {
            _keyBusy = false;
        }
    }

    private async Task SearchKeysAsync()
    {
        _browseKeysBusy = true;
        try {
            var queryService = Workbench.QueryService;
            if (queryService != null) {
                _keySearchResults = (await queryService.SearchKeysAsync(new(NullIfWhiteSpace(_keySearchText), Math.Clamp(_keySearchTake, 1, 250))).ConfigureAwait(false)).ToList();
                return;
            }

            var keyStore = Workbench.KeyStore;
            var inventoryStore = Workbench.InventoryStore;
            if (keyStore == null || inventoryStore == null) {
                _keySearchResults = [];
                return;
            }

            var take = Math.Clamp(_keySearchTake, 1, 250);
            var available = Workbench.AvailableKeyIds;
            var matchingKeyIds = available.Where(keyId => string.IsNullOrWhiteSpace(_keySearchText) || keyId.Contains(_keySearchText, StringComparison.OrdinalIgnoreCase)).OrderBy(keyId => keyId).ToList();
            var results = new List<FileStorageWorkbenchKeyRecord>();
            foreach (var keyId in matchingKeyIds) {
                var currentVersion = await keyStore.GetCurrentVersionAsync(keyId).ConfigureAwait(false);
                foreach (var version in Workbench.GetKnownVersionsForKey(keyId)) {
                    var metadata = await keyStore.GetKeyMetadataAsync(keyId, version).ConfigureAwait(false);
                    results.Add(new(keyId, version, version == currentVersion, metadata));
                    if (results.Count >= take)
                        break;
                }

                if (results.Count >= take)
                    break;
            }

            _keySearchResults = results;
        }
        catch (Exception ex) {
            Workbench.SetStatus(ex.Message, Severity.Error);
        }
        finally {
            _browseKeysBusy = false;
        }
    }

    private void SelectKey(FileStorageWorkbenchKeyRecord record)
    {
        _keyIdInput = record.KeyId;
        _keyVersionInput = record.Version;
        _currentKeyVersion = record.IsCurrent ? record.Version : _currentKeyVersion;
        _resolvedKeyVersion = record.Version;
        _inspectedKeyExists = true;
        _inspectedKeyMetadata = record.Metadata;
        Workbench.RememberKnownKey(record.KeyId, record.Version);
        LoadKeyMetadataEditor(record.Metadata);
    }

    private Task ToggleSecretVisibility(MouseEventArgs _)
    {
        _showSecret = !_showSecret;
        return Task.CompletedTask;
    }

    private void LoadKeyMetadataEditor(KeyMetadata? metadata)
    {
        _inspectedKeyMetadata = metadata;
        _keyAlgorithm = metadata?.Algorithm ?? string.Empty;
        _keyExpiresAtText = metadata?.ExpiresAt?.ToString("O") ?? string.Empty;
        _keyAdditionalDataJson = metadata?.AdditionalData == null || metadata.AdditionalData.Count == 0 ? "{}" : JsonSerializer.Serialize(metadata.AdditionalData, new JsonSerializerOptions { WriteIndented = true });
    }

    private static string? NullIfWhiteSpace(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? BuildKeyFingerprint(byte[]? keyBytes)
    {
        if (keyBytes == null || keyBytes.Length == 0)
            return null;

        var hash = SHA256.HashData(keyBytes);
        return $"{keyBytes.Length} bytes / {Convert.ToHexString(hash)[..16]}";
    }
}
