using System.Net;
using Lyo.Common.Metadata.Records;
using Lyo.Exceptions.Models;
using Lyo.Http.Client;
using Lyo.Http.Client.Pipeline;
using Lyo.Http.Client.Plan;

namespace Lyo.Http.Client.Tests;

public sealed class HttpClientPlanJsonTests
{
    [Fact]
    public void Build_RoundTripsWithoutHeaderAndCookie()
    {
        var plan = HttpClientPlanBuilder.New("roundtrip")
            .Get("/gallery")
            .WithHeader(HttpHeaderInfo.AcceptLanguage, "en-US")
            .WithoutHeader("X-Debug")
            .WithoutCookie("session")
            .ThenExtractImages("imageUrls")
            .Commit()
            .FilterList("imageUrls", "imageUrls", excludeRegex: "sprite|1x1")
            .DownloadUrls("imageUrls", "{{downloadDir}}/img", "img")
            .Build();

        var json = System.Text.Json.JsonSerializer.Serialize(plan);
        var restored = System.Text.Json.JsonSerializer.Deserialize<HttpClientPlan>(json);
        Assert.NotNull(restored);
        Assert.Equal("roundtrip", restored.Name);
        Assert.Contains(restored.Steps, s => s is HttpRequestPlanStep);
        var request = restored.Steps.OfType<HttpRequestPlanStep>().Single();
        Assert.Contains("X-Debug", request.HeadersToRemove!);
        Assert.Contains("session", request.CookiesToRemove!);
        Assert.Equal("en-US", request.Headers![HttpHeaderInfo.AcceptLanguage]);
        Assert.Contains(restored.Steps, s => s is HttpFilterListPlanStep);
        Assert.Contains(restored.Steps, s => s is HttpDownloadUrlsPlanStep);
    }

    [Fact]
    public void Interpolation_ReplacesBindings()
        => Assert.Equal("https://x.test/a/b", HttpClientPlanInterpolation.Interpolate("https://x.test/{{slug}}/{{id}}", new Dictionary<string, string> { ["slug"] = "a", ["id"] = "b" }));
}

