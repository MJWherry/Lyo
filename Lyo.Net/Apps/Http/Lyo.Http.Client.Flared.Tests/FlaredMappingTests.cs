using System.Net;
using System.Text;
using FlareSolverrSharp.Types;
using Lyo.Http.Client;
using Lyo.Http.Client.Flared;
using Lyo.Http.Client.Session;

namespace Lyo.Http.Client.Flared.Tests;

public sealed class FlaredMappingTests
{
    [Fact]
    public async Task ToResponse_UsesSolutionBodyAndStatus()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.test/");
        var solution = new Solution {
            Status = "200",
            Response = "<html>ok</html>",
            Url = "https://example.test/",
            UserAgent = "Mozilla/5.0 Test"
        };
        using var response = FlaredHttpMessageHandler.ToResponse(request, solution);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("<html>ok</html>", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.True(response.Headers.Contains("X-FlareSolverr-UserAgent"));
    }

    [Fact]
    public void MergeSolution_PinsUaAndCookies()
    {
        var session = new FlareSession();
        LyoHttpSessionAccessor.Session = session;
        try {
            using var handler = new FlaredHttpMessageHandler(new());
            var solved = new FlareSolverrResponse {
                Solution = new Solution {
                    UserAgent = "Mozilla/5.0 Pinned",
                    Cookies = [
                        new FlareSolverrSharp.Types.Cookie { Name = "cf_clearance", Value = "tok", Domain = "example.test", Path = "/" }
                    ]
                }
            };
            handler.MergeSolution(solved);
            Assert.Equal("Mozilla/5.0 Pinned", session.UserAgent);
            Assert.Contains(session.CookieJar.Export(), c => c.Name == "cf_clearance" && c.Value == "tok");
        }
        finally {
            LyoHttpSessionAccessor.Session = null;
        }
    }

    [Fact]
    public async Task StreamBinary_SkipsThroughSolver()
    {
        var inner = new RecordingHandler(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(Encoding.UTF8.GetBytes("bin")) });
        using var handler = new FlaredHttpMessageHandler(new()) { InnerHandler = inner };
        using var client = new HttpClient(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.test/file.bin");
        LyoHttpRequestMarkers.SetStreamBinary(request);
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        Assert.Equal(1, inner.Sends);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void Validate_RejectsPerRequestRotation()
    {
        var options = new FlaredHttpOptions { UserAgent = { Rotation = LyoHttpUserAgentRotation.PerRequest } };
        Assert.Throws<InvalidOperationException>(options.Validate);
    }

    private sealed class RecordingHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        public int Sends { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Sends++;
            return Task.FromResult(response);
        }
    }
}
