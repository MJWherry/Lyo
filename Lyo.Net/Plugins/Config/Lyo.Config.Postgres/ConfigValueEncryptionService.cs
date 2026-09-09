using System.Text;
using Lyo.Encryption;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Config.Postgres;

/// <summary>Wraps encrypt/decrypt of config JSON using an optional keyed <see cref="IEncryptionService" />. Call from API-hosted store paths only.</summary>
public sealed class ConfigValueEncryptionService : IConfigValueEncryptionService
{
    private const string MaskedPlaceholder = "***";

    private readonly IEncryptionService? _encryptionService;
    private readonly string? _keyName;
    private readonly ILogger<ConfigValueEncryptionService> _logger;

    /// <summary>
    /// Constructs a new encryption wrapper. When <paramref name="encryptionService" /> is null, <see cref="EncryptValue" /> leaves plaintext and logs a warning. API hosts must
    /// register encryption so <c>PostgresConfigStore</c> can persist ciphertext.
    /// </summary>
    public ConfigValueEncryptionService(IEncryptionService? encryptionService = null, string? keyName = null, ILogger<ConfigValueEncryptionService>? logger = null)
    {
        _encryptionService = encryptionService;
        _keyName = keyName;
        _logger = logger ?? NullLogger<ConfigValueEncryptionService>.Instance;
    }

    /// <inheritdoc />
    public bool IsEncryptionEnabled => _encryptionService is not null;

    /// <inheritdoc />
    public bool UsesEncryptedStorage(byte[]? encryptedValueMarker) => encryptedValueMarker is not null;

    /// <inheritdoc />
    public void EncryptValue(ref string? json, ref byte[]? encryptedValue)
    {
        if (!UsesEncryptedStorage(encryptedValue) && string.IsNullOrEmpty(json))
            return;

        if (_encryptionService is null) {
            if (UsesEncryptedStorage(encryptedValue) && !string.IsNullOrEmpty(json))
                _logger.LogWarning("Encrypted config storage requested but no IEncryptionService is registered");

            return;
        }

        var plaintext = json;
        if (string.IsNullOrEmpty(plaintext) && encryptedValue is { Length: > 0 })
            plaintext = Encoding.UTF8.GetString(encryptedValue);

        if (string.IsNullOrEmpty(plaintext))
            return;

        encryptedValue = _encryptionService.EncryptString(plaintext, _keyName);
        json = null;
    }

    /// <inheritdoc />
    public string? DecryptValue(byte[]? encryptedValue)
    {
        if (encryptedValue is null or { Length: 0 })
            return null;

        if (_encryptionService is null) {
            _logger.LogWarning("Cannot decrypt config value — no IEncryptionService is registered");
            return null;
        }

        try {
            return _encryptionService.DecryptString(encryptedValue, _keyName);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to decrypt config value");
            return null;
        }
    }

    /// <inheritdoc />
    public string? MaskValue(string? json, byte[]? encryptedValueMarker) => UsesEncryptedStorage(encryptedValueMarker) ? MaskedPlaceholder : json;
}
