using System.Net;
using System.Text;
using Lyo.ContentThreatScan.Intel;

namespace Lyo.ContentThreatScan.Tests;

public sealed class AssessmentAndReputationTests
{
    sealed class ScriptedHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> fn)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => fn(request, cancellationToken);
    }

    [Fact]
    public void Mapper_respects_intel_confirmation_switch()
    {
        ContentThreatAssessment flagged =
            ContentThreatAssessment.FromContributions([], intelConfirmedMalicious: true, dispositionScoreCap: 999m);

        ContentThreatAssessmentOptions forceThreatIntel =
            new()
            {
                SuspectThreshold = decimal.MaxValue,
                ThreatThreshold = decimal.MaxValue,
                ForceThreatOnConfirmedIntel = true,
            };

        Assert.Equal(ContentThreatDisposition.Threat, ContentThreatDispositionMapper.Resolve(flagged, forceThreatIntel));

        ContentThreatAssessmentOptions ignoreIntelThreat =
            new()
            {
                SuspectThreshold = decimal.MaxValue,
                ThreatThreshold = decimal.MaxValue,
                ForceThreatOnConfirmedIntel = false,
            };

        Assert.Equal(ContentThreatDisposition.Clean, ContentThreatDispositionMapper.Resolve(flagged, ignoreIntelThreat));
    }

    [Fact]
    public void Composer_limits_total_disposition_even_when_external_points_explode()
    {
        ExternalReputationEnvelope extEnvelope =
            new(contributions: [new( "reputation.case", ContentThreatCategory.Reputation, 500m)],
            intelConfirmedMalicious: false);

        var capped = ContentThreatAssessmentComposer.Compose( [], extEnvelope, new() { DispositionScoreCap = 42m });
        Assert.Equal(42m, capped.DispositionScore);
    }

    [Fact]
    public async Task MalwareBazaar_http_stub_flags_known_entries()
    {
        // Base Malware Bazaar hit only (a non-empty signature adds an extra contribution).
        const string body = "{\"query_status\":\"ok\"}";
        using var scriptedClient = BazaarClientFactory(body);
        ReputationPipelineOptions pipelineOptions =
            new()
            {
                MalwareBazaarEndpoint = new("https://mb.fake/api/v1/"),
                MalwareBazaarAuthKey = "test-key",
                ProviderTimeout = TimeSpan.FromSeconds(5),
                MalwareBazaarFailureDisposition = ExternalReputationFailureDisposition.Ignore,
                VirusTotalFailureDisposition = ExternalReputationFailureDisposition.Ignore,
            };

        var pipelineImplementation = new DefaultContentThreatReputationPipeline(scriptedClient, pipelineOptions);
        var plaintextSample = "{\"alive\":true}"u8.ToArray();
        var digestBuffer = ContentThreatBuffering.ComputeSha256(plaintextSample);
        var verdict =
            await pipelineImplementation.InspectAsync(
                new(new(digestBuffer), new ReadOnlyMemory<byte>(plaintextSample)),
                new("dropper.json"),
                TestContext.Current.CancellationToken);

        Assert.True(verdict.IntelConfirmedMalicious);
        Assert.Single(verdict.Contributions);
        Assert.True(verdict.Contributions[0].Points > 0m);
    }

    private static HttpClient BazaarClientFactory(string cannedJsonPayload) =>
        new(
            new ScriptedHandler((request, cancellationToken) => {
                cancellationToken.ThrowIfCancellationRequested();
                Assert.Equal(HttpMethod.Post, request.Method);
                Assert.Equal("mb.fake", request.RequestUri?.Host);
                Assert.True(request.Headers.TryGetValues("Auth-Key", out var values));
                Assert.NotNull(values);
                return Task.FromResult(
                    new HttpResponseMessage(HttpStatusCode.OK)
                        { Content = new StringContent(cannedJsonPayload, Encoding.UTF8, "application/json"), });
            }));
}
