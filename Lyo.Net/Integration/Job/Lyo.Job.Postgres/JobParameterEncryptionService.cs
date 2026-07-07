using System.Text;
using Lyo.Encryption;
using Lyo.Job.Models.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Job.Postgres;

/// <summary>Encrypts and decrypts job parameter values using an optional keyed <see cref="IEncryptionService" />.</summary>
public sealed class JobParameterEncryptionService : IJobParameterEncryptionService
{
    private const string MaskedPlaceholder = "***";

    private readonly IEncryptionService? _encryptionService;
    private readonly ILogger<JobParameterEncryptionService> _logger;
    private readonly string? _keyName;

    public JobParameterEncryptionService(IEncryptionService? encryptionService = null, string? keyName = null, ILogger<JobParameterEncryptionService>? logger = null)
    {
        _encryptionService = encryptionService;
        _keyName = keyName;
        _logger = logger ?? NullLogger<JobParameterEncryptionService>.Instance;
    }

    /// <inheritdoc />
    public bool IsEncryptionEnabled => _encryptionService is not null;

    /// <inheritdoc />
    public bool UsesEncryptedStorage(byte[]? encryptedValueMarker) => encryptedValueMarker is not null;

    /// <inheritdoc />
    public void EncryptParameterValue(ref string? value, ref byte[]? encryptedValue)
    {
        if (!UsesEncryptedStorage(encryptedValue) && string.IsNullOrEmpty(value))
            return;

        if (_encryptionService is null) {
            if (UsesEncryptedStorage(encryptedValue) && !string.IsNullOrEmpty(value))
                _logger.LogWarning("Encrypted job parameter storage requested but no IEncryptionService is registered");

            return;
        }

        var plaintext = value;
        if (string.IsNullOrEmpty(plaintext) && encryptedValue is { Length: > 0 }) {
            // Treat existing bytes as plaintext when re-encrypting during update.
            plaintext = Encoding.UTF8.GetString(encryptedValue);
        }

        if (string.IsNullOrEmpty(plaintext))
            return;

        encryptedValue = _encryptionService.EncryptString(plaintext, _keyName);
        value = null;
    }

    /// <inheritdoc />
    public string? DecryptValue(byte[]? encryptedValue)
    {
        if (encryptedValue is null or { Length: 0 })
            return null;

        if (_encryptionService is null) {
            _logger.LogWarning("Cannot decrypt job parameter — no IEncryptionService is registered");
            return null;
        }

        try {
            return _encryptionService.DecryptString(encryptedValue, _keyName);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to decrypt job parameter value");
            return null;
        }
    }

    /// <inheritdoc />
    public string? MaskValue(string? value, byte[]? encryptedValueMarker)
        => UsesEncryptedStorage(encryptedValueMarker) ? MaskedPlaceholder : value;
}
