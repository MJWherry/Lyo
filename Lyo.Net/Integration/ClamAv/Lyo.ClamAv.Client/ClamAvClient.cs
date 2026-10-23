using System.Net;
using Lyo.Exceptions;
using Lyo.Http.Client;
using Microsoft.Extensions.Logging;

namespace Lyo.ClamAv.Client;

/// <summary>clamav-rest client. Register with <c>AddClamAvClient</c> so <see cref="HttpClient" /> comes from <c>IHttpClientFactory</c>.</summary>
public class ClamAvClient : LyoHttpClient, IClamAvClient
{
    /// <summary>Creates a client. <paramref name="httpClient" /> is supplied by <c>IHttpClientFactory</c> when the client is resolved from DI.</summary>
    public ClamAvClient(ClamAvClientOptions options, ILogger<ClamAvClient>? logger = null, HttpClient? httpClient = null)
        : base(logger, httpClient, serializerOptions: null, options)
    {
        ArgumentHelpers.ThrowIfNull(options);
    }

    private ClamAvClientOptions Options => (ClamAvClientOptions)BaseOptions;

    /// <inheritdoc />
    public async Task<ClamScanResult> ScanAsync(Stream content, string fileName, CancellationToken cancellationToken = default)
    {
        ArgumentHelpers.ThrowIfNull(content);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(fileName);
        var body = await PostFileAsAsync<ScanPayload>(Options.ScanPath, content, fileName, ct: cancellationToken).ConfigureAwait(false);
        return Map(body);
    }

    /// <inheritdoc />
    public async Task<string> GetVersionAsync(CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Get, Options.VersionPath);
        using var response = await SendAsync(request, null, cancellationToken).ConfigureAwait(false);
        if (response.Content == null)
            return "";

#if NET5_0_OR_GREATER
        var text = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
#else
        var text = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
#endif
        return text.Trim();
    }

    /// <summary>clamav-rest reports a hit as HTTP 406 with a JSON body. That status is still a verdict.</summary>
    protected override Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.StatusCode == HttpStatusCode.NotAcceptable)
            return Task.CompletedTask;

        return base.EnsureSuccessAsync(response, ct);
    }

    private static ClamScanResult Map(ScanPayload body)
    {
        var status = body.Status?.Trim() ?? "";
        if (status.Equals("OK", StringComparison.OrdinalIgnoreCase))
            return new(ClamScanStatus.Clean, null);

        if (status.Equals("FOUND", StringComparison.OrdinalIgnoreCase)) {
            var description = body.Description;
            var signature = string.IsNullOrWhiteSpace(description) ? null : description!.Trim();
            return new(ClamScanStatus.Infected, signature);
        }

        return new(ClamScanStatus.Error, null);
    }

    private sealed class ScanPayload
    {
        public string? Status { get; set; }

        public string? Description { get; set; }
    }
}
