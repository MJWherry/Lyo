using Lyo.Exceptions;

namespace Lyo.Diagnostic.Correlation;

/// <summary>
/// Outbound <see cref="DelegatingHandler" /> that stamps a correlation id on every request flowing through the typed <see cref="HttpClient" /> it's registered on. Pairs with
/// <c>Lyo.Diagnostic.AspNetCore.HttpContextCorrelationIdResolver</c> on ASP.NET hosts (propagates the inbound id) and with <see cref="AmbientCorrelationIdResolver" /> everywhere else
/// (mints a fresh id rooted at <see cref="System.Diagnostics.Activity.Current" />).
/// </summary>
/// <remarks>
/// Register as the <strong>outermost</strong> handler in the pipeline so any nested handlers (e.g. <c>LyoAuthDelegatingHandler</c>'s own refresh roundtrip) also carry the
/// header.
/// </remarks>
public sealed class LyoCorrelationDelegatingHandler : DelegatingHandler
{
    private readonly CorrelationHandlerOptions _options;
    private readonly ICorrelationIdResolver _resolver;

    /// <summary>Creates a new handler. <paramref name="options" /> is optional — when omitted, the handler uses <see cref="CorrelationHandlerOptions.DefaultHeaders" />.</summary>
    public LyoCorrelationDelegatingHandler(ICorrelationIdResolver resolver, CorrelationHandlerOptions? options = null)
    {
        ArgumentHelpers.ThrowIfNull(resolver);
        _resolver = resolver;
        _options = options ?? new CorrelationHandlerOptions();
    }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentHelpers.ThrowIfNull(request);
        if (!RequestAlreadyHasCorrelation(request)) {
            var id = _resolver.Resolve();
            if (!string.IsNullOrWhiteSpace(id)) {
                foreach (var headerName in _options.WriteHeaderNames) {
                    if (string.IsNullOrWhiteSpace(headerName))
                        continue;

                    request.Headers.TryAddWithoutValidation(headerName, id);
                }
            }
        }

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }

    private bool RequestAlreadyHasCorrelation(HttpRequestMessage request)
    {
        foreach (var headerName in _options.DetectHeaderNames) {
            if (string.IsNullOrWhiteSpace(headerName))
                continue;

            if (request.Headers.TryGetValues(headerName, out var values)) {
                foreach (var value in values) {
                    if (!string.IsNullOrWhiteSpace(value))
                        return true;
                }
            }
        }

        return false;
    }
}