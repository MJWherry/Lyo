using Lyo.Exceptions;

namespace Lyo.RequestSigning;

/// <summary>Adds <see cref="RequestSigner.HeaderName" /> to an outbound request.</summary>
public sealed class RequestSigningHandler : DelegatingHandler
{
    private readonly RequestSigner _signer;

    /// <summary>Builds a handler. Set <see cref="HttpMessageHandler.InnerHandler" /> before sending, or pass it here.</summary>
    public RequestSigningHandler(RequestSigner signer, HttpMessageHandler? innerHandler = null)
    {
        ArgumentHelpers.ThrowIfNull(signer);
        _signer = signer;
        if (innerHandler is not null)
            InnerHandler = innerHandler;
    }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentHelpers.ThrowIfNull(request);
        var uri = request.RequestUri ?? throw new ArgumentException("The request URI is required.", nameof(request));
        var body = await ReadBodyAsync(request).ConfigureAwait(false);
        var host = uri.IsDefaultPort ? uri.Host : uri.Authority;
        var input = new RequestSignatureInput(request.Method.Method, uri.Scheme, host, uri.PathAndQuery, body);
        var header = _signer.CreateHeader(input);
        request.Headers.Remove(_signer.HeaderName);
        request.Headers.TryAddWithoutValidation(_signer.HeaderName, header);
        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<byte[]> ReadBodyAsync(HttpRequestMessage request)
    {
        if (request.Content is null)
            return [];
        var body = await request.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
        var replacement = new ByteArrayContent(body);
        foreach (var header in request.Content.Headers)
            replacement.Headers.TryAddWithoutValidation(header.Key, header.Value);
        request.Content = replacement;
        return body;
    }
}
