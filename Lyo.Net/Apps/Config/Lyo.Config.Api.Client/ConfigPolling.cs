using Lyo.Config.Api.Models;
using Lyo.Exceptions;

namespace Lyo.Config.Api.Client;

/// <summary>Loop that probes app-config routes and honors 304 / not-modified answers.</summary>
public static class ConfigPolling
{
    /// <summary>Sleeps between probes until the resolved payload changes (<see cref="ConfigResolveOutcome.Ok" />).</summary>
    /// <remarks>Feeds the newest <c>ETag</c> (or the prior <paramref name="ifNoneMatch" />) into <see cref="IConfigApiClient.ResolveForAppAsync" />.</remarks>
    public static async Task<ResolvedConfigRecord> PollUntilChangedAsync(
        IConfigApiClient client,
        string appKind,
        string appId,
        string? ifNoneMatch,
        TimeSpan delayWhenNotModified,
        CancellationToken ct = default)
    {
        var probe = ifNoneMatch;
        while (!ct.IsCancellationRequested) {
            var result = await client.ResolveForAppAsync(appKind, appId, probe, null, false, ct).ConfigureAwait(false);
            probe = result.ETag ?? probe;
            switch (result.Outcome) {
                case ConfigResolveOutcome.Ok:
                    OperationHelpers.ThrowIfNull(result.Resolved, "OK response omitted resolved payload.");
                    return result.Resolved;
                case ConfigResolveOutcome.NotModified:
                    await Task.Delay(delayWhenNotModified, ct).ConfigureAwait(false);
                    break;
                default:
                    throw new InvalidOperationException($"Config API poll failed: HTTP {result.Failure?.StatusCode} {result.Failure?.ReasonPhrase}".Trim());
            }
        }

        ct.ThrowIfCancellationRequested();
        throw new OperationCanceledException(ct);
    }
}