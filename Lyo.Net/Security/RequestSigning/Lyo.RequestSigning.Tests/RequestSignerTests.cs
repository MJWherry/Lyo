using System.Text;
using Lyo.Common.Core.Enums;
using Lyo.Hashing;

namespace Lyo.RequestSigning.Tests;

public sealed class RequestSignerTests
{
    private static readonly DateTime Start = new(2026, 10, 14, 12, 0, 0, DateTimeKind.Utc);
    private const string Key = "current-signing-key";
    private const string PreviousKey = "previous-signing-key";

    [Fact]
    public void CreateHeader_Verify_SameRequest_Valid()
    {
        var signer = Signer();
        var input = Input("{\"a\":1}");
        var header = signer.CreateHeader(input);
        Assert.Equal(RequestSignatureStatus.Valid, signer.Verify(input, header));
    }

    [Fact]
    public void Verify_TamperedBody_Invalid()
    {
        var signer = Signer();
        var header = signer.CreateHeader(Input("{\"a\":1}"));
        Assert.Equal(RequestSignatureStatus.Invalid, signer.Verify(Input("{\"a\":2}"), header));
    }

    [Fact]
    public void Verify_OlderThanMaxAge_Stale()
    {
        var clock = new Clock();
        var signer = Signer(clock);
        var header = signer.CreateHeader(Input("body"));
        clock.UtcNow = clock.UtcNow.AddMinutes(5).AddSeconds(1);
        Assert.Equal(RequestSignatureStatus.Stale, signer.Verify(Input("body"), header));
    }

    [Fact]
    public void Verify_PreviousKey_StillValid()
    {
        var clock = new Clock();
        var signed = Signer(clock, previousKey: null);
        var header = signed.CreateHeader(Input("body"));
        var rotated = Signer(clock, key: PreviousKey, previousKey: Key);
        Assert.Equal(RequestSignatureStatus.Valid, rotated.Verify(Input("body"), header));
    }

    [Fact]
    public void Verify_PreviousKey_Absent_Invalid()
    {
        var clock = new Clock();
        var signed = Signer(clock, key: PreviousKey);
        var header = signed.CreateHeader(Input("body"));
        var current = Signer(clock);
        Assert.Equal(RequestSignatureStatus.Invalid, current.Verify(Input("body"), header));
    }

    [Fact]
    public void CreateHeader_UsesKeyOnly()
    {
        var signer = Signer(previousKey: PreviousKey);
        var input = Input("body");
        var header = signer.CreateHeader(input);
        var hashing = HashingService.Shared;
        Assert.True(RequestSignature.TryParse(header, out var unix, out var macHex));
        var current = RequestSignature.Mac(Key, input, unix, hashing);
        var previous = RequestSignature.Mac(PreviousKey, input, unix, hashing);
        Assert.True(hashing.EqualsHex(current, macHex));
        Assert.False(hashing.EqualsHex(previous, macHex));
    }

    [Fact]
    public async Task Handler_Header_AcceptedByVerify()
    {
        var signer = Signer();
        var capture = new CaptureHandler();
        var handler = new RequestSigningHandler(signer, capture);
        var body = Encoding.UTF8.GetBytes("{\"a\":1}");
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.example/pay?x=1") {
            Content = new ByteArrayContent(body)
        };
        using var client = new HttpClient(handler);
        var response = await client.SendAsync(request, TestCt);
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.True(capture.Request!.Headers.TryGetValues("Lyo-Signature", out var values));
        var input = new RequestSignatureInput("POST", "https", "api.example", "/pay?x=1", body);
        Assert.Equal(RequestSignatureStatus.Valid, signer.Verify(input, values!.Single()));
    }

    private static RequestSignatureInput Input(string body)
        => new("post", "HTTPS", "api.example", "/pay?x=1", Encoding.UTF8.GetBytes(body));

    private static RequestSigner Signer(Clock? clock = null, string? key = null, string? previousKey = null)
    {
        var options = new RequestSignatureOptions {
            Key = key ?? Key,
            PreviousKey = previousKey,
            MaxAge = TimeSpan.FromMinutes(5)
        };
        return new RequestSigner(HashingService.Shared, options, () => (clock ?? new Clock()).UtcNow);
    }

    private sealed class Clock
    {
        public DateTime UtcNow { get; set; } = Start;
    }

    private sealed class CaptureHandler : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        }
    }
}
