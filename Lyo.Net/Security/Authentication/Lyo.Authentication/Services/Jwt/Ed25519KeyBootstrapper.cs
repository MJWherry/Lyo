using Lyo.Authentication.Audit;
using Lyo.Authentication.Models.Audit;
using Lyo.Authentication.Options;
using Lyo.Common.Security;
using Lyo.Exceptions;
using Lyo.KeyStore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lyo.Authentication.Services.Jwt;

/// <summary>
/// On startup, ensures a Lyo JWT signing key exists in <see cref="IKeyStore" />. When <see cref="LyoJwtOptions.AutoGenerateSigningKey" /> is true (the default) and no key is
/// present under <see cref="LyoJwtOptions.SigningKeyId" />, a fresh 32-byte Ed25519 private seed is generated and stored as version <c>v1</c>.
/// </summary>
public sealed class Ed25519KeyBootstrapper : IHostedService
{
    private readonly IAuthAuditRecorder _audit;
    private readonly IAuthAuditContextAccessor _auditContext;
    private readonly IKeyStore _keys;
    private readonly ILogger<Ed25519KeyBootstrapper> _logger;
    private readonly LyoJwtOptions _options;

    /// <summary>Creates a new bootstrapper.</summary>
    public Ed25519KeyBootstrapper(
        IKeyStore keys,
        IOptions<LyoJwtOptions> options,
        ILogger<Ed25519KeyBootstrapper> logger,
        IAuthAuditRecorder? audit = null,
        IAuthAuditContextAccessor? auditContext = null)
    {
        ArgumentHelpers.ThrowIfNull(keys);
        ArgumentHelpers.ThrowIfNull(options);
        ArgumentHelpers.ThrowIfNull(logger);
        _keys = keys;
        _options = options.Value;
        _logger = logger;
        _audit = audit ?? NullAuthAuditRecorder.Instance;
        _auditContext = auditContext ?? NullAuthAuditContextAccessor.Instance;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.AutoGenerateSigningKey) {
            _logger.LogInformation(
                "Skipping Ed25519 signing-key bootstrap because AutoGenerateSigningKey=false. Operator must provision '{KeyId}' out of band.", _options.SigningKeyId);

            return;
        }

        if (await _keys.HasKeyAsync(_options.SigningKeyId, null, cancellationToken).ConfigureAwait(false)) {
            _logger.LogDebug("Signing key '{KeyId}' already present; nothing to do.", _options.SigningKeyId);
            return;
        }

        _logger.LogWarning(
            "No signing key found under '{KeyId}'. Generating a fresh Ed25519 keypair (v1). For production, provision and back this up out of band before the host accepts traffic.",
            _options.SigningKeyId);

        var seed = CryptographicRandom.GetBytes(Ed25519Constants.PrivateSeedLength);
        await _keys.AddKeyAsync(_options.SigningKeyId, "v1", seed, cancellationToken).ConfigureAwait(false);
        await _keys.SetCurrentVersionAsync(_options.SigningKeyId, "v1", cancellationToken).ConfigureAwait(false);
        await _audit.RecordAsync(
                _auditContext, _logger, AuthAuditEventKind.SigningKeyBootstrapped, subject: $"{_options.SigningKeyId}:v1", outcome: "success", ct: cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}