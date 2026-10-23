using System.Net;
using System.Text;

namespace Lyo.ClamAv.Client.Tests;

internal sealed class RecordingHandler : HttpMessageHandler
{
    public List<CapturedRequest> Calls { get; } = [];

    public Func<CapturedRequest, HttpResponseMessage> Respond { get; set; } = _ => new(HttpStatusCode.OK);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content == null ? null : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var captured = new CapturedRequest(request.Method, request.RequestUri!, body);
        Calls.Add(captured);
        return Respond(captured);
    }
}

internal sealed record CapturedRequest(HttpMethod Method, Uri Uri, string? Body);
