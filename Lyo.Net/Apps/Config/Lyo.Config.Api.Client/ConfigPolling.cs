using Lyo.Config.Api.Models;
using Lyo.Config;
using Lyo.Exceptions;

namespace Lyo.Config.Api.Client;

/// <summary>Background polling that respects non-modified probes on app-config routes.</summary>
public static class ConfigPolling
{
    /// <summary>Waits between probes until the resolved payload changes (<see cref="ConfigResolveOutcome.Ok"/>).</summary>
    /// <remarks>Reuses the latest <c>ETag</c> (or your previous <paramref name="ifNoneMatch" />) for <see cref="IConfigApiClient.ResolveForAppAsync" />.</remarks>
    public static async Task<ResolvedConfigRecord> PollUntilChangedAsync(
        IConfigApiClient client,
        string appKind,
        string appId,
        string? ifNoneMatch,
        TimeSpan delayWhenNotModified,
        CancellationToken cancellationToken = default)
    {
        var probe = ifNoneMatch;
        while (!cancellationToken.IsCancellationRequested) {
            var result = await client.ResolveForAppAsync(appKind, appId, probe, version: null, headOnly: false, cancellationToken).ConfigureAwait(false);
            probe = result.ETag ?? probe;

            switch (result.Outcome) {
                case ConfigResolveOutcome.Ok:
                    OperationHelpers.ThrowIfNull(result.Resolved, "OK response omitted resolved payload.");
                    return result.Resolved;

                case ConfigResolveOutcome.NotModified:
                    await Task.Delay(delayWhenNotModified, cancellationToken).ConfigureAwait(false);
                    break;

                default:
                    throw new InvalidOperationException(
                        $"Config API poll failed: HTTP {result.Failure?.StatusCode} {result.Failure?.ReasonPhrase}".Trim());
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        throw new OperationCanceledException(cancellationToken);
    }
}
