using System;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Lyo.Authentication.Services.Jwt;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lyo.Authentication.AspNetCore.Schemes.Jwt;

/// <summary>ASP.NET Core authentication handler for Lyo-signed JWTs. Delegates to <see cref="ILyoJwtValidator"/>.</summary>
public sealed class LyoJwtAuthenticationHandler : AuthenticationHandler<LyoJwtAuthenticationOptions>
{
    private readonly ILyoJwtValidator _validator;

    /// <summary>Creates a new handler.</summary>
    public LyoJwtAuthenticationHandler(
        IOptionsMonitor<LyoJwtAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ILyoJwtValidator validator)
        : base(options, logger, encoder) =>
        _validator = validator;

    /// <inheritdoc/>
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var header = Request.Headers[Options.HeaderName].ToString();
        if (string.IsNullOrWhiteSpace(header))
            return AuthenticateResult.NoResult();

        var prefix = Options.Scheme + " ";
        if (!header.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return AuthenticateResult.NoResult();

        var token = header.Substring(prefix.Length).Trim();
        if (string.IsNullOrEmpty(token))
            return AuthenticateResult.NoResult();

        var principal = await _validator.ValidateAsync(token, Context.RequestAborted).ConfigureAwait(false);
        return principal is null
            ? AuthenticateResult.Fail("Invalid Lyo JWT.")
            : AuthenticateResult.Success(new(principal, Scheme.Name));
    }
}
