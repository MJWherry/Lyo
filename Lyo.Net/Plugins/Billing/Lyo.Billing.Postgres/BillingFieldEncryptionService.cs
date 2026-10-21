using Lyo.Encryption;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Billing.Postgres;

/// <summary>Encrypts billing secrets using an optional keyed <see cref="IEncryptionService" />.</summary>
public sealed class BillingFieldEncryptionService : IBillingFieldEncryptionService
{
    private readonly IEncryptionService? _encryptionService;
    private readonly string? _keyName;
    private readonly ILogger<BillingFieldEncryptionService> _logger;

    /// <summary>Constructs a wrapper. When <paramref name="encryptionService" /> is null, encrypting a secret fails at the store.</summary>
    public BillingFieldEncryptionService(IEncryptionService? encryptionService = null, string? keyName = null, ILogger<BillingFieldEncryptionService>? logger = null)
    {
        _encryptionService = encryptionService;
        _keyName = keyName;
        _logger = logger ?? NullLogger<BillingFieldEncryptionService>.Instance;
    }

    /// <inheritdoc />
    public bool IsEncryptionEnabled => _encryptionService is not null;

    /// <inheritdoc />
    public void Encrypt(ref string? plaintext, ref byte[]? encrypted)
    {
        if (string.IsNullOrEmpty(plaintext))
            return;

        if (_encryptionService is null)
            throw new BillingEncryptionRequiredException("Saving a tax id or remittance secret requires IBillingFieldEncryptionService with a registered IEncryptionService.");

        encrypted = _encryptionService.EncryptString(plaintext, _keyName);
        plaintext = null;
    }

    /// <inheritdoc />
    public string? Decrypt(byte[]? encrypted)
    {
        if (encrypted is null or { Length: 0 })
            return null;

        if (_encryptionService is null) {
            _logger.LogWarning("Cannot decrypt billing field — no IEncryptionService is registered");
            return null;
        }

        try {
            return _encryptionService.DecryptString(encrypted, _keyName);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to decrypt billing field");
            return null;
        }
    }
}