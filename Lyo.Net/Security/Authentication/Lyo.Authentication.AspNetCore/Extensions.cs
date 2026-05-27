using Lyo.Authentication.AspNetCore.Audit;
using Lyo.Authentication.AspNetCore.Authorization;
using Lyo.Authentication.AspNetCore.Defaults;
using Lyo.Authentication.AspNetCore.Schemes.Bearer;
using Lyo.Authentication.AspNetCore.Schemes.Jwt;
using Lyo.Authentication.AspNetCore.Schemes.Opaque;
using Lyo.Authentication.Audit;
using Lyo.Diagnostic.Correlation;
using Lyo.Exceptions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lyo.Authentication.AspNetCore;

/// <summary>Top-level DI surface for <c>Lyo.Authentication.AspNetCore</c>.</summary>
public static class Extensions
{
    /// <param name="services">The service collection.</param>
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers all three Lyo authentication schemes (opaque, JWT, dispatcher), the scope policy provider, and the HTTP-aware <see cref="IAuthAuditContextAccessor"/>
        /// (replacing the no-op default registered by <c>AddLyoAuthentication</c>). Call <c>AddLyoAuthentication</c> first to register the underlying issuers/validators/stores.
        /// </summary>
        public AuthenticationBuilder AddLyoApiTokenAuthentication()
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.AddSingleton<IAuthorizationPolicyProvider, ScopeAuthorizationPolicyProvider>();
            services.AddSingleton<IAuthorizationHandler, ScopeAuthorizationHandler>();
            services.AddLyoAuthHttpContextAccessor();
            return services
                .AddAuthentication(LyoAuthenticationSchemes.Bearer)
                .AddPolicyScheme(
                    LyoAuthenticationSchemes.Bearer, "Lyo Bearer",
                    o => o.ForwardDefaultSelector = ctx => LyoBearerPolicySchemeHandler.SelectScheme(ctx, new()))
                .AddScheme<OpaqueTokenAuthenticationOptions, OpaqueTokenAuthenticationHandler>(LyoAuthenticationSchemes.OpaqueToken, _ => { })
                .AddScheme<LyoJwtAuthenticationOptions, LyoJwtAuthenticationHandler>(LyoAuthenticationSchemes.LyoJwt, _ => { });
        }

        /// <summary>
        /// Registers <see cref="IHttpContextAccessor"/>, the fallback <see cref="AmbientCorrelationIdResolver"/> (only if no <see cref="ICorrelationIdResolver"/> is already
        /// registered — <c>AddLyoDiagnosticsWeb</c> wins via the same <c>TryAdd</c> when both packages are present), and replaces the registered
        /// <see cref="IAuthAuditContextAccessor"/> with <see cref="HttpAuthAuditContextAccessor"/> so audit events automatically carry the inbound caller's IP, User-Agent, and
        /// correlation id (the same id the structured logs and outbound HTTP headers use). Idempotent.
        /// </summary>
        public IServiceCollection AddLyoAuthHttpContextAccessor()
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.AddHttpContextAccessor();
            services.TryAddSingleton<ICorrelationIdResolver>(_ => AmbientCorrelationIdResolver.Instance);
            services.Replace(ServiceDescriptor.Singleton<IAuthAuditContextAccessor, HttpAuthAuditContextAccessor>());
            return services;
        }
    }
}
