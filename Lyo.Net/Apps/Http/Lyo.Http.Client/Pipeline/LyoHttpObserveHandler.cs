using Lyo.Exceptions;

namespace Lyo.Http.Client.Pipeline;

/// <summary>Optional observer that sees every request this named client sends (not page subresources). Outer than metrics.</summary>
public sealed class LyoHttpObserveHandler : DelegatingHandler
{
    private readonly LyoHttpClientHooks? _hooks;

    /// <summary>Creates the handler. When <paramref name="hooks" /> is null, this is a pass-through.</summary>
    public LyoHttpObserveHandler(LyoHttpClientHooks? hooks = null) => _hooks = hooks;

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfNull(request);
        var context = new LyoHttpCallContext(request);
        if (_hooks?.OnRequestObserved != null)
            await _hooks.OnRequestObserved(context, ct).ConfigureAwait(false);

        var response = await base.SendAsync(request, ct).ConfigureAwait(false);
        context.Response = response;
        if (_hooks?.OnResponseObserved != null)
            await _hooks.OnResponseObserved(context, ct).ConfigureAwait(false);

        return response;
    }
}
