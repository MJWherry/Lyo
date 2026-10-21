using System.Net;

namespace Lyo.OpenSubtitles.Client.Tests;

internal sealed class RecordingHandler : HttpMessageHandler
{
    public List<CapturedRequest> Calls { get; } = [];

    public Func<CapturedRequest, HttpResponseMessage> Respond { get; set; } = _ => new(HttpStatusCode.OK);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content == null ? null : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var captured = new CapturedRequest(request.Method, request.RequestUri!, body, HeaderMap(request));
        Calls.Add(captured);
        return Respond(captured);
    }

    private static Dictionary<string, string> HeaderMap(HttpRequestMessage request)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in request.Headers) {
            var separator = header.Key.Equals("User-Agent", StringComparison.OrdinalIgnoreCase) ? " " : ",";
            headers[header.Key] = string.Join(separator, header.Value);
        }

        return headers;
    }
}

internal sealed record CapturedRequest(HttpMethod Method, Uri Uri, string? Body, IReadOnlyDictionary<string, string> Headers);