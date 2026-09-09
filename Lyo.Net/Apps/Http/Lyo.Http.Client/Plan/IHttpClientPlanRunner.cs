namespace Lyo.Http.Client.Plan;

/// <summary>Executes an <see cref="HttpClientPlan" /> against an <see cref="ILyoHttpClient" />.</summary>
public interface IHttpClientPlanRunner
{
    /// <summary>Runs <paramref name="plan" />. Interpolates <c>{{var}}</c>, honors extract/filter/download steps, and uses the client's rate limiter.</summary>
    Task<HttpClientPlanRunResult> RunAsync(
        ILyoHttpClient client,
        HttpClientPlan plan,
        HttpClientPlanRuntime? runtime = null,
        Microsoft.Extensions.Logging.ILogger? logger = null,
        CancellationToken ct = default);
}
